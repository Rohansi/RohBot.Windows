using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class ChatHistory : IJsonDeserializable
    {
        public string ShortName { get; }
        public bool Requested { get; }
        public IReadOnlyList<HistoryLine> Lines { get; }
        public long OldestLine { get; }

        public ChatHistory(JsonObject obj)
        {
            ShortName = obj.GetNamedString("ShortName");
            Requested = obj.GetNamedBoolean("Requested");
            Lines = obj.GetNamedArray("Lines").Select(o => new HistoryLine(o.GetObject())).ToList();
            OldestLine = (long)obj.GetNamedNumber("OldestLine");
        }
    }
}
