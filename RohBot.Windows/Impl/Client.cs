using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.UI.Core;
using RohBot.Annotations;
using RohBot.Impl.Packets;
using RohBot.Views;

namespace RohBot.Impl
{
    public sealed class Client : INotifyPropertyChanged
    {
        private delegate IJsonDeserializable PacketConstructor(JsonObject obj);

        private static readonly Lazy<Client> Lazy = new Lazy<Client>(() => new Client());
        public static Client Instance => Lazy.Value;

        public delegate void ClientAuthenticated(bool loggedIn, string message);
        public delegate void ClientMessageReceived(Room room, HistoryLine line);
        public delegate void ClientSysMessageReceived(string message);

        private readonly Dictionary<string, PacketConstructor> _packetTypes;
        private readonly List<string> _receivedRooms;
        private Room _currentRoom;
        private bool _isLoggedIn;
        private string _name;
        private bool _hasReceivedRooms;

        public Connection Connection { get; }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            private set
            {
                if (value == _isLoggedIn) return;
                _isLoggedIn = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool HasReceivedRooms
        {
            get => _hasReceivedRooms;
            private set
            {
                if (value == _hasReceivedRooms) return;
                _hasReceivedRooms = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Room> Rooms { get; }

        public Room CurrentRoom
        {
            get => _currentRoom;
            set
            {
                if (value == _currentRoom) return;
                _currentRoom = value;
                OnPropertyChanged();
            }
        }

        public event ClientAuthenticated Authenticated;
        public event ClientMessageReceived MessageReceived;
        public event ClientSysMessageReceived SysMessageReceived;

        private Client()
        {
            _packetTypes = new Dictionary<string, PacketConstructor>
            {
                { "authResponse", o => new AuthenticateResponse(o) },
                { "chat", o => new Chat(o) },
                { "chatHistory", o => new ChatHistory(o) },
                { "message", o => new Message(o) },
                { "notificationSubscription", o => new NotificationSubscription(o) },
                { "ping", o => new Ping(o) },
                { "sysMessage", o => new SysMessage(o) },
                { "userList", o => new UserList(o) },
            };

            _receivedRooms = new List<string>();

            Connection = new Connection();
            Connection.Connected += OnConnected;
            Connection.Disconnected += OnDisconnected;
            Connection.MessageReceived += OnMessageReceived;

            Rooms = new ObservableCollection<Room>();
        }

        public Task SendAsync(IJsonSerializable packet)
        {
            var message = packet.Serialize().Stringify();
            return Connection.SendAsync(message);
        }

        private async void OnConnected(object sender, EventArgs args)
        {
            Debug.WriteLine("Connected");
            IsLoggedIn = false;

            if (Settings.LoggedIn.Value)
            {
                try
                {
                    await SendAsync(new Authenticate(AuthenticateMethod.Login,
                        Settings.Username.Value, "", Settings.Token.Value));
                }
                catch
                {
                    Debug.WriteLine("Failed to autologin");
                    Connection.Disconnect(); // TODO: retry??
                }
            }
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            Debug.WriteLine("Disconnected");
            IsLoggedIn = false;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Debug.WriteLine($"Message received: {args.Message}");

            var obj = JsonObject.Parse(args.Message); // TODO: can fail
            var typeId = obj.GetNamedString("Type"); // TODO: can fail
            var typeBuilder = _packetTypes[typeId]; // TODO: can fail
            var packet = typeBuilder(obj); // TODO: can fail

            // TODO: dispatch packet to handlers

            var x = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (packet is AuthenticateResponse authResponse)
                {
                    Settings.Token.Value = authResponse.Tokens;

                    Name = authResponse.Name;
                    _receivedRooms.Clear();
                    return;
                }

                if (packet is Chat chat)
                {
                    switch (chat.Method)
                    {
                        case ChatMethod.Join:
                        {
                            var existingRoom = false;
                            if (!IsLoggedIn)
                            {
                                _receivedRooms.Add(chat.ShortName);
                                existingRoom = Rooms.Any(r => r.ShortName == chat.ShortName);
                            }

                            if (!existingRoom)
                                Rooms.Add(new Room(chat.ShortName, chat.Name));
                            
                            if (IsLoggedIn)
                                AppShell.Current?.Join(chat.ShortName); // /join'd

                            // TODO: defer this and periodically request
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await SendAsync(new UserListRequest(chat.ShortName));
                                }
                                catch
                                {
                                    Debug.WriteLine("Failed to request userlist");
                                }
                            });

                            return;
                        }

                        case ChatMethod.Leave:
                        {
                            var room = Rooms.FirstOrDefault(r => r.ShortName == chat.ShortName);
                            if (room != null)
                                Rooms.Remove(room);
                            return;
                        }

                        default:
                            throw new NotSupportedException();
                    }
                }

                if (packet is ChatHistory chatHistory)
                {
                    var room = Rooms.FirstOrDefault(r => r.ShortName == chatHistory.ShortName);
                    if (room == null) return;

                    // make a copy so we can combine
                    var newMessages = new List<HistoryLine>(chatHistory.Lines.Count);
                    foreach (var line in chatHistory.Lines)
                    {
                        AddLine(newMessages, line);
                    }

                    // move them into the room's messages
                    room.Messages.ReplaceWith(newMessages);

                    return;
                }

                if (packet is UserList userList)
                {
                    var room = Rooms.FirstOrDefault(r => r.ShortName == userList.Target);
                    room?.UpdateUsers(userList.Users);
                    return;
                }

                if (packet is Message message)
                {
                    var line = message.Line;
                    var room = Rooms.FirstOrDefault(r => r.ShortName == line.Chat);
                    if (room == null) return;

                    AddLineToRoom(room, line);
                    MessageReceived?.Invoke(room, line);

                    if (line.Type == HistoryLineType.State)
                    {
                        var users = room.Users;

                        switch (line.State)
                        {
                            case HistoryLineState.Enter:
                                room.AddUser(new User
                                {
                                    UserId = line.ForId,
                                    Name = line.For,
                                    Web = line.ForType == "RohBot",
                                    Style = line.ForStyle
                                });
                                break;

                            case HistoryLineState.Disconnected:
                            case HistoryLineState.Left:
                            case HistoryLineState.Kicked:
                            case HistoryLineState.Banned:
                                if (line.State == HistoryLineState.Banned && line.ForType != "Steam") // only steam removes!
                                    break;

                                users.Remove(users.FirstOrDefault(u => u.UserId == line.ForId));
                                break;
                        }
                    }

                    return;
                }

                if (packet is SysMessage sysMessage)
                {
                    var content = sysMessage.Content;
                    if (!IsLoggedIn)
                    {
                        var oldRooms = Rooms
                            .Where(r => !_receivedRooms.Contains(r.ShortName))
                            .ToList();

                        foreach (var room in oldRooms)
                        {
                            Rooms.Remove(room);
                        }

                        if (content.StartsWith("Logged in as") ||
                            content == "You are already logged in." ||
                            content == "You can not register while logged in.")
                        {
                            IsLoggedIn = true;
                            HasReceivedRooms = true;
                            Authenticated?.Invoke(true, content);
                        }
                        else
                        {
                            Authenticated?.Invoke(false, content);
                        }
                    }
                    else
                    {
                        var room = CurrentRoom;

                        if (room == null)
                        {
                            SysMessageReceived?.Invoke(content);
                            return;
                        }

                        room.Messages.Add(new HistoryLine
                        {
                            Type = HistoryLineType.State,
                            State = HistoryLineState.Client,
                            Content = content,
                            Date = sysMessage.Date
                        });
                    }

                    return;
                }
            });
        }

        private static void AddLineToRoom(Room room, HistoryLine line) =>
            AddLine(room.Messages, line);

        private static void AddLine(ICollection<HistoryLine> messages, HistoryLine line)
        {
            var prevLine = messages.LastOrDefault();
            if (prevLine != null &&
                line.Type == HistoryLineType.Chat &&
                prevLine.Type == HistoryLineType.Chat &&
                line.SenderId != "0" &&
                line.SenderId == prevLine.SenderId &&
                line.Date - prevLine.Date < TimeSpan.FromMinutes(2).TotalMilliseconds)
            {
                prevLine.Messages.Add(line.Content);
            }
            else
            {
                line.Simplify();
                messages.Add(line);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var x = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}
