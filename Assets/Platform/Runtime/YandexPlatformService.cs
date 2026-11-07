#if !PLATFORM_CRAZYGAMES
using System;
using YG;

namespace GamePlatform
{
    public sealed class YandexPlatformService : IPlatformService
    {
        public bool IsReady => YG2.isSDKEnabled;
        public bool IsAdShowing => YG2.nowAdsShow;
        public bool IsPaused => YG2.isPauseGame;
        public string Language => YG2.lang;
        public event Action<string> LanguageChanged;
        private Action ready;
        private Action adFinished;
        private bool disposed;

        public void Initialize(Action onReady, Action<string> onError)
        {
            ready = onReady;
            YG2.onSwitchLang += OnLanguage;
            YG2.onGetSDKData += OnReady;
            if (IsReady) OnReady();
        }

        private void OnReady()
        {
            YG2.onGetSDKData -= OnReady;
            var callback = ready;
            ready = null;
            callback?.Invoke();
        }

        private void OnLanguage(string language) => LanguageChanged?.Invoke(language);
        public void GameReady() => YG2.GameReadyAPI();
        public void GameplayStart() => YG2.GameplayStart();
        public void GameplayStop() => YG2.GameplayStop();
        public void SetBanner(bool visible)
        {
            if (visible) { YG2.SetBannerPosition(YG2.BannerPosition.Bottom); YG2.ShowBanner(); }
            else YG2.HideBanner();
        }

        public void ShowInterstitial(Action onFinished)
        {
            if (!IsReady || IsAdShowing || !YG2.isTimerAdvCompleted || adFinished != null)
            {
                onFinished?.Invoke();
                return;
            }
            adFinished = onFinished ?? (() => { });
            YG2.onCloseInterAdv += OnAdFinished;
            YG2.onErrorInterAdv += OnAdFinished;
            try { YG2.InterstitialAdvShow(); }
            catch { OnAdFinished(); throw; }
        }

        private void OnAdFinished()
        {
            YG2.onCloseInterAdv -= OnAdFinished;
            YG2.onErrorInterAdv -= OnAdFinished;
            var callback = adFinished;
            adFinished = null;
            callback?.Invoke();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            YG2.onGetSDKData -= OnReady;
            YG2.onSwitchLang -= OnLanguage;
            YG2.onCloseInterAdv -= OnAdFinished;
            YG2.onErrorInterAdv -= OnAdFinished;
            ready = null;
            adFinished = null;
        }
    }
}
#endif
