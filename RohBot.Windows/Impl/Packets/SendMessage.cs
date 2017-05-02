using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class SendMessage : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "sendMessage";

        [JsonProperty(Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Content { get; set; }

        public SendMessage([NotNull] string target, [NotNull] string content)
        {
            Target = target;
            Content = content;
        }
    }
}
