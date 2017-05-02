using System.Collections.Generic;
using Newtonsoft.Json;

namespace RohBot.Impl.Packets
{
    internal class UserList : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "userList";

        [JsonProperty("ShortName", Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<User> Users { get; set; }
    }
}
