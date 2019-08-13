using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Services.Store;
using Windows.UI.ViewManagement;
using System.ComponentModel;
using bosxixi.Toolkit;
using Windows.System.Threading;
using System.Net.Http;
using System.Text;
using Windows.UI.Popups;
using Newtonsoft.Json;
using System.Diagnostics;
using Windows.Media.Core;
using Windows.ApplicationModel.Resources;
using System.Text.RegularExpressions;
using Windows.System.Profile;
using System.Threading;
using bosxixi.ScorpioPlayer.Core;
using Xamarin.Essentials;
using System.Windows.Input;
using MonkeyCache.LiteDB;
using Newtonsoft.Json.Linq;
using Windows.UI;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IELTSWord
{
    public class Word : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Elaborate { get; set; }
        public int Level { get; set; }
        public int Order { get; set; }
        public DateTimeOffset HitDate { get; set; }
        public List<DateTimeOffset> HitDates { get; set; }
        public void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word.Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word.Level)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word.Order)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word.HitDate)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word.Elaborate)));
        }

        public static IEnumerable<Word> GetAll()
        {
            var newIds = AppGlobalSettings.ID?.Split(',').ToList();
            if (!newIds.IsNullOrCountEqualsZero())
            {
                foreach (var item in newIds)
                {
                    var w = Word.Load(item);
                    if (w != null)
                    {
                        yield return w;
                    }
                }
                //return newIds.Select(id => Word.Load(id)).Where(c => c != null).ToList();
            }
            yield break;
        }
        public static Word Load(string id)
        {
            try
            {
                return new CompressedStorage(CompressedStorage.Storage.Word).TryGet<Word>(id);
            }
            catch (Exception)
            {
            }

            return null;
        }
        public static Word Create(string str)
        {
            Word word = new Word()
            {
                //Elaborate = tab.Last(),
                Id = str,
                Level = 0,
                Order = 20000,
                Name = str,
                HitDate = DateTimeOffset.UtcNow
            };
            return word;
        }
        [JsonIgnore]
        TimeSpan Elasped => DateTimeOffset.UtcNow - this.HitDate;
        public void Save()
        {
            var newIds = AppGlobalSettings.ID?.Split(',').ToList();
            newIds.Add(this.Id);
            newIds = newIds.Distinct().ToList();
            AppGlobalSettings.ID = string.Join(",", newIds);
            new CompressedStorage(CompressedStorage.Storage.Word).TrySet(this.Id, this);
            //var word = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            // SettingService.Set<string>(this.Id.ToString(), word);
            this.Raise();
        }
        public void Yes()
        {
            var duration = Duration[this.Level];
            if (Elasped > duration)
            {
                this.Level++;
            }
            this.HitDate = DateTimeOffset.UtcNow;
            AddHit();
            Save();
        }
        void AddHit()
        {
            if (this.HitDates == null)
            {
                this.HitDates = new List<DateTimeOffset>();
            }
            this.HitDates.Add(this.HitDate);
        }
        public void Blur()
        {
            var duration = Duration[this.Level];
            if (this.Level != 0)
            {
                this.Level--;
            }
            this.HitDate = DateTimeOffset.UtcNow;
            AddHit();
            Save();
        }
        public void No()
        {
            this.Level = 0;
            this.HitDate = DateTimeOffset.UtcNow;
            AddHit();
            Save();
        }
        public bool IsValid(Word other)
        {
            if (other != null && other.Name == this.Name)
            {
                return false;
            }
            var lastest = Word.Load(this.Id);
            if (lastest != null)
            {
                this.Level = lastest.Level;
                this.HitDate = lastest.HitDate;
                this.HitDates = lastest.HitDates;
                if (lastest.Level > 11)
                {
                    return false;
                }
                var duration = Duration[lastest.Level];
                if (Elasped > duration)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (this.Level > 11)
                {
                    return false;
                }
                var duration = Duration[this.Level];
                if (Elasped > duration)
                {
                    return true;
                }
                return false;
            }

        }
        public void Complete()
        {
            this.Level = 100;
            this.HitDate = DateTimeOffset.UtcNow;
            AddHit();
            Save();
        }
        [JsonIgnore]
        static Dictionary<int, TimeSpan> Duration = new Dictionary<int, TimeSpan>()
        {
            [0] = TimeSpan.FromSeconds(1),
            [1] = TimeSpan.FromSeconds(5),
            [2] = TimeSpan.FromSeconds(25),
            [3] = TimeSpan.FromMinutes(2),
            [4] = TimeSpan.FromMinutes(10),
            [5] = TimeSpan.FromHours(1),
            [6] = TimeSpan.FromHours(5),
            [7] = TimeSpan.FromDays(1),
            [8] = TimeSpan.FromDays(5),
            [9] = TimeSpan.FromDays(25),
            [10] = TimeSpan.FromDays(120),
            [11] = TimeSpan.FromDays(730),
        };
        public bool IsConsiderCompleted()
        {
            if (Duration.TryGetValue(this.Level, out TimeSpan duration))
            {
                if (Elasped > duration)
                {
                    return false;
                }
            }

            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    public static class AppGlobalSettings
    {
        public const string RemoteIconApiUrl = "http://scorpioplayer.com/api/setting/allowremotecontrol";
        public const string HasFlagApiUrl = "http://scorpioplayer.com/api/setting/HasFlag";
        public const string HubApiUrl = "http://scorpioplayer.com/api/setting/hub";
        public const string IndexFileUrl = "http://scorpioplayer.com/downloads/playlist/index.json";
        public const string PublicIndexFileUrl = "http://scorpioplayer.com/downloads/playlist/public_index.json";
        public static void TryVibration()
        {
            try
            {
                // Use default vibration length
                //Vibration.Vibrate();

                // Or use specified time
                var duration = TimeSpan.FromMilliseconds(10);
                Vibration.Vibrate(duration);
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
            }
            catch (Exception ex)
            {
                // Other error has occurred.
            }
        }
        public static int Test
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(Test), 3);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(Test), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(Test), 3);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(Test), value); }
            //#endif
        }

        public static bool ReviewAll
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ReviewAll), true);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ReviewAll), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ReviewAll), true);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ReviewAll), value); }
            //#endif
        }
        public static bool AutoSkip
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(AutoSkip), true);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(AutoSkip), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ReviewAll), true);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ReviewAll), value); }
            //#endif
        }
        public static bool SpeakNatural
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(SpeakNatural), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(SpeakNatural), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ReviewAll), true);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ReviewAll), value); }
            //#endif
        }
        public static bool ReportEvent
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ReportEvent), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ReportEvent), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ReviewAll), true);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ReviewAll), value); }
            //#endif
        }

        public static bool ShowExamples
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShowExamples), true);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShowExamples), value);
        }

        public static bool ShowOneExamples
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShowOneExamples), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShowOneExamples), value);
        }

        public static bool ShowWordRoot
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShowWordRoot), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShowWordRoot), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowExamples), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowExamples), value); }
            //#endif
        }

        public static bool ShowSynonyms
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShowSynonyms), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShowSynonyms), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowSynonyms), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowSynonyms), value); }
            //#endif
        }
        public static bool AudoPlayAudio
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(AudoPlayAudio), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(AudoPlayAudio), value);
        }
        public static bool AutoSync
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(AutoSync), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(AutoSync), value);
        }
        public static bool UsePointer
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(UsePointer), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(UsePointer), value);
        }
        //UseLightThemeButton
        public static bool UseLightThemeButton
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(UseLightThemeButton), true);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(UseLightThemeButton), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowAntonyms), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowAntonyms), value); }
            //#endif
        }
        public static bool ShowAntonyms
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShowAntonyms), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShowAntonyms), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowAntonyms), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowAntonyms), value); }
            //#endif
        }


        public static bool ShakeEnabled
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShakeEnabled), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShakeEnabled), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowAntonyms), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowAntonyms), value); }
            //#endif
        }

        public static bool ShakeLevelStrong
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(ShakeLevelStrong), false);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(ShakeLevelStrong), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(ShowAntonyms), false);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(ShowAntonyms), value); }
            //#endif
        }

        public static int Level
        {
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(Level), -1);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(Level), value);
        }


        public static string LastIndexOrBookId
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(LastIndexOrBookId), string.Empty);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(LastIndexOrBookId), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(LastIndex), 0);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(LastIndex), value); }
            //#endif
        }
        public static int LastLevelIndex
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(LastLevelIndex), 0);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(LastLevelIndex), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(LastIndex), 0);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(LastIndex), value); }
            //#endif
        }
        public class SString
        {
            public string Value { get; set; }
        }
        public static string ID
        {
            //#if WINDOWS_UWP || __WASM__
            get => new CompressedStorage(CompressedStorage.Storage.ID).TryGet<string>("IDS") ?? string.Empty;
            set => new CompressedStorage(CompressedStorage.Storage.ID).TrySet("IDS", value ?? string.Empty);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(LastIndex), 0);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(LastIndex), value); }
            //#endif
        }
        //public async static Task<string> GetIDsAsync()
        //{
        //    var ids = await new CompressedStorage(CompressedStorage.Storage.ID).TryGetAsync<SString>("IDS");
        //    return ids?.Value ?? string.Empty;
        //}
        //public async static Task SetIDsAsync(string value)
        //{
        //    await new CompressedStorage(CompressedStorage.Storage.ID).TrySetAsync<SString>("IDS", new SString { Value = value });
        //}
        public static string Email
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(Email), string.Empty);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(Email), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(Email), string.Empty);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(Email), value); }
            //#endif
        }
        public static string Password
        {
            //#if WINDOWS_UWP || __WASM__
            get => SettingService.Get(nameof(AppGlobalSettings) + nameof(Password), string.Empty);
            set => SettingService.Set(nameof(AppGlobalSettings) + nameof(Password), value);
            //#else
            //            get => Plugin.Settings.CrossSettings.Current.GetValueOrDefault(nameof(Password), string.Empty);
            //            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(nameof(Password), value); }
            //#endif
        }
        public static bool UseSound
        {
            get => SettingService.Get(nameof(UseSound), true);
            set
            {

                SettingService.Set<bool>(nameof(UseSound), value);
                //GenericRaisePropertyChanged(nameof(UseSound));
#if WINDOWS_UWP
                if (value)
                {
                    ElementSoundPlayer.State = ElementSoundPlayerState.On;
                }
                else
                {
                    ElementSoundPlayer.State = ElementSoundPlayerState.Auto;
                }
#endif
                //logger.Event(nameof(UseSound), value);

            }
        }

    }
    public class SettingService
    {
        //static LoggingService logger = new LoggingService();
        static SettingService()
        {
            //if (ApplicationData.Current.RoamingSettings.Containers.ContainsKey(SETTINGCONTAINERS.LASTPLAYBACKFILEPOSITION))
            //{
            //    ApplicationData.Current.RoamingSettings.DeleteContainer(SETTINGCONTAINERS.LASTPLAYBACKFILEPOSITION);
            //}
        }
        public static void Set<T>(string key, T value, bool useRoaming = false)
        {
            if (String.IsNullOrEmpty(key))
            {
                return;
            }
            try
            {
                ApplicationDataContainer localSettings = useRoaming ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
                localSettings.Values[key] = value;
            }
            catch (Exception)
            {
                //logger.Event("Set_Fail.", $"{ex.Message}_{containerName}_{key}_{value}_{useRoaming}");
            }

        }
        public static T Get<T>(string key, T defaultValue, bool useRoaming = false)
        {
            if (String.IsNullOrEmpty(key))
            {
                return defaultValue;
            }
            try
            {
                ApplicationDataContainer localSettings = useRoaming ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey(key))
                {
                    return (T)localSettings.Values[key];
                }
            }
            catch (Exception)
            {
                //logger.Event("Get_Fail.", $"{ex.Message}_{containerName}_{key}_{useRoaming}");
            }

            return defaultValue;
        }
    }
    public class CCommand : System.Windows.Input.ICommand
    {
        Action<object> _action;
        public CCommand(Action<object> action)
        {
            _action = action;
        }
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }
    }
    public class WorkBook
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Words { get; set; }
        [JsonIgnore]
        public int Count => Words?.Count ?? 0;
    }
    public class WorkBookService
    {
        public static async Task<List<WorkBook>> GetAllBooksAsync()
        {
            return await Task.Run(() =>
            {
                var result = new CompressedStorage(CompressedStorage.Storage.WordBook).TryGet<List<WorkBook>>(nameof(WorkBookService));
                if (!result.IsNullOrCountEqualsZero())
                {
                    return result;
                }
                return null;
            });
        }

        public static async Task SaveBooksAsync(WorkBook workBook)
        {
            if (String.IsNullOrEmpty(workBook?.Name))
            {
                return;
            }
            await Task.Run(() =>
            {
                List<WorkBook> workBooks = new List<WorkBook>();
                var result = new CompressedStorage(CompressedStorage.Storage.WordBook).TryGet<List<WorkBook>>(nameof(WorkBookService));
                if (!result.IsNullOrCountEqualsZero())
                {
                    var origin = result.FirstOrDefault(c => c.Name == workBook.Name);
                    if (origin != null)
                    {
                        result.Remove(origin);
                    }
                    if (!result.IsNullOrCountEqualsZero())
                    {
                        workBooks.AddRange(result);
                    }
                }
                workBooks.Add(workBook);
                new CompressedStorage(CompressedStorage.Storage.WordBook).TrySet<List<WorkBook>>(nameof(WorkBookService), workBooks);
            });
        }

        public static async Task DeleteBooksAsync(WorkBook workBook)
        {
            if (String.IsNullOrEmpty(workBook?.Name))
            {
                return;
            }
            await Task.Run(() =>
            {
                var result = new CompressedStorage(CompressedStorage.Storage.WordBook).TryGet<List<WorkBook>>(nameof(WorkBookService));
                if (!result.IsNullOrCountEqualsZero())
                {
                    var origin = result.FirstOrDefault(c => c.Name == workBook.Name);
                    if (origin != null)
                    {
                        result.Remove(origin);
                        new CompressedStorage(CompressedStorage.Storage.WordBook).TrySet<List<WorkBook>>(nameof(WorkBookService), result);
                    }
                }
            });

        }
    }
    public static class FlagService
    {

        public static async Task<bool?> LiveReportAsync(string message)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    //var res = await client.GetAsync($"https://search.scorpioplayer.com/api/google/hub?u=IELTSWord&m={message}").ConfigureAwait(false);
                    //if (res.IsSuccessStatusCode)
                    //{
                    //    return true;
                    //}

                    var res = await client.PostAsync($"https://search.scorpioplayer.com/api/google/hub?u=IELTSWord", new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Value = message }), Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    if (res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return false;
        }
        public static async Task<bool?> HasFlagAsync(string flag)
        {
            try
            {
                HttpClient client = new HttpClient();
                var res = await client.GetAsync($"{AppGlobalSettings.HasFlagApiUrl}?flag={flag}").ConfigureAwait(false);
                if (res.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return false;
        }
    }
    public class LoggingService
    {
        public void Event(string message, Exception exception = null, object additionJsonObj = null)
        {
            if (exception != null)
            {
                Task.Run(() => { bosxixi.ScorpioPlayer.Core.LogService.Event($"{"IELTSWord"}_FromLogger" + message + exception.Message, exception.StackTrace ?? string.Empty + Environment.NewLine + Environment.StackTrace); });
            }

            //if (AppGlobalSettings.InsiderFeatureEnabled)
            //{
            //    Debug.WriteLine(message);
            //    if (exception == null)
            //    {
            //        //TraceLogger.LogAsync($"{DateTime.Now.Millisecond} {message}");
            //        Views.MainView.Instance?.WriteMessage(message);
            //    }
            //    else
            //    {
            //        //TraceLogger.LogAsync($"{DateTime.Now.Millisecond} {message}_{exception.Message}");
            //        var mes = $"{message}_{exception.Message}";
            //        Views.MainView.Instance?.WriteMessage(mes);
            //    }

            //}
#if !DEBUG
#else
            try
            {
                //if (message == null)
                //{
                //    return;
                //}
                //if (exception == null && additionJsonObj == null)
                //{
                //    //Analytics.TrackEvent(message);
                //}
                //else if (exception != null && additionJsonObj != null)
                //{
                //    Analytics.TrackEvent(message, new Dictionary<string, string>
                //    {
                //        [nameof(exception)] = exception.Message,
                //        [nameof(exception.StackTrace)] = exception.StackTrace ?? string.Empty,
                //        [nameof(additionJsonObj)] = Newtonsoft.Json.JsonConvert.SerializeObject(additionJsonObj)
                //    });
                //}
                //else if (exception != null)
                //{
                //    Analytics.TrackEvent(message, new Dictionary<string, string>
                //    {
                //        [nameof(exception)] = exception.Message,
                //        [nameof(exception.StackTrace)] = exception.StackTrace ?? string.Empty
                //    });
                //}
                Debug.WriteLine(message);

                if (AppGlobalSettings.ReportEvent)
                {
                    FlagService.LiveReportAsync(message);
                }

                if (exception != null)
                {
                    FlagService.LiveReportAsync(exception.Message);
                    FlagService.LiveReportAsync(exception.StackTrace);
                }
            }
            catch (Exception)
            {
            }
#endif

        }

        public void Event(string message, object enumValue)
        {
            try
            {
                if (enumValue is string str)
                {
                    Event($"{message}_{str.PadRight(20, '0')}");
                }
                else
                {
                    if (enumValue is Enum)
                    {
                        Event($"{message}_{Enum.GetName(enumValue.GetType(), enumValue)}");
                    }
                    else
                    {
                        Event($"{message}_{enumValue.ToString()}");
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        public class AdditionJson
        {

        }
        public void Event(string message, bool value)
        {
            Event($"{message}_{value.ToString()}");
        }
        public void Event(string message, string value)
        {
            Event($"{message}_{value}");
        }
        public void Event(string message, string value, string value2)
        {
            Event($"{message}_{value}_{value2}");
        }
    }
    public static class Extension
    {
        public static string ToUnicode(this string literal)
        {
            return rx.Replace($"\\u{literal}", match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
        }
        static Regex rx = new Regex(@"\\[uU]([0-9A-F]{4})");
        static readonly ResourceLoader RL = new ResourceLoader();

        public static string Translate(this string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            try
            {
                var result = RL.GetString(key);
                return String.IsNullOrEmpty(result) ? key : result;
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }

    }
    public class BooleanToVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter?.ToString() == "r")
            {
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class PositiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int inte && inte == -1)
            {
                return string.Empty;
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class CountListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<DateTimeOffset> inte)
            {
                return inte.Count.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var bo = (bool)value;
            if (bo)
            {
                return new SolidColorBrush(Colors.White);
            }
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class NullToVisiblityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter?.ToString() == "x")
            {
                var tar = value?.ToString() ?? string.Empty;
                tar = tar.Trim();

                if (tar == string.Empty)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            if (parameter?.ToString() == "r")
            {
                if (value is string vs)
                {
                    return !String.IsNullOrEmpty(vs) ? Visibility.Collapsed : Visibility.Visible;
                }
                return value != null ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is string vs2)
            {
                return !String.IsNullOrEmpty(vs2) ? Visibility.Visible : Visibility.Visible;
            }
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class OneStringToVisiblityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter?.ToString() == "x")
            {
                var tar = value?.ToString() ?? string.Empty;
                tar = tar.Trim();

                var words = tar.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (!words.IsNullOrCountEqualsZero() && words.Length == 1)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
                if (tar == string.Empty)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            if (parameter?.ToString() == "r")
            {
                if (value is string vs)
                {
                    return !String.IsNullOrEmpty(vs) ? Visibility.Collapsed : Visibility.Visible;
                }
                return value != null ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is string vs2)
            {
                return !String.IsNullOrEmpty(vs2) ? Visibility.Visible : Visibility.Visible;
            }
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 202, 81, 0)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 122, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolReverveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class ExampleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (AppGlobalSettings.ShowOneExamples)
            {
                if (value is EXAMExample[] exam && exam.Length > 1)
                {
                    return new EXAMExample[] { exam.FirstOrDefault(), exam.LastOrDefault() };
                }
                else if (value is EXAMExample[] exam1 && exam1.Length > 0)
                {
                    return new EXAMExample[] { exam1.FirstOrDefault() };
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class EXAMRoot
    {
        public string normalizedSource { get; set; }
        public string normalizedTarget { get; set; }
        public EXAMExample[] examples { get; set; }
    }
    public class EXAMExample
    {
        public EXAMExample()
        {
            this.SpeechSource = new CCommand(c =>
            {
                AppGlobalSettings.TryVibration();
                var target = $"{sourcePrefix}{sourceTerm}{sourceSuffix}".Trim();
                MainPage.Instance.logger.Event(nameof(SpeechTarget), target);
                MainPage.Instance.SpeechNow(target);
            });
            this.SpeechTarget = new CCommand(c =>
            {
                AppGlobalSettings.TryVibration();
                var target = $"{targetPrefix}{targetTerm}{targetSuffix}".Trim();
                MainPage.Instance.logger.Event(nameof(SpeechTarget), target);
                MainPage.Instance.SpeechNow(target, "zh");
            });
        }
        public string sourcePrefix { get; set; }
        public string sourceTerm { get; set; }
        public string sourceSuffix { get; set; }
        public string targetPrefix { get; set; }
        public string targetTerm { get; set; }
        public string targetSuffix { get; set; }

        [JsonIgnore]
        public ICommand SpeechTarget { get; set; }
        [JsonIgnore]
        public ICommand SpeechSource { get; set; }
        //private ICommand _SpeechTarget;

        //public ICommand SpeechTarget
        //{
        //    get { return _SpeechTarget; }
        //    set { _SpeechTarget = value; GenericRaisePropertyChanged(nameof(SpeechTarget)); }
        //}

        //private ICommand _SpeechSource;

        //public ICommand SpeechSource
        //{
        //    get { return _SpeechSource; }
        //    set { _SpeechSource = value; GenericRaisePropertyChanged(nameof(SpeechSource)); }
        //}
    }

    public class TM
    {
        public class Root
        {
            public string normalizedSource { get; set; }
            public string displaySource { get; set; }
            public Translation[] translations { get; set; }
        }

        public class Translation
        {
            public string normalizedTarget { get; set; }
            public string displayTarget { get; set; }
            public string posTag { get; set; }
            public float confidence { get; set; }
            public string prefixWord { get; set; }
            public Backtranslation[] backTranslations { get; set; }
        }

        public class Backtranslation
        {
            public string normalizedText { get; set; }
            public string displayText { get; set; }
            public int numExamples { get; set; }
            public int frequencyCount { get; set; }
        }

    }
    public class WordRoot
    {
        public string antonyms { get; set; }
        public string synonyms { get; set; }
        public string @class { get; set; }
        public string[] example { get; set; }
        public string meaning { get; set; }
        public string root { get; set; }
        public string origin { get; set; }
    }
    public class WordDetails
    {
        public string Phonetic { get; set; }
        public WordRoot[] WordRoot { get; set; }
        public string Lemma { get; set; }
        public TM.Root LoopUp { get; set; }
        public List<EXAMRoot> Examples { get; set; }
        [JsonIgnore]
        public string WordRootStr
        {
            get
            {
                try
                {
                    if (WordRoot != null && !WordRoot.IsNullOrCountEqualsZero())
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var item in WordRoot)
                        {
                            sb.AppendLine($"{"root".Translate()}:{item.root}, {"meaning".Translate()}:{item.meaning}, {"GGclass".Translate()}:{item.@class}, {"synonyms".Translate()}:{item.synonyms}, {"antonyms".Translate()}:{item.antonyms}, {"origin".Translate()}:{item.origin}");
                            sb.Append("examples".Translate());
                            sb.AppendLine(string.Join(", ", item.example));
                            sb.AppendLine();
                        }

                        return sb.ToString();

                    }
                }
                catch (Exception)
                {

                    throw;
                }

                return string.Empty;
            }
        }
        [JsonIgnore]
        public string PhoneticStr
        {
            get
            {
                if (String.IsNullOrEmpty(Phonetic))
                {
                    return string.Empty;
                }
                return $"[ {Phonetic} ]";
            }
        }
        public string LoopUpStr
        {
            get
            {
                if (LoopUp != null && !LoopUp.translations.IsNullOrCountEqualsZero())
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in LoopUp.translations.GroupBy(c => c.posTag))
                    {
                        sb.AppendLine($"{item.Key.ToLowerInvariant()}. {string.Join(", ", item.Select(c => c.displayTarget))}");
                    }
                    return sb.ToString();
                }
                return string.Empty;
            }
        }
        public string ExamplesStr
        {
            get
            {
                if (Examples != null && !Examples.IsNullOrCountEqualsZero())
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in Examples)
                    {
                        sb.AppendLine($"{item.normalizedSource} {item.normalizedTarget}");

                        foreach (var item2 in item.examples)
                        {
                            sb.AppendLine($"{item2.sourcePrefix} {item2.sourceTerm} {item2.sourceSuffix}");
                            sb.AppendLine($"{item2.targetPrefix} {item2.targetTerm} {item2.targetSuffix}");
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                }
                return string.Empty;
            }
        }
    }
    public class CompressedStorage
    {
        private readonly Storage storage;

        public enum Storage
        {
            Detail,
            ID,
            WordBook,
            Word
        }
        public CompressedStorage(Storage storage)
        {
            this.storage = storage;
        }
        StorageFolder _folder;
        //public async Task EnsureFolderAsync()
        //{
        //    try
        //    {
        //        if (_folder != null)
        //        {
        //            return;
        //        }
        //        _folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(this.storage.ToString());
        //        if (_folder == null)
        //        {
        //            _folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(this.storage.ToString(), CreationCollisionOption.ReplaceExisting);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        _folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(this.storage.ToString(), CreationCollisionOption.ReplaceExisting);
        //    }

        //}
        //public async Task DeleteAsync()
        //{
        //    await EnsureFolderAsync();
        //    await this._folder.DeleteAsync();
        //}
        //public async Task<IEnumerable<T>> TryGetAllAsync<T>() where T : class, new()
        //{
        //    try
        //    {
        //        await EnsureFolderAsync();
        //        var files = await _folder.GetFilesAsync();
        //        List<T> ts = new List<T>();
        //        foreach (var item in files)
        //        {
        //            using (var stream = await item.OpenStreamForReadAsync())
        //            {
        //                var bytes = stream.ReadFully();
        //                var compressed = Encoding.UTF8.GetString(bytes);
        //                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(compressed);
        //                ts.Add(result);
        //            }
        //        }
        //        return ts;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}
        public T TryGet<T>(string id) where T : class
        {
            var key = $"{this.storage}_{id}";
            if (Barrel.Current.Exists(key))
            {
                return Barrel.Current.Get<T>(key);
            }
            return null;
        }

        public void TrySet<T>(string id, T value) where T : class
        {
            var key = $"{this.storage}_{id}";
            if (Barrel.Current.Exists(key))
            {
                Barrel.Current.Empty(key);
            }
            Barrel.Current.Add<T>(key, value, TimeSpan.MaxValue);
        }
        //public async Task<T> TryGetAsync<T>(string id) where T : class
        //{
        //    string compressed = "failed";
        //    try
        //    {
        //        var key = $"{this.storage}_{id}";
        //        if (Barrel.Current.Exists(key))
        //        {
        //            return Barrel.Current.Get<T>(key);
        //        }

        //        //await EnsureFolderAsync();
        //        //using (var stream = await (await _folder.GetFileAsync(id)).OpenStreamForReadAsync())
        //        //{
        //        //    var bytes = stream.ReadFully();
        //        //    compressed = Encoding.UTF8.GetString(bytes);
        //        //    try
        //        //    {
        //        //        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(compressed);
        //        //        return result;
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        await DeleteAsync();
        //        //        new LoggingService().Event(id, compressed);
        //        //        new LoggingService().Event(nameof(TryGetAsync), ex);
        //        //        new LoggingService().Event(typeof(T).FullName, id);
        //        //    }
        //        //}

        //    }
        //    catch (Exception ex)
        //    {
        //        new LoggingService().Event(id, compressed);
        //        new LoggingService().Event(nameof(TryGetAsync), ex);
        //        new LoggingService().Event(typeof(T).FullName, id);
        //    }
        //    return null;
        //}
        //public async Task TrySetAsync<T>(string id, T value) where T : class
        //{
        //    try
        //    {
        //        var key = $"{this.storage}_{id}";
        //        if (Barrel.Current.Exists(key))
        //        {
        //            Barrel.Current.Empty(key);
        //        }
        //        Barrel.Current.Add<T>(key, value, TimeSpan.MaxValue);
        //        //await EnsureFolderAsync();
        //        //var val = Newtonsoft.Json.JsonConvert.SerializeObject(value);

        //        //Newtonsoft.Json.JsonConvert.DeserializeObject<T>(val);

        //        //var file = await _folder.CreateFileAsync(id, CreationCollisionOption.ReplaceExisting);
        //        //using (var stream = await file.OpenStreamForWriteAsync())
        //        //{
        //        //    var ut = Encoding.UTF8.GetBytes(val);
        //        //    stream.Write(ut, 0, ut.Length);
        //        //    stream.Flush();
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        new LoggingService().Event(nameof(TrySetAsync), ex);
        //        return;
        //    }
        //}
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        const string SPEECH_KEY = "520d0d28080b463aa5a399152eba3212";
        public LoggingService logger = new LoggingService();
        public static MainPage Instance;
        public MainPage()
        {
            Instance = this;
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            Accelerometer.ShakeDetected += Accelerometer_ShakeDetected;
        }

        void Accelerometer_ShakeDetected(object sender, EventArgs e)
        {
            logger.Event(nameof(Accelerometer_ShakeDetected));
            // Process shake event
            if (!AppGlobalSettings.ShakeEnabled)
            {
                return;
            }
            AppGlobalSettings.TryVibration();
            if (this.CurrentWordDetails != null)
            {
                Yes_Click(null, null);
            }
            else
            {
                Blur_Click(null, null);
            }
        }

        public void ToggleAccelerometer()
        {
            try
            {
                logger.Event(nameof(ToggleAccelerometer));
                if (Accelerometer.IsMonitoring)
                {
                    logger.Event(nameof(ToggleAccelerometer), 1);
                    Accelerometer.Stop();
                }
                else
                {
                    logger.Event(nameof(ToggleAccelerometer), 2);
                    Accelerometer.Start(AppGlobalSettings.ShakeLevelStrong ? SensorSpeed.Default : SensorSpeed.UI);

                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                logger.Event(nameof(ToggleAccelerometer), 3);
                // Feature not supported on device
            }
            catch (Exception ex)
            {
                logger.Event(nameof(ToggleAccelerometer), 4);
                // Other error has occurred.
            }
        }
        public string Email
        {
            get => AppGlobalSettings.Email;
            set
            {
                AppGlobalSettings.Email = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Email)));
            }
        }
        public int Level
        {
            get => AppGlobalSettings.Level;
            set
            {
                AppGlobalSettings.Level = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
            }
        }
        public bool ReviewAll
        {
            get => AppGlobalSettings.ReviewAll;
            set
            {
                AppGlobalSettings.ReviewAll = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReviewAll)));
            }
        }
        public bool AutoSkip
        {
            get => AppGlobalSettings.AutoSkip;
            set
            {
                AppGlobalSettings.AutoSkip = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoSkip)));
            }
        }
        public bool SpeakNatural
        {
            get => AppGlobalSettings.SpeakNatural;
            set
            {
                AppGlobalSettings.SpeakNatural = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakNatural)));
            }
        }

        public bool ShowExamples
        {
            get => AppGlobalSettings.ShowExamples;
            set
            {
                AppGlobalSettings.ShowExamples = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowExamples)));
            }
        }
        public bool ShowOneExamples
        {
            get => AppGlobalSettings.ShowOneExamples;
            set
            {
                AppGlobalSettings.ShowOneExamples = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowOneExamples)));
            }
        }
        public bool ShowWordRoot
        {
            get => AppGlobalSettings.ShowWordRoot;
            set
            {
                AppGlobalSettings.ShowWordRoot = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowWordRoot)));
            }
        }
        public bool ShowSynonyms
        {
            get => AppGlobalSettings.ShowSynonyms;
            set
            {
                AppGlobalSettings.ShowSynonyms = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSynonyms)));
            }
        }
        public bool AudoPlayAudio
        {
            get => AppGlobalSettings.AudoPlayAudio;
            set
            {
                AppGlobalSettings.AudoPlayAudio = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudoPlayAudio)));
            }
        }
        public bool UseSound
        {
            get => AppGlobalSettings.UseSound;
            set
            {
                AppGlobalSettings.UseSound = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseSound)));
            }
        }
        public bool AutoSync
        {
            get => AppGlobalSettings.AutoSync;
            set
            {
                AppGlobalSettings.AutoSync = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoSync)));
            }
        }
        public bool UsePointer
        {
            get => AppGlobalSettings.UsePointer;
            set
            {
                AppGlobalSettings.UsePointer = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsePointer)));
            }
        }
        //UseLightThemeButton
        public bool UseLightThemeButton
        {
            get => AppGlobalSettings.UseLightThemeButton;
            set
            {
                AppGlobalSettings.UseLightThemeButton = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseLightThemeButton)));
            }
        }
        public bool ShowAntonyms
        {
            get => AppGlobalSettings.ShowAntonyms;
            set
            {
                AppGlobalSettings.ShowAntonyms = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAntonyms)));
            }
        }
        public bool ShakeLevelStrong
        {
            get => AppGlobalSettings.ShakeLevelStrong;
            set
            {
                AppGlobalSettings.ShakeLevelStrong = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShakeLevelStrong)));
            }
        }
        public bool ShakeEnabled
        {
            get => AppGlobalSettings.ShakeEnabled;
            set
            {
                AppGlobalSettings.ShakeEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShakeEnabled)));
            }
        }

        private bool _downloading;

        public bool IsDownloading
        {
            get { return _downloading; }
            set { _downloading = value; GenericRaisePropertyChanged(nameof(IsDownloading)); }
        }

        private WordDetails wordDetails;

        public WordDetails CurrentWordDetails
        {
            get { return wordDetails; }
            set { wordDetails = value; GenericRaisePropertyChanged(nameof(CurrentWordDetails)); }
        }

        Windows.Media.Playback.MediaPlayer _media;
        void Speaker_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentWord?.Name is string str)
            {
                if (sender != null)
                {


                    AppGlobalSettings.TryVibration();
                }
                SpeechNow(str);
            }
        }

        //async void examplesListView_ItemClick(object sender, ItemClickEventArgs e)
        //{
        //    logger.Event(nameof(examplesListView_ItemClick));
        //    if (e.ClickedItem is EXAMExample examp)
        //    {
        //        var target = $"{examp.sourcePrefix}{examp.sourceTerm}{examp.sourceSuffix}".Trim();
        //        logger.Event(nameof(examplesListView_ItemClick), target);
        //        SpeechNow(target);
        //    }
        //}

        public void SpeechNow(string str, string lang = "en")
        {
            logger.Event(nameof(SpeechNow));
            Task.Run(() =>
            {
                var uri = $"https://search.scorpioplayer.com/api/google/ToSpeech?word={Uri.EscapeDataString(str)}&lang={lang}";
                if (AppGlobalSettings.SpeakNatural)
                {
#if WINDOWS_UWP
                    if (_media == null)
                    {
                        _media = new Windows.Media.Playback.MediaPlayer();
                        try
                        {
                            _media.CommandManager.IsEnabled = false;
                            _media.SystemMediaTransportControls.IsEnabled = false;
                        }
                        catch (Exception)
                        {

                        }

                    }
                    _media.Source = MediaSource.CreateFromUri(new Uri(uri));
                    _media.AutoPlay = true;
#else
                    logger.Event(uri);
                    try
                    {
                        MediaManager.CrossMediaManager.Current.Play(uri);
                    }
                    catch (Exception)
                    {
                        try
                        {
#if __DROID__
                    IELTSWord.Droid.MainActivity.Instance.PlayAudio(uri);
#endif
                        }
                        catch (global::System.Exception ex)
                        {
                            var message = ex.Message;
                        }
                    }

#endif
                }
                else
                {
                    CancelSpeech();
                    SpeakNowDefaultSettings(str);
                }
            });
        }
        //AutoResetEvent _lockerChose = new AutoResetEvent(true);
        async void Yes_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(Yes_Click));
            try
            {
                //_lockerChose.WaitOne();
                if (this.Words != null && this.CurrentWord != null)
                {
                    this.CurrentWord.Yes();

                    if (this.CurrentWordDetails == null)
                    {
                        if (AppGlobalSettings.AudoPlayAudio)
                        {
                            SpeechNow(this.CurrentWord?.Name ?? string.Empty);
                        }
                        await UpdateDetailAsync(this.CurrentWord);
                        if (AppGlobalSettings.AutoSkip)
                        {
                            ThreadPoolTimer.CreateTimer(_ =>
                            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            this.GenericDispatherActionAsync(async () =>
                                {
                                    await GetNextAsync();

                                    UpdateStatistics();
                                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        }, TimeSpan.FromSeconds(5));
                        }
                    }
                    else
                    {
                        await GetNextAsync();

                        UpdateStatistics();
                    }
                }
                else
                {
                    await FirstTimeLoadBookAsync();
                }
            }
            finally
            {
                //_lockerChose.Set();
            }

        }

        private async Task FirstTimeLoadBookAsync()
        {
            MessageDialog messageDialog = new MessageDialog("LoadBookContent".Translate(), "LoadBookTitle".Translate());
            await messageDialog.ShowAsync();
            this.pivot.SelectedIndex = 2;
        }

        async void Blur_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(Blur_Click));
            try
            {
                //_lockerChose.WaitOne();
                if (this.Words != null && this.CurrentWord != null)
                {
                    this.CurrentWord.Blur();
                    await UpdateDetailAsync(this.CurrentWord);
                    UpdateStatistics();
                }
                else
                {
                    await FirstTimeLoadBookAsync();
                }
            }
            finally
            {
                //_lockerChose.Set();
            }
        }

        private async Task UpdateDetailAsync(Word word)
        {
            if (this.CurrentWordDetails == null)
            {
                this.CurrentWordDetails = await GetDetailsAsync(word);
            }
        }

        private async Task<WordDetails> GetDetailsAsync(Word word)
        {
            try
            {
                this.IsDownloading = true;
                var ds = new CompressedStorage(CompressedStorage.Storage.Detail).TryGet<WordDetails>(word.Name);
                if (ds == null)
                {
                    HttpClient client = new HttpClient();
                    var post = await client.GetStringAsync($"https://search.scorpioplayer.com/api/google/worddetail?word={Uri.EscapeDataString(this.CurrentWord.Name)}");
                    WordDetails item = SaveWordDetail(word.Name, post);
                    if (!String.IsNullOrEmpty(item.LoopUpStr))
                    {
                        word.Elaborate = item.LoopUpStr;
                        word.Save();
                        word.Raise();
                    }
                    return item;
                }
                else
                {
                    if (word.Elaborate is null && !String.IsNullOrEmpty(ds.LoopUpStr))
                    {
                        word.Elaborate = ds.LoopUpStr;
                        word.Save();
                        word.Raise();
                    }
                    //ds.Phonetic += ".Cache";
                    return ds;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                this.IsDownloading = false;
            }
            return null;
        }
        private async void scorpioplayer_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(scorpioplayer_Click));

