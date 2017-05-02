using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RohBot.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace RohBot.Impl
{
    public enum HistoryLineType
    {
        Chat,
        State
    }

    public enum HistoryLineState
    {
        Enter,
        Left,
        Disconnected,
        Kicked,
        Banned,
        Unbanned,
        Action,
        Client // gay shit
    }

    public sealed class HistoryLine : INotifyPropertyChanged
    {
        private string _content;

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter), true)] // camelCase
        public HistoryLineType Type { get; set; }

        [JsonProperty(Required = Required.Always)]
        public long Date { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Chat { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        public string Content
        {
            get { return _content; }
            set
            {
                if (value == _content) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        #region Type == Chat

        [JsonProperty(Required = Required.DisallowNull)]
        public string UserType { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        public string Sender { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string SenderId { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string SenderStyle { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public bool InGame { get; set; }

        #endregion

        #region Type == State

        [JsonProperty(Required = Required.DisallowNull)]
        [JsonConverter(typeof(StringEnumConverter), false)] // PascalCase
        public HistoryLineState State { get; set; }

        [JsonProperty(Required = Required.Default)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        [CanBeNull]
        public string For { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ForId { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ForType { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ForStyle { get; set; }

        [JsonProperty(Required = Required.Default)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        [CanBeNull]
        public string By { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ById { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ByType { get; set; }

        [JsonProperty(Required = Required.Default)]
        [CanBeNull]
        public string ByStyle { get; set; }

        #endregion

        public ObservableCollection<string> Messages { get; private set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void ResetPropertyChanged()
        {
            PropertyChanged = null;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var changed = PropertyChanged;

            if (changed != null)
            {
                var x = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        #region Username

        private sealed class HistoryLineUsername : IUsername
        {
            public string Name { get; }
            public UserRank Rank { get; }
            public bool InGame { get; }
            public bool IsWeb { get; }
            public string Style { get; }

            public HistoryLineUsername(string name, string type, string playing, string style)
            {
                Name = name;
                Rank = UserRank.Member;
                InGame = playing != null;
                IsWeb = type == "RohBot";
                Style = style;
            }
            
            public event PropertyChangedEventHandler PropertyChanged
            {
                add { /* this is readonly */ }
                remove { /* this is readonly */ }
            }
        }
        
        public IUsername Username =>
            Sender == null ? null : new HistoryLineUsername(Sender, UserType, InGame ? "A" : null, SenderStyle);

        public IUsername ForUsername =>
            For == null ? null : new HistoryLineUsername(For, ForType, null, ForStyle);

        public IUsername ByUsername =>
            By == null ? null : new HistoryLineUsername(By, ByType, null, ByStyle);

        #endregion

        public void Simplify() // TODO: choose a better name
        {
            if (Type == HistoryLineType.Chat)
            {
                Messages = new ObservableCollection<string> { Content };
                return;
            }

            if (Type != HistoryLineType.State)
                return;

            switch (State)
            {
                case HistoryLineState.Enter:
                    Content = "entered chat.";
                    break;
                case HistoryLineState.Left:
                    Content = "left chat.";
                    break;
                case HistoryLineState.Disconnected:
                    Content = "disconnected.";
                    break;
                case HistoryLineState.Kicked:
                    Content = "was kicked by";
                    break;
                case HistoryLineState.Banned:
                    Content = "was banned by";
                    break;
                case HistoryLineState.Unbanned:
                    Content = "was unbanned by";
                    break;
                case HistoryLineState.Action:
                    Content = Content.Substring(For.Length + 1);
                    break;
            }
        }
    }
}

