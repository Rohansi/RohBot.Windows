using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace RohBot.Converters
{
    public abstract class LinkifyBase
    {
        protected static Brush LinkBrush { get; }

        static LinkifyBase()
        {
            if (DesignMode.DesignModeEnabled)
                return; // this code can't run in design mode

            var res = Application.Current.Resources;
            LinkBrush = (Brush)res["RohBotMessageLinkForegroundBrush"];
        }
    }

    public sealed class LinkifyStringConverter : LinkifyBase, IValueConverter
    {
        // TODO: short links, eg: rohbot.net
        // TODO: don't fuck up emails
        // TODO: optimize me!
        private static readonly Regex LinkRegex = new Regex(
            //@"(?<Before>.*?)(?<Content>(?<Protocol>[-a-zA-Z]+://)?[-a-zA-Z0-9:%_\+.~#?&//=]{2,256}\.(?:(?:xn--[a-z0-9]{2,25})|(?:[a-z]{2,15}))\b(?:\/[-a-zA-Z0-9@:%_\+.,~#?&/=\(\)]*)?(?:\?\S+)?)(?<After>.*)",
            @"(?<Before>.*?)(?<Content>(?:(?<Protocol>https?://)|(?:www.))[A-Za-z0-9-._~:/?#\[\]@!$&'()*+,;=`%]+)(?<After>.*)",
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
                var protocol = match.Groups["Protocol"];
                var content = match.Groups["Content"].Value;
                var after = match.Groups["After"].Value;

                Uri uri = null;
                try
                {
                    var location = content;
                    if (!protocol.Success)
                        location = "http://" + location;

                    uri = new Uri(location);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Invalid URI: {e}");
                }
                
                if (!string.IsNullOrEmpty(before))
                    inlines.Add(new Run { Text = before });

                if (uri == null)
                {
                    inlines.Add(new Run { Text = content });
                }
                else
                {
                    inlines.Add(new Hyperlink
                    {
                        Foreground = LinkBrush,
                        NavigateUri = uri,
                        Inlines = { new Run { Text = content } }
                    });
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
