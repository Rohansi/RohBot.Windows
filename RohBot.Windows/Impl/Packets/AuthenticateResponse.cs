using Windows.Data.Json;

namespace RohBot.Impl.Packets
{
    internal class AuthenticateResponse : IJsonDeserializable
    {
        public string Name { get; }
        public string Tokens { get; }
        public bool Success { get; }

        public AuthenticateResponse(JsonObject obj)
        {
            Name = obj.GetNamedStringOrNull("Name");
            Tokens = obj.GetNamedString("Tokens");
            Success = obj.GetNamedBoolean("Success");
        }
    }
}
