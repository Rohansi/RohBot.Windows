﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using RohBot.Impl;
using RohBot.Impl.Packets;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Foundation.Metadata;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using RohBot.Annotations;

namespace RohBot.Views
{
    public sealed partial class ChatPage : Page, INotifyPropertyChanged
    {
        private static readonly Dictionary<string, string> SavedMessageText =
            new Dictionary<string, string>();

        private static bool _uploadingImage;
        private static bool _uploadIntederminate;
        private static double _uploadValue;

        public bool UploadingImage
        {
            get => _uploadingImage;
            private set
            {
                if (value == _uploadingImage) return;
                _uploadingImage = value;
                OnPropertyChanged();
            }
        }

        public bool UploadIndeterminate
        {
            get => _uploadIntederminate;
            private set
            {
                if (value == _uploadIntederminate) return;
                _uploadIntederminate = value;
                OnPropertyChanged();
            }
        }

        public double UploadValue
        {
            get => _uploadValue;
            private set
            {
                if (Math.Abs(value - _uploadValue) < 0.01) return;
                _uploadValue = value;
                OnPropertyChanged();
            }
        }

        public AppShell Shell { get; }

        public Room CurrentRoom
        {
            get => _currentRoom;
            set
            {
                if (Equals(value, _currentRoom)) return;
                _currentRoom = value;
                OnPropertyChanged();
            }
        }
        
        private Room _currentRoom;
        private string _shortName;
        private ScrollViewer _messagesScrollViewer;
        private ItemsStackPanel _messagesItemsPanel;

        private List<string> _completionNames;
        private int _completionIndex;
        private int _completionStart;
        private int _completionEnd;

        public ChatPage()
        {
            Shell = AppShell.Current;

            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _shortName = (string)e.Parameter;
            if (_shortName == null)
                return;

            CurrentRoom = Client.Instance.Rooms.FirstOrDefault(r => r.ShortName == _shortName);
            Client.Instance.CurrentRoom = CurrentRoom;

            if (CurrentRoom != null)
                CurrentRoom.HasUnreadMessages = false;

            if (SavedMessageText.TryGetValue(_shortName, out var savedText))
                MessageTextBox.Text = savedText ?? "";
        }
        
        private void ChatPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            _messagesScrollViewer = Utils.GetScrollViewer(MessagesListView);
            _messagesScrollViewer.ViewChanged += MessagesScrollViewer_ViewChanged;
            _messagesItemsPanel = (ItemsStackPanel)MessagesListView.ItemsPanelRoot;

            if (!ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                MessageTextBox.Focus(FocusState.Programmatic);
                MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            }
        }

        private void ChatPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // the first instance of a DataTemplate with bindings will never unsubscribe
            // from events, so we need to do it ourselves...
            if (CurrentRoom != null)
            {
                foreach (var line in CurrentRoom.Messages)
                {
                    line.ResetPropertyChanged();
                }
            }

            // must clean up!
            _messagesScrollViewer.ViewChanged -= MessagesScrollViewer_ViewChanged;
            MessagesListView.ItemsSource = null;
            
            _messagesScrollViewer = null;
            _messagesItemsPanel = null;

