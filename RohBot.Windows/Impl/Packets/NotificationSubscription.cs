using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class NotificationSubscription : IJsonDeserializable
    {
        public string RegexPattern { get; }
        public string DeviceToken { get; }
        public bool Registered { get; }

        public NotificationSubscription(JsonObject obj)
        {
            RegexPattern = obj.GetNamedString("RegexPattern");
            DeviceToken = obj.GetNamedString("DeviceToken");
            Registered = obj.GetNamedBoolean("Registered");
        }
    }
}
