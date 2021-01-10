﻿using Android.Views;
using OpenTK.Graphics.ES11;
using osum.Audio;
using osum.GameModes;
using osum.Input;
using osum.Input.Sources;
using Xamarin.Essentials;

namespace osum.Support.Android
{
    class GameBaseAndroid : GameBase
    {
        private MainActivity _activity;

        public GameWindowAndroid Window;

        public GameBaseAndroid(MainActivity activity, OsuMode mode = OsuMode.Unknown) : base(mode)
        {
            _activity = activity;
        }

        public override void Run()
        {
            Window = new GameWindowAndroid(_activity);
            Window.Run();

            _activity.SetContentView(Window);
        }

        protected override BackgroundAudioPlayer InitializeBackgroundAudio()
        {
            return new BackgroundAudioPlayerCommon();
        }

        protected override SoundEffectPlayer InitializeSoundEffects()
        {
            return new SoundEffectPlayerBass();
        }

        protected override void InitializeInput()
        {
            InputSource source = new InputSourceAndroid();
            InputManager.AddSource(source);
        }

        public override void SetupScreen()
        {
            NativeSize = new System.Drawing.Size((int)DeviceDisplay.MainDisplayInfo.Width, (int)DeviceDisplay.MainDisplayInfo.Height);

            base.SetupScreen();
        }
    }
}