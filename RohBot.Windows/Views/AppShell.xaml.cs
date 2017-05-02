using RohBot.Annotations;
using RohBot.Impl;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using RohBot.Impl.Packets;

namespace RohBot.Views
{
    public sealed partial class AppShell : Page, INotifyPropertyChanged
    {
        public static AppShell Current { get; private set; }

        private readonly long _splitViewToken;
        private Thickness _splitViewMargin;

        public Client Client => Client.Instance;
        public Connection Connection => Client.Connection;

        public Thickness SplitViewMargin
        {
            get { return _splitViewMargin; }
            set
            {
                if (value == _splitViewMargin) return;
                _splitViewMargin = value;
                OnPropertyChanged();
            }
        }

        public AppShell()
        {
            InitializeComponent();

            _splitViewToken = RootSplitView.RegisterPropertyChangedCallback(SplitView.DisplayModeProperty, (sender, args) =>
            {
                CheckSplitViewMargin();
            });
            
            SystemNavigationManager.GetForCurrentView().BackRequested += AppShell_BackRequested;

            Client.Authenticated += Client_OnLoggedIn;
            Client.MessageReceived += Client_OnMessageReceived;
        }

        private void AppShell_OnLoaded(object sender, RoutedEventArgs e)
        {
            Frame.BackStack.Clear(); // so we can't go back to login

            Current = this;
            CheckSplitViewMargin();

            if (Client.IsLoggedIn)
                SwitchToPreferredRoom();
        }

        private void AppShell_OnUnloaded(object sender, RoutedEventArgs e)
        {
            RootSplitView.UnregisterPropertyChangedCallback(SplitView.DisplayModeProperty, _splitViewToken);
            SystemNavigationManager.GetForCurrentView().BackRequested -= AppShell_BackRequested;
            Client.Authenticated -= Client_OnLoggedIn;
            Client.MessageReceived -= Client_OnMessageReceived;

            Bindings?.StopTracking();
        }

        public async void Join(string shortName)
        {
            var room = Client.Rooms.FirstOrDefault(r => r.ShortName == shortName);
            if (room != null)
            {
                RoomsListView.SelectedItem = room;
                return;
            }

            try
            {
                await Client.SendAsync(new SendMessage("home", $"/join {shortName}"));
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to send join: {e}");
            }
        }

        private void AppShell_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!ContentFrame.CanGoBack)
                return;

            ContentFrame.GoBack();
            e.Handled = true;
        }

        private void Client_OnLoggedIn(bool success, string message)
        {
            if (success)
            {
                SwitchToPreferredRoom();
            }
            else
            {
                Frame.Navigate(typeof(LoginPage), message);
                Frame.BackStack.Clear();
            }
        }

        private void Client_OnMessageReceived(Room room, HistoryLine line)
        {
            if (room == Client.CurrentRoom || line.Type != HistoryLineType.Chat)
                return;

            room.HasUnreadMessages = true;
        }

        private void RoomsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selectedItem = (Room)RoomsListView.SelectedItem;
            if (selectedItem == null)
            {
                if (ContentFrame.CurrentSourcePageType == typeof(ChatPage))
                    NavigateTo<ChatPage>("home");

                // HACK: this doesn't get cleared when the selection is set to null
                UsersListView.ItemsSource = null;
                return;
            }

            var shortName = selectedItem.ShortName;
            if (shortName != "home")
                Settings.SelectedRoom.Value = shortName;

            NavigateTo<ChatPage>(shortName);
        }

        private async void RoomFlyoutLeave_OnClick(object sender, RoutedEventArgs args)
        {
            var room = (Room)((FrameworkElement)args.OriginalSource).DataContext;
            
            try
            {
                await Client.SendAsync(new SendMessage("home", $"/leave {room.ShortName}"));
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to send leave: {e}");
            }

        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            NavigateTo<SettingsPage>();
            RoomsListView.SelectedItem = null;
        }

        private void NavigateTo<T>(string parameter = null)
            where T : Page
        {
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                RootSplitView.IsPaneOpen = false;
            
            ContentFrame.Navigate(typeof(T), parameter, new EntranceNavigationTransitionInfo());

            var backStack = ContentFrame.BackStack;

            // make sure home is at the bottom of the stack
            if (backStack.Count == 0 || !(backStack[0].SourcePageType == typeof(ChatPage) && (string)backStack[0].Parameter == "home"))
                backStack.Insert(0, new PageStackEntry(typeof(ChatPage), "home", new EntranceNavigationTransitionInfo()));

            if (typeof(T) == typeof(ChatPage))
            {
                if (parameter == "home")
                {
                    // can't go back from home
                    backStack.Clear();
                }
                else
                {
                    // can only go back to home, which is at the bottom of the stack
                    while (backStack.Count > 1)
                    {
                        backStack.RemoveAt(1);
                    }
                }
            }
            else
            {
                // only allow one type of page at a time
                var prevOfType = backStack.FirstOrDefault(e => e.SourcePageType == typeof(T));
                if (prevOfType != null)
                    backStack.Remove(prevOfType);
            }

            // update back button visibility
            var backButtonVisibility = ContentFrame.CanGoBack
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility
                = backButtonVisibility;
        }

        private void SwitchToPreferredRoom()
        {
            var selectedRoom = Settings.SelectedRoom.Value;

            var room = Client.Rooms.FirstOrDefault(r => r.ShortName == selectedRoom);
            if (room == null && Client.Rooms.Count > 0)
                room = Client.Rooms[0];

            RoomsListView.SelectedItem = room;
        }

        private void CheckSplitViewMargin()
        {
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                SplitViewMargin = new Thickness(TogglePaneButton.ActualWidth, 0, 0, 0);
            else if (RootSplitView.DisplayMode == SplitViewDisplayMode.CompactInline)
                SplitViewMargin = new Thickness(0);
            else
                throw new NotSupportedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
