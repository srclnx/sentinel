namespace Sentinel.Controls
{
    using System;
    using System.Windows;

    using Common.Logging;

    using Sentinel.Interfaces;
    using Sentinel.Services;

#pragma warning disable CS3009 // Base type is not CLS-compliant

    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow
    {
        private static readonly ILog Log = LogManager.GetLogger<PreferencesWindow>();

        public PreferencesWindow()
            : this(0)
        {
        }

        public PreferencesWindow(int selectedTabIndex)
        {
            InitializeComponent();
            Preferences = ServiceLocator.Instance.Get<IUserPreferences>();
            SelectedTabIndex = selectedTabIndex;
            DataContext = this;

            Closing += (s, e) =>
                           {
                               if (HideOnClose)
                               {
                                   Log.Trace("Hiding window");
                                   Hide();
                                   e.Cancel = true;
                               }
                           };
        }

        public bool HideOnClose { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public int SelectedTabIndex { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public IUserPreferences Preferences { get; private set; }

        public void Launch()
        {
            Owner = Application.Current.MainWindow;

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Show();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Log.Trace("Window closed, setting Preferences.Show to false");
            Preferences.Show = false;
        }
    }

#pragma warning restore CS3009 // Base type is not CLS-compliant

}