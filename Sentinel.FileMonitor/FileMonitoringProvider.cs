namespace Sentinel.FileMonitor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Common.Logging;

    using Sentinel.FileMonitor.Support;
    using Sentinel.Interfaces;
    using Sentinel.Interfaces.CodeContracts;
    using Sentinel.Interfaces.Providers;

    public class FileMonitoringProvider : ILogProvider, IDisposable
    {
        private const string LoggerIdentifier = "Logger";

        private static readonly ILog Log = LogManager.GetLogger(nameof(FileMonitoringProvider));

        private readonly bool loadExistingContent;

        private readonly Regex patternMatching;

        private readonly Queue<ILogEntry> pendingQueue = new Queue<ILogEntry>();

        private readonly int refreshInterval = 250;

        private readonly List<string> usedGroupNames = new List<string>();

        private long positionReadTo;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000: DisposeObjectsBeforeLosingScope",
            Justification = "Both Worker and PurgeWorker are disposed in the IDispose implementation (or finalizer)")]
        public FileMonitoringProvider(IProviderSettings settings)
        {
            settings.ThrowIfNull(nameof(settings));

            FileMonitorProviderSettings = settings as IFileMonitoringProviderSettings;

            Debug.Assert(
                FileMonitorProviderSettings != null,
                "The FileMonitoringProvider class expects configuration information to be of IFileMonitoringProviderSettings type");

            ProviderSettings = FileMonitorProviderSettings;
            FileName = FileMonitorProviderSettings.FileName;
            Information = settings.Info;
            refreshInterval = FileMonitorProviderSettings.RefreshPeriod;
            loadExistingContent = FileMonitorProviderSettings.LoadExistingContent;
            patternMatching = new Regex(FileMonitorProviderSettings.MessageDecoder, RegexOptions.Singleline | RegexOptions.Compiled);

            PredetermineGroupNames(FileMonitorProviderSettings.MessageDecoder);

            // Chain up callbacks to the workers.
            Worker.DoWork += DoWork;
            Worker.RunWorkerCompleted += DoWorkComplete;
            PurgeWorker.DoWork += PurgeWorkerDoWork;
        }

        ~FileMonitoringProvider()
        {
            Dispose(false);
        }

        public static IProviderRegistrationRecord ProviderRegistrationInformation { get; } =
            new ProviderRegistrationInformation(new ProviderInfo());

        public IProviderInfo Information { get; }

        public IProviderSettings ProviderSettings { get;  }

        public ILogger Logger { get; set; }

        public string Name { get; set; }

        public bool IsActive => Worker.IsBusy;

        private static string[] DateFormats { get; } = { "d", "yyyy-MM-dd HH:mm:ss,fff", "O" };

        private IFileMonitoringProviderSettings FileMonitorProviderSettings { get; }

        private string FileName { get; }

        private BackgroundWorker Worker { get; set; } = new BackgroundWorker();

        private BackgroundWorker PurgeWorker { get; set; } = new BackgroundWorker { WorkerReportsProgress = true };

        public void Start()
        {
            Debug.Assert(!string.IsNullOrEmpty(FileName), "Filename not specified");
            Debug.Assert(Logger != null, "No logger has been registered, this is required before starting a provider");

            lock (pendingQueue)
            {
                Log.Trace($"Starting of file-monitor upon {FileName}");
            }

            Worker.RunWorkerAsync();
            PurgeWorker.RunWorkerAsync();
        }

        public void Close()
        {
            Worker.CancelAsync();
            PurgeWorker.CancelAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Pause()
        {
            if (Worker != null)
            {
                // TODO: need a better pause mechanism...
                Close();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Worker?.Dispose();
            Worker = null;
            PurgeWorker?.Dispose();
            PurgeWorker = null;
        }

        private void PurgeWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                // Go to sleep.
                Thread.Sleep(refreshInterval);
                lock (pendingQueue)
                {
                    if (pendingQueue.Any())
                    {
                        Log.Trace($"Adding a batch of {pendingQueue.Count()} entries to the logger");
                        Logger.AddBatch(pendingQueue);
                        Log.Trace("Done adding the batch");
                    }
                }
            }
        }

        private void PredetermineGroupNames(string messageDecoder)
        {
            var decoder = messageDecoder.ToUpperInvariant();
            if (decoder.Contains("(?<DESCRIPTION>"))
            {
                usedGroupNames.Add("Description");
            }

            if (decoder.Contains("(?<DATETIME>"))
            {
                usedGroupNames.Add("DateTime");
            }

            if (decoder.Contains("(?<TYPE>"))
            {
                usedGroupNames.Add("Type");
            }

            if (decoder.Contains("(?<LOGGER>"))
            {
                usedGroupNames.Add(LoggerIdentifier);
            }

            if (decoder.Contains("(?<SYSTEM>"))
            {
                usedGroupNames.Add("System");
            }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            // Read existing content.
            var fi = new FileInfo(FileName);

            // Keep hold of incomplete lines, if any.
            var incomplete = string.Empty;
            var sb = new StringBuilder();

            if (!loadExistingContent)
            {
                positionReadTo = fi.Length;
            }

            while (!e.Cancel)
            {
                fi.Refresh();

                if (fi.Exists)
                {
                    fi.Refresh();

                    var fileLength = fi.Length;

                    // TODO: what happens if file get shortened?  E.g. rolled over to a new one.
                    if (fileLength < positionReadTo)
                    {
                        Log.Debug("Detected file truncation, rollover or other such event on the file being monitored");
                        positionReadTo = 0;
                    }

                    if (fileLength > positionReadTo)
                    {
                        try
                        {
                            using (var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                var position = fs.Seek(positionReadTo, SeekOrigin.Begin);
                                Debug.Assert(position == positionReadTo, "Seek did not go to where we asked.");

                                // Calculate length of file.
                                var bytesToRead = fileLength - position;
                                Debug.Assert(bytesToRead < int.MaxValue, "Too much data to read using this method!");

                                var buffer = new byte[bytesToRead];

                                var bytesSuccessfullyRead = fs.Read(buffer, 0, (int)bytesToRead);
                                Debug.Assert(bytesSuccessfullyRead == bytesToRead, "Did not get as much as expected!");

                                // Put results into a buffer (prepend any unprocessed data retained from last read).
                                sb.Length = 0;
                                sb.Append(incomplete);
                                sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesSuccessfullyRead));

                                using (var sr = new StringReader(sb.ToString()))
                                {
                                    DecodeAndQueueMessages(sr);
                                }

                                // Can we determine whether any tailing data was unprocessed?
                                positionReadTo = position + bytesSuccessfullyRead;
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Warn("Exception caught processing file entries", exception);

                            // TODO: determine fatal conditions.
                        }
                    }
                }

                Thread.Sleep(refreshInterval);
            }
        }

        private void DecodeAndQueueMessages(TextReader inputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            Debug.Assert(patternMatching != null, "Regular expression has not be set");

            lock (pendingQueue)
            {
                var messages = LogMessageEntryConsumer.GetMessages(inputStream, patternMatching);
                var entriesAdded = 0;

                foreach (var message in messages)
                {
                    var m = patternMatching.Match(message.Entry);

                    if (!m.Success)
                    {
                        Log.Warn("Message decoding did not work!");
                        Log.Debug($"Message: {message.Entry}");
                        Log.Debug($"Pattern: {FileMonitorProviderSettings.MessageDecoder}");
                        return;
                    }

                    var meta = new Dictionary<string, object>
                                   {
                                       { "Classification", string.Empty },
                                       { "Host", FileName },
                                       { "ReceivedTime", DateTime.UtcNow }
                                   };
                    var entry = new LogEntry { Metadata = meta };

                    if (usedGroupNames.Contains("DateTime"))
                    {
                        const DateTimeStyles Styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
                        var value = m.Groups["DateTime"].Value;

                        DateTime dt;
                        if (!DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, Styles, out dt))
                        {
                            Log.Warn($"Failed to parse date '{value}'");
                            Log.Debug("Overriding date/time that failed to parse to 'now'");
                            dt = DateTime.UtcNow;
                        }

                        entry.DateTime = dt;
                    }
                    else
                    {
                        entry.DateTime = DateTime.UtcNow;
                    }

                    entry.Type = usedGroupNames.Contains("Type") ? m.Groups["Type"].Value : "INFO";
                    entry.System = usedGroupNames.Contains("System") ? m.Groups["System"].Value : string.Empty;

                    if (usedGroupNames.Contains(LoggerIdentifier))
                    {
                        entry.Source = m.Groups[LoggerIdentifier].Value;
                        entry.System = m.Groups[LoggerIdentifier].Value;
                    }

                    if (usedGroupNames.Contains("Description"))
                    {
                        var description = m.Groups["Description"].Value;

                        if (message.Extra != null && message.Extra.Any())
                        {
                            var sb = new StringBuilder(description);
                            sb.AppendLine();

                            foreach (var extraLine in message.Extra)
                            {
                                sb.AppendLine(extraLine);
                            }

                            description = sb.ToString();
                        }

                        entry.Description = description;
                    }

                    // TODO: Basic way of identifying exceptions, other than just the word!
                    if (entry.Description.ToUpper(CultureInfo.InvariantCulture).Contains("EXCEPTION"))
                    {
                        entry.Metadata.Add("Exception", true);
                    }

                    pendingQueue.Enqueue(entry);
                    entriesAdded++;
                }

                Log.Trace($"Added {entriesAdded} new entries");
            }
        }

        private void DoWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Worker.CancelAsync();
        }

        private class ProviderInfo : IProviderInfo
        {
            public Guid Identifier => new Guid("1a2f8249-b390-4baa-ba5e-3d67804ba1ed");

            public string Name => "File Monitoring Provider";

            public string Description => "Monitor a text file for new log entries.";
        }
    }
}