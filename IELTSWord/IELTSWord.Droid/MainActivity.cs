using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;
using System.IO;

namespace IELTSWord.Droid
{
    [Activity(
        Label = "乖乖背单词",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
            MainLauncher = true,
            ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
            WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
        )]
    public class MainActivity : Windows.UI.Xaml.ApplicationActivity
    {
        public static MainActivity Instance;
        protected override void OnCreate(Bundle bundle)
        {
            Instance = this;
            base.OnCreate(bundle);

            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.SetStatusBarColor(Android.Graphics.Color.Argb(255, 45, 45, 48));
            }
            //System.IO.Stream input = Assets.Open("my_asset.txt");
        }

        public Stream OpenAsset(string folder, string file)
        {
            return  Assets.Open($"{folder}/{file}");
        }
    }
}

