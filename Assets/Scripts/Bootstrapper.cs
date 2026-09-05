using System.Collections;
using UnityEngine;
using YG; // Пространство имен PluginYG v2

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
        // 2. Ждем, пока плагин инициализирует SDK Яндекса и получит данные окружения (язык, устройство)
        // YG2.isSDKEnabled становится true, когда JS-библиотека ysdk загружена и ответила
        while (!YG2.isSDKEnabled)
        {
            yield return null;
        }

        YG2.SwitchLanguage(YG2.lang);

        // Даем 1 кадр на завершение работы встроенных подписчиков LanguageYG,
        // чтобы тексты на сцене успели применить полученный язык (YG2.lang)
        yield return null;

        // 3. Прячем загрузочный экран и активируем основной интерфейс
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (mainScreen != null) mainScreen.SetActive(true);

        // 4. Оповещаем Яндекс Игры о полной готовности игры к взаимодействию
        // Вызываем СТРОГО после того, как экран загрузки скрыт и меню уже перед глазами игрока
        YG2.GameReadyAPI();

        // 5. Подрубаем баннерную рекламу (RTB / Sticky-баннер)
        if (showBannerOnStart)
        {
            EnableBanner();
        }

        AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.OST, 1);
    }

    private void EnableBanner()
    {
        YG2.SetBannerPosition(YG2.BannerPosition.Bottom);
        YG2.ShowBanner();
    }
}