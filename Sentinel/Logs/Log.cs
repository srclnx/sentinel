namespace Sentinel.Logs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Sentinel.Classification.Interfaces;
    using Sentinel.Interfaces;
    using Sentinel.Services;

    using WpfExtras;

    public class Log : ViewModelBase, ILogger
    {
        private readonly IClassifyingService<IClassifier> classifier;

        public IEnumerable<ILogEntry> Entries { get; } = new List<ILogEntry>();

        public IEnumerable<ILogEntry> NewEntries { get; } = new List<ILogEntry>();

        private bool enabled = true;

        private string name;

        public Log()
        {
            classifier = ServiceLocator.Instance.Get<IClassifyingService<IClassifier>>();

            // Observe the NewEntries to maintain a full history.
            PropertyChanged += OnPropertyChanged;
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public void Clear()
        {
            lock (Entries)
            {
                (Entries as List<ILogEntry>)?.Clear();
            }

            lock (NewEntries)
            {
                (NewEntries as List<ILogEntry>)?.Clear();
            }

            OnPropertyChanged("Entries");
            OnPropertyChanged("NewEntries");
            GC.Collect();
        }

        public void AddBatch(Queue<ILogEntry> entries)
        {
            if (!enabled || entries.Count <= 0)
            {
                return;
            }

            var processed = new Queue<ILogEntry>();
            while (entries.Count > 0)
            {
                if (classifier != null)
                {
                    var entry = classifier.Classify(entries.Dequeue());
                    processed.Enqueue(entry);
                }
            }

            lock (NewEntries)
            {
                var newEntries = NewEntries as List<ILogEntry>;
                newEntries?.Clear();
                newEntries?.AddRange(processed);
            }

            OnPropertyChanged("NewEntries");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NewEntries")
            {
                lock (NewEntries)
                {
                    lock (Entries)
                    {
                        (Entries as List<ILogEntry>)?.AddRange(NewEntries);
                    }

                    OnPropertyChanged("Entries");
                }
            }
        }
    }
}