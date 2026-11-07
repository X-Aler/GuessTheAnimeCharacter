using System;
using UnityEngine;

namespace GamePlatform
{
    public static class PlatformServices
    {
        public static IPlatformService Service { get; private set; }
        public static bool IsReady { get; private set; }
        public static string Error { get; private set; }
        public static string Language { get; private set; } = "en";
        public static bool CanPlay => IsReady && !Service.IsAdShowing && !Service.IsPaused && Application.isFocused;
        public static event Action Ready;
        public static event Action<string> LanguageChanged;
        public static event Action<string> Failed;
        private static bool gameReady;
        private static PlatformRuntime runtime;

        public static void Initialize()
        {
            if (Service != null) return;
#if PLATFORM_CRAZYGAMES
            Service = new CrazyGamesPlatformService();
#else
            Service = new YandexPlatformService();
#endif
            runtime = new GameObject("PlatformServices").AddComponent<PlatformRuntime>();
            UnityEngine.Object.DontDestroyOnLoad(runtime.gameObject);
            Service.LanguageChanged += ApplyLanguage;
            try
            {
                Service.Initialize(() => {
                    if (IsReady || Error != null) return;
                    IsReady = true;
                    ApplyLanguage(Service.Language);
                    Ready?.Invoke();
                }, Fail);
            }
            catch (Exception error) { Fail(error.Message); }
        }

        public static string NormalizeLanguage(string locale)
        {
            var language = (locale ?? "").Trim().Split('-', '_')[0];
            return string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
        }

        private static void ApplyLanguage(string locale)
        {
            Language = NormalizeLanguage(locale);
            LanguageChanged?.Invoke(Language);
        }

        internal static void Fail(string message)
        {
            if (IsReady || Error != null) return;
            Error = string.IsNullOrWhiteSpace(message) ? "Platform initialization failed." : message;
            Debug.LogError(Error);
            Failed?.Invoke(Error);
        }

        public static void GameReady(bool showBanner)
        {
            if (!IsReady || gameReady) return;
            Service.GameReady();
            Service.SetBanner(showBanner);
            gameReady = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Service?.Dispose();
            Service = null;
            runtime = null;
            IsReady = false;
            Error = null;
            Language = "en";
            Ready = null;
            LanguageChanged = null;
            Failed = null;
            gameReady = false;
        }

        internal static void Shutdown()
        {
            Service?.Dispose();
            runtime = null;
        }
    }
}
