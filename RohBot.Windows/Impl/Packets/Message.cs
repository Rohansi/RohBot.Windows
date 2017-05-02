using Newtonsoft.Json;

namespace RohBot.Impl.Packets
{
    internal class Message : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "message";

        [JsonProperty(Required = Required.Always)]
        public HistoryLine Line { get; set; }
    }
}
