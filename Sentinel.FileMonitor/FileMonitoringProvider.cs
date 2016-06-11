namespace Sentinel.FileMonitor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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

    using LogMessageMetaCollection = System.Collections.Generic.IDictionary<string, object>;

    public class FileMonitoringProvider : ILogProvider, IDisposable
    {
        private const string LoggerIdentifier = "Logger";

        private static readonly ILog Log = LogManager.GetLogger(nameof(FileMonitoringProvider));

        private readonly bool loadExistingContent;

        private readonly Regex patternMatching;

        private readonly Queue<ILogEntry> pendingQueue = new Queue<ILogEntry>();

        private readonly int refreshInterval = 250;

        private long positionReadTo;

        [SuppressMessage(
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

            FieldMapper = new LogFieldMapper();
            FieldMapper.AddMapping("Type", (v, e, m) => e.Type = SelectValueOrDefault(v, "DEBUG"));
            FieldMapper.AddMapping("System", (v, e, m) => e.System = (string)v);
            FieldMapper.AddMapping("DateTime", (v, e, m) => e.DateTime = DateParserHelper.ParseDateTime((string)v, DateTime.UtcNow));
            FieldMapper.AddMapping("Logger", (v, e, m) => e.Source = (string)v);
            FieldMapper.AddMapping("Description", (v, e, m) => e.Description = CombineLines((string)v, m.Extra));
            FieldMapper.AddMapping("Thread", (v, e, m) => e.Thread = (string)v);
            FieldMapper.AddMetaMapping("Classification", (v, meta, m) => meta.Add("Classification", v));
            FieldMapper.AddMetaMapping("Host", (v, meta, m) => meta.Add("Host", FileName));
            FieldMapper.AddMetaMapping("ReceivedTime", (v, meta, m) => meta.Add("ReceivedTime", DateTime.UtcNow));

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

        private LogFieldMapper FieldMapper { get; }

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

        private static string SelectValueOrDefault(object value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value as string) ? defaultValue : value.ToString();
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
                    var entry = PopulateLogEntry(message, patternMatching, FieldMapper);

                    //////////////////////////////////////////////////////////////////////
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

        private string CombineLines(string firstLine, IList<string> extraLines)
        {
            var lines = extraLines ?? Enumerable.Empty<string>();
            return string.Join(Environment.NewLine, Enumerable.Repeat(firstLine, 1).Concat(lines));
        }

        private void DoWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Worker.CancelAsync();
        }

        private ILogEntry PopulateLogEntry(LogMessage message, Regex parser, LogFieldMapper mapper)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            try
            {
                var match = parser.Match(message.Entry);

                var entry = new LogEntry();

                foreach (var map in mapper.Mappings)
                {
                    var value = match.Groups[map.FieldName]?.Value ?? map.DefaultValue;
                    map.Writer(value, entry, message);
                }

                foreach (var metaMapEntry in mapper.MetaMappings)
                {
                    if (entry.Metadata == null)
                    {
                        entry.Metadata = new Dictionary<string, object>();
                    }

                    var value = match.Groups[metaMapEntry.FieldName]?.Value ?? metaMapEntry.DefaultValue;
                    metaMapEntry.Writer(value, entry.Metadata, message);
                }

                return entry;
            }
            catch (RegexMatchTimeoutException e)
            {
                Log.Error("Regex took too long to execute", e);
            }

            return null;
        }

        private class LogFieldMapper
        {
            public delegate void MapWriter(object value, ILogEntry entry, LogMessage message);

            public delegate void MetaMapWriter(object value, LogMessageMetaCollection meta, LogMessage message);

            public IList<LogFieldMapEntry> Mappings { get; } = new List<LogFieldMapEntry>();

            public IList<LogFieldMetaMapEntry> MetaMappings { get; } = new List<LogFieldMetaMapEntry>();

            public void AddMapping(string fieldName, MapWriter writer, object defaultValue = null)
            {
                var mapEntry = new LogFieldMapEntry(fieldName, writer);

                if (defaultValue != null)
                {
                    mapEntry.DefaultValue = defaultValue;
                }

                Mappings.Add(mapEntry);
            }

            public void AddMetaMapping(string fieldName, MetaMapWriter writer, object defaultValue = null)
            {
                var metaMapEntry = new LogFieldMetaMapEntry(fieldName, writer);

                if (defaultValue != null)
                {
                    metaMapEntry.DefaultValue = defaultValue;
                }

                MetaMappings.Add(metaMapEntry);
            }

            public class LogFieldMapEntry
            {
                public LogFieldMapEntry(string fieldName, MapWriter writer)
                {
                    FieldName = fieldName;
                    Writer = writer;
                }

                public string FieldName { get; }

                public MapWriter Writer { get; }

                public object DefaultValue { get; set; }
            }

            public class LogFieldMetaMapEntry
            {
                public LogFieldMetaMapEntry(string fieldName, MetaMapWriter writer)
                {
                    FieldName = fieldName;
                    Writer = writer;
                }

                public string FieldName { get; }

                public MetaMapWriter Writer { get; }

                public object DefaultValue { get; set; }
            }
        }

        private class ProviderInfo : IProviderInfo
        {
            public Guid Identifier => new Guid("1a2f8249-b390-4baa-ba5e-3d67804ba1ed");

            public string Name => "File Monitoring Provider";

            public string Description => "Monitor a text file for new log entries.";
        }
    }
}