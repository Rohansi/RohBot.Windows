using System;
using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal enum ChatMethod
    {
        Join,
        Leave
    }

    internal class Chat : IJsonDeserializable
    {
        public ChatMethod Method { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }

        public Chat(JsonObject obj)
        {
            Method = ParseMethod(obj.GetNamedString("Method"));
            Name = obj.GetNamedString("Name");
            ShortName = obj.GetNamedString("ShortName");
        }

        private ChatMethod ParseMethod(string value)
        {
            switch (value)
            {
                case "join": return ChatMethod.Join;
                case "leave": return ChatMethod.Leave;
                default: throw new NotSupportedException(nameof(Chat) + nameof(ParseMethod));
            }
        }
    }
}
