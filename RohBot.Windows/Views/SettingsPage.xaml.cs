using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using OneSignalSDK_WP_WNS;
using RohBot.Converters;
using RohBot.Impl;
using RohBot.Impl.Packets;

namespace RohBot.Views
{
    public sealed class ComboBoxDescriptor<T>
    {
        public string Name { get; }
        public T Value { get; }

        public ComboBoxDescriptor(string name, T value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => Name;
    }

    public sealed partial class SettingsPage : Page
    {
        private AppShell Shell => AppShell.Current;

        private bool _settingNotificationToggleSwitch;

        public SettingsPage()
        {
            InitializeComponent();

            var themes = new ObservableCollection<string>
            {
                "Dark",
                "Light"
            };

            ThemeCombo.ItemsSource = themes;
            ThemeCombo.SelectedIndex = themes.IndexOf(Settings.Theme.Value);

            var timeFormats = new ObservableCollection<ComboBoxDescriptor<TimeFormat>>
            {
                new ComboBoxDescriptor<TimeFormat>("System", TimeFormat.System),
                new ComboBoxDescriptor<TimeFormat>("12-hour", TimeFormat.TwelveHour),
                new ComboBoxDescriptor<TimeFormat>("24-hour", TimeFormat.TwentyFourHour)
            };

            TimeFormatCombo.ItemsSource = timeFormats;
            TimeFormatCombo.SelectedIndex = timeFormats
                .Select(d => d.Value)
                .ToList()
                .IndexOf((TimeFormat)Settings.TimeFormat.Value);

            var imageSizes = new ObservableCollection<ComboBoxDescriptor<ImageScale>>
            {
                new ComboBoxDescriptor<ImageScale>("Small (500px)", ImageScale.Small),
                new ComboBoxDescriptor<ImageScale>("Medium (1000px)", ImageScale.Medium),
                new ComboBoxDescriptor<ImageScale>("Large (2000px)", ImageScale.Large),
                new ComboBoxDescriptor<ImageScale>("Original", ImageScale.Original)
            };

            ImageSizeCombo.ItemsSource = imageSizes;
            ImageSizeCombo.SelectedIndex = imageSizes
                .Select(d => d.Value)
                .ToList()
                .IndexOf((ImageScale)Settings.ImageSize.Value);

            _settingNotificationToggleSwitch = true;
            NotificationToggleSwitch.IsOn = Settings.NotificationsEnabled.Value;
            NotificationPatternText.Text = Settings.NotificationPattern.Value;

            // sometimes we need this?
            _settingNotificationToggleSwitch = false;
        }

        private void SettingsPage_OnUnloaded(object sender, RoutedEventArgs args)
        {
            TimeFormatCombo.ItemsSource = null;
            ThemeCombo.ItemsSource = null;
        }

        private void TimeFormatCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (TimeFormatCombo.ItemsSource != null && TimeFormatCombo.SelectedItem != null)
            {
                var descriptor = (ComboBoxDescriptor<TimeFormat>)TimeFormatCombo.SelectedItem;
                Settings.TimeFormat.Value = (int)descriptor.Value;
            }
        }

        private void ImageSizeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (ImageSizeCombo.ItemsSource != null && ImageSizeCombo.SelectedItem != null)
            {
                var descriptor = (ComboBoxDescriptor<ImageScale>)ImageSizeCombo.SelectedItem;
                Settings.ImageSize.Value = (int)descriptor.Value;
            }
        }

        private void ThemeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (ThemeCombo.ItemsSource != null && ThemeCombo.SelectedItem != null)
                Settings.Theme.Value = (string)ThemeCombo.SelectedItem;
        }

        private async void LogoutButton_OnClick(object sender, RoutedEventArgs args)
        {
            LogoutButton.IsEnabled = false;

            var playerId = OneSignal.GetPlayerId();
            if (Settings.NotificationsEnabled.Value && playerId != null)
            {
                try
                {
                    await OneSignal.SetSubscriptionAsync(false);
                    await Client.Instance.SendAsync(new NotificationUnsubscriptionRequest(playerId));
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to unsubscribe on logout: {e}");
                }
            }

            Settings.LoggedIn.Value = false;
            Settings.Username.Value = null;
            Settings.Token.Value = null;

            Settings.NotificationsEnabled.Value = false;
            Settings.NotificationPattern.Value = "";

            Client.Instance.Connection.Disconnect();

            var rootFrame = (Frame)Window.Current.Content;
            rootFrame.Navigate(typeof(LoginPage));
        }

        private async void NotificationToggleSwitch_OnToggled(object sender, RoutedEventArgs args)
        {
            if (_settingNotificationToggleSwitch)
            {
                _settingNotificationToggleSwitch = false;
                return;
            }

            var enabled = NotificationToggleSwitch.IsOn;

            try
            {
                var playerId = OneSignal.GetPlayerId();
                if (playerId == null)
                {
                    await App.ShowMessage("Device token is not available.");
                    return;
                }

                if (string.IsNullOrEmpty(NotificationPatternText.Text))
                    NotificationPatternText.Text = Client.Instance.Name;

                NotificationToggleSwitch.IsEnabled = false;

                try
                {
                    await OneSignal.SetSubscriptionAsync(enabled);
                }
                catch
                {
                    await App.ShowMessage("Failed to toggle OneSignal subscription.");
                    throw;
                }

                try
                {
                    if (enabled)
                    {
                        await SaveNotificationPattern(playerId, NotificationPatternText.Text);
                    }
                    else
                    {
                        /* TODO:
                         * unsubscribe causes a sysMessage error if we toggle within a few seconds
                         * skipping this should be fine because onesignal won't push to us, and
                         * rohbot will auto unsubscribe!
                         */
                        //await Client.Instance.SendAsync(new NotificationUnsubscriptionRequest(playerId));
                    }
                }
                catch
                {
                    await App.ShowMessage("Failed to toggle RohBot subscription.");
                    throw;
                }

                Settings.NotificationsEnabled.Value = enabled;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to toggle notifications: {e}");

                _settingNotificationToggleSwitch = true;
                NotificationToggleSwitch.IsOn = !enabled;
            }

            NotificationToggleSwitch.IsEnabled = true;
        }

        private async void NotificationPatternSaveButton_OnClick(object sender, RoutedEventArgs args)
        {
            var playerId = OneSignal.GetPlayerId();
            if (playerId == null)
            {
                await App.ShowMessage("Device token is not available.");
                return;
            }

            SetNotificationFormEnabled(false);

            try
            {
                await SaveNotificationPattern(playerId, NotificationPatternText.Text);
            }
            catch (Exception e)
            {
                await App.ShowMessage("Failed to save RohBot subscription.");
                Debug.WriteLine($"Failed to save notification regex: {e}");
            }

            SetNotificationFormEnabled(true);
        }

        private static async Task SaveNotificationPattern(string playerId, string pattern)
        {
            await Client.Instance.SendAsync(new NotificationSubscriptionRequest(playerId, pattern));
            Settings.NotificationPattern.Value = pattern;
        }

        private void SetNotificationFormEnabled(bool isEnabled)
        {
            NotificationToggleSwitch.IsEnabled = isEnabled;
            NotificationPatternSaveButton.IsEnabled = isEnabled;
        }
    }
}
