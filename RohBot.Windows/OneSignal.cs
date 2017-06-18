using RohBot.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Networking.PushNotifications;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.UI.Core;
using RohBot;
using RohBot.Impl;

namespace OneSignalSDK_WP_WNS
{
    public static class OneSignal
    {
        public const string VERSION = "010101";

        private const string BASE_URL = "https://onesignal.com/api/v1/";
        private static string mAppId;
        private static string mPlayerId, mChannelUri;
        private static long lastPingTime;
        private static ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        private static bool initDone = false;
        private static bool foreground = true;

        public delegate void NotificationReceived(string message, IDictionary<string, string> additionalData, bool isActive);
        public static NotificationReceived notificationDelegate = null;

        public delegate void IdsAvailable(string playerID, string pushToken);
        public static IdsAvailable idsAvailableDelegate = null;

        public delegate void TagsReceived(IDictionary<string, string> tags);
        public static TagsReceived tagsReceivedDelegate = null;

        private static IDisposable fallBackOneSignalSession;

        private static bool sessionCallInProgress, sessionCallDone, subscriptionChangeInProgress;

        public static void Init(string appId, string launchArgs, NotificationReceived inNotificationDelegate = null)
        {
            mAppId = appId;

            if (inNotificationDelegate != null)
                notificationDelegate = inNotificationDelegate;

            mPlayerId = (string)settings.Values["OneSignalPlayerId"];
            mChannelUri = (string)settings.Values["OneSignalChannelUri"];

            checkForNotificationOpened(launchArgs);


            if (initDone)
                return;

            fallBackOneSignalSession = new Timer(o => { SendSession(null); }, null, 20000, Timeout.Infinite);

            /*Windows.UI.Core.CoreWindow.GetForCurrentThread().VisibilityChanged -= OneSignal_VisibilityChanged_Window_Current;
            Windows.UI.Core.CoreWindow.GetForCurrentThread().VisibilityChanged += OneSignal_VisibilityChanged_Window_Current;*/
            Windows.UI.Core.CoreWindow.GetForCurrentThread().Activated -= OneSignal_Activated_Window_Current;
            Windows.UI.Core.CoreWindow.GetForCurrentThread().Activated += OneSignal_Activated_Window_Current;

            lastPingTime = DateTime.Now.Ticks;

            // async
            GetPushUri();

            initDone = true;
        }

        [CanBeNull]
        public static string GetPlayerId()
        {
            return mPlayerId;
        }

        /*private static void OneSignal_VisibilityChanged_Window_Current(object sender, VisibilityChangedEventArgs args)
        {
            foreground = args.Visible;

            if (foreground)
                lastPingTime = DateTime.Now.Ticks;
            else
            {
                var time_elapsed = (long)((((DateTime.Now.Ticks) - lastPingTime) / 10000000) + 0.5);
                lastPingTime = DateTime.Now.Ticks;

                if (time_elapsed < 0 || time_elapsed > 604800)
                    return;

                var unSentActiveTime = GetSavedActiveTime();
                var totalTimeActive = unSentActiveTime + time_elapsed;

                if (totalTimeActive < 30)
                {
                    settings.Values["OneSignalActiveTime"] = totalTimeActive;
                    return;
                }

                SendPing(totalTimeActive);
                settings.Values["OneSignalActiveTime"] = (long)0;
            }
        }*/

        private static void OneSignal_Activated_Window_Current(CoreWindow sender, WindowActivatedEventArgs args)
        {
            foreground = args.WindowActivationState != CoreWindowActivationState.Deactivated;

            if (foreground)
                lastPingTime = DateTime.Now.Ticks;
            else
            {
                var time_elapsed = (long)((((DateTime.Now.Ticks) - lastPingTime) / 10000000) + 0.5);
                lastPingTime = DateTime.Now.Ticks;

                if (time_elapsed < 0 || time_elapsed > 604800)
                    return;

                var unSentActiveTime = GetSavedActiveTime();
                var totalTimeActive = unSentActiveTime + time_elapsed;

                if (totalTimeActive < 30)
                {
                    settings.Values["OneSignalActiveTime"] = totalTimeActive;
                    return;
                }

                SendPing(totalTimeActive);
                settings.Values["OneSignalActiveTime"] = (long)0;
            }
        }

        private static void checkForNotificationOpened(string args)
        {
            if (!args.Equals(""))
            {
                var json = JsonObject.Parse(args);
                if (json.ContainsKey("custom"))
                    NotificationOpened("", json, true);
            }
        }

        private static async void GetPushUri()
        {
            try
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

                channel.PushNotificationReceived -= channel_PushNotificationReceived;
                channel.PushNotificationReceived += channel_PushNotificationReceived;

                SendSession(channel.Uri);
            }
            catch
            {
                Debug.WriteLine("OneSignal: failed to create push notification channel");
            }
        }

