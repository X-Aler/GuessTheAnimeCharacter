using System.Collections;
using UnityEngine;

namespace GamePlatform
{
    public sealed class PlatformRuntime : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!PlatformServices.IsReady && PlatformServices.Error == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!PlatformServices.IsReady && PlatformServices.Error == null)
                PlatformServices.Fail("Platform SDK initialization timed out. Please reload the page.");
        }

        private void OnDestroy() => PlatformServices.Shutdown();
    }
}
