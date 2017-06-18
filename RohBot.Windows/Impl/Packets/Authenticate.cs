using System;
using Windows.Data.Json;
using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    internal enum AuthenticateMethod
    {
        Login,
        Register,
        Guest
    }

    internal class Authenticate : IJsonSerializable
    {
        public AuthenticateMethod Method { get; }
        public string Username { get; }
        public string Password { get; }
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

        public JsonObject Serialize()
        {
            var obj = new JsonObject
            {
                { "Type", JsonValue.CreateStringValue("auth") },
                { "Method", JsonValue.CreateStringValue(MethodString) }
            };

            if (Method != AuthenticateMethod.Guest)
            {
                obj.Add("Username", JsonValue.CreateStringValue(Username));
                obj.Add("Password", JsonValue.CreateStringValue(Password));
            }

            if (Method == AuthenticateMethod.Login && !string.IsNullOrEmpty(Tokens))
            {
                obj.Add("Tokens", JsonValue.CreateStringValue(Tokens));
            }

            return obj;
        }

        private string MethodString
        {
            get
            {
                switch (Method)
                {
                    case AuthenticateMethod.Guest: return "guest";
                    case AuthenticateMethod.Login: return "login";
                    case AuthenticateMethod.Register: return "register";
                    default: throw new NotSupportedException(nameof(MethodString));
                }
            }
        }
    }
}
