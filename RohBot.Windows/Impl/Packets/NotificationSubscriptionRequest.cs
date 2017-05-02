using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class NotificationSubscriptionRequest : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "notificationSubscriptionRequest";

        [JsonProperty(Required = Required.Always)]
        public string RegexPattern { get; }

        [JsonProperty(Required = Required.Always)]
        public string DeviceToken { get; }

        public NotificationSubscriptionRequest([NotNull] string deviceToken, [NotNull] string regexPattern)
        {
            RegexPattern = regexPattern;
            DeviceToken = deviceToken;
        }
    }
}
