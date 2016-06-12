namespace Sentinel.Controls.Flyouts
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Data;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using Sentinel.Filters.Interfaces;

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

            // The Xaml designer is quite poor at coping with markup extensions, flagging
            // errors that do not appear at runtime, instead of using the FilterExtension, will
            // do it in code-behind.
            var standard = Resources["StandardFilters"] as CollectionViewSource;
            if (standard != null)
            {
                standard.Filter += (o, args) => args.Accepted = args.Item is IStandardDebuggingFilter;
            }

            var custom = Resources["CustomFilters"] as CollectionViewSource;
            if (custom != null)
            {
                custom.Filter += (o, args) => args.Accepted = !(args.Item is IStandardDebuggingFilter);
            }
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
