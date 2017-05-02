using Newtonsoft.Json;

namespace RohBot.Impl.Packets
{
    internal class NotificationSubscription : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "notificationSubscription";

        [JsonProperty(Required = Required.Always)]
        public string RegexPattern { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string DeviceToken { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool Registered { get; set; }
    }
}
