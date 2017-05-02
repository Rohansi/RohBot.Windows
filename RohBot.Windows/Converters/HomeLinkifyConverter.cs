using System;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using RohBot.Views;

namespace RohBot.Converters
{
    public sealed class HomeLinkifyConverter : LinkifyBase, IValueConverter
    {
        private static readonly Regex LinkRegex = new Regex(
            @"(?<Before>.*?)<a (?<Attr>\w+)=""(?<Val>.*?)"" ?(?:target=""_blank"")?>(?<Text>.*?)<\/a>(?<After>.*)",
            RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var str = (string)value;
            var paragraph = new Paragraph();
            var inlines = paragraph.Inlines;

            Match match;
            while ((match = LinkRegex.Match(str)).Success)
            {
                var before = match.Groups["Before"].Value;
                var attr = match.Groups["Attr"].Value;
                var val = match.Groups["Val"].Value;
                var text = match.Groups["Text"].Value;
                var after = match.Groups["After"].Value;
                
                if (!string.IsNullOrEmpty(before))
                    inlines.Add(new Run { Text = before });

                if (attr == "href")
                {
                    var uri = new Uri(val);
                    inlines.Add(new Hyperlink
                    {
                        Foreground = LinkBrush,
                        NavigateUri = uri,
                        Inlines = { new Run { Text = text } }
                    });
                }
                else if (attr == "onclick")
                {
                    var shortName = Regex.Match(val, @"^join\('(?<Name>\w+)'\)$").Groups["Name"].Value;
                    var link = new Hyperlink
                    {
                        Foreground = LinkBrush,
                        Inlines = { new Run { Text = text } }
                    };

                    link.Click += (sender, args) => AppShell.Current?.Join(shortName);
                    inlines.Add(link);
                }
                else
                {
                    inlines.Add(new Run { Text = match.Value }); // not supported
                }

                str = after;
            }
            
            // always add text on the end to terminate a hyperlink
            inlines.Add(new Run
            {
                Text = string.IsNullOrEmpty(str) ? " " : str
            });

            return paragraph;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
