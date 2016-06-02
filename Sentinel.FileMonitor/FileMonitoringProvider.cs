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

        private long bytesRead;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000: DisposeObjectsBeforeLosingScope",
            Justification = "Both Worker and PurgeWorker are disposed in the IDispose implementation (or finalizer)")]
        public FileMonitoringProvider(IProviderSettings settings)
        {
            settings.ThrowIfNull(nameof(settings));

            var fileSettings = settings as IFileMonitoringProviderSettings;

            Debug.Assert(
                fileSettings != null,
                "The FileMonitoringProvider class expects configuration information to be of IFileMonitoringProviderSettings type");

            ProviderSettings = fileSettings;
            FileName = fileSettings.FileName;
            Information = settings.Info;
            refreshInterval = fileSettings.RefreshPeriod;
            loadExistingContent = fileSettings.LoadExistingContent;
            patternMatching = new Regex(fileSettings.MessageDecoder, RegexOptions.Singleline | RegexOptions.Compiled);

            PredetermineGroupNames(fileSettings.MessageDecoder);

            // Chain up callbacks to the workers.
            Worker.DoWork += DoWork;
            Worker.RunWorkerCompleted += DoWorkComplete;
            PurgeWorker.DoWork += PurgeWorkerDoWork;
        }

        ~FileMonitoringProvider()
        {
            Dispose(false);
        }

        private static string[] DateFormats { get; } = { "d", "yyyy-MM-dd HH:mm:ss,fff", "O" };

        public static IProviderRegistrationRecord ProviderRegistrationInformation { get; } =
            new ProviderRegistrationInformation(new ProviderInfo());

        public string FileName { get; }

        public IProviderInfo Information { get; }

        public IProviderSettings ProviderSettings { get;  }

        public ILogger Logger { get; set; }

        public string Name { get; set; }

        public bool IsActive => Worker.IsBusy;

        private BackgroundWorker Worker { get; set; } = new BackgroundWorker();

        private BackgroundWorker PurgeWorker { get; set; } = new BackgroundWorker { WorkerReportsProgress = true };

        public void Start()
        {
            Debug.Assert(!string.IsNullOrEmpty(FileName), "Filename not specified");
            Debug.Assert(Logger != null, "No logger has been registered, this is required before starting a provider");

            lock (pendingQueue)
            {
                Log.Trace(string.Format(CultureInfo.InvariantCulture, "Starting of file-monitor upon {0}", FileName));
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
                        Log.Trace(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Adding a batch of {0} entries to the logger",
                                pendingQueue.Count()));
                        Logger.AddBatch(pendingQueue);
                        Trace.WriteLine("Done adding the batch");
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
                bytesRead = fi.Length;
            }

            while (!e.Cancel)
            {
                fi.Refresh();

                if (fi.Exists)
                {
                    fi.Refresh();

                    var length = fi.Length;
                    if (length > bytesRead)
                    {
                        using (var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Write))
                        {
                            var position = fs.Seek(bytesRead, SeekOrigin.Begin);
                            Debug.Assert(position == bytesRead, "Seek did not go to where we asked.");

                            // Calculate length of file.
                            var bytesToRead = length - position;
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
                                while (sr.Peek() != -1)
                                {
                                    var line = sr.ReadLine();

                                    // Trace.WriteLine("Read: " + line);
                                    DecodeAndQueueMessage(line);
                                }
                            }

                            // Can we determine whether any tailing data was unprocessed?
                            bytesRead = position + bytesSuccessfullyRead;
                        }
                    }
                }

                Thread.Sleep(refreshInterval);
            }
        }

        private void DecodeAndQueueMessage(string message)
        {
            Debug.Assert(patternMatching != null, "Regular expression has not be set");
            var m = patternMatching.Match(message);

            if (!m.Success)
            {
                Trace.WriteLine("Message decoding did not work!");
                return;
            }

            lock (pendingQueue)
            {
                var entry = new LogEntry
                                {
                                    Metadata = new Dictionary<string, object>
                                            {
                                                { "Classification", string.Empty },
                                                { "Host", FileName },
                                                { "ReceivedTime", DateTime.UtcNow }
                                            }
                                };
                if (usedGroupNames.Contains("Description"))
                {
                    entry.Description = m.Groups["Description"].Value;
                }

                if (usedGroupNames.Contains("DateTime"))
                {
                    const DateTimeStyles Styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
                    var value = m.Groups["DateTime"].Value;

                    DateTime dt;
                    if (
                        !DateTime.TryParseExact(
                            value,
                            DateFormats,
                            CultureInfo.InvariantCulture,
                            Styles,
                            out dt))
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

                if (entry.Description.ToUpper(CultureInfo.InvariantCulture).Contains("EXCEPTION"))
                {
                    entry.Metadata.Add("Exception", true);
                }

                pendingQueue.Enqueue(entry);
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