using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class NotificationSubscriptionRequest : IJsonSerializable
    {
        public string RegexPattern { get; }
        public string DeviceToken { get; }

        public NotificationSubscriptionRequest([NotNull] string deviceToken, [NotNull] string regexPattern)
        {
            RegexPattern = regexPattern;
            DeviceToken = deviceToken;
        }

        public JsonObject Serialize()
        {
            return new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("notificationSubscriptionRequest") },
                { "RegexPattern", JsonValue.CreateStringValue(RegexPattern) },
                { "DeviceToken", JsonValue.CreateStringValue(DeviceToken) }
            };
        }
    }
}
