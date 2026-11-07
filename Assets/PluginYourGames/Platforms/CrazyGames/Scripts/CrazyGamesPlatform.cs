#if CrazyGamesPlatform_yg
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YG
{
    // Plugin Your Games remains the common API; only this adapter knows CrazyGames.
    public partial class PlatformYG2 : IPlatformsYG2
    {
        internal static string CrazyLanguage = "en";
        [DllImport("__Internal")] private static extern void CG_Init();
        [DllImport("__Internal")] private static extern void CG_Event(int kind);
        [DllImport("__Internal")] private static extern void CG_Interstitial();

        public void InitAwake()
        {
#if UNITY_EDITOR
            YG2.SyncInitialization();
#else
            CG_Init();
#endif
        }

        public string GetLanguage() => CrazyLanguage;
        public void GameReadyAPI() => SendEvent(0);
        public void GameplayStart() => SendEvent(1);
        public void GameplayStop() => SendEvent(2);
        public void HappyTime() => SendEvent(3);
        public void InterstitialAdvShow()
        {
#if !UNITY_EDITOR
            CG_Interstitial();
#endif
        }
        private static void SendEvent(int kind)
        {
#if !UNITY_EDITOR
            CG_Event(kind);
#endif
        }
    }
}

namespace YG.Insides
{
    public partial class YGSendMessage
    {
        private bool crazyWasPaused;
        private bool crazyMuted;
        private float crazyAudioVolume;
        public void CrazyInit(string locale)
        {
            PlatformYG2.CrazyLanguage = NormalizeCrazyLocale(locale);
            YG2.SyncInitialization();
        }

        public static string NormalizeCrazyLocale(string locale)
        {
            var language = (locale ?? "").Trim().Split('-', '_')[0].ToLowerInvariant();
            return language == "ru" ? "ru" : "en";
        }

        public void CrazyInitError(string error) => Debug.LogError("CrazyGames initialization failed: " + error);
        public void CrazyFocus(string focused) => YG2.SetFocusWindowGame(focused == "true");
        public void CrazyMute(string muted)
        {
            bool mute = muted == "true";
            if (mute == crazyMuted) return;
            if (mute) { crazyAudioVolume = AudioListener.volume; AudioListener.volume = 0f; }
            else AudioListener.volume = crazyAudioVolume;
            crazyMuted = mute;
        }
        public void CrazyAdOpen()
        {
            crazyWasPaused = YG2.isPauseGame;
            YGInsides.OpenInterAdv();
        }
        public void CrazyAdClose(string shown)
        {
            YG2.nowInterAdv = false;
            if (shown == "true") YGInsides.SetTimerInterAdv();
            YG2.onCloseInterAdv?.Invoke();
            YG2.onCloseInterAdvWasShow?.Invoke(shown == "true");
            YG2.onCloseAnyAdv?.Invoke();
            if (!crazyWasPaused && YG2.isFocusWindowGame) YG2.PauseGame(false);
        }
        public void CrazyAdError(string error)
        {
            Debug.LogWarning("CrazyGames ad unavailable: " + error);
            YGInsides.ErrorInterAdv();
        }
    }
}
#endif
