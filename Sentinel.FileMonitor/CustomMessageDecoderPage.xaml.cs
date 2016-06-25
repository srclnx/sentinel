namespace Sentinel.FileMonitor
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Common.Logging;

    using WpfExtras;

    /// <summary>
    /// Interaction logic for CustomMessageDecoderPage.xaml
    /// </summary>
    public partial class CustomMessageDecoderPage : IWizardPage, IDataErrorInfo
    {
        private static readonly ILog Log = LogManager.GetLogger<CustomMessageDecoderPage>();

        private readonly ObservableCollection<IWizardPage> children = new ObservableCollection<IWizardPage>();

        private readonly ReadOnlyObservableCollection<IWizardPage> readonlyChildren;

        private string customFormat;

        private string error;

        private bool isValid;

        public CustomMessageDecoderPage()
        {
            InitializeComponent();
            DataContext = this;

            readonlyChildren = new ReadOnlyObservableCollection<IWizardPage>(children);

            TestRegex = new DelegateCommand(
                a =>
                    {
                        string errorText;
                        IsValid = TestCustomRegex(CustomFormat, out errorText);
                        Error = errorText;
                        OnPropertyChanged(nameof(Error));
                        OnPropertyChanged(nameof(IsValid));
                    });

            PropertyChanged += PropertyChangedHandler;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand TestRegex { get; private set; }

        public string CustomFormat
        {
            get
            {
                return customFormat;
            }

            set
            {
                if (customFormat != value)
                {
                    customFormat = value;
                    OnPropertyChanged(nameof(CustomFormat));
                }
            }
        }

        public string Title => "Custom Message Decoder";

        public ReadOnlyObservableCollection<IWizardPage> Children => readonlyChildren;

        public string Description => "Specify how to decompose the message into its individual fields.";

        public bool IsValid
        {
            get
            {
                return isValid;
            }

            set
            {
                if (isValid != value)
                {
                    isValid = value;
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public Control PageContent => this;

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        /// <returns>
        /// An error message indicating what is wrong with this object. The default is a null.</returns>
        public string Error
        {
            get
            {
                return error;
            }

            private set
            {
                if (error != value)
                {
                    error = value;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <returns>
        /// The error message for the property. The default is a null.</returns>
        /// <param name="columnName">The name of the property whose error message to get.</param>
        public string this[string columnName]
        {
            get
            {
                if (columnName == "CustomFormat")
                {
                    return Error;
                }

                return null;
            }
        }

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
            var settings = saveData as IFileMonitoringProviderSettings;
            if (settings != null)
            {
                settings.MessageDecoder = CustomFormat;
            }
            else
            {
                Log.Warn(
                    $"The supplied 'saveData' was not type IFileMonitoringProviderSettings, can not save this page's information");
            }

            return saveData;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        private static bool ContainsKeyFields(string pattern)
        {
            var p = pattern.ToLower();

            return p.Contains("(?<description>") || p.Contains("(?<type>") || p.Contains("(?<datetime>");
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CustomFormat))
            {
                // LIVE testing of the Regex has been disabled, so when the CustomFormat field changes, invalidate
                // any previous test results - user needs to click the "Test" button.
                IsValid = false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Set the custom format after the constructor to force the validation
            // to show immediately, this will retrigger the validation.
            OnPropertyChanged(nameof(CustomFormat));
        }

        private bool TestCustomRegex(string regex, out string errorText)
        {
            var sw = Stopwatch.StartNew();

            errorText = null;
            var returnCode = false;

            if (string.IsNullOrEmpty(regex))
            {
                errorText = "Pattern can not be empty";
            }
            else
            {
                try
                {
                    // See whether the string validates as a Regex
                    var r = new Regex(regex);

                    // See if it contains the minimal fields
                    if (!ContainsKeyFields(regex))
                    {
                        errorText = "The pattern does not define any of the core fields, Description, Type or "
                                    + "DateTime.  At least one of these should be defined.";
                    }
                    else
                    {
                        returnCode = true;
                    }
                }
                catch (ArgumentException)
                {
                    errorText = "The custom pattern does not equate to a valid regular expression";
                }
            }

            Debug.Assert(
                (!returnCode && errorText != null) || (returnCode && errorText == null),
                "Error text must be when false");

            Log.Debug($"Evaluating regex took {sw.ElapsedMilliseconds} ms: {regex}");

            return returnCode;
        }
    }
}