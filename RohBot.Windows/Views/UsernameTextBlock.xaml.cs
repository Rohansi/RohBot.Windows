using System;
using Windows.ApplicationModel;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using RohBot.Impl;

namespace RohBot.Views
{
    public sealed partial class UsernameTextBlock : UserControl
    {
        private static readonly Brush GoldStarBrush;
        private static readonly Brush SilverStarBrush;
        private static readonly Brush WebBrush;
        private static readonly Brush SteamBrush;
        private static readonly Brush SteamInGameBrush;

        private static readonly Brush GlowBrush;
        private static readonly Brush PinkGlowBrush;
        private static readonly Brush OrangepeelBrush;
        private static readonly Brush UpsidedownBrush;
        private static readonly Brush RedBrush;
        private static readonly Brush AfricanBrush;
        private static readonly Brush HeartBrush;
        private static readonly Brush GhostBrush;
        private static readonly Brush HackerBrush;
        private static readonly Brush BeanifyBrush;
        private static readonly Brush HotBrush;
        private static readonly Brush ThinBlueBrush;
        private static readonly Brush CrackBrush;
        private static readonly Brush MemeBlueBrush;
        private static readonly Brush PissYellowBrush;
        private static readonly Brush LexiBrush;

        static UsernameTextBlock()
        {
            if (DesignMode.DesignModeEnabled)
                return; // this code can't run in design mode

            var res = Application.Current.Resources;

            GoldStarBrush = (Brush)res["RohBotGoldStarBrush"];
            SilverStarBrush = (Brush)res["RohBotSilverStarBrush"];
            WebBrush = (Brush)res["RohBotWebBrush"];
            SteamBrush = (Brush)res["RohBotSteamBrush"];
            SteamInGameBrush = (Brush)res["RohBotSteamInGameBrush"];

            GlowBrush = (Brush)res["RohBotNameGlowBrush"];
            PinkGlowBrush = (Brush)res["RohBotNamePinkGlowBrush"];
            OrangepeelBrush = (Brush)res["RohBotNameOrangepeelBrush"];
            UpsidedownBrush = (Brush)res["RohBotNameUpsidedownBrush"];
            RedBrush = (Brush)res["RohBotNameRedBrush"];
            AfricanBrush = (Brush)res["RohBotNameAfricanBrush"];
            HeartBrush = (Brush)res["RohBotNameHeartBrush"];
            GhostBrush = (Brush)res["RohBotNameGhostBrush"];
            HackerBrush = (Brush)res["RohBotNameHackerBrush"];
            BeanifyBrush = (Brush)res["RohBotNameBeanifyBrush"];
            HotBrush = (Brush)res["RohBotNameHotBrush"];
            ThinBlueBrush = (Brush)res["RohBotNameThinBlueBrush"];
            CrackBrush = (Brush)res["RohBotNameCrackBrush"];
            MemeBlueBrush = (Brush)res["RohBotNameMemeBlueBrush"];
            PissYellowBrush = (Brush)res["RohBotNamePissYellowBrush"];
            LexiBrush = (Brush)res["RohBotNameLexiBrush"];
        }

        private IUsername _username;

        public UsernameTextBlock()
        {
            TrackUsername = false;

            InitializeComponent();
        }

        private void UsernameTextBlock_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (TrackUsername && _username != null)
                _username.PropertyChanged -= Username_PropertyChanged;
        }

        public bool TrackUsername { get; set; }
        public bool AdjustWidth { get; set; }
        public bool AdjustHeight { get; set; }

        public IUsername Username
        {
            get => _username;
            set
            {
                if (value == _username)
                    return;

                if (TrackUsername && _username != null)
                    _username.PropertyChanged -= Username_PropertyChanged;

                _username = value;

                if (_username != null)
                {
                    if (TrackUsername)
                        _username.PropertyChanged += Username_PropertyChanged;

                    UpdateView(_username);
                }
            }
        }

        private void Username_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Name")
                return; // to reduce number of updates

