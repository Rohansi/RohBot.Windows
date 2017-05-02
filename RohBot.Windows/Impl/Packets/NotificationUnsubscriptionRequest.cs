using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class NotificationUnsubscriptionRequest : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "notificationUnsubscriptionRequest";

        [JsonProperty(Required = Required.AllowNull)]
        [CanBeNull]
        public string RegexPattern { get; }

        [JsonProperty(Required = Required.Always)]
        public string DeviceToken { get; }

        public NotificationUnsubscriptionRequest([NotNull] string deviceToken)
        {
            RegexPattern = "";
            DeviceToken = deviceToken;
        }
    }
}
