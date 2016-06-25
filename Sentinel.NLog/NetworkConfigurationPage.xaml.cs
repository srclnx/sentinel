namespace Sentinel.NLog
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows.Controls;

    using Sentinel.Interfaces.Providers;

    using WpfExtras;

    /// <summary>
    /// Interaction logic for NetworkConfigurationPage.xaml
    /// </summary>
    public partial class NetworkConfigurationPage : IWizardPage
    {
        private readonly ObservableCollection<IWizardPage> children = new ObservableCollection<IWizardPage>();

        private bool isValid;

        private int port;

        private bool isUdp = true;

        public NetworkConfigurationPage()
        {
            InitializeComponent();
            DataContext = this;

            Children = new ReadOnlyObservableCollection<IWizardPage>(children);

            // Register to self so that we can handler user interactions.
            PropertyChanged += SelectProviderPagePropertyChanged;

            Port = 9999;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual bool SupportsUdp => true;

        public virtual bool SupportsTcp => true;

        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                if (port != value)
                {
                    port = value;
                    OnPropertyChanged("Port");
                }
            }
        }

        public bool IsUdp
        {
            get
            {
                return isUdp;
            }

            set
            {
                if (isUdp != value)
                {
                    isUdp = value;
                    OnPropertyChanged("IsUdp");
                }
            }
        }

        public string Title => "Configure Provider";

        public ReadOnlyObservableCollection<IWizardPage> Children { get; }

        public string Description => "Network settings to be used by new provider";

        public bool IsValid
        {
            get
            {
                return isValid;
            }

            private set
            {
                if (isValid != value)
                {
                    isValid = value;
                    OnPropertyChanged("IsValid");
                }
            }
        }

        public Control PageContent => this;

        public void AddChild(IWizardPage child)
        {
            children.Add(child);
            OnPropertyChanged(nameof(Children));
        }

        public void RemoveChild(IWizardPage child)
        {
            children.Remove(child);
            OnPropertyChanged(nameof(Children));
        }

        public object Save(object saveData)
        {
            Debug.Assert(saveData != null, "Expecting the save-data component to have details from the previous pages.");
            Debug.Assert(saveData is IProviderSettings, "Expecting the save-data component to be of an IProviderSettings type.");

            var previousInfo = (IProviderSettings)saveData;

            return new NetworkSettings
                       {
                           Name = previousInfo.Name,
                           Info = previousInfo.Info,
                           Port = Port,
                           Protocol = IsUdp ? NetworkProtocol.Udp : NetworkProtocol.Tcp
                       };
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        private void SelectProviderPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Port))
            {
                var state = port > 2000;
                Trace.WriteLine($"Setting PageValidates to {state}");
                IsValid = state;
            }
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Establish default selection
            Debug.Assert(SupportsUdp || SupportsTcp, "The provider needs to support at least one of UDP or TCP");
            IsUdp = SupportsUdp;
        }
    }
}
