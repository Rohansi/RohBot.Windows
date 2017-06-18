using RohBot.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Data.Json;

namespace RohBot.Impl
{
    public enum UserRank
    {
        Guest,
        Member,
        Moderator,
        Administrator
    }

    public class User : IUsername, INotifyPropertyChanged, IEquatable<User>, IComparable<User>
    {
        private string _name;
        private UserRank _rank;
        private string _avatar;
        private string _status;
        private string _playing;
        private string _style;
        
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string UserId { get; set; }

        public UserRank Rank
        {
            get => _rank;
            set
            {
                if (value == _rank) return;
                _rank = value;
                OnPropertyChanged();
            }
        }

        public string Avatar
        {
            get => _avatar;
            set
            {
                if (value == _avatar) return;
                _avatar = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public string Playing
        {
            get => _playing;
            set
            {
                if (value == _playing) return;
                _playing = value;
                OnPropertyChanged();
                OnPropertyChanged("InGame"); // IUsername depends on this
            }
        }

        public bool Web { get; set; }

        public string Style
        {
            get => _style;
            set
            {
                if (value == _style) return;
                _style = value;
                OnPropertyChanged();
            }
        }
        
        public User() { }

        public User(JsonObject obj)
        {
            Name = HtmlEncoder.Decode(obj.GetNamedString("Name"));
            UserId = obj.GetNamedString("UserId");
            Rank = ParseRank(obj.GetNamedString("Rank"));
            Avatar = obj.GetNamedStringOrNull("Avatar");
            Status = obj.GetNamedStringOrNull("Status");
            Playing = HtmlEncoder.Decode(obj.GetNamedStringOrNull("Playing"));
            Web = obj.GetNamedBoolean("Web");
            Style = obj.GetNamedStringOrNull("Style");
        }
        
        bool IUsername.InGame => !string.IsNullOrWhiteSpace(Playing);
        bool IUsername.IsWeb => Web;

        public IUsername Username => this;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override int GetHashCode() => UserId.GetHashCode();

        public bool Equals(User other) => UserId == other.UserId;

        public int CompareTo(User other) =>
            StringComparer.OrdinalIgnoreCase.Compare(Name, other.Name);

        private static UserRank ParseRank(string value)
        {
            switch (value)
            {
                case "Guest": return UserRank.Guest;
                case "Member": return UserRank.Member;
                case "Moderator": return UserRank.Moderator;
                case "Administrator": return UserRank.Administrator;
                default: throw new NotSupportedException(nameof(User) + nameof(ParseRank));
            }
        }
    }
}
