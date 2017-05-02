using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace RohBot.Converters
{
    public sealed class UnreadToBrushConverter : IValueConverter
    {
        private static Brush NormalBrush { get; }
        private static Brush UnreadBrush { get; }

        static UnreadToBrushConverter()
        {
            if (DesignMode.DesignModeEnabled)
                return; // this code can't run in design mode

            var res = Application.Current.Resources;
            NormalBrush = (Brush)res["RohBotNavIconBrush"];
            UnreadBrush = (Brush)res["RohBotNavHighlightBrush"];
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var hasUnread = (bool)value;
            return hasUnread ? UnreadBrush : NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
