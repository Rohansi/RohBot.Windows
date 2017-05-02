using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RohBot.Annotations;

namespace RohBot.Impl
{
    public sealed class Room : INotifyPropertyChanged
    {
        private bool _hasUnreadMessages;

        /// <summary>
        /// Short name of the room, used as an identifier.
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Full name of the room, for description.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Abbreviation to show in the icon.
        /// </summary>
        public string Abbreviation { get; }

        /// <summary>
        /// Messages shown in the chat.
        /// </summary>
        public ObservableCollection<HistoryLine> Messages { get; }

        /// <summary>
        /// Users in the chat.
        /// </summary>
        public ObservableCollection<User> Users { get; }

        /// <summary>
        /// True when the room has unread messages.
        /// </summary>
        public bool HasUnreadMessages
        {
            get { return _hasUnreadMessages; }
            set
            {
                if (value == _hasUnreadMessages) return;
                _hasUnreadMessages = value;
                OnPropertyChanged();
            }
        }

        public Room(string shortName, string name)
        {
            ShortName = shortName;
            Name = name;

            Abbreviation = ShortName.Substring(0, Math.Min(ShortName.Length, 3));

            Messages = new ObservableCollection<HistoryLine>();
            Users = new ObservableCollection<User>();
        }

        public void UpdateUsers(List<User> newUserList)
        {
            var removedUsers = Users.Except(newUserList).ToList();
            var addedUsers = newUserList.Except(Users).ToList();
            var updatedUsers = Users.Intersect(newUserList).ToList();

            foreach (var user in removedUsers)
            {
                Users.Remove(user);
            }

            foreach (var user in updatedUsers)
            {
                var newUser = newUserList.First(u => u.Equals(user));
                UpdateUser(user, newUser);
            }

            foreach (var user in addedUsers)
            {
                AddUser(user);
            }
        }

        public void AddUser(User newUser)
        {
            var index = Users.BinarySearch(newUser);

            if (index >= 0)
                UpdateUser(Users[index], newUser);
            else
                Users.Insert(~index, newUser);
        }

        private void UpdateUser(User oldUser, User newUser)
        {
            oldUser.Status = newUser.Status;
            oldUser.Playing = newUser.Playing;
            oldUser.Rank = newUser.Rank;
            oldUser.Avatar = newUser.Avatar;
            oldUser.Style = newUser.Style;
            oldUser.Name = newUser.Name; // should be last, triggers update!
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
