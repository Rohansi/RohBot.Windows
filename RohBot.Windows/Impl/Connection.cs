using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using RohBot.Annotations;
using System.Diagnostics;

namespace RohBot.Impl
{
    public sealed class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }

    public sealed class Connection : INotifyPropertyChanged
    {
        private const uint MaxMessageSize = 1024 * 1024;

        public delegate void ConnectedHandler(object sender, EventArgs args);

        public delegate void DisconnectedHandler(object sender, EventArgs args);

        public delegate void MessageReceivedHandler(object sender, MessageReceivedEventArgs args);
        
        private readonly object _sync = new object();
        private MessageWebSocket _webSocket;
        private bool _connecting;
        private bool _isConnected;

        public event ConnectedHandler Connected;
        public event DisconnectedHandler Disconnected;
        public event MessageReceivedHandler MessageReceived;

        public async Task ConnectAsync()
        {
            lock (_sync)
            {
                if (_connecting || IsConnected)
                    return; // already connected

                _connecting = true;
            }

            try
            {
                Disconnect();

                _webSocket = new MessageWebSocket();
                _webSocket.Control.MessageType = SocketMessageType.Utf8;
                _webSocket.Control.MaxMessageSize = MaxMessageSize;
                _webSocket.Closed += WebSocket_Closed;
                _webSocket.MessageReceived += WebSocket_MessageReceived;
                _webSocket.SetRequestHeader("User-Agent", "Windows Phone 10.0");

                await _webSocket.ConnectAsync(new Uri("wss://rohbot.net/ws/"));
                IsConnected = true;
            }
            catch (Exception e)
            {
                throw new ClientException("Failed to connect to server.", e);
            }
            finally
            {
                lock (_sync)
                {
                    _connecting = false;
                }
            }

            if (IsConnected)
                Connected?.Invoke(this, EventArgs.Empty);
        }

        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (var reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = UnicodeEncoding.Utf8;

                    var message = reader.ReadString(reader.UnconsumedBufferLength);
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                }
            }
            catch (Exception e)
            {
                Disconnect();
                Debug.WriteLine($"Failed to read message: {e}");
            }
        }

        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            lock (_sync)
            {
                IsConnected = false;
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
            _webSocket = null;
        }

        public void Disconnect()
        {
            try
            {
                _webSocket?.Close(0, "Disconnected");
            }
            catch
            {
                WebSocket_Closed(_webSocket, null);
            }
            finally
            {
                _webSocket = null;
            }
        }

        public async Task SendAsync(string message)
        {
            lock (_sync)
            {
                if (!IsConnected)
                    throw new ClientException("Not connected to server.");
            }

            var writer = new DataWriter(_webSocket.OutputStream);

            try
            {
                writer.WriteString(message);
                await writer.StoreAsync();
            }
            catch (Exception e)
            {
                throw new ClientException("Failed to send message to server.", e);
            }
            finally
            {
                writer.DetachStream();
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (value == _isConnected) return;
                _isConnected = value;
                OnPropertyChanged();
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
