using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GamePlatform.Editor;

public class PlatformBuildWindow : EditorWindow
{
    private PopupField<string> platform;
    private Label status;
    private VisualElement actions;

    [MenuItem("Build/Platform/Build Window", priority = 0)]
    public static void Open()
    {
        var window = GetWindow<PlatformBuildWindow>("Game Platforms");
        window.minSize = new Vector2(420, 300);
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.Clear();
        root.style.paddingLeft = root.style.paddingRight = 12;
        root.style.paddingTop = root.style.paddingBottom = 12;
        root.Add(new Label("Платформа сборки"));
        platform = new PopupField<string>(new List<string> { "YandexGames", "CrazyGames" },
            PlatformBuildMenu.SelectedPlatform == "CrazyGames" ? 1 : 0);
        root.Add(platform);
        status = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 10, marginBottom = 10 } };
        root.Add(status);
        actions = new VisualElement();
        actions.Add(new Button(() => Run(() => PlatformBuildMenu.Select(platform.value))) { text = "Применить платформу" });
        actions.Add(new Button(() => Run(() => {
            if (platform.value == "CrazyGames") PlatformBuildMenu.Build("CrazyGames");
            else PlatformBuildMenu.Build("YandexGames");
        })) { text = "Собрать выбранную платформу" });
        actions.Add(new Button(() => Run(PlatformBuildMenu.BuildBoth)) { text = "Собрать обе платформы" });
        root.Add(actions);
        root.Add(new HelpBox("Результаты: Builds/YandexGames и Builds/CrazyGames.\nНа CrazyGames выбирайте тип Unity и загружайте все файлы сборки без ZIP.", HelpBoxMessageType.Info));
        root.Add(new Button(() => EditorUtility.RevealInFinder(System.IO.Path.GetFullPath("Builds"))) { text = "Открыть папку сборок" });
        root.schedule.Execute(UpdateStatus).Every(250);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool busy = PlatformBuildMenu.IsBusy;
        status.text = $"Применена: {PlatformBuildMenu.SelectedPlatform}\n" +
            (BuildPipeline.isBuildingPlayer ? "Идёт сборка…" : busy ? "Компиляция и импорт…" : "Готово к выбору и сборке.");
        actions.SetEnabled(!busy && !EditorApplication.isPlayingOrWillChangePlaymode);
        platform.SetEnabled(!busy && !EditorApplication.isPlayingOrWillChangePlaymode);
    }

    private static void Run(Action action)
    {
        try { action(); }
        catch (Exception error) { Debug.LogException(error); }
    }
}
