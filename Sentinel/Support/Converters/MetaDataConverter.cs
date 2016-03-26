namespace Sentinel.Support.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;

    using Sentinel.Interfaces;
    using Sentinel.Interfaces.CodeContracts;

    public class MetadataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            value.ThrowIfNull(nameof(value));

            var member = parameter as string;
            var metadata = value as IDictionary<string, object>;

            if (metadata != null && !string.IsNullOrWhiteSpace(member))
            {
                object metaDataValue;
                metadata.TryGetValue(member, out metaDataValue);
                return metaDataValue;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}