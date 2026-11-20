using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace JKit.Monetize.Ads.Editor
{
    /// <summary>
    /// Declares a scripting define for each API this copy of the plugin actually ships.
    /// Other packages (e.g. RCKit) should use these instead of guessing from the
    /// package version: alpha and beta share one version line but not the same code.
    ///
    /// An asmdef/csc.rsp define only applies inside the plugin's own assembly, so the
    /// defines go into Player Settings (Scripting Define Symbols) of each build target.
    /// </summary>
    [InitializeOnLoad]
    internal static class JNativeFeatureDefines
    {
        // Add a define here whenever a new public API needs feature detection.
        private static readonly string[] Features =
        {
            "JNATIVE_HAS_ADSOURCE", // NativeOverlay.OnAdPaidInfo + NativeAdSourceInfo
        };

        private static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone,
        };

        static JNativeFeatureDefines()
        {
            foreach (var target in Targets)
            {
                var current = PlayerSettings.GetScriptingDefineSymbols(target)
                    .Split(';')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                var missing = Features.Where(f => !current.Contains(f)).ToList();
                if (missing.Count == 0) continue;

                current.AddRange(missing);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", current));
            }
        }
    }
}
