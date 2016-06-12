namespace Sentinel.Controls.Flyouts
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Data;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using Sentinel.Highlighters.Interfaces;
    using Sentinel.MarkupExtensions;

    using WpfExtras;

#pragma warning disable CS3009 // Base type is not CLS-compliant

    /// <summary>
    /// Interaction logic for HighlightersFlyout.xaml
    /// </summary>
    public sealed partial class HighlightersFlyout : INotifyPropertyChanged
    {
        private bool flyoutIsOpen;

        public HighlightersFlyout()
        {
            InitializeComponent();

            CloseFlyout = new DelegateCommand(o => FlyoutIsOpen = false);

            // The Xaml designer is quite poor at coping with markup extensions, flagging
            // errors that do not appear at runtime, instead of using the FilterExtension, will
            // do it in code-behind.
            var standard = Resources["StandardHighlighters"] as CollectionViewSource;
            if (standard != null)
            {
                standard.Filter += (o, args) => args.Accepted = args.Item is IStandardDebuggingHighlighter;
            }

            var custom = Resources["CustomHighlighters"] as CollectionViewSource;
            if (custom != null)
            {
                custom.Filter += (o, args) => args.Accepted = !(args.Item is IStandardDebuggingHighlighter);
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
}

#pragma warning restore CS3009 // Base type is not CLS-compliant
