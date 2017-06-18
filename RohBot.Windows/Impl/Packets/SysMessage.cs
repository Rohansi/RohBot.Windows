using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class SysMessage : IJsonDeserializable
    {
        public long Date { get; set; }
        public string Content { get; set; }

        public SysMessage(JsonObject obj)
        {
            Date = (long)obj.GetNamedNumber("Date");
            Content = HtmlEncoder.Decode(obj.GetNamedString("Content"));
        }
    }
}
