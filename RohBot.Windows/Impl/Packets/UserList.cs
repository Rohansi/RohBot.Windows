using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class UserList : IJsonDeserializable
    {
        public string Target { get; }
        public IReadOnlyList<User> Users { get; }

        public UserList(JsonObject obj)
        {
            Target = obj.GetNamedString("ShortName");
            Users = obj.GetNamedArray("Users").Select(o => new User(o.GetObject())).ToList();
        }
    }
}
