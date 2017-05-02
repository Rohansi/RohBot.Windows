using Newtonsoft.Json;

namespace RohBot.Impl.Packets
{
    internal class SysMessage : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "sysMessage";

        [JsonProperty(Required = Required.Always)]
        public long Date { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        public string Content { get; set; }
    }
}
