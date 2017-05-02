using System;
using System.Diagnostics;
using Windows.Storage;

namespace RohBot
{
    public static class Settings
    {
        /// <summary>
        /// Selected app theme. Dark or Light.
        /// </summary>
        public static readonly Setting<string> Theme = new Setting<string>("Theme", "Dark");

        /// <summary>
        /// The currently selected room. Used to restore selection between sessions.
        /// </summary>
        public static readonly Setting<string> SelectedRoom = new Setting<string>("SelectedRoom", "home");

        /// <summary>
        /// True when login was successful. Saved username and password should be valid
        /// so we can skip the login page.
        /// </summary>
        public static readonly Setting<bool> LoggedIn = new Setting<bool>("LoggedIn", false);

        /// <summary>
        /// Saved account username.
        /// </summary>
        public static readonly Setting<string> Username = new Setting<string>("Username");

        /// <summary>
        /// Saved account token.
        /// </summary>
        public static readonly Setting<string> Token = new Setting<string>("Token");

        /// <summary>
        /// True when subscribed to push notifications.
        /// </summary>
        public static readonly Setting<bool> NotificationsEnabled = new Setting<bool>("NotificationsEnabled", false);

        /// <summary>
        /// The regex pattern to use for push notifications.
        /// </summary>
        public static readonly Setting<string> NotificationPattern = new Setting<string>("NotificationPattern", "");

        /// <summary>
        /// The time format used in messages. Must be a value in <see cref="Converters.TimeFormat"/>.
        /// </summary>
        public static readonly Setting<int> TimeFormat = new Setting<int>("TimeFormat", (int)Converters.TimeFormat.System);

        /// <summary>
        /// Maximum size of images before they are scaled down. Must be a value in <see cref="ImageScale"/>.
        /// </summary>
        public static readonly Setting<int> ImageSize = new Setting<int>("ImageSize", (int)ImageScale.Medium);
    }

    public abstract class SettingBase
    {
        /// <summary>
        /// Access to LocalSettings must be atomic.
        /// </summary>
        protected static readonly object SettingsLock = new object();

        /// <summary>
        /// Keep only one shared instance of LocalSettings.
        /// </summary>
        protected static readonly ApplicationDataContainer Local = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Encapsulates a key/value pair stored in Application Settings.
    /// Note limit for each object is about 4kB of data!
    /// </summary>
    /// <typeparam name="T">Type to store, must be serializable.</typeparam>
    public class Setting<T> : SettingBase
    {
        private readonly string _name;
        private T _value;
        private bool _hasValue;

        public Setting(string name)
            : this(name, default(T))
        {
            
        }

        public Setting(string name, T defaultValue)
        {
            _name = name;
            DefaultValue = defaultValue;
        }

        public T Value
        {
            get
            {
                lock (SettingsLock)
                {
                    if (_hasValue)
                        return _value;

                    try
                    {
                        if (Local.Values.TryGetValue(_name, out object rawValue))
                        {
                            _value = (T)rawValue;
                        }
                        else
                        {
                            _value = DefaultValue;
                            Local.Values[_name] = DefaultValue;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Setting Get error, name: {0}, {1}", _name, e);
                        _value = DefaultValue;
                        Local.Values[_name] = DefaultValue;
                    }

                    _hasValue = true;
                    return _value;
                }
            }
            set
            {
                lock (SettingsLock)
                {
                    Local.Values[_name] = value;
                    _value = value;
                    _hasValue = true;
                }
            }
        }
        
        public T DefaultValue { get; }
    }
}
