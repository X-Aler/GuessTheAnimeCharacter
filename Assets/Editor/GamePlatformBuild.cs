using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Profile;
using UnityEngine;
using YG;
using YG.Insides;

// Uses the existing Plugin Your Games platform assets and symbols.
[InitializeOnLoad]
public class GamePlatformBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string PendingKey = "AnimeQuiz.PlatformBuild";
    private static double readyAfter;
#if CrazyGamesPlatform_yg && !YandexGamesPlatform_yg
    private const string CompiledPlatform = "CrazyGames";
#elif YandexGamesPlatform_yg && !CrazyGamesPlatform_yg
    private const string CompiledPlatform = "YandexGames";
#else
    private const string CompiledPlatform = "Invalid";
#endif
    static GamePlatformBuild()
    {
        readyAfter = EditorApplication.timeSinceStartup + 3;
        EditorApplication.update += ContinueBuild;
    }

    [MenuItem("Game/Platform/Select Yandex Games")]
    public static void SelectYandex() => Select("YandexGames");
    [MenuItem("Game/Platform/Select CrazyGames")]
    public static void SelectCrazy() => Select("CrazyGames");
    [MenuItem("Game/Build/Yandex Games")]
    public static void BuildYandex() => Queue("YandexGames");
    [MenuItem("Game/Build/CrazyGames")]
    public static void BuildCrazy() => Queue("CrazyGames");
    [MenuItem("Game/Build/Both platforms")]
    public static void BuildBoth() => Queue("YandexGames,CrazyGames");

    private static void Queue(string platforms)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Stop Play Mode and any current build first.");
        SessionState.SetString(PendingKey, platforms);
        Select(platforms.Split(',')[0]);
    }

    public static void Select(string platform)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before switching platform.");
        if (platform != "CrazyGames" && platform != "YandexGames")
            throw new ArgumentException("Unknown game platform: " + platform);
        var asset = AssetDatabase.LoadAssetAtPath<PlatformSettings>(
            $"Assets/PluginYourGames/Platforms/{platform}/{platform}.asset");
        if (asset == null) throw new InvalidOperationException("Missing platform settings: " + platform);
        YG2.infoYG.Basic.platform = asset;
        // Readiness belongs to AppBootstrap, after the scene has become interactive.
        YG2.infoYG.Basic.autoGRA = false;
        YG2.infoYG.Basic.syncInitSDK = true;
        YG2.infoYG.Basic.archivingBuild = platform != "CrazyGames";
        YG2.infoYG.Localization.setLanguageMod = InfoYG.LocalizationSettings.SetLangMod.EveryGameLaunch;
        asset.ApplyProjectSettings();
        PlayerSettings.WebGL.template = "PROJECT:" + platform;
        if (platform == "CrazyGames")
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
        }
        EditorUtility.SetDirty(YG2.infoYG);
        AssetDatabase.SaveAssets();
        // Preserve non-platform symbols, including all selected YG modules.
        YG.EditorScr.DefineSymbols.UpdateDefineSymbols();
        var targets = new[] { NamedBuildTarget.WebGL, NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup) };
        foreach (var target in targets.Distinct())
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "YandexGamesPlatform_yg" && s != "CrazyGamesPlatform_yg").ToList();
            defines.Add(platform + "Platform_yg");
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }
        readyAfter = EditorApplication.timeSinceStartup + 3;
        Debug.Log("Selected " + platform + ". Wait for script compilation before building.");
    }

    private static void ContinueBuild()
    {
        var pending = SessionState.GetString(PendingKey, "");
        if (pending == "" || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            BuildPipeline.isBuildingPlayer || EditorApplication.timeSinceStartup < readyAfter) return;
        SessionState.EraseString(PendingKey);
        try
        {
            var platforms = pending.Split(',');
            if (CompiledPlatform != platforms[0])
                throw new BuildFailedException("Platform scripts are not compiled. Select the platform again after resolving Console errors.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = BuildScenes(),
                locationPathName = "Builds/" + platforms[0], target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            File.WriteAllText("Builds/" + platforms[0] + "/platform-build-result.json",
                JsonUtility.ToJson(new Result { platform = platforms[0], result = report.summary.result.ToString(),
                    errors = (int)report.summary.totalErrors, utc = DateTime.UtcNow.ToString("O") }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + platforms[0]);
            if (platforms.Length > 1) Queue(string.Join(",", platforms.Skip(1)));
        }
        catch (Exception error) { Debug.LogException(error); }
    }

    [Serializable] private class Result { public string platform, result, utc; public int errors; }
    private static string[] BuildScenes()
    {
        var profile = BuildProfile.GetActiveBuildProfile();
        var scenes = profile != null && profile.overrideGlobalScenes ? profile.scenes : EditorBuildSettings.scenes;
        var paths = scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (paths.Length == 0) throw new BuildFailedException("No enabled scenes in the active build configuration.");
        return paths;
    }
    public int callbackOrder => -2000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL) return;
        var selected = PlatformSettings.currentPlatformBaseName;
        if (CompiledPlatform != selected || PlayerSettings.WebGL.template != "PROJECT:" + selected)
            throw new BuildFailedException("Platform, compiled symbols and WebGL template disagree. Use Game/Platform, wait for compilation, then build.");
        if (!YG2.infoYG.Basic.syncInitSDK || YG2.infoYG.Basic.autoGRA)
            throw new BuildFailedException("Enable syncInitSDK and disable autoGRA: AppBootstrap sends readiness.");
        // Profile overrides must agree with the selected platform as well.
        var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL).Split(';');
        if (symbols.Count(s => s == "CrazyGamesPlatform_yg" || s == "YandexGamesPlatform_yg") != 1 ||
            !symbols.Contains(selected + "Platform_yg")) throw new BuildFailedException("Select exactly one platform symbol.");
        var profile = BuildProfile.GetActiveBuildProfile();
        if (profile != null && profile.scriptingDefines.Length > 0)
        {
            var overrides = profile.scriptingDefines;
            if (overrides.Count(s => s == "CrazyGamesPlatform_yg" || s == "YandexGamesPlatform_yg") != 1 ||
                !overrides.Contains(selected + "Platform_yg"))
                throw new BuildFailedException("Active Build Profile scripting defines override the selected platform. Update the profile to match Game/Platform.");
        }
        if (selected == "CrazyGames" && (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Brotli ||
            PlayerSettings.WebGL.decompressionFallback)) throw new BuildFailedException("CrazyGames requires Brotli without decompression fallback.");
        foreach (var path in AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/PluginYourGames/") && p.EndsWith(".jslib")))
        {
            var importer = (PluginImporter)AssetImporter.GetAtPath(path);
            var text = File.ReadAllText(path);
            if (path.Contains("/CrazyGames/") && !importer.DefineConstraints.Contains("CrazyGamesPlatform_yg"))
                throw new BuildFailedException("CrazyGames bridge must be constrained: " + path);
            if ((text.Contains("ysdk") || text.Contains("InterAdvShow();") || path.Contains("/YandexGames/")) &&
                !importer.DefineConstraints.Contains("YandexGamesPlatform_yg"))
                throw new BuildFailedException("Yandex bridge must be constrained: " + path);
        }
    }
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL) return;
        var html = File.ReadAllText(Path.Combine(report.summary.outputPath, "index.html"));
        if (CompiledPlatform == "CrazyGames" && (!html.Contains("crazygames-sdk-v3.js") || html.Contains("yandex") || html.Contains("ysdk")))
            throw new BuildFailedException("CrazyGames output contains an incorrect SDK template.");
        if (CompiledPlatform == "YandexGames" && html.Contains("crazygames-sdk"))
            throw new BuildFailedException("Yandex output contains CrazyGames SDK.");
    }
}
