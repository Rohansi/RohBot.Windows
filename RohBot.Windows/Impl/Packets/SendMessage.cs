using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class SendMessage : IJsonSerializable
    {
        public string Target { get; set; }
        public string Content { get; set; }

        public SendMessage([NotNull] string target, [NotNull] string content)
        {
            Target = target;
            Content = content;
        }

        public JsonObject Serialize()
        {
            return new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("sendMessage") },
                { "Target", JsonValue.CreateStringValue(Target) },
                { "Content", JsonValue.CreateStringValue(Content) }
            };
        }
    }
}
