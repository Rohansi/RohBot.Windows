using Newtonsoft.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal class UserListRequest : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "userListRequest";

        [JsonProperty(Required = Required.Always)]
        public string Target { get; }

        public UserListRequest([NotNull] string target)
        {
            Target = target;
        }
    }
}
