using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using RohBot.Impl;
using RohBot.Impl.Packets;

namespace RohBot.Views
{
    public sealed partial class LoginPage : Page
    {
        public Client Client => Client.Instance;
        public Connection Connection => Client.Connection;

        public LoginPage()
        {
            InitializeComponent();
        }
        
        private void LoginPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Frame.BackStack.Clear();

            Client.Authenticated += Client_OnLoggedIn;
        }

        private void LoginPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Client.Authenticated -= Client_OnLoggedIn;
        }

        private async void Client_OnLoggedIn(bool loggedIn, string message)
        {
            LoginButton.IsEnabled = true;

            if (loggedIn)
            {
                Settings.LoggedIn.Value = true;
                Settings.Username.Value = UsernameBox.Text;

                Frame.Navigate(typeof(AppShell));
                return;
            }

            Settings.LoggedIn.Value = false;
            await App.ShowMessage(message);
        }

        private async void LoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await Client.SendAsync(new Authenticate(
                    AuthenticateMethod.Login, UsernameBox.Text, PasswordBox.Password));

                LoginButton.IsEnabled = false;
            }
            catch
            {
                await App.ShowMessage("Failed to send login.");
            }
        }

        private void RegisterButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;
            
            LoginButton_OnClick(null, null);
            e.Handled = true;
        }
    }
}
