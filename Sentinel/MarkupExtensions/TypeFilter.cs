namespace Sentinel.MarkupExtensions
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;

    using Common.Logging;

    using JetBrains.Annotations;

    using MahApps.Metro.Controls;

    public class TypeFilter : DependencyObject, IFilter
    {
        public static readonly DependencyProperty NotImplementingInterfaceProperty = DependencyProperty.Register(
            "NotImplementingInterface",
            typeof(string),
            typeof(TypeFilter),
            new UIPropertyMetadata(null));

        public static readonly DependencyProperty ImplementsInterfaceProperty = DependencyProperty.Register(
            "ImplementsInterface",
            typeof(string),
            typeof(TypeFilter),
            new UIPropertyMetadata(null));

        private static readonly ILog Log = LogManager.GetLogger<TypeFilter>();

        public string NotImplementingInterface
        {
            get
            {
                return (string)GetValue(NotImplementingInterfaceProperty);
            }

            set
            {
                SetValue(NotImplementingInterfaceProperty, value);
            }
        }

        public string ImplementsInterface
        {
            get
            {
                return (string)GetValue(ImplementsInterfaceProperty);
            }

            set
            {
                SetValue(ImplementsInterfaceProperty, value);
            }
        }

        public bool Filter(object item)
        {
            try
            {
                var type = item.GetType();

                if (!string.IsNullOrWhiteSpace(ImplementsInterface))
                {
                    return type.GetInterfaces().Any(t => t.Name.EndsWith(ImplementsInterface));
                }
                else if (!string.IsNullOrWhiteSpace(NotImplementingInterface))
                {
                    return !type.GetInterfaces().Any(t => t.Name.EndsWith(NotImplementingInterface));
                }
            }
            catch (Exception e)
            {
                Log.Error("Ooops", e);
            }

            return false;
        }
    }
}