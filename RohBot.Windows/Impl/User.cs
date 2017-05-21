using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RohBot.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
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

        [JsonProperty(Required = Required.Always)]
        public string UserId { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter), false)]
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

        [JsonProperty(Required = Required.Always)]
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

        [JsonProperty(Required = Required.Always)]
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

        [JsonProperty(Required = Required.AllowNull)]
        [JsonConverter(typeof(HtmlEncodeConverter))]
        [CanBeNull]
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

        [JsonProperty(Required = Required.Always)]
        public bool Web { get; set; }

        [JsonProperty(Required = Required.Always)]
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
        
        bool IUsername.InGame => !string.IsNullOrWhiteSpace(Playing);
        bool IUsername.IsWeb => Web;

        public IUsername Username => this;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public bool Equals(User other)
        {
            return UserId == other.UserId;
        }

        public int CompareTo(User other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(Name, other.Name);
        }
    }
}