#if WINDOWS_UWP
            await Launcher.OpenAsync(new Uri("ms-windows-store://pdp/?ProductId=9npkq7srlv1l&cid=IELTSWord"));
#else
            await Launcher.OpenAsync(new Uri("https://www.microsoft.com/store/apps/9NPKQ7SRLV1L"));
#endif
        }
        private static WordDetails SaveWordDetail(string word, string post)
        {
            var item = JsonConvert.DeserializeObject<WordDetails>(post);
            new CompressedStorage(CompressedStorage.Storage.Detail).TrySet(word, item);
            return item;
        }

        public string CustomTextUri { get; set; }
        public string CustomText { get; set; }
        //

        async void DOWNLOAD_SUBTITLE_CLICK(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (subhubResultListView.SelectedItem is SubtitleSelection ss)
                {
                    HttpClient client = new HttpClient();
                    var post = await client.PostAsync("https://search.scorpioplayer.com/api/Subtitle/DownloadLines",
                        new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(ss), Encoding.UTF8, "application/json"));
                    if (post.IsSuccessStatusCode && await post.Content.ReadAsStringAsync() is string res)
                    {
                        CustomText = res;
                        pivot_word.SelectedIndex = 1;
                        this.GenericRaisePropertyChanged(nameof(CustomText));
                        subhubResultListView.SelectedItem = null;
                    }
                    else
                    {
                        subhubResultListView.SelectedItem = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }

        }
        async void SEARCH_SUBTITLE_CLICK(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(SEARCH_SUBTITLE_CLICK));
            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(SubtitleQuery))
                {
                    HttpClient client = new HttpClient();
                    var post = await client.GetStringAsync($"https://search.scorpioplayer.com/api/subtitle/Search?q={Uri.EscapeDataString(SubtitleQuery)}");
                    var selections = Newtonsoft.Json.JsonConvert.DeserializeObject<SubtitleSelection[]>(post);
                    subhubResultListView.ItemsSource = selections;
                }
                else
                {
                    subhubResultListView.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
                subhubResultListView.ItemsSource = null;
            }
            finally
            {
                this.IsDownloading = false;
            }

        }
        async void DOWNLOAD_CUSTOM_TEXT(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(CustomTextUri) && Uri.TryCreate(CustomTextUri, UriKind.Absolute, out Uri _))
                {
                    HttpClient client = new HttpClient();
                    var post = await client.GetStringAsync($"https://search.scorpioplayer.com/api/google/DownloadText?uri={Uri.EscapeDataString(CustomTextUri)}");
                    CustomText = post;
                    this.GenericRaisePropertyChanged(nameof(CustomText));
                }
                else
                {
                    MessageDialog messageDialog = new MessageDialog("URI_IS_EMPTY".Translate(), "Error_Title".Translate());
                    await messageDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }

        }
        public string NewWordBookName { get; set; }
        async void ADD_OR_CREATE_WORDBOOK(object sender, RoutedEventArgs e)
        {

            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(CustomText) && !String.IsNullOrEmpty(NewWordBookName))
                {
                    await ExtractCustomTextAsync();
                    var words = CustomText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!words.IsNullOrCountEqualsZero())
                    {
                        var books = await WorkBookService.GetAllBooksAsync();
                        if (books?.FirstOrDefault(c => c.Name == NewWordBookName) is WorkBook wb)
                        {
                            wb.Words.AddRange(words);
                            wb.Words = wb.Words.Distinct().ToList();
                            await WorkBookService.SaveBooksAsync(wb);
                        }
                        else
                        {
                            await WorkBookService.SaveBooksAsync(new WorkBook
                            {
                                Id = $"WB{NewWordBookName.GetStableHashCode()}",
                                Name = NewWordBookName,
                                Words = words.Distinct().ToList()
                            });
                        }

                        await UpdateMyBooksAsync();
                        pivot_word.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        async void SPEECH_CUSTOM_TEXT(object sender, RoutedEventArgs e)
        {

            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(CustomText))
                {
                    MenuFlyout mf = new MenuFlyout();
                    mf.Items.Add(new MenuFlyoutItem()
                    {
                        Text = "中文".Translate(),
                        Command = new CCommand((c) =>
                        {
                            SpeechNow(CustomText, "zh");
                            //Launcher.OpenAsync($"https://cn.bing.com/dict/search?q={Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}&qs=n&form=Z9LH5&sp=-1&pq=hello&sc=7-5&sk=&cvid=DA527C397FB74913A4837D4E3C5DCA3E");
                        })
                    });
                    mf.Items.Add(new MenuFlyoutItem()
                    {
                        Text = "English".Translate(),
                        Command = new CCommand((c) =>
                        {
                            SpeechNow(CustomText);
                            //Launcher.OpenAsync($"https://dictionary.cambridge.org/dictionary/english-chinese-simplified/{Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                        })
                    });

                    mf.ShowAt(More_Button);
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        async void ENTER_CUSTOM_TEXT(object sender, RoutedEventArgs e)
        {

            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(CustomText))
                {
                    await ExtractCustomTextAsync();
                    var words = CustomText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!words.IsNullOrCountEqualsZero() && words.Length == 1)
                    {
                        var w = Word.Load(words.First());
                        if (w == null)
                        {
                            w = Word.Create(words.First());
                        }
                        w.Order = t20000.IndexOf(this.CurrentWord?.Name ?? string.Empty);
                        this.CurrentWord = w;
                        this.CurrentWordDetails = null;
                        GetDetailsAsync(w);
                        pivot.SelectedIndex = 0;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentWord)));
                        if (AppGlobalSettings.AudoPlayAudio)
                        {
                            Speaker_Click(null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        CancellationTokenSource cts;
        public async Task SpeakNowDefaultSettings(string word)
        {
            cts = new CancellationTokenSource();
            await TextToSpeech.SpeakAsync(word, cancelToken: cts.Token);

            // This method will block until utterance finishes.
        }

        public void CancelSpeech()
        {
            if (cts?.IsCancellationRequested ?? false)
                return;

            cts?.Cancel();
        }
        async void EXTRACT_CUSTOM_TEXT(object sender, RoutedEventArgs e)
        {
            await ExtractCustomTextAsync();

        }

        private async Task ExtractCustomTextAsync()
        {
            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(CustomText))
                {
                    HttpClient client = new HttpClient();
                    var post = await client.PostAsync("https://search.scorpioplayer.com/api/google/ExtractText",
                        new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            Value = CustomText
                        }), Encoding.UTF8, "application/json"));
                    if (post.IsSuccessStatusCode && await post.Content.ReadAsStringAsync() is string res)
                    {
                        CustomText = res;
                    }
                    this.GenericRaisePropertyChanged(nameof(CustomText));
                }
                else
                {
                    MessageDialog messageDialog = new MessageDialog("CustomText_IS_EMPTY".Translate(), "Error_Title".Translate());
                    await messageDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        static string DeCompress(string text)
        {
            var bytess = Convert.FromBase64String(text);
            var nmsout = new MemoryStream();
            ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(bytess.ToMemoryStream(), nmsout, true);
            var outt = nmsout.ToArray();
            var outttt = Encoding.UTF8.GetString(outt);
            return outttt;
        }
        class wd
        {
            public string d { get; set; }
            public string w { get; set; }
        }
        //
        async void BACKUPTOEMAIL_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(AppGlobalSettings.Email))
                {
                    var ws = Word.GetAll();

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                           WordItems
                    { Id = AppGlobalSettings.ID, Items = ws.ToList(), Books = await WorkBookService.GetAllBooksAsync() });

                    var josnobj = JObject.Parse(json);
                    var target = Newtonsoft.Json.JsonConvert.SerializeObject(new { To = AppGlobalSettings.Email, Obj = josnobj });
                    var post = await new HttpClient().PostAsync("https://search.scorpioplayer.com/api/google/mail", new StringContent(target, Encoding.UTF8, "application/json"));
                    if (post.IsSuccessStatusCode)
                    {
                        MessageDialog messageDialog = new MessageDialog($"{"UPLOAD_SUCCESS_DETAIL".Translate()}: {ws.Count()}", "UPLOAD_SUCCESS".Translate());
                        await messageDialog.ShowAsync();
                    }
                }
                else
                {
                    if (sender != null)
                    {
                        MessageDialog messageDialog = new MessageDialog("REQUERED_MORE_INFO_DETAIL".Translate(), "REQUERED_MORE_INFO".Translate());
                        await messageDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                if (sender != null)
                {
                    MessageDialog messageDialog = new MessageDialog(ex.Message, "Error");
                    await messageDialog.ShowAsync();
                }
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        async void PRELOAD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (!this.Words.IsNullOrCountEqualsZero())
                {
                    var target = this.Words.Where(c => new CompressedStorage(CompressedStorage.Storage.Detail).TryGet<WordDetails>(c.Name) == null).Select(c => c.Name).ToArray();
                    HttpClient client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(2);
                    var post = await client.PostAsync("https://search.scorpioplayer.com/api/google/allp",
                      new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new
                      {
                          Id = _id,
                          Value = Newtonsoft.Json.JsonConvert.SerializeObject(target)
                      }), Encoding.UTF8, "application/json"));
                    if (post.IsSuccessStatusCode)
                    {
                        var body = await post.Content.ReadAsStringAsync();
                        var item = JsonConvert.DeserializeAnonymousType(body, new { count = 0, words = string.Empty });
                        var words = DeCompress(item.words);
                        var wordss = JsonConvert.DeserializeObject<wd[]>(words);
                        foreach (var w in wordss)
                        {
                            SaveWordDetail(w.w, w.d);
                        }
                        MessageDialog messageDialog1 = new MessageDialog($"{"DOWNLOAD_SUCCESS_DETAIL".Translate()}: {wordss.Length}", "DOWNLOAD_SUCCESS".Translate());
                        await messageDialog1.ShowAsync();
                    }
                }
                else
                {
                    MessageDialog messageDialog = new MessageDialog("REQUERED_MORE_INFO_DETAIL".Translate(), "REQUERED_MORE_INFO".Translate());
                    await messageDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                if (sender != null)
                {
                    MessageDialog messageDialog = new MessageDialog(ex.Message, "Error");
                    await messageDialog.ShowAsync();
                }
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        async void UPLOADS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (!String.IsNullOrEmpty(AppGlobalSettings.Email) && !String.IsNullOrEmpty(AppGlobalSettings.Password))
                {
                    var ws = Word.GetAll();
                    HttpClient client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(2);
                    var post = await client.PostAsync("https://search.scorpioplayer.com/api/google/AddUpdateKeyValue",
                        new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            Id = _id,
                            Value = Newtonsoft.Json.JsonConvert.SerializeObject(new
                          WordItems
                            { Id = AppGlobalSettings.ID, Items = ws.ToList(), Books = await WorkBookService.GetAllBooksAsync() })
                        }), Encoding.UTF8, "application/json"));
                    if (post.IsSuccessStatusCode)
                    {
                        var body = await post.Content.ReadAsStringAsync();
                        var item = JsonConvert.DeserializeObject<KeyValue>(body);
                        if (item.Id == _id)
                        {
                            var items = JsonConvert.DeserializeObject<WordItems>(item.Value);
                            if (items.Id != null && items.Items != null)
                            {
                                AppGlobalSettings.ID = items.Id;
                                foreach (var word in items.Items)
                                {
                                    word.Save();
                                }

                                if (items.Books != null)
                                {
                                    foreach (var book in items.Books)
                                    {
                                        await WorkBookService.SaveBooksAsync(book);
                                    }
                                    await UpdateMyBooksAsync();
                                }
                                UpdateStatistics(hardmode: true);
                                if (sender != null)
                                {
                                    MessageDialog messageDialog1 = new MessageDialog($"{"DOWNLOAD_SUCCESS_DETAIL".Translate()}: {items.Items.Count}", "DOWNLOAD_SUCCESS".Translate());
                                    await messageDialog1.ShowAsync();
                                }
                                return;
                            }
                            else
                            {
                                if (sender != null)
                                {
                                    MessageDialog messageDialog2 = new MessageDialog("data crupt", "Error");
                                    await messageDialog2.ShowAsync();
                                }
                            }
                        }
                        if (sender != null)
                        {
                            MessageDialog messageDialog = new MessageDialog($"{"UPLOAD_SUCCESS_DETAIL".Translate()}: {ws.Count()}", "UPLOAD_SUCCESS".Translate());
                            await messageDialog.ShowAsync();
                        }
                    }
                }
                else
                {
                    if (sender != null)
                    {
                        MessageDialog messageDialog = new MessageDialog("REQUERED_MORE_INFO_DETAIL".Translate(), "REQUERED_MORE_INFO".Translate());
                        await messageDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                if (sender != null)
                {
                    MessageDialog messageDialog = new MessageDialog(ex.Message, "Error");
                    await messageDialog.ShowAsync();
                }
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            AppGlobalSettings.Password = passwordBox.Password;
        }
        public class KeyValue
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        public class WordItems
        {
            public string Id { get; set; }
            public List<Word> Items { get; set; }
            public List<WorkBook> Books { get; set; }
        }
        string _id => $"{AppGlobalSettings.Email}_{AppGlobalSettings.Password}";
        void Test_Click(object sender, RoutedEventArgs e)
        {
            UpdateToggle();
        }
        void UpdateToggle()
        {
            try
            {
                //logger.Event(nameof(UpdateToggle), "1");
                ReviewAllToggleSwitch.IsOn = !ReviewAllToggleSwitch.IsOn;
                ReviewAllToggleSwitch.IsOn = !ReviewAllToggleSwitch.IsOn;

                AutoSkipToggleSwitch.IsOn = !AutoSkipToggleSwitch.IsOn;
                AutoSkipToggleSwitch.IsOn = !AutoSkipToggleSwitch.IsOn;


                SpeakNaturalToggleSwitch.IsOn = !SpeakNaturalToggleSwitch.IsOn;
                SpeakNaturalToggleSwitch.IsOn = !SpeakNaturalToggleSwitch.IsOn;

                ShowExamplesToggleSwitch.IsOn = !ShowExamplesToggleSwitch.IsOn;
                ShowExamplesToggleSwitch.IsOn = !ShowExamplesToggleSwitch.IsOn;

                ShowOneExamplesToggleSwitch.IsOn = !ShowOneExamplesToggleSwitch.IsOn;
                ShowOneExamplesToggleSwitch.IsOn = !ShowOneExamplesToggleSwitch.IsOn;

                ShowWordRootToggleSwitch.IsOn = !ShowWordRootToggleSwitch.IsOn;
                ShowWordRootToggleSwitch.IsOn = !ShowWordRootToggleSwitch.IsOn;

                ShowSynonymsToggleSwitch.IsOn = !ShowSynonymsToggleSwitch.IsOn;
                ShowSynonymsToggleSwitch.IsOn = !ShowSynonymsToggleSwitch.IsOn;

                ShowAntonymsToggleSwitch.IsOn = !ShowAntonymsToggleSwitch.IsOn;
                ShowAntonymsToggleSwitch.IsOn = !ShowAntonymsToggleSwitch.IsOn;

                AudoPlayAudioToggleSwitch.IsOn = !AudoPlayAudioToggleSwitch.IsOn;
                AudoPlayAudioToggleSwitch.IsOn = !AudoPlayAudioToggleSwitch.IsOn;

                AutoSyncToggleSwitch.IsOn = !AutoSyncToggleSwitch.IsOn;
                AutoSyncToggleSwitch.IsOn = !AutoSyncToggleSwitch.IsOn;

                UsePointerToggleSwitch.IsOn = !UsePointerToggleSwitch.IsOn;
                UsePointerToggleSwitch.IsOn = !UsePointerToggleSwitch.IsOn;

                UseSoundToggleSwitch.IsOn = !UseSoundToggleSwitch.IsOn;
                UseSoundToggleSwitch.IsOn = !UseSoundToggleSwitch.IsOn;

                AudoPlayAudioToggleSwitch.IsOn = !AudoPlayAudioToggleSwitch.IsOn;
                AudoPlayAudioToggleSwitch.IsOn = !AudoPlayAudioToggleSwitch.IsOn;

                UseLightThemeButtonToggleSwitch.IsOn = !UseLightThemeButtonToggleSwitch.IsOn;
                UseLightThemeButtonToggleSwitch.IsOn = !UseLightThemeButtonToggleSwitch.IsOn;

                ShakeEnabledToggleSwitch.IsOn = !ShakeEnabledToggleSwitch.IsOn;
                ShakeEnabledToggleSwitch.IsOn = !ShakeEnabledToggleSwitch.IsOn;

                ShakeLevelStrongToggleSwitch.IsOn = !ShakeLevelStrongToggleSwitch.IsOn;
                ShakeLevelStrongToggleSwitch.IsOn = !ShakeLevelStrongToggleSwitch.IsOn;

                //logger.Event(nameof(UpdateToggle), "2");
            }
            catch (Exception)
            {
                logger.Event(nameof(UpdateToggle), "3");
            }
        }
        //async void DOWNLOAD_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        this.IsDownloading = true;
        //        if (!String.IsNullOrEmpty(AppGlobalSettings.Email) && !String.IsNullOrEmpty(AppGlobalSettings.Password))
        //        {
        //            try
        //            {
        //                HttpClient client = new HttpClient();
        //                var post = await client.GetStringAsync($"https://search.scorpioplayer.com/api/google/GetKeyValue?key={Uri.EscapeDataString(_id)}");
        //                var item = JsonConvert.DeserializeObject<KeyValue>(post);
        //                if (item.Id == _id)
        //                {
        //                    var items = JsonConvert.DeserializeObject<WordItems>(item.Value);
        //                    if (items.Id != null && items.Items != null)
        //                    {
        //                        AppGlobalSettings.ID = items.Id;
        //                        foreach (var word in items.Items)
        //                        {
        //                            word.Save();
        //                        }

        //                        if (items.Books != null)
        //                        {
        //                            foreach (var book in items.Books)
        //                            {
        //                                await WorkBookService.SaveBooksAsync(book);
        //                            }
        //                            await UpdateMyBooksAsync();
        //                        }
        //                        UpdateStatistics();
        //                        MessageDialog messageDialog = new MessageDialog($"{"DOWNLOAD_SUCCESS_DETAIL".Translate()}: {items.Items.Count}", "DOWNLOAD_SUCCESS".Translate());
        //                        await messageDialog.ShowAsync();
        //                        return;
        //                    }
        //                    else
        //                    {
        //                        MessageDialog messageDialog = new MessageDialog("data crupt", "Error");
        //                        await messageDialog.ShowAsync();
        //                    }
        //                }
        //            }
        //            catch (Exception)
        //            {
        //                MessageDialog messageDialog = new MessageDialog("ERROR_MORE_INFO_DETAIL".Translate(), "ERROR_MORE_INFO".Translate());
        //                await messageDialog.ShowAsync();
        //            }

        //        }
        //        else
        //        {
        //            MessageDialog messageDialog = new MessageDialog("please set up email to sync", "Email");
        //            await messageDialog.ShowAsync();
        //        }
        //    }
        //    finally
        //    {
        //        this.IsDownloading = false;
        //    }

        //}
        async void No_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //_lockerChose.WaitOne();
                if (this.Words != null && this.CurrentWord != null)
                {
                    this.CurrentWord.No();
                    if (this.CurrentWordDetails == null)
                    {
                        await UpdateDetailAsync(this.CurrentWord);
                    }
                    else
                    {
                        await GetNextAsync();
                    }

                    UpdateStatistics();
                }
                else
                {
                    await FirstTimeLoadBookAsync();
                }
            }
            finally
            {
                //_lockerChose.Set();
            }
        }

        private void Pivot_Changed(object sender, SelectionChangedEventArgs e)
        {
            //PackageVersion.Text += ".";
            //GenericRaisePropertyChanged(nameof(AudoPlayAudio));
            //var all = Word.GetAll();
            //if (all != null)
            //{
            //    all = all.OrderByDescending(c => c.HitDate).ToList();
            //    TotalWords.Text = all.Count.ToString();
            //    WordsListView.DisplayMemberPath = nameof(Word.Name);
            //    WordsListView.ItemsSource = all;
            //}
            //else
            //{
            //    WordsListView.ItemsSource = null;
            //}
        }
        async private void Complete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Event(nameof(Complete_Click));
                //_lockerChose.WaitOne();
                if (this.Words != null && this.CurrentWord != null)
                {
                    logger.Event(nameof(Complete_Click), 1);
                    this.CurrentWord.Complete();
                    logger.Event(nameof(Complete_Click), 2);
                    await GetNextAsync();
                    UpdateStatistics();
                }
                else
                {
                    await FirstTimeLoadBookAsync();
                }
            }
            finally
            {
                //_lockerChose.Set();
            }
        }
        private async void scorpioplayercom_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(scorpioplayercom_Click));
            await Launcher.OpenAsync(new Uri("http://scorpioplayer.com"));
        }

        private async void supportscorpioplayercom_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(supportscorpioplayercom_Click));
            await Launcher.OpenAsync(new Uri("mailto:support@scorpioplayer.com"));
        }
        private async void scorpioplayerweibo_Click(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(scorpioplayerweibo_Click));
            await Launcher.OpenAsync(new Uri("https://www.weibo.com/u/6597236243?is_hot=1"));

        }
        public string SubtitleQuery { get; set; }
        async void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Event(nameof(Load_Click));

                if (wordBooksListView.SelectedItem != null)
                {
                    LOAD_SUBTITLE_BOOK_CLICK(sender, e);
                }
                else if (DicsCombo.SelectedItem is KeyValuePair<string, string> item)
                {
                    logger.Event(nameof(Load_Click), $"{item.Key}");

                    //return;
                    var index = Dics.Select(c => c.Key).ToList().IndexOf(item.Key);
                    AppGlobalSettings.LastIndexOrBookId = index.ToString();
                    this.Words = await GetWordsAsync(item.Value);
                    if (!item.Key.Contains("-"))
                    {
                        var except = this.Words.Select(c => c.Name).Except(t20000).ToArray();
                        var exceptWords = this.Words.Where(c => except.Contains(c.Name)).ToList();
                        this.Words = this.Words.Where(c => !except.Contains(c.Name)).OrderBy(c =>

                         t20000.IndexOf(c.Name)

                        ).ToList();
                        this.Words.AddRange(exceptWords);
                    }
                    PackageVersion.Text = $"{item.Key}  ( {this.Words.Count} )";
                    if (sender != null)
                    {
                        MessageDialog messageDialog = new MessageDialog(item.Key, "LOADED_SUCCESS".Translate());
                        await messageDialog.ShowAsync();
                    }

                    await Task.Delay(1000);
                    await GetNextAsync();

                    UpdateStatistics(hardmode: true);

                }


            }
            catch (Exception ex)
            {
                logger.Event(nameof(Load_Click), ex.Message);
            }
        }
        public static List<string> t20000 = new List<string>();
        async void LOAD_SUBTITLE_BOOK_CLICK(object sender, RoutedEventArgs e)
        {
            if (wordBooksListView.SelectedItem is WorkBook wb)
            {
                var result = await TryLoadWorkBookAsync(wb);
                if (sender != null && result)
                {
                    MessageDialog messageDialog = new MessageDialog(wb.Name, "LOADED_SUCCESS".Translate());
                    await messageDialog.ShowAsync();
                }
            }

        }

        private async Task<bool> TryLoadWorkBookAsync(WorkBook wb)
        {
            try
            {
                logger.Event(nameof(TryLoadWorkBookAsync));
                int baseCount = 0;

                this.Words = GetWords(wb, baseCount);

                this.Words = this.Words.OrderBy(c => t20000.IndexOf(c.Name)).ToList();
                PackageVersion.Text = $"{wb.Name}  ( {this.Words.Count} )";

                await Task.Delay(1000);
                await GetNextAsync();

                UpdateStatistics(hardmode: true);

                await UpdateMyBooksAsync();

                pivot.SelectedIndex = 0;
                AppGlobalSettings.LastIndexOrBookId = wb.Id;
                logger.Event(nameof(TryLoadWorkBookAsync), AppGlobalSettings.LastIndexOrBookId);
                return true;
            }
            catch (Exception ex)
            {
                AppGlobalSettings.LastIndexOrBookId = string.Empty;
                logger.Event(nameof(TryLoadWorkBookAsync), 2);
                logger.Event(ex.Message, ex);
                return false;
            }
        }

        async void DELETE_SUBTITLE_BOOK_CLICK(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsDownloading = true;
                if (wordBooksListView.SelectedItem is WorkBook wb)
                {
                    await WorkBookService.DeleteBooksAsync(wb);
                    HttpClient client = new HttpClient();
                    var post = await client.PostAsync("https://search.scorpioplayer.com/api/google/DeleteWorkbook",
                        new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            Id = _id,
                            Value = wb.Name
                        }), Encoding.UTF8, "application/json"));
                    await UpdateMyBooksAsync();
                }
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error_Title".Translate());
                await messageDialog.ShowAsync();
            }
            finally
            {
                this.IsDownloading = false;
            }
        }
        void REMOVE_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WordsListView?.SelectedItem is Word word)
                {
                    word.No();
                }

                UpdateStatistics(hardmode: true);

            }
            catch (Exception)
            {
            }
        }
        void UpdateUpComing()
        {
            try
            {
                UpcomingListView.ItemsSource = this.Words.OrderBy(c =>

                {

                    var index = t20000.IndexOf(c.Name);
                    c.Order = index;

                    return index;
                }


                ).Where(c => c.Level == 0).OrderBy(c => c.Order == -1).ToList();
            }
            catch (Exception)
            {
            }
        }

        void statistics_Update_Click(object sender, RoutedEventArgs e)
        {
            REFLESH_MostHit.IsChecked = false;
            _sortByHitCount = false;
            UpdateStatistics(hardmode: true);
        }
        bool _sortByHitCount;
        void statistics_Update_Click_Order_low(object sender, RoutedEventArgs e)
        {
            _sortByHitCount = REFLESH_MostHit.IsChecked ?? false;
            UpdateStatistics(hardmode: true);
        }
        void Upcoming_Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateUpComing();
        }
        void Upcoming_REMOVE_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UpcomingListView.SelectedItems.IsNullOrCountEqualsZero() && UpcomingListView.SelectedItems.Cast<Word>().ToList() is List<Word> words)
                {
                    foreach (var item in words)
                    {
                        item.Complete();
                    }
                }

                UpdateUpComing();

                UpdateStatistics(hardmode: true);
            }
            catch (Exception)
            {
            }
        }
        async void MORE_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyout mf = new MenuFlyout();
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Bing".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://cn.bing.com/dict/search?q={Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}&qs=n&form=Z9LH5&sp=-1&pq=hello&sc=7-5&sk=&cvid=DA527C397FB74913A4837D4E3C5DCA3E");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Cambridge".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://dictionary.cambridge.org/dictionary/english-chinese-simplified/{Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Urban Dictionary".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://www.urbandictionary.com/define.php?term={Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Oxford Dictionary".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://en.oxforddictionaries.com/definition/{Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            //https://en.oxforddictionaries.com/definition/quack
            mf.ShowAt(More_Button);
        }

        async void WordsListView_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyout mf = new MenuFlyout();
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Bing".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://cn.bing.com/dict/search?q={Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}&qs=n&form=Z9LH5&sp=-1&pq=hello&sc=7-5&sk=&cvid=DA527C397FB74913A4837D4E3C5DCA3E");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Cambridge".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://dictionary.cambridge.org/dictionary/english-chinese-simplified/{Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Urban Dictionary".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://www.urbandictionary.com/define.php?term={Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            mf.Items.Add(new MenuFlyoutItem()
            {
                Text = "Oxford Dictionary".Translate(),
                Command = new CCommand((c) =>
                {
                    Launcher.OpenAsync($"https://en.oxforddictionaries.com/definition/{Uri.EscapeDataString(this.CurrentWord?.Name ?? string.Empty)}");
                })
            });
            //https://en.oxforddictionaries.com/definition/quack
            mf.ShowAt(More_Button);
        }

        async void DELETE_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WordsListView?.SelectedItem is Word word)
                {
                    var ids = AppGlobalSettings.ID?.Split(",");
                    if (!ids.IsNullOrCountEqualsZero())
                    {
                        if (ids.Contains(word.Id))
                        {
                            AppGlobalSettings.ID = string.Join(",", ids.Where(c => c != word.Id));
                        }
                    }
                }

                UpdateStatistics(hardmode: true);

            }
            catch (Exception)
            {

            }
        }
        void UpdateStatistics(Func<Word, bool> func = null, bool hardmode = false)
        {
            if (hardmode)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var all = Word.GetAll();
                        if (all != null)
                        {
                            if (_sortByHitCount)
                            {
                                all = all.OrderByDescending(c => c.HitDates?.Count ?? -1).ThenBy(c => c.Level).ToList();
                            }
                            else
                            {
                                all = all.OrderByDescending(c => c.HitDate).ToList();

                            }
                            if (func != null)
                            {
                                all = all.Where(c => func(c)).ToList();
                            }

                            var perc = string.Empty;
                            if (!this.Words.IsNullOrCountEqualsZero())
                            {
                                var comp = this.Words.Where(c => c.IsConsiderCompleted()).Count();
                                var percent = ((double)comp / (double)this.Words.Count) * 100;
                                perc = $" {percent.ToString("0.00")} %";
                            }

                            var new_today = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                            .Where(c => c.HitDates.All(d => DateTimeOffset.UtcNow - d < TimeSpan.FromHours(24))).Count();

                            var new_week = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                            .Where(c => c.HitDates.All(d => DateTimeOffset.UtcNow - d < TimeSpan.FromDays(7))).Count();

                            var new_month = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                            .Where(c => c.HitDates.All(d => DateTimeOffset.UtcNow - d < TimeSpan.FromDays(30))).Count();

                            var re_today = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                         .Where(c => c.HitDates.Any(d => DateTimeOffset.UtcNow - d < TimeSpan.FromHours(24)) && c.HitDates.Any(d => DateTimeOffset.UtcNow - d > TimeSpan.FromHours(24))).Count();

                            var re_week = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                            .Where(c => c.HitDates.Any(d => DateTimeOffset.UtcNow - d < TimeSpan.FromDays(7)) && c.HitDates.Any(d => DateTimeOffset.UtcNow - d > TimeSpan.FromDays(7))).Count();

                            var re_month = all.Where(c => c.HitDates != null).Where(c => c.Level < 20)
                            .Where(c => c.HitDates.Any(d => DateTimeOffset.UtcNow - d < TimeSpan.FromDays(30)) && c.HitDates.Any(d => DateTimeOffset.UtcNow - d > TimeSpan.FromDays(30))).Count();

                            GenericDispatherActionAsync(() =>
                            {

                                Report_Text11.Text = " " + new_today.ToString();
                                Report_Text22.Text = " " + new_week.ToString();
                                Report_Text33.Text = " " + new_month.ToString();

                                Report_Text11_r.Text = " " + re_today.ToString();
                                Report_Text22_r.Text = " " + re_week.ToString();
                                Report_Text33_r.Text = " " + re_month.ToString();

                                percentRun.Text = perc;
                                if (TotalWords != null)
                                {
                                    TotalWords.Text = all.Count().ToString();
                                }
                                if (WordsListView != null)
                                {
                                    WordsListView.ItemsSource = all;
                                }
                            });
                        }
                        else
                        {
                            GenericDispatherActionAsync(() =>
                            {
                                if (WordsListView != null)
                                {
                                    WordsListView.ItemsSource = null;
                                }
                            });

                        }
                    }
                    finally
                    {
                    }
                });

            }
            else
            {
                Task.Run(async () =>
                {

                    var perc = string.Empty;
                    if (!this.Words.IsNullOrCountEqualsZero())
                    {
                        var comp = this.Words.Where(c => c.IsConsiderCompleted()).Count();
                        var percent = ((double)comp / (double)this.Words.Count) * 100;
                        perc = $" {percent.ToString("0.00")} %";
                    }
                    GenericDispatherActionAsync(() =>
                    {
                        percentRun.Text = perc;
                    });
                });
            }


        }
        void CheckCurrentWordInCurrentBook()
        {
            if (!this.Words.IsNullOrCountEqualsZero() && this.CurrentWord != null)
            {
                if (this.Words.FirstOrDefault(c => c.Name == this.CurrentWord.Name) is null)
                {
                    this.IsCurrentWordInCurrentBook = false;
                }
                else
                {
                    this.IsCurrentWordInCurrentBook = true;
                }
            }
        }
        async Task GetNextAsync()
        {
            if (AppGlobalSettings.ReviewAll)
            {
                var rs = Word.GetAll().OrderBy(c => c.Level);
                Word w = null;
                foreach (var word in rs)
                {
                    w = word;
                    if (w.IsValid(this.CurrentWord))
                    {
                        w.Order = t20000.IndexOf(this.CurrentWord?.Name ?? string.Empty);
                        if (this.Words != null && this.Words.FirstOrDefault(c => c.Name == w.Name) is Word wordd && wordd != null)
                        {
                            w = wordd;
                        }
                        this.CurrentWord = w;
                        this.CurrentWordDetails = null;
                        GetDetailsAsync(w);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentWord)));
                        if (AppGlobalSettings.AudoPlayAudio)
                        {
                            Speaker_Click(null, null);
                        }
                        return;
                    }
                }
            }
            if (this.Words != null)
            {
                int index = 0;
            PICK:
                if (index < this.Words.Count)
                {
                    var word = this.Words[index];
                    if (word.IsValid(this.CurrentWord))
                    {
                        word.Order = t20000.IndexOf(this.CurrentWord?.Name ?? string.Empty);
                        this.CurrentWord = word;
                        this.CurrentWordDetails = null;
                        GetDetailsAsync(word);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentWord)));
                        if (AppGlobalSettings.AudoPlayAudio)
                        {
                            Speaker_Click(null, null);
                        }
                        return;
                    }
                    else
                    {
                        index++;
                        goto PICK;
                    }
                }
            }
        }
        private void Level_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                AppGlobalSettings.LastLevelIndex = LevelCombo.SelectedIndex;
                var item = (LevelCombo.SelectedItem as ComboBoxItem).Content.ToString();
                if (int.TryParse(item, out int level))
                {
                    this.Level = level;
                    UpdateStatistics(w => w.Level == level, hardmode: true);
                }
                else if (item == "None")
                {
                    this.Level = -1;
                    UpdateStatistics(hardmode: true);
                }
                else if (item == "Forever")
                {
                    this.Level = -1;
                    UpdateStatistics(w => w.Level > 11, hardmode: true);
                }
            }
            catch (Exception ex)
            {
                logger.Event(ex.Message);
            }

        }
        public enum DeviceFamily
        {
            Unknow = 0,
            Xbox = 1,
            Desktop = 2,
            Mobile = 3
        }
        public static DeviceFamily CurrentDeviceFamily
        {
            get
            {
#if !WINDOWS_UWP
                return DeviceFamily.Unknow;
#endif
#pragma warning disable Uno0001 // Uno type or member is not implemented
#pragma warning disable CS0162 // Unreachable code detected
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
#pragma warning restore CS0162 // Unreachable code detected
                {
                    return DeviceFamily.Mobile;
                }
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
                {
                    return DeviceFamily.Desktop;

                }
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                {
                    return DeviceFamily.Xbox;

                }
#pragma warning restore Uno0001 // Uno type or member is not implemented
                return DeviceFamily.Unknow;
            }
        }


        async private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Event(nameof(MainPage_Loaded));
            var file = await GetAssetsFileAsync("word_books", "t-20000.txt");

            if (file != null)
            {
                using (var st = new StreamReader(file))
                {
                    while (st.ReadLine() is string str && !String.IsNullOrEmpty(str))
                    {
                        t20000.Add(str);
                    }
                }
            }
