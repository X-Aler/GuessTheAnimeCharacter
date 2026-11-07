# Platform services

This project follows `D:\UnityGames\Shred the Sercrets`:

- `Contract/IPlatformService.cs` and `Platform.Contract.asmdef`: game-owned SDK-independent contract.
- `Runtime/PlatformServices.cs`: factory, initialization, readiness, normalized language and events.
- `Runtime/YandexPlatformService.cs`: Plugin Your Games adapter.
- `Runtime/CrazyGamesPlatformService.cs`: official CrazyGames Unity SDK adapter.
- `Editor/PlatformBuildMenu.cs`: platform selection, validation and WebGL builds.
- `Editor/PlatformBuildWindow.cs`: dedicated selector and build window.

Gameplay, bootstrap and localized text use `GamePlatform`, never the vendor SDKs.
The quiz has no persistent progress or rewarded feature; no artificial save schema
or cross-platform migration has been introduced. Existing Yandex local preferences
remain in Plugin Your Games storage.

## Select and build

Open **Build > Platform > Build Window**. Choose a platform and apply it, or use
the build button to select, wait for compilation and build automatically.
Equivalent select/build commands are under **Build > Platform**.

`PLATFORM_CRAZYGAMES` selects CrazyGames; its absence selects Yandex. The selector
clears the active Build Profile to prevent embedded settings from overriding the
selection. It preserves enabled global Build Settings scenes, sets the template,
updates Editor/WebGL symbols and moves inactive SDK `Resources` to `PlatformResources`
through AssetDatabase. Resource folder moves preserve GUIDs. Selecting again after
activating a Build Profile returns to this shared configuration.

Outputs are `Builds/YandexGames` and `Builds/CrazyGames`. The optional both-platform
queue builds Yandex first, then CrazyGames, with script reload between them. A failed
build stops the queue. Results are recorded as `platform-build-result.json`.

CrazyGames uses Brotli without decompression fallback. Upload all generated files
directly with game type **Unity**, without ZIP. This is a standard WebGL build;
the optional SDK custom release builder and its `crazygames_build_report.json` are
not required for this workflow.

## Startup and SDK isolation

`AppBootstrap.Start` initializes the platform after vendor runtime initialization,
waits for readiness and language, and only then reveals the menu. A 30-second
initialization timeout keeps gameplay blocked and displays an error when a loading
label is present. Yandex readiness/banner are sent through the adapter. The
CrazyGames Unity loader manages loading notifications; the game sends gameplay
start/stop when entering/leaving a playable quiz round.

CrazyGames Unity SDK **5.36.0** was imported from the official package, excluding
demo assets. Its native `Assets/Plugins/crazySDK.jslib` downloads and initializes
the SDK inside the framework. No startup code depends on `index.html`, which the
portal ignores for Unity submissions. The local HTML template only loads Unity.

`Vendor.Yandex`, `Vendor.CrazyGames`, `Vendor.CrazyGames.Editor` and
`CrazyGamesDefineManager` have matching define constraints; all native plugins
are also constrained. Yandex module symbols stay configured but its assembly,
resources and native code are absent from the CrazyGames build. Direct SDK scene
components were replaced with game-owned localization. YG automatic symbol/settings
updates are disabled so they cannot undo the shared selection.

CrazyGames interstitial requests have a 180-second minimum interval and block quiz
input while pending. The official SDK preserves/restores time scale and audio.
Platform mute is applied separately. Yandex retains its own ad timer and banner.
No gameplay-start event is sent on ad completion while the results screen is shown.

## Verify and maintain

- Test both selections in Editor and inspect errors and advancing frames.
- Run `node Tools/test-crazygames-unity-bridge.cjs` for native startup without custom HTML.
- Run `node Tools/check-platform-build.cjs Builds/CrazyGames CrazyGames` (or YandexGames)
  after each build to inspect compressed framework/data and build results.
- Test the uploaded Unity preview for startup, SDK detection, language, focus and ads.
  Editor simulation and successful builds do not prove portal compatibility.

When updating SDKs, preserve vendor asmdefs, native-plugin constraints, YG
`UNITY_EDITOR` guards and the settings that disable automatic symbol/project
overrides. Recheck both selections and builds after updates. The reference project
defines the architecture, not this game's data schema.

Official references: https://docs.crazygames.com/sdk/intro/ and
https://docs.crazygames.com/resources/optimization-tips/.
