using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class Message : IJsonDeserializable
    {
        public HistoryLine Line { get; }

        public Message(JsonObject obj)
        {
            Line = new HistoryLine(obj.GetNamedObject("Line"));
        }
    }
}
