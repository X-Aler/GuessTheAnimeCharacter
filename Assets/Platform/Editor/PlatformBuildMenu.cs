using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace GamePlatform.Editor
{
    [InitializeOnLoad]
    public sealed class PlatformBuildMenu : IPreprocessBuildWithReport
    {
        private const string Symbol = "PLATFORM_CRAZYGAMES";
        private const string PendingKey = "AnimeQuiz.ServicePlatformBuild";
        private const string ReloadKey = "AnimeQuiz.ServicePlatformReload";
        private const string NextKey = "AnimeQuiz.ServicePlatformNext";
#if PLATFORM_CRAZYGAMES
        public const string CompiledPlatform = "CrazyGames";
#else
        public const string CompiledPlatform = "YandexGames";
#endif
        public static string SelectedPlatform => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL)
            .Split(';').Contains(Symbol) ? "CrazyGames" : "YandexGames";
        public static bool IsBusy => EditorApplication.isCompiling || EditorApplication.isUpdating ||
            BuildPipeline.isBuildingPlayer || SessionState.GetBool(ReloadKey, false) ||
            SessionState.GetString(PendingKey, "") != "" || SessionState.GetString(NextKey, "") != "";

        static PlatformBuildMenu() { EditorApplication.update += ContinueBuild; }

        [DidReloadScripts]
        private static void Reloaded()
        {
            SessionState.SetBool(ReloadKey, false);
        }

        [MenuItem("Build/Platform/Select Yandex Games")]
        public static void SelectYandex() => Select("YandexGames");
        [MenuItem("Build/Platform/Select CrazyGames")]
        public static void SelectCrazy() => Select("CrazyGames");
        [MenuItem("Build/Platform/Build selected platform")]
        public static void BuildSelected() => Queue(SelectedPlatform);
        [MenuItem("Build/Platform/Build both platforms")]
        public static void BuildBoth() => Queue("YandexGames,CrazyGames");

        public static void Select(string platform)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Stop Play Mode and the current build before selecting a platform.");
            if (platform != "CrazyGames" && platform != "YandexGames") throw new ArgumentException(platform);
            bool crazy = platform == "CrazyGames";
            var targets = new[] { NamedBuildTarget.WebGL,
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup) }.Distinct().ToArray();
            var previousSymbols = targets.ToDictionary(t => t, t => PlayerSettings.GetScriptingDefineSymbols(t));
            BuildProfile.SetActiveBuildProfile(null);
            SetResources("Assets/CrazySDK", crazy);
            SetResources("Assets/PluginYourGames", !crazy);
            PlayerSettings.WebGL.template = "PROJECT:" + platform;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            foreach (var target in targets)
            {
                var symbols = (previousSymbols[target] + ";" + PlayerSettings.GetScriptingDefineSymbols(target)).Split(';')
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s != Symbol && s != "CrazyGamesPlatform_yg").Distinct().ToList();
                if (!symbols.Contains("YandexGamesPlatform_yg")) symbols.Add("YandexGamesPlatform_yg");
                if (crazy) symbols.Add(Symbol);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            }
            AssetDatabase.SaveAssets();
            SessionState.SetBool(ReloadKey, true);
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log("Selected " + platform + ". Waiting for script reload before building.");
        }

        private static void SetResources(string root, bool enabled)
        {
            var source = root + (enabled ? "/PlatformResources" : "/Resources");
            var destination = root + (enabled ? "/Resources" : "/PlatformResources");
            if (!AssetDatabase.IsValidFolder(source)) return;
            if (AssetDatabase.IsValidFolder(destination)) throw new BuildFailedException("Duplicate SDK resources: " + root);
            var error = AssetDatabase.MoveAsset(source, destination);
            if (error != "") throw new BuildFailedException(error);
        }

        public static void Build(string platform) => Queue(platform);
        private static void Queue(string platforms)
        {
            if (IsBusy || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Wait for the current compilation/build, and stop Play Mode first.");
            Select(platforms.Split(',')[0]);
            SessionState.SetString(PendingKey, platforms);
        }

        private static void ContinueBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer ||
                SessionState.GetBool(ReloadKey, false)) return;
            var next = SessionState.GetString(NextKey, "");
            if (next != "") { SessionState.EraseString(NextKey); Queue(next); return; }
            var pending = SessionState.GetString(PendingKey, "");
            if (pending == "") return;
            SessionState.EraseString(PendingKey);
            try
            {
                if (EditorUtility.scriptCompilationFailed) throw new BuildFailedException("Fix compilation errors before building.");
                var platforms = pending.Split(',');
                if (CompiledPlatform != platforms[0]) throw new BuildFailedException("The selected platform has not compiled.");
                var folder = "Builds/" + platforms[0];
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = folder, target = BuildTarget.WebGL, options = BuildOptions.None
                });
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "platform-build-result.json"), JsonUtility.ToJson(new Result {
                    platform = platforms[0], result = report.summary.result.ToString(), errors = (int)report.summary.totalErrors,
                    utc = DateTime.UtcNow.ToString("O") }, true));
                if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + platforms[0]);
                if (platforms.Length > 1) SessionState.SetString(NextKey, string.Join(",", platforms.Skip(1)));
            }
            catch (Exception error) { Debug.LogException(error); }
        }

        [Serializable] private class Result { public string platform, result, utc; public int errors; }
        public int callbackOrder => -2000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            ValidateConfiguration();
        }

        public static void ValidateConfiguration()
        {
            if (SelectedPlatform != CompiledPlatform || PlayerSettings.WebGL.template != "PROJECT:" + CompiledPlatform)
                throw new BuildFailedException("Platform, compiled code and template disagree. Select the platform again.");
            if (BuildProfile.GetActiveBuildProfile() != null)
                throw new BuildFailedException("Use Build/Platform to clear Build Profile overrides before building.");
            bool crazy = CompiledPlatform == "CrazyGames";
            foreach (var pair in new[] { (root: "Assets/CrazySDK", active: crazy), (root: "Assets/PluginYourGames", active: !crazy) })
            {
                if (AssetDatabase.IsValidFolder(pair.root + "/Resources") != pair.active ||
                    AssetDatabase.IsValidFolder(pair.root + "/PlatformResources") == pair.active)
                    throw new BuildFailedException("Incorrect SDK resource selection: " + pair.root);
            }
            if (crazy && (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Brotli || PlayerSettings.WebGL.decompressionFallback))
                throw new BuildFailedException("CrazyGames requires Brotli without decompression fallback.");
            if (!EditorBuildSettings.scenes.Any(s => s.enabled)) throw new BuildFailedException("No enabled build scenes.");
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.EndsWith(".jslib") &&
                (p.StartsWith("Assets/PluginYourGames/") || p == "Assets/Plugins/crazySDK.jslib")))
            {
                var importer = (PluginImporter)AssetImporter.GetAtPath(path);
                var constraint = path.StartsWith("Assets/PluginYourGames/") ? "!" + Symbol : Symbol;
                if (!importer.DefineConstraints.Contains(constraint)) throw new BuildFailedException("Missing SDK constraint: " + path);
            }
#if !PLATFORM_CRAZYGAMES
            if (!YG.YG2.infoYG.Basic.syncInitSDK || YG.YG2.infoYG.Basic.autoGRA || YG.YG2.infoYG.Basic.autoDefineSymbols || YG.YG2.infoYG.Basic.autoApplySettings)
                throw new BuildFailedException("YG automatic settings/symbols/readiness must stay disabled; syncInitSDK must stay enabled.");
#endif
        }
    }
}
