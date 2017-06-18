using Windows.Data.Json;

namespace RohBot.Impl
{
    public interface IJsonSerializable
    {
        JsonObject Serialize();
    }
}