#if WINDOWS_UWP
            ShakeEnabledToggleSwitchGrid.Visibility = Visibility.Collapsed;
            ShakeLevelStrongToggleSwitchGrid.Visibility = Visibility.Collapsed;
            ElementSoundPlayer.State = AppGlobalSettings.UseSound ? ElementSoundPlayerState.On : ElementSoundPlayerState.Auto;
            if (CurrentDeviceFamily == DeviceFamily.Desktop)
            {
                UseLightThemeToggleSwitch.Visibility = Visibility.Visible;
                UseSoundToggleSwitchGrid.Visibility = Visibility.Visible;
            }
            if (CurrentDeviceFamily == DeviceFamily.Xbox)
            {
                UsePointerToggleSwitchGrid.Visibility = Visibility.Visible;
            }
            if (CurrentDeviceFamily == DeviceFamily.Desktop && AppGlobalSettings.UseLightThemeButton)
            {
                root.Background = Application.Current.Resources["SystemControlChromeHighAcrylicWindowMediumBrush"] as Brush;
            }
#endif
            //dicsbob
            DicsCombo.DisplayMemberPath = "Key";
            DicsCombo.ItemsSource = Dics;
            LevelCombo.SelectedIndex = AppGlobalSettings.LastLevelIndex;

            try
            {
                logger.Event(nameof(MainPage_Loaded), $"{nameof(AppGlobalSettings.LastIndexOrBookId)}:{AppGlobalSettings.LastIndexOrBookId}");
                if (int.TryParse(AppGlobalSettings.LastIndexOrBookId, out int index))
                {
                    logger.Event(nameof(MainPage_Loaded), 1);
                    try
                    {
                        DicsCombo.SelectedIndex = index;
                        Load_Click(null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.Event(nameof(MainPage_Loaded), 5);
                        logger.Event(ex.Message, ex);
                    }
                }
                else if (!String.IsNullOrEmpty(AppGlobalSettings.LastIndexOrBookId))
                {
                    logger.Event(nameof(MainPage_Loaded), 2);
                    var books = await WorkBookService.GetAllBooksAsync();
                    if (books?.FirstOrDefault(c => c.Id == AppGlobalSettings.LastIndexOrBookId) is WorkBook wb)
                    {
                        await TryLoadWorkBookAsync(wb);
                    }
                }
            }
            catch (Exception ex)
            {
                AppGlobalSettings.LastIndexOrBookId = string.Empty;
                logger.Event(nameof(MainPage_Loaded), 3);
                logger.Event(ex.Message, ex);
            }


            passwordBox.Password = AppGlobalSettings.Password;

            Level_Changed(null, null);
            try
            {
                await UpdateMyBooksAsync();
                ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            }
            catch (Exception ex)
            {
                logger.Event(nameof(MainPage_Loaded), 6);
                logger.Event(ex.Message, ex);
            }

            ToggleAccelerometer();

            if (AppGlobalSettings.AutoSync)
            {
                UPLOADS_Click(null, null);
            }
#if __IOS__
            if (_timer == null)
            {
                _timer = new Timer(a =>
                {
                    GenericDispatherActionAsync(() =>
                    {
                        UpdateToggle();
                    });
                }, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
            }
#endif
        }

        private async Task UpdateMyBooksAsync()
        {
            try
            {
                var books = await WorkBookService.GetAllBooksAsync();
                wordBooksListView.ItemsSource = books;
            }
            catch (Exception)
            {
            }
        }

        public async Task<Stream> GetAssetsFileAsync(string folder, string name)
        {
            try
            {
#if WINDOWS_UWP
                var uri = new Uri($"ms-appx:///Assets/{folder}/{name}");
                var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                return await file.OpenStreamForReadAsync();
#elif __DROID__
                return IELTSWord.Droid.MainActivity.Instance.OpenAsset(folder, name);
#elif __IOS__
                return IELTSWord.iOS.Application.OpenAsset(folder, name);
#else
                var file = await StorageFile.GetFileFromPathAsync($"Assets/{folder}/{name}");
                return await file.OpenStreamForReadAsync();
#endif
            }
            catch (Exception ex)
            {
                logger.Event(nameof(GetAssetsFileAsync), ex.Message);
                return null;
            }
        }
        Timer _timer;
        private void PanePivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            logger.Event(nameof(PanePivot_PivotItemLoaded), (pivot.SelectedItem as TextBlock)?.Text ?? "empty");

            UpdateToggle();

        }
        async Task<List<Word>> GetWordsAsync(string source)
        {
            List<Word> words = new List<Word>();
            var file = await GetAssetsFileAsync("word_books", source);
            if (file != null)
            {
                using (var st = new StreamReader(file))
                {
                    while (st.ReadLine() is string str && !String.IsNullOrEmpty(str))
                    {
                        if (Word.Load(str) is Word w)
                        {
                            words.Add(w);
                        }
                        else
                        {
                            Word word = Word.Create(str);
                            words.Add(word);
                        }

                    }
                }
            }
            return words.DistinctBy(c => c.Name).ToList();
        }



        List<Word> GetWords(WorkBook source, int order = 0)
        {
            List<Word> words = new List<Word>();
            var items = source.Words.Where(c => c.Length < 100).ToList();
            foreach (var item in items)
            {
                try
                {
                    order++;
                    if (Word.Load(item) is Word w)
                    {
                        words.Add(w);
                    }
                    else
                    {
                        Word word = new Word()
                        {
                            //Elaborate = tab.Last(),
                            Id = item,
                            Level = 0,
                            Order = order,
                            Name = item,
                            HitDate = DateTimeOffset.UtcNow
                        };
                        words.Add(word);
                    }
                }
                catch (Exception ex)
                {
                    logger.Event(nameof(GetWords), ex.Message);
                }

            }

            return words.DistinctBy(c => c.Name).ToList();
        }





        List<Word> Words;

        private Word _word;

        public Word CurrentWord
        {
            get { return _word; }
            set { _word = value; CheckCurrentWordInCurrentBook(); }
        }

        private bool _IsCurrentWordInCurrentBook;

        public bool IsCurrentWordInCurrentBook
        {
            get { return _IsCurrentWordInCurrentBook; }
            set { _IsCurrentWordInCurrentBook = value; GenericRaisePropertyChanged(nameof(IsCurrentWordInCurrentBook)); }
        }

        Dictionary<string, string> Dics = new Dictionary<string, string>()
        {
            ["20000-1000-1".Translate()] = "t-1000-1.txt",
            ["20000-1000-2".Translate()] = "t-1000-2.txt",
            ["20000-1000-3".Translate()] = "t-1000-3.txt",
            ["20000-1000-4".Translate()] = "t-1000-4.txt",
            ["20000-1000-5".Translate()] = "t-1000-5.txt",
            ["20000-1000-6".Translate()] = "t-1000-6.txt",
            ["20000-1000-7".Translate()] = "t-1000-7.txt",
            ["20000-1000-8".Translate()] = "t-1000-8.txt",
            ["20000-1000-9".Translate()] = "t-1000-9.txt",
            ["20000-1000-10".Translate()] = "t-1000-10.txt",
            ["20000-1000-11".Translate()] = "t-1000-11.txt",
            ["20000-1000-12".Translate()] = "t-1000-12.txt",
            ["20000-1000-13".Translate()] = "t-1000-13.txt",
            ["20000-1000-14".Translate()] = "t-1000-14.txt",
            ["20000-1000-15".Translate()] = "t-1000-15.txt",
            ["20000-1000-16".Translate()] = "t-1000-16.txt",
            ["20000-1000-17".Translate()] = "t-1000-17.txt",
            ["20000-1000-18".Translate()] = "t-1000-18.txt",
            ["20000-1000-19".Translate()] = "t-1000-19.txt",
            ["20000-1000-20".Translate()] = "t-1000-20.txt",
            ["20000-1000-21".Translate()] = "t-1000-21.txt",
            ["All20000".Translate()] = "t-20000.txt",
            //zk,gk,cet4,cet6,ky,toefl,gre,ielts
            ["zk".Translate()] = "zk.txt",
            ["gk".Translate()] = "gk.txt",
            ["cet4".Translate()] = "cet4.txt",
            ["cet6".Translate()] = "cet6.txt",
            ["ky".Translate()] = "ky.txt",
            ["toefl".Translate()] = "toefl.txt",
            ["gre".Translate()] = "gre.txt",
            ["ielts".Translate()] = "ielts.txt",
        };

        public bool NeedDispather => !this.Dispatcher.HasThreadAccess;
        public async void GenericRaisePropertyChanged(string propertyName)
        {
            if (NeedDispather)
            {
                await this.GenericDispatherActionAsync(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }
        public async Task GenericDispatherActionAsync(Action action, Windows.UI.Core.CoreDispatcherPriority level = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            if (NeedDispather)
            {
                await this.Dispatcher.RunAsync(level, () =>
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception)
                    {

                    }

                });
            }
            else
            {
                try
                {

                    action?.Invoke();
                }
                catch (Exception)
                {

                }
            }

        }


        public event PropertyChangedEventHandler PropertyChanged;
    }

}
