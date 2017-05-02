using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class AuthenticateResponse : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "authResponse";

        [JsonProperty(Required = Required.AllowNull)]
        [CanBeNull]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Tokens { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool Success { get; set; }
    }
}
