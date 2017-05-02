using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RohBot.Impl.Packets
{
    internal enum ChatMethod
    {
        Join,
        Leave
    }

    internal class Chat : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "chat";

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public ChatMethod Method { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ShortName { get; set; }
    }
}
