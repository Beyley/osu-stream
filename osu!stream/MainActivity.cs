using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using osum.Support.Android;
using Android.Views;
using Android.Content.PM;
using osum.Input.Sources;
using osum.Input;

namespace osum
{
    [Activity(Label = "@string/app_name"
        , MainLauncher = true
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.UserLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            GameBase game = new GameBaseAndroid(this, GameModes.OsuMode.Tutorial);

            // TODO: Move elsewhere?
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen |
                    SystemUiFlags.ImmersiveSticky | SystemUiFlags.Immersive);

                Immersive = true;
            }

            game.Run();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            InputSourceAndroid source = InputManager.RegisteredSources[0] as InputSourceAndroid;
            source.HandleTouches(e);

            return base.OnTouchEvent(e);
        }
    }
}