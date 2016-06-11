namespace Sentinel.FileMonitor.Support
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;

    using Common.Logging;

    public static class DateParserHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(DateParserHelper));

        private static string[] DateFormats { get; } = { "d", "yyyy-MM-dd HH:mm:ss,fff", "O" };

        public static DateTime? ParseDateTime(string value)
        {
            const DateTimeStyles Styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;

            DateTime dt;
            if (!DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, Styles, out dt))
            {
                Log.Warn($"Failed to parse date '{value}'");
                return null;
            }

            return dt;
        }

        public static DateTime ParseDateTime(string value, DateTime defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            var convertedValue = ParseDateTime(value);
            return convertedValue ?? defaultValue;
        }
    }
}