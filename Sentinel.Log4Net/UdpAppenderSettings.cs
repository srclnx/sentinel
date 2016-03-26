namespace Sentinel.Log4Net
{
    using System;

    using Interfaces.Providers;

    public class UdpAppenderSettings : IUdpAppenderListenerSettings
    {
        public UdpAppenderSettings()
        {
            Name = "Log4net UDP Appender";
            Info = Log4NetProvider.ProviderRegistrationInformation.Info;
        }

        public UdpAppenderSettings(IProviderSettings providerInfo)
        {
            if (providerInfo == null)
            {
                throw new ArgumentNullException(nameof(providerInfo));
            }

            Name = providerInfo.Name;
            Info = providerInfo.Info;
        }

        public string Name { get; set; }

        public string Summary => $"{Name}: Listens on port {Port}";

        public IProviderInfo Info { get; set; }

        public int Port
        {
            get;
            set;
        }
    }
}