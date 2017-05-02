using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using RohBot.Impl;

namespace RohBot
{
    internal sealed class MessageDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Chat { get; set; }
        public DataTemplate State { get; set; }
        public DataTemplate Enter { get; set; }
        public DataTemplate Left { get; set; }
        public DataTemplate Disconnected { get; set; }
        public DataTemplate Kicked { get; set; }
        public DataTemplate Banned { get; set; }
        public DataTemplate Unbanned { get; set; }
        public DataTemplate Action { get; set; }
        public DataTemplate Client { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var line = (HistoryLine)item;

            if (line.Type == HistoryLineType.Chat)
            {
                if (line.SenderId == "0")
                    return State; // from ~

                return Chat;
            }

            if (line.Type == HistoryLineType.State)
            {
                switch (line.State)
                {
                    case HistoryLineState.Enter:
                        return Enter ?? State;
                    case HistoryLineState.Left:
                        return Left ?? State;
                    case HistoryLineState.Disconnected:
                        return Disconnected ?? State;
                    case HistoryLineState.Kicked:
                        return Kicked ?? State;
                    case HistoryLineState.Banned:
                        return Banned ?? State;
                    case HistoryLineState.Unbanned:
                        return Unbanned ?? State;
                    case HistoryLineState.Action:
                        return Action ?? State;
                    case HistoryLineState.Client:
                        return Client ?? State;
                    default:
                        return State;
                }
            }

            throw new NotSupportedException();
        }
    }
}
