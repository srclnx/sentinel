namespace Sentinel.Startup
{
    using System.Collections.Generic;

    using CommandLine;

    public class FileMonitorOptions
    {
        public enum DecoderOptions
        {
            Unknown = 0,

            NLogDefault = 1,

            Custom = int.MaxValue
        }

        [Option('r', "refresh", HelpText = "Refresh period in milliseconds (default 250)", DefaultValue = 250)]
        public int RefreshPeriod { get; set; } = 250;

        [Option('l', "load", DefaultValue = true, HelpText = "Load existing content")]
        public bool LoadExistingContent { get; set; } = true;

        [ValueList(typeof(List<string>))]
        public List<string> FileNames { get; set; }

        [Option('d', "decoder", HelpText = "Message decoder", DefaultValue = DecoderOptions.NLogDefault)]
        public DecoderOptions MessageDecoder { get; set; } = DecoderOptions.Unknown;

        [Option("custom", HelpText = "Regular expression for customer message decoder")]
        public string CustomDecoderFormat { get; set; }
    }
}