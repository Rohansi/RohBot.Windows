using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using RohBot.Impl;
using Windows.Networking.Connectivity;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.HockeyApp;
using OneSignalSDK_WP_WNS;
using RohBot.Views;

namespace RohBot
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private CancellationTokenSource _cts;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            HockeyClient.Current.Configure("33b5247d019b4fbda6555d0a480e2d01");

            RequestedTheme = Settings.Theme.Value == "Dark"
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = false;
            }
#endif

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            if (titleBar != null)
            {
                titleBar.ForegroundColor =
                    titleBar.InactiveForegroundColor = (Color)Resources["RohBotTitleBarForegroundColor"];

                titleBar.ButtonForegroundColor =
                    titleBar.ButtonInactiveForegroundColor =
                    titleBar.ButtonHoverForegroundColor =
                    titleBar.ButtonPressedForegroundColor = (Color)Resources["RohBotTitleBarButtonForegroundColor"];

                titleBar.BackgroundColor = (Color)Resources["RohBotTitleBarBackgroundColor"];
                titleBar.InactiveBackgroundColor = (Color)Resources["RohBotTitleBarBackgroundInactiveColor"];

                titleBar.ButtonBackgroundColor = (Color)Resources["RohBotTitleBarButtonBackgroundColor"];
                titleBar.ButtonHoverBackgroundColor = (Color)Resources["RohBotTitleBarButtonBackgroundHoverColor"];
                titleBar.ButtonPressedBackgroundColor = (Color)Resources["RohBotTitleBarButtonBackgroundPressedColor"];
                titleBar.ButtonInactiveBackgroundColor = (Color)Resources["RohBotTitleBarButtonBackgroundInactiveColor"];
            }

            Client.Instance.SysMessageReceived -= Client_OnSysMessageReceived;
            Client.Instance.SysMessageReceived += Client_OnSysMessageReceived;

            StartConnectionTask();

            OneSignal.Init("97f9b9a8-dcd0-46a0-a6f3-c61600d38cd3", e.Arguments);

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.Loaded += async (sender, args) =>
                {
                    if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
                    {
                        var statusBar = StatusBar.GetForCurrentView();
                        await statusBar.HideAsync();
                    }
                };

                rootFrame.Navigated += (sender, args) =>
                {
                    var navManager = SystemNavigationManager.GetForCurrentView();

                    var pageType = args.SourcePageType;
                    if (pageType == typeof(LoginPage) || pageType == typeof(AppShell))
                    {
                        rootFrame.BackStack.Clear();
                        navManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                    }
                    else
                    {
                        var backButtonVisibility = rootFrame.CanGoBack
                               ? AppViewBackButtonVisibility.Visible
                               : AppViewBackButtonVisibility.Collapsed;

                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility
                            = backButtonVisibility;
                    }
                };
                
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter

                    rootFrame.Navigate(Settings.LoggedIn.Value
                        ? typeof(AppShell)
                        : typeof(LoginPage));
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private async void Client_OnSysMessageReceived(string message)
        {
            await ShowMessage(message);
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;
            
            if (rootFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }
        
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            _cts.Cancel();

            deferral.Complete();
        }

        private void OnResuming(object sender, object o)
        {
            StartConnectionTask();
        }

        private void StartConnectionTask()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            Task.Run(() => ClientConnectionTask(_cts.Token));
        }

        private static async Task ClientConnectionTask(CancellationToken ct)
        {
            var connection = Client.Instance.Connection;

            if (connection.IsConnected)
            {
                // send a packet to test if its still alive

                try
                {
                    await connection.SendAsync("{\"Type\":\"ping\"}");
                }
                catch
                {
                    connection.Disconnect();
                }
            }
            
            while (!ct.IsCancellationRequested)
            {
                if (NetworkInformation.GetInternetConnectionProfile() == null)
                {
                    Debug.WriteLine("No internet connection!");

                    // close the connection now so we dont wait for timeout
                    Client.Instance.Connection.Disconnect();
                }
                else if (!connection.IsConnected)
                {
                    Debug.WriteLine("Connecting to server");

                    try
                    {
                        await connection.ConnectAsync();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Failed to connect to server: {e}");
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        public static async Task ShowMessage(string message)
        {
            var messageDialog = new MessageDialog(message)
            {
                Commands =
                {
                    new UICommand("OK")
                },
                DefaultCommandIndex = 0,
                CancelCommandIndex = 0
            };

            await messageDialog.ShowAsync();
        }
    }
}
