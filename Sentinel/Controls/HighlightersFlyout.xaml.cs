namespace Sentinel.Controls
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    using MahApps.Metro.Controls;

    using Sentinel.Annotations;

    using WpfExtras;

    /// <summary>
    /// Interaction logic for HighlightersFlyout.xaml
    /// </summary>
    public partial class HighlightersFlyout : INotifyPropertyChanged
    {
        private bool flyoutIsOpen;

        public HighlightersFlyout()
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
