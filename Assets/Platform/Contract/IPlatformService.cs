using System;

namespace GamePlatform
{
    public interface IPlatformService : IDisposable
    {
        bool IsReady { get; }
        bool IsAdShowing { get; }
        bool IsPaused { get; }
        string Language { get; }
        event Action<string> LanguageChanged;
        void Initialize(Action onReady, Action<string> onError);
        void GameReady();
        void GameplayStart();
        void GameplayStop();
        void ShowInterstitial(Action onFinished);
        void SetBanner(bool visible);
    }
}
