namespace Sentinel.FileMonitor
{
    using System.Collections.Generic;

    public class LogMessage
    {
        public string Entry { get; set; }

        public IList<string> Extra { get; set; }
    }
}