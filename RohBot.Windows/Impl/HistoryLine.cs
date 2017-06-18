using System;
using System.Collections.ObjectModel;
using RohBot.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
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

    public sealed class HistoryLine : INotifyPropertyChanged // TODO: remove prop change
    {
        private string _content;
        
        public HistoryLineType Type { get; set; }
        public long Date { get; set; }
        public string Chat { get; set; }

        public string Content
        {
            get => _content;
            set
            {
                if (value == _content) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        #region Type == Chat
        
        public string UserType { get; }
        public string Sender { get; }
        public string SenderId { get; }
        public string SenderStyle { get; }
        public bool InGame { get; }

        #endregion

        #region Type == State
        
        public HistoryLineState State { get; set; }
        
        public string For { get; set; }
        public string ForId { get; set; }
        public string ForType { get; set; }
        public string ForStyle { get; set; }
        
        public string By { get; set; }
        public string ById { get; set; }
        public string ByType { get; set; }
        public string ByStyle { get; set; }

        #endregion

        public HistoryLine() { }

        public HistoryLine(JsonObject obj)
        {
            Type = ParseType(obj.GetNamedString("Type"));
            Date = (long)obj.GetNamedNumber("Date");
            Chat = obj.GetNamedString("Chat");
            Content = HtmlEncoder.Decode(obj.GetNamedString("Content"));

            if (Type == HistoryLineType.Chat)
            {
                UserType = obj.GetNamedStringOrNull("UserType");
                Sender = HtmlEncoder.Decode(obj.GetNamedStringOrNull("Sender"));
                SenderId = obj.GetNamedStringOrNull("SenderId");
                SenderStyle = obj.GetNamedStringOrNull("SenderStyle");
                InGame = obj.GetNamedBoolean("InGame");
            }

            if (Type == HistoryLineType.State)
            {
                State = ParseState(obj.GetNamedString("State"));
                For = HtmlEncoder.Decode(obj.GetNamedStringOrNull("For"));
                ForId = obj.GetNamedStringOrNull("ForId");
                ForType = obj.GetNamedStringOrNull("ForType");
                ForStyle = obj.GetNamedStringOrNull("ForStyle");

                By = HtmlEncoder.Decode(obj.GetNamedStringOrNull("By"));
                ById = obj.GetNamedStringOrNull("ById");
                ByType = obj.GetNamedStringOrNull("ByType");
                ByStyle = obj.GetNamedStringOrNull("ByStyle");
            }
        }

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

        private static HistoryLineType ParseType(string value)
        {
            switch (value)
            {
                case "chat": return HistoryLineType.Chat;
                case "state": return HistoryLineType.State;
                default: throw new NotSupportedException(nameof(HistoryLine) + nameof(ParseType));
            }
        }

        private static HistoryLineState ParseState(string value)
        {
            switch (value)
            {
                case "Enter": return HistoryLineState.Enter;
                case "Left": return HistoryLineState.Left;
                case "Disconnected": return HistoryLineState.Disconnected;
                case "Kicked": return HistoryLineState.Kicked;
                case "Banned": return HistoryLineState.Banned;
                case "Unbanned": return HistoryLineState.Unbanned;
                case "Action": return HistoryLineState.Action;
                case "Client": return HistoryLineState.Client;
                default: throw new NotSupportedException(nameof(HistoryLine) + nameof(ParseState));
            }
        }
    }
}

