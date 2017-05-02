using System.Collections.Generic;
using Newtonsoft.Json;

namespace RohBot.Impl.Packets
{
    internal class ChatHistory : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "chatHistory";

        [JsonProperty(Required = Required.Always)]
        public string ShortName { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool Requested { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ICollection<HistoryLine> Lines { get; set; }

        [JsonProperty(Required = Required.Always)]
        public long OldestLine { get; set; }
    }
}
