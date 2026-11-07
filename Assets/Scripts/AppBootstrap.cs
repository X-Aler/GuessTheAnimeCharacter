using System.Collections;
using UnityEngine;
using GamePlatform;
using TMPro;

public class AppBootstrap : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("Панель/экран загрузки, закрывающий интерфейс на старте")]
    [SerializeField] private GameObject loadingScreen;

    [Tooltip("Главное меню или основной экран квиза, который показываем после загрузки")]
    [SerializeField] private GameObject mainScreen;

    [Header("Banner Settings")]
    [Tooltip("Включать ли нижний/верхний баннер сразу после готовности игры")]
    [SerializeField] private bool showBannerOnStart = true;

    private void Awake()
    {
        // 1. Немедленно показываем экран загрузки и прячем основной контент
        if (loadingScreen != null) loadingScreen.SetActive(true);
        if (mainScreen != null) mainScreen.SetActive(false);
    }

    private void Start()
    {
        // Запускаем корутину безопасной инициализации
        StartCoroutine(InitSequenceRoutine());
    }

    private IEnumerator InitSequenceRoutine()
    {
        PlatformServices.Initialize();
        while (!PlatformServices.IsReady && PlatformServices.Error == null)
        {
            yield return null;
        }

        if (PlatformServices.Error != null)
        {
            var label = loadingScreen != null ? loadingScreen.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null) label.text = "Could not load the game. Please reload the page.\n" + PlatformServices.Error;
            yield break;
        }

        // Let localized UI apply the SDK language before showing the menu.
        yield return null;

        // 3. Прячем загрузочный экран и активируем основной интерфейс
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (mainScreen != null) mainScreen.SetActive(true);

        // 4. Оповещаем выбранную платформу о готовности интерфейса.
        // Вызываем СТРОГО после того, как экран загрузки скрыт и меню уже перед глазами игрока
        PlatformServices.GameReady(showBannerOnStart);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.OST, 1);
    }

}
