using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace RohBot.Converters
{
    public abstract class AbstractBooleanToVisibilityConverter : IValueConverter
    {
        private readonly bool _visibleValue;

        protected AbstractBooleanToVisibilityConverter(bool visibleValue)
        {
            _visibleValue = visibleValue;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var visible = (bool)value == _visibleValue;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var visible = (Visibility)value == Visibility.Visible;
            return visible ? _visibleValue : !_visibleValue;
        }
    }

    public sealed class BooleanToVisibilityConverter : AbstractBooleanToVisibilityConverter
    {
        public BooleanToVisibilityConverter()
            : base(true)
        {
            
        }
    }

    public sealed class InvertedBooleanToVisibilityConverter : AbstractBooleanToVisibilityConverter
    {
        public InvertedBooleanToVisibilityConverter()
            : base(false)
        {

        }
    }
}
