using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace RohBot.Converters
{
    public sealed class AvatarImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var avatar = (string)value;

            if (string.IsNullOrEmpty(avatar))
                return new BitmapImage(new Uri("ms-appx:///Assets/Logo.png"));

            var prefix = avatar.Substring(0, 2);
            return new BitmapImage(new Uri($"https://rohbot.net/steamcommunity/public/images/avatars/{prefix}/{avatar}_full.jpg"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