        static void channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            if (foreground
               && args.NotificationType == PushNotificationType.Toast
               && args.ToastNotification.Content.FirstChild.Attributes.GetNamedItem("launch") != null)
            {

                try
                {
                    string lauchJson = (string)args.ToastNotification.Content.FirstChild.Attributes.GetNamedItem("launch").NodeValue;
                    var json = JsonObject.Parse(lauchJson);

                    if (json.ContainsKey("custom"))
                    {
                        var bindingNode = args.ToastNotification.Content.SelectSingleNode("/toast/visual/binding");
                        string text1 = bindingNode.SelectSingleNode("text[@id='2']").InnerText;

                        var custom = json.GetNamedObject("custom");
                        if (custom.ContainsKey("a"))
                        {
                            var a = custom.GetNamedObject("a");
                            if (a.ContainsKey("chat"))
                            {
                                var chat = a.GetNamedString("chat");

                                if (chat == Client.Instance.CurrentRoom?.ShortName) // TODO: have a callback for this
                                {
                                    args.ToastNotification.SuppressPopup = true;
                                    args.Cancel = true;
                                }
                            }
                        }

                        NotificationOpened(text1, json, false);
                    }
                }
                catch (Exception)
                {
                    Debug.WriteLine("OneSignal: failed to handle received notification");
                }
            }
        }

        private static long GetSavedActiveTime()
        {
            if (settings.Values.ContainsKey("OneSignalActiveTime"))
                return (long)(settings.Values["OneSignalActiveTime"]);
            return 0;
        }

        private static void SendPing(long activeTime)
        {
            if (mPlayerId == null)
                return;

            var jsonObject = new JsonObject
            {
                { "state", JsonValue.CreateStringValue("ping") },
                { "active_time", JsonValue.CreateNumberValue(activeTime) }
            };

            Task.Run(async () =>
            {
                try
                {
                    var client = GetHttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "players/" + mPlayerId + "/on_focus");
                    request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");

                    await client.SendAsync(request);
                }
                catch
                {
                    Debug.WriteLine("OneSignal: failed to ping");
                }
            });

            
        }

        private static void SendSession(string currentChannelUri)
        {
            if (sessionCallInProgress || sessionCallDone)
                return;

            sessionCallInProgress = true;

            string adId = Windows.System.UserProfile.AdvertisingManager.AdvertisingId;

            if (currentChannelUri != null && mChannelUri != currentChannelUri)
            {
                mChannelUri = currentChannelUri;
                settings.Values["OneSignalChannelUri"] = mChannelUri;
            }

            var deviceInformation = new EasClientDeviceInformation();

            PackageVersion pv = Package.Current.Id.Version;
            String appVersion = pv.Major + "." + pv.Minor + "." + pv.Revision + "." + pv.Build;

            var jsonObject = new JsonObject
            {
                { "device_type", JsonValue.CreateNumberValue(6) },
                { "app_id", JsonValue.CreateStringValue(mAppId) },
                { "identifier", JsonValue.CreateStringValue(mChannelUri) },
                { "ad_id", JsonValue.CreateStringValue(adId) },
                { "device_model", JsonValue.CreateStringValue(deviceInformation.SystemProductName) },
                { "game_version", JsonValue.CreateStringValue(appVersion) },
                { "language", JsonValue.CreateStringValue(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) },
                { "timezone", JsonValue.CreateStringValue(TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds.ToString()) },
                { "sdk", JsonValue.CreateStringValue(VERSION) }
            };

            string urlString = "players";
            if (mPlayerId != null)
                urlString += "/" + mPlayerId + "/on_session";
            else
                jsonObject.Add("sdk_type", JsonValue.CreateStringValue("native"));

            Task.Run(async () =>
            {
                try
                {
                    var client = GetHttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, urlString);
                    request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");

                    var result = await client.SendAsync(request);

                    if (result.IsSuccessStatusCode)
                    {
                        sessionCallDone = true;
                        string content = await result.Content.ReadAsStringAsync();
                        var jObject = JsonObject.Parse(content);
                        string newId = jObject.GetNamedStringOrNull("id");
                        if (newId != null)
                        {
                            mPlayerId = newId;
                            settings.Values["OneSignalPlayerId"] = newId;
                            if (idsAvailableDelegate != null)
                                idsAvailableDelegate(mPlayerId, mChannelUri);
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine("OneSignal: failed to send session");
                }
            });
        }

        private static void NotificationOpened(string message, JsonObject jObject, bool openedFromNotification)
        {
            var jsonObject = new JsonObject
            {
                { "app_id", JsonValue.CreateStringValue(mAppId) },
                { "player_id", JsonValue.CreateStringValue(mPlayerId) },
                { "opened", JsonValue.CreateBooleanValue(true) }
            };

            var custom = jObject.GetNamedObject("custom");

            Task.Run(async () =>
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "notifications/" + custom.GetNamedString("i"));
                    request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
                    await GetHttpClient().SendAsync(request);
                }
                catch
                {
                    Debug.WriteLine("OneSignal: failed to mark notification as opened");
                }
            });
            

            if (openedFromNotification && custom.ContainsKey("u"))
            {
                var uri = new Uri(custom.GetNamedString("u"), UriKind.Absolute);
                Task.Run(() => Windows.System.Launcher.LaunchUriAsync(uri));
            }

            if (notificationDelegate != null)
            {
                IDictionary<string, string> additionalData = null;

                if (custom.ContainsKey("a"))
                {
                    additionalData = custom.GetNamedObject("a")
                        .ToDictionary(kv => kv.Key, kv => kv.Value.GetString());
                }

                notificationDelegate(message, additionalData, initDone);
            }
        }

        private static HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BASE_URL);
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
        
        public static async Task SetSubscriptionAsync(bool status)
        {
            if (mPlayerId == null)
                throw new ArgumentException("Player ID is not set.");

            if (mAppId == null)
                throw new ArgumentException("App ID is not set.");

            if (subscriptionChangeInProgress)
                throw new ArgumentException("A subscription change is already in progress.");

            subscriptionChangeInProgress = true;

            try
            {
                var jsonObject = new JsonObject
                {
                    { "notification_types", JsonValue.CreateStringValue(status ? "1" : "-2") },
                };
                string urlString = "players/" + mPlayerId;

                var client = GetHttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, urlString);
                request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");

                await client.SendAsync(request);
            }
            finally
            {
                subscriptionChangeInProgress = false;
            }
        }

        /*public static void SendTag(string key, string value)
        {
            var dictionary = new Dictionary<string, object>();
            dictionary.Add(key, value);
            SendTags((IDictionary<string, object>)dictionary);
        }

        public static void SendTags(IDictionary<string, string> keyValues)
        {
            var newDict = new Dictionary<string, object>();
            foreach (var item in keyValues)
                newDict.Add(item.Key, item.Value.ToString());

            SendTags(newDict);
        }

        public static void SendTags(IDictionary<string, int> keyValues)
        {
            var newDict = new Dictionary<string, object>();
            foreach (var item in keyValues)
                newDict.Add(item.Key, item.Value.ToString());

            SendTags(newDict);
        }

        public static void SendTags(IDictionary<string, object> keyValues)
        {
            if (mPlayerId == null)
                return;

            JObject jsonObject = JObject.FromObject(new
            {
                tags = keyValues
            });

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "players/" + mPlayerId);
            request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
            GetHttpClient().SendAsync(request);
        }

        public static void DeleteTags(IList<string> tags)
        {
            if (mPlayerId == null)
                return;

            var dictionary = new Dictionary<string, string>();
            foreach (string key in tags)
                dictionary.Add(key, "");

            JObject jsonObject = JObject.FromObject(new
            {
                tags = dictionary
            });

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "players/" + mPlayerId);
            request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
            GetHttpClient().SendAsync(request);
        }

        public static void DeleteTag(string tag)
        {
            DeleteTags(new List<string>() { tag });
        }

        public static void SendPurchase(double amount)
        {
            SendPurchase((decimal)amount);
        }

        public static void SendPurchase(decimal amount)
        {
            if (mPlayerId == null)
                return;

            JObject jsonObject = JObject.FromObject(new
            {
                amount = amount
            });

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "players/" + mPlayerId + "/on_purchase");
            request.Content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
            GetHttpClient().SendAsync(request);
        }

        public static void GetIdsAvailable()
        {
            if (idsAvailableDelegate == null)
                throw new ArgumentNullException("Assign idsAvailableDelegate before calling or call GetIdsAvailable(IdsAvailable)");

            if (mPlayerId != null)
                idsAvailableDelegate(mPlayerId, mChannelUri);
        }

        public static void GetIdsAvailable(IdsAvailable inIdsAvailableDelegate)
        {
            idsAvailableDelegate = inIdsAvailableDelegate;

            if (mPlayerId != null)
                idsAvailableDelegate(mPlayerId, mChannelUri);
        }

        public static void GetTags()
        {
            if (mPlayerId == null)
                return;

            if (tagsReceivedDelegate == null)
                throw new ArgumentNullException("Assign tagsReceivedDelegate before calling or call GetTags(TagsReceived)");

            SendGetTagsMessage();
        }

        public static void GetTags(TagsReceived inTagsReceivedDelegate)
        {
            if (mPlayerId == null)
                return;

            tagsReceivedDelegate = inTagsReceivedDelegate;

            SendGetTagsMessage();
        }

        private static void SendGetTagsMessage()
        {
            GetHttpClient().GetAsync("players/" + mPlayerId).ContinueWith(async (responseTask) => {
                if (responseTask.Result.IsSuccessStatusCode)
                {
                    string content = await responseTask.Result.Content.ReadAsStringAsync();
                    tagsReceivedDelegate(JObject.Parse(content)["tags"].ToObject<Dictionary<string, string>>());
                }
            });

        }*/
    }
}