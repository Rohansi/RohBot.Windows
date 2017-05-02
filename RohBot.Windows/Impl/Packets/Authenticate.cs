using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal enum AuthenticateMethod
    {
        Login,
        Register,
        Guest
    }

    internal class Authenticate : IPacket
    {
        [JsonProperty(Required = Required.Always)]
        public string Type => "auth";

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public AuthenticateMethod Method { get; }

        [JsonProperty(Required = Required.Always)]
        public string Username { get; }

        [JsonProperty(Required = Required.Always)]
        public string Password { get; }

        [JsonProperty(Required = Required.Always)]
        public string Tokens { get; }
        
        public Authenticate()
        {
            Method = AuthenticateMethod.Guest;
            Username = "";
            Password = "";
            Tokens = "";
        }

        public Authenticate(AuthenticateMethod method, [NotNull] string username, [NotNull] string password, string tokens = null)
        {
            if (method == AuthenticateMethod.Guest)
                throw new ArgumentOutOfRangeException(nameof(method), "Authenticate as guest with wrong constructor.");

            Method = method;
            Username = username;
            Password = password;
            Tokens = tokens ?? "";
        }
    }
}
