namespace Sentinel.Controls.Flyouts
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using WpfExtras;

#pragma warning disable CS3009 // Base type is not CLS-compliant

    /// <summary>
    /// Interaction logic for FiltersFlyout.xaml
    /// </summary>
    public sealed partial class FiltersFlyout : INotifyPropertyChanged
    {
        private bool flyoutIsOpen;

        public FiltersFlyout()
        {
            InitializeComponent();

            CloseFlyout = new DelegateCommand(o => FlyoutIsOpen = false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand CloseFlyout { get; }

        public bool FlyoutIsOpen
        {
            get
            {
                return flyoutIsOpen;
            }

            set
            {
                if (!Equals(value, flyoutIsOpen))
                {
                    flyoutIsOpen = value;
                    OnPropertyChanged(nameof(FlyoutIsOpen));
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

#pragma warning restore CS3009 // Base type is not CLS-compliant

}
