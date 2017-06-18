using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class NotificationUnsubscriptionRequest : IJsonSerializable
    {
        public string DeviceToken { get; }

        public NotificationUnsubscriptionRequest([NotNull] string deviceToken)
        {
            DeviceToken = deviceToken;
        }

        public JsonObject Serialize()
        {
            return new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("notificationUnsubscriptionRequest") },
                { "RegexPattern", JsonValue.CreateStringValue("") },
                { "DeviceToken", JsonValue.CreateStringValue(DeviceToken) }
            };
        }
    }
}