            UpdateView(_username);
        }

        private void UpdateView(IUsername username)
        {
            // reset all properties
            Main.Visibility = Visibility.Visible;
            Before.Visibility = After.Visibility = Visibility.Collapsed;
            Main.FontWeight = FontWeights.Normal;
            Main.Foreground = Before.Foreground = After.Foreground = WebBrush;
            Main.RenderTransform = Before.RenderTransform = After.RenderTransform = null;
            Before.Margin = After.Margin = new Thickness(0);
            Before.FontSize = After.FontSize = Main.FontSize; // reset to default

            if (username == null)
            {
                Main.Text = "<null>";
                return;
            }

            Main.Text = username.Name;

            switch (username.Rank)
            {
                case UserRank.Administrator:
                    Rank.Visibility = Visibility.Visible;
                    Rank.Foreground = GoldStarBrush;
                    break;
                case UserRank.Moderator:
                    Rank.Visibility = Visibility.Visible;
                    Rank.Foreground = SilverStarBrush;
                    break;
                default:
                    Rank.Visibility = Visibility.Collapsed;
                    break;
            }

            if (!username.IsWeb)
            {
                Main.Foreground = username.InGame ? SteamInGameBrush : SteamBrush;
                return;
            }
            
            switch (username.Style)
            {
                case "heart":
                    After.Text = "\u2764";
                    After.Foreground = HeartBrush;
                    After.Visibility = Visibility.Visible;
                    break;

                case "glow":
                    Main.Foreground = GlowBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "pink-glow":
                    Main.Foreground = PinkGlowBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "hacker":
                    Main.Foreground = After.Foreground = HackerBrush;
                    After.Visibility = Visibility.Visible;
                    After.Text = ".c";
                    break;

                case "hitler":
                    Before.Text = After.Text = "\u5350";
                    Before.Visibility = After.Visibility = Visibility.Visible;
                    Before.FontSize = After.FontSize = 12;
                    Before.Margin = new Thickness(0, 0, 3, 0);
                    After.Margin = new Thickness(3, 0, 0, 0);

                    Before.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new RotateTransform { Angle = 45 },
                            new TranslateTransform { Y = 3 }
                        }
                    };

                    After.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new RotateTransform { Angle = 45 },
                            new TranslateTransform { Y = 3 }
                        }
                    };
                    break;

                case "upsidedown":
                    Main.Foreground = UpsidedownBrush;
                    Before.Text = "⛄";
                    Before.Visibility = Visibility.Visible;

                    Before.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new RotateTransform { Angle = 180 },
                            new TranslateTransform { X = 0.125, Y = 2.125 }
                        }
                    };

                    Main.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new RotateTransform { Angle = 180 },
                            new TranslateTransform { X = 0.125, Y = 2.125 }
                        }
                    };
                    break;

                case "red":
                    Main.Foreground = RedBrush;
                    break;

                case "hot":
                    Main.Foreground = HotBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "orangepeel":
                    Main.Foreground = OrangepeelBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "ghostmatyr":
                    Main.Foreground = GhostBrush;
                    Main.RenderTransform = new RotateTransform { Angle = 3 };
                    break;

                case "african":
                    Main.Foreground = AfricanBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "thinBlue":
                    Main.Foreground = ThinBlueBrush;
                    Main.FontWeight = FontWeights.Light;
                    break;

                case "crack":
                    Main.Foreground = CrackBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;

                case "spans":
                    Before.Text = "<";
                    Before.Visibility = Visibility.Visible;
                    After.Text = ">";
                    After.Visibility = Visibility.Visible;
                    break;

                case "meme-blue":
                    Main.Foreground = MemeBlueBrush;
                    break;

                case "piss-yellow":
                    Main.Foreground = PissYellowBrush;
                    break;

                case "jossie":
                    After.Text = "🍼";
                    After.Margin = new Thickness(3, 0, 0, 0);
                    After.Visibility = Visibility.Visible;
                    break;
                    
                case "beanify":
                    Main.Foreground = BeanifyBrush;
                    Main.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new RotateTransform { Angle = 180 },
                            new TranslateTransform { X = 0.125, Y = 2.125 }
                        }
                    };
                    break;

                case "eggplant":
                    After.Text = "🍆";
                    After.Margin = new Thickness(3, 0, 0, 0);
                    After.Visibility = Visibility.Visible;
                    break;

                case "lexi":
                    Main.Foreground = LexiBrush;
                    Main.FontWeight = FontWeights.Bold;
                    break;
            }
        }

        private void Container_LayoutUpdated(object sender, object e)
        {
            if (Username == null)
                return;

            var size = Container.DesiredSize;
            Outer.Width = Math.Max(size.Width - (AdjustWidth ? 4 : 0), 0);
            Outer.Height = Math.Max(size.Height * (AdjustHeight ? 0.81 : 1), 0);
        }
    }
}
