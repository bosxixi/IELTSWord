using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Android.Content.PM;
using Android.Views;

namespace IELTSWord.Droid
{
    [Activity(Label = "乖乖背单词", Theme = "@style/MyTheme.Splash", MainLauncher = false, NoHistory = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
            WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
    public class SplashActivity : Windows.UI.Xaml.ApplicationActivity
    {
        static readonly string TAG = "X:" + typeof(SplashActivity).Name;

        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
            //Log.Debug(TAG, "SplashActivity.OnCreate");
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.SetStatusBarColor(Android.Graphics.Color.Argb(255, 45, 45, 48));
            }
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();
            //StartActivity(typeof(MainActivity));
            //Task startupWork = new Task(() => { SimulateStartup(); });
            //startupWork.Start();
        }
    }
}

