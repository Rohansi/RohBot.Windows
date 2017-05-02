using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace RohBot.Converters
{
    public enum TimeFormat
    {
        System = 0,
        TwelveHour = 1,
        TwentyFourHour = 2
    }

    public sealed class FriendlyDateConverter : IValueConverter
    {
        private static readonly string SystemFormatString =
            " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;

        private static readonly DateTime UnixEpoch =
               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var unixTimeMs = (long)value;
            var dateTime = UnixEpoch.AddMilliseconds(unixTimeMs).ToLocalTime();

            string formatString;
            switch (Settings.TimeFormat.Value)
            {
                case (int)TimeFormat.System:
                    formatString = SystemFormatString;
                    break;

                case (int)TimeFormat.TwelveHour:
                    formatString = " h:mm tt";
                    break;

                case (int)TimeFormat.TwentyFourHour:
                    formatString = " HH:mm";
                    break;

                default:
                    throw new NotSupportedException();
            }


            return dateTime.ToString(formatString);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
