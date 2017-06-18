using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class UserListRequest : IJsonSerializable
    {
        public string Target { get; }

        public UserListRequest([NotNull] string target)
        {
            Target = target;
        }

        public JsonObject Serialize()
        {
            return new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("userListRequest") },
                { "Target", JsonValue.CreateStringValue(Target) }
            };
        }
    }
}
