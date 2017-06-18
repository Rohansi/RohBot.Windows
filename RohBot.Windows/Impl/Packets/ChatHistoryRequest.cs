using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class ChatHistoryRequest : IJsonSerializable
    {
        public string Target { get; }
        public long AfterDate { get; }

        public ChatHistoryRequest([NotNull] string target, long afterDate)
        {
            Target = target;
            AfterDate = afterDate;
        }

        public JsonObject Serialize()
        {
            return new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("chatHistoryRequest") },
                { "Target", JsonValue.CreateStringValue(Target) },
                { "AfterDate", JsonValue.CreateNumberValue(AfterDate) }
            };
        }
    }
}
