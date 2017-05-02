using System.ComponentModel;

namespace RohBot.Impl
{
    public interface IUsername : INotifyPropertyChanged
    {
        string Name { get; }

        UserRank Rank { get; }

        bool InGame { get; }

        bool IsWeb { get; }

        string Style { get; }
    }
}
