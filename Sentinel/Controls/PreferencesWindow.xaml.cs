namespace Sentinel.Controls
{
    using System;
    using System.Windows;

    using Sentinel.Interfaces;
    using Sentinel.Services;

    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow
    {
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Preferences.Show = false;
        }

        public void Launch()
        {
            Owner = Application.Current.MainWindow;

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Show();
        }
    }
}