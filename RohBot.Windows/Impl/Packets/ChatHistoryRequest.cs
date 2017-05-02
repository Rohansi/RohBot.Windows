using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class ChatHistoryRequest : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "chatHistoryRequest";

        [JsonProperty(Required = Required.Always)]
        public string Target { get; }

        [JsonProperty(Required = Required.Always)]
        public long AfterDate { get; }

        public ChatHistoryRequest([NotNull] string target, long afterDate)
        {
            Target = target;
            AfterDate = afterDate;
        }
    }
}
