using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace RohBot
{
    public sealed class RichTextBlockHelper
    {
        public static Paragraph GetParagraph(DependencyObject obj)
        {
            return (Paragraph)obj.GetValue(ParagraphProperty);
        }

        public static void SetParagraph(DependencyObject obj, Paragraph value)
        {
            obj.SetValue(ParagraphProperty, value);
        }

        public static readonly DependencyProperty ParagraphProperty =
           DependencyProperty.RegisterAttached("Paragraph", typeof(Paragraph),
           typeof(RichTextBlockHelper), new PropertyMetadata(null, OnParagraphChanged));

        private static void OnParagraphChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (RichTextBlock)sender;
            var paragraph = (Paragraph)e.NewValue;

            control.Blocks.Clear();

            if (paragraph != null)
                control.Blocks.Add(paragraph);
        }
    }
}
