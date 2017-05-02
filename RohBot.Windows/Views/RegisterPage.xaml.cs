using System;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using RohBot.Impl;
using RohBot.Impl.Packets;

namespace RohBot.Views
{
    public sealed partial class RegisterPage : Page
    {
        public Client Client => Client.Instance;
        public Connection Connection => Client.Connection;

        public RegisterPage()
        {
            InitializeComponent();
        }

        private void RegisterPage_OnLoaded(object sender, RoutedEventArgs args)
        {
            Client.Authenticated += Client_OnAuthenticated;
        }

        private void RegisterPage_OnUnloaded(object sender, RoutedEventArgs args)
        {
            Client.Authenticated -= Client_OnAuthenticated;
        }

        private async void Client_OnAuthenticated(bool loggedIn, string message)
        {
            RegisterButton.IsEnabled = true;

            if (loggedIn)
            {
                Frame.Navigate(typeof(AppShell)); // already logged in
                return;
            }

            await App.ShowMessage(message);

            if (message == "Account created. You can now login.")
                Frame.Navigate(typeof(LoginPage));
        }

        private async void RegisterButton_OnClick(object sender, RoutedEventArgs args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UsernameBox.Text))
                {
                    await App.ShowMessage("Username is required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    await App.ShowMessage("Password is required.");
                    return;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    await App.ShowMessage("Passwords do not match.");
                    return;
                }

                await Client.SendAsync(new Authenticate(
                    AuthenticateMethod.Register, UsernameBox.Text, PasswordBox.Password));

                RegisterButton.IsEnabled = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to send registration: {e}");
                await App.ShowMessage("Failed to send registration.");
            }
        }

        private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            RegisterButton_OnClick(null, null);
            e.Handled = true;
        }
    }
}
