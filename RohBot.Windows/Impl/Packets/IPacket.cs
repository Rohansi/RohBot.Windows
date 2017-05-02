using RohBot.Annotations;

namespace RohBot.Impl.Packets
{
    public interface IPacket
    {
        [NotNull]
        string Type { get; }
    }
}
