# Web platforms

The project uses Plugin Your Games (`YG2` / `IPlatformsYG2`) as its common platform API.
`YandexGamesPlatform_yg` selects the existing Yandex adapter. `CrazyGamesPlatform_yg`
selects the CrazyGames adapter, which calls the official HTML5 SDK v3 through a
small Unity JavaScript bridge. No additional Unity SDK package is required.

## Selecting and building

- `Game > Platform > Select Yandex Games` or `Select CrazyGames` selects the
  existing YG platform asset, symbols and matching WebGL template. Wait for compilation.
- `Game > Build > Yandex Games`, `CrazyGames`, or `Both platforms` selects the
  platform, waits for compilation, then builds the enabled Build Settings scenes
  (or the active Build Profile's scene override).
- Outputs: `Builds/YandexGames` and `Builds/CrazyGames`. The combined command builds
  Yandex first, then CrazyGames. A failed build stops the queue.
- The standard Build window also works after selecting a platform. Build Profile
  overrides must agree with that selection. The build validator rejects mismatched
  templates, symbols, initialization settings and CrazyGames compression settings.
- CrazyGames uses Brotli without Unity decompression fallback. Upload the generated
  folder/files directly, not a ZIP. This is a standard Unity WebGL build, not the
  optional CrazyGames custom build, so it does not generate `crazygames_build_report.json`.

## Runtime behavior

`AppBootstrap` waits for SDK initialization, applies the language and removes the
loading screen before signaling readiness. Automatic YG readiness is disabled.
CrazyGames uses `SDK.user.systemInfo.locale`; Russian locales select Russian,
and empty, unavailable or unsupported locales select English. Yandex continues to
use its SDK language. The shared localization events update scene text and cards.

Quiz start/end send gameplay start/stop. Interstitials are requested only at the end
of a round, with the existing YG interval. CrazyGames pauses input/audio while an ad
request is pending and handles errors and duplicate terminal callbacks. Platform
audio mute is respected. Yandex retains its startup banner. The CrazyGames adapter
does not request banners; its default shared banner methods are no-ops.

There is no persistent quiz progress or rewarded-ad feature in the current game.
No new progress schema or cross-platform save migration is introduced. YG local
language/session preferences remain compatible; they are not cloud progress saves.

## Validation and maintenance

Run `node Tools/test-crazygames-bridge.cjs` for native bridge callback checks.
Test both platform selections in Editor Play Mode. Editor ads/language use the
existing YG simulation settings; this does not exercise the live browser SDK.
Build results from the menu commands are recorded in each output folder as
`platform-build-result.json`; check the result and Unity Console before uploading.
Test the resulting builds in the respective portals' preview environments, including
locale, mute, focus loss during ads, ad failure and gameplay lifecycle.

Official references:
- https://docs.crazygames.com/sdk/intro/
- https://docs.crazygames.com/sdk/user/
- https://docs.crazygames.com/sdk/game/
- https://docs.crazygames.com/sdk/video-ads/

Vendor update maintenance: preserve the Yandex-only importer constraints on the
Yandex, interstitial and split language/environment bridges. Keep shared browser
language/environment functions separate. `ProcessBuild` skips its vendor template
patcher/archive hooks for CrazyGames. SDK updates may overwrite these small changes;
review them and rebuild both targets after updating Plugin Your Games.