            Bindings?.StopTracking();
        }

        private void MessagesScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var topEdge = _messagesScrollViewer.ScrollableHeight - _messagesScrollViewer.ViewportHeight - 1;
            var isAtBottom = _messagesScrollViewer.VerticalOffset >= topEdge;

            _messagesItemsPanel.ItemsUpdatingScrollMode = isAtBottom
                ? ItemsUpdatingScrollMode.KeepLastItemInView
                : ItemsUpdatingScrollMode.KeepScrollOffset;
        }

        private async void SendButton_Tapped(object sender, TappedRoutedEventArgs args)
        {
            try
            {
                await Client.Instance.SendAsync(new SendMessage(_shortName, MessageTextBox.Text));

                MessageTextBox.Text = "";

                if (args != null)
                    args.Handled = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to send message: {e}");
            }
        }

        private async void CameraButton_OnTapped(object sender, TappedRoutedEventArgs args)
        {
            if (UploadingImage)
                return;

            try
            {
                var openPicker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    FileTypeFilter = {".jpg", ".jpeg", ".png"}
                };

                var file = await openPicker.PickSingleFileAsync();
                if (file == null)
                    return;
                
                UploadingImage = true;
                UploadIndeterminate = true;

                var inputStream = await file.OpenReadAsync();
                using (var imageStream = await Utils.ProcessImage(inputStream, (ImageScale)Settings.ImageSize.Value))
                {
                    var fileContent = new HttpStreamContent(imageStream);
                    var mime = Path.GetExtension(file.Name) == ".png" ? "image/png" : "image/jpeg";
                    fileContent.Headers.ContentType = HttpMediaTypeHeaderValue.Parse(mime);

                    var multipartContent = new HttpMultipartFormDataContent();
                    multipartContent.Add(fileContent, "file", file.Name);

                    var postTask = new HttpClient().PostAsync(new Uri("https://vgy.me/upload"), multipartContent);

                    postTask.Progress += (info, progressInfo) =>
                    {
                        var x = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            UploadIndeterminate = !progressInfo.TotalBytesToSend.HasValue;
                            if (UploadIndeterminate)
                                return;

                            UploadIndeterminate = false;
                            UploadValue = (progressInfo.BytesSent / (double)progressInfo.TotalBytesToSend.Value) * 100;

                            if (UploadValue > 99)
                                UploadIndeterminate = true; // waiting on server
                        });
                    };

                    var result = await postTask;
                    var resultObj = JsonObject.Parse(await result.Content.ReadAsStringAsync());
                    var imageLink = resultObj.GetNamedString("image");

                    for (var i = 0; i < 5; i++)
                    {
                        if (Client.Instance.IsLoggedIn)
                        {
                            try
                            {
                                await Client.Instance.SendAsync(
                                    new SendMessage(_shortName, imageLink));

                                return;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Failed to send image link: {e}");
                                break;
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    // failed to reconnect in time, save link for later
                    MessageTextBox.Text = imageLink;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed up upload image: {e}");
                await App.ShowMessage("Failed to upload image.");
            }
            finally
            {
                UploadingImage = false;
            }
        }

        private void MessageTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SavedMessageText[_shortName] = MessageTextBox.Text;
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            var isCtrlDown = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
            var isShiftDown = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

            if (args.Key == VirtualKey.Enter)
            {
                if (!isCtrlDown)
                {
                    SendButton_Tapped(null, null);
                }
                else
                {
                    Debug.WriteLine("Ctrl+Enter");

                    var start = MessageTextBox.SelectionStart;
                    var end = start + MessageTextBox.SelectionLength;

                    var text = MessageTextBox.Text;
                    var before = text.Substring(0, start);
                    var after = text.Substring(end);

                    MessageTextBox.Text = before + "\r" + after;
                    MessageTextBox.SelectionStart = start + 1;
                }

                args.Handled = true;
            }

            if (args.Key == VirtualKey.Tab && !isCtrlDown && !isShiftDown)
            {
                Debug.WriteLine("Tab");
                
                args.Handled = DoNameCompletion();
            }
            else
            {
                _completionNames = null;
            }
        }

        private bool DoNameCompletion()
        {
            var val = MessageTextBox.Text;
            var selectionStart = MessageTextBox.SelectionStart;
            var selectionEnd = selectionStart + MessageTextBox.SelectionLength;

            if (_completionNames == null)
            {
                var wordStart = val.Substring(0, selectionStart).LastIndexOf(' ') + 1;

                var completionWord = val.Substring(wordStart, selectionStart - wordStart);
                if (completionWord.Length == 0)
                    return false;

                _completionNames = _currentRoom.Users
                    .Select(u => u.Name)
                    .Where(n => n.StartsWith(completionWord, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();

                if (_completionNames.Count == 0)
                {
                    _completionNames = null;
                    return true;
                }

                _completionNames.Add(completionWord);

                _completionIndex = 0;
                _completionStart = wordStart;
                _completionEnd = selectionEnd;
            }
            else
            {
                _completionIndex++;
                _completionIndex %= _completionNames.Count;
            }

            var completionStr = _completionNames[_completionIndex];

            if (_completionIndex != _completionNames.Count - 1)
            {
                if (_completionStart == 0)
                    completionStr += ":";

                completionStr += " ";
            }

            var begin = val.Substring(0, _completionStart);
            var end = val.Substring(_completionEnd);
            MessageTextBox.Text = begin + completionStr + end;
            MessageTextBox.SelectionStart = _completionEnd = _completionStart + completionStr.Length;

            return true;
        }
        
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() == MessageTextBox)
                return;

            if (e.Key == VirtualKey.Control ||
                e.Key == VirtualKey.Shift ||
                e.Key == VirtualKey.Tab ||
                e.Key == VirtualKey.Left ||
                e.Key == VirtualKey.Right ||
                e.Key == VirtualKey.Up ||
                e.Key == VirtualKey.Down)
            {
                return;
            }

            MessageTextBox.Focus(FocusState.Programmatic);

            base.OnKeyDown(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
