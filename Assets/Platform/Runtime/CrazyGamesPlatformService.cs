#if PLATFORM_CRAZYGAMES
using System;
using CrazyGames;
using UnityEngine;

namespace GamePlatform
{
    public sealed class CrazyGamesPlatformService : IPlatformService
    {
        public bool IsReady => CrazySDK.IsInitialized;
        public bool IsAdShowing { get; private set; }
        public bool IsPaused => Time.timeScale == 0f;
        public string Language { get; private set; } = "en";
        public event Action<string> LanguageChanged;
        private float nextInterstitial = Time.realtimeSinceStartup + 180f;
        private bool disposed;
        private bool platformMuted;
        private bool previousPause;
        private GameModule game;

        public void Initialize(Action onReady, Action<string> onError)
        {
            if (!CrazySDK.IsAvailable) { onError("CrazyGames SDK is unavailable on this host."); return; }
            CrazySDK.Init(() => {
                if (disposed) return;
                try
                {
                    try { Language = PlatformServices.NormalizeLanguage(CrazySDK.User.SystemInfo?.locale); }
                    catch (Exception error) { Debug.LogWarning("CrazyGames locale unavailable: " + error.Message); }
                    game = CrazySDK.Game;
                    ApplySettings(game.Settings);
                    game.AddSettingsChangeListener(ApplySettings);
                    LanguageChanged?.Invoke(Language);
                    onReady();
                }
                catch (Exception error) { onError(error.Message); }
            });
        }

        private void ApplySettings(GameSettings settings)
        {
            bool mute = settings != null && settings.muteAudio;
            if (mute == platformMuted) return;
            if (mute) { previousPause = AudioListener.pause; AudioListener.pause = true; }
            else AudioListener.pause = previousPause;
            platformMuted = mute;
        }

        // The CrazyGames Unity loader manages loadingStart/loadingStop itself.
        public void GameReady() { }
        public void GameplayStart() => CrazySDK.Game.GameplayStart();
        public void GameplayStop() => CrazySDK.Game.GameplayStop();
        public void SetBanner(bool visible) { }

        public void ShowInterstitial(Action onFinished)
        {
            if (!IsReady || IsAdShowing || Time.realtimeSinceStartup < nextInterstitial)
            {
                onFinished?.Invoke();
                return;
            }
            IsAdShowing = true;
            nextInterstitial = Time.realtimeSinceStartup + 180f;
            bool finished = false;
            void Complete()
            {
                if (finished) return;
                finished = true;
                IsAdShowing = false;
                onFinished?.Invoke();
            }
            try
            {
                // The official SDK preserves/restores time scale and audio for the whole request.
                CrazySDK.Ad.RequestAd(CrazyAdType.Midgame, () => { }, error => {
                    Debug.LogWarning("CrazyGames ad unavailable: " + error);
                    Complete();
                }, Complete);
            }
            catch (Exception error) { Debug.LogWarning(error.Message); Complete(); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Do not call the singleton getter during scene teardown: it can recreate the SDK.
            if (game != null) game.RemoveSettingsChangeListener(ApplySettings);
            game = null;
            if (platformMuted) AudioListener.pause = previousPause;
        }
    }
}
#endif
