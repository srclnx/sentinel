namespace Sentinel.Classification
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using Interfaces;
    using Sentinel.Interfaces;

    using WpfExtras;

    [DataContract]
    public class Classifier : ViewModelBase, IClassifier
    {
        private bool enabled = true;

        private LogEntryField field;

        private MatchMode mode;

        private string name;

        private string type;

        private string pattern;

        private Regex regex;

        public Classifier()
        {
            PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode)
                        || e.PropertyName == nameof(Pattern))
                    {
                        if (Mode == MatchMode.RegularExpression && Pattern != null)
                        {
                            regex = new Regex(Pattern);
                        }

                        OnPropertyChanged(nameof(Description));
                    }
                };
        }

        public Classifier(string name, bool enabled, LogEntryField field, MatchMode mode, string pattern, string type)
        {
            Name = name;
            Enabled = enabled;
            Field = field;
            Mode = mode;
            Pattern = pattern;
            Type = type;
            regex = new Regex(pattern);

            PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode)
                        || e.PropertyName == nameof(Pattern))
                    {
                        if (Mode == MatchMode.RegularExpression && Pattern != null)
                        {
                            regex = new Regex(Pattern);
                        }

                        OnPropertyChanged(nameof(Description));
                    }
                };
        }

        public string Description
        {
            get
            {
                var modeDescription = "Exact";

                switch (Mode)
                {
                    case MatchMode.RegularExpression:
                        modeDescription = "RegEx";
                        break;
                    case MatchMode.CaseSensitive:
                        modeDescription = "Case sensitive";
                        break;
                    case MatchMode.CaseInsensitive:
                        modeDescription = "Case insensitive";
                        break;
                }

                return $"{modeDescription} match of {Pattern} in the {Field} field";
            }
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    OnPropertyChanged(nameof(Enabled));
                }
            }
        }

        public LogEntryField Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
                OnPropertyChanged(nameof(Field));
            }
        }

        public string HighlighterType => "Basic Highlighter";

        public MatchMode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                if (mode != value)
                {
                    mode = value;
                    OnPropertyChanged(nameof(Mode));
                }
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Pattern
        {
            get
            {
                return pattern;
            }

            set
            {
                if (pattern != value)
                {
                    pattern = value;
                    OnPropertyChanged(nameof(Pattern));
                }
            }
        }

        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public ILogEntry Classify(ILogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (IsMatch(entry))
            {
                entry.Metadata[nameof(Classification)] = Type;
                entry.Type = Type;
            }

            return entry;
        }

        public bool IsMatch(ILogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (string.IsNullOrWhiteSpace(Pattern))
            {
                return false;
            }

            string target;

            switch (Field)
            {
                case LogEntryField.None:
                    target = string.Empty;
                    break;
                case LogEntryField.Type:
                    target = entry.Type;
                    break;
                case LogEntryField.System:
                    target = entry.System;
                    break;
                case LogEntryField.Classification:
                    target = string.Empty;
                    break;
                case LogEntryField.Thread:
                    target = entry.Thread;
                    break;
                case LogEntryField.Source:
                    target = entry.Source;
                    break;
                case LogEntryField.Description:
                    target = entry.Description;
                    break;
                case LogEntryField.Host:
                    target = string.Empty;
                    break;
                default:
                    target = string.Empty;
                    break;
            }

            switch (Mode)
            {
                case MatchMode.Exact:
                    return target.Equals(Pattern);
                case MatchMode.CaseSensitive:
                    return target.Contains(Pattern);
                case MatchMode.CaseInsensitive:
                    return target.ToLower().Contains(Pattern.ToLower());
                case MatchMode.RegularExpression:
                    return regex != null && regex.IsMatch(target);
            }

            return false;
        }
    }
}