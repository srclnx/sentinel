namespace Sentinel.Log4Net
{
    using System;
    using System.Collections.Generic;

    using Sentinel.Interfaces;

    internal class LogEntry : ILogEntry
    {
        public LogEntry()
        {
            Type = "DEBUG";
            DateTime = DateTime.Now;
            Description = "Fake Message";
            Source = "Fake source";
            System = "System";
            Thread = "123";
        }

        /// <summary>
        /// Gets or sets the classification for the log entry.  Can be free-text but will typically
        /// contain values like "DEBUG" or "ERROR".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the Date/Time for the original log entry.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the main body of the log entry.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the source of the log entry, e.g. where it came from.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the system (e.g. machine) where this message came from.
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// Gets or sets the thread identifier for the source of the message.
        /// </summary>
        public string Thread { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of any meta-data that doesn't fit into the above values.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }
}