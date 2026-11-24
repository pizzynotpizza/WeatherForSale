using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Builds the game for WebGL (itch.io / a phone browser) into <c>Builds/WebGL</c>:
    /// Brotli compression with the decompression fallback on (itch.io and plain static servers do not
    /// always send the Content-Encoding header), and the scenes enabled in the Build Settings.
    /// Menu: Weather For Sale / Build WebGL. Batch: <c>-executeMethod WeatherForSale.EditorTools.WebGlBuilder.BuildFromCommandLine</c>.
    /// Implements: story-001 criterion 7 (WebGL build to test on PC and phone).
    /// </summary>
    public static class WebGlBuilder
    {
        private const string OutputFolder = "Builds/WebGL";

        /// <summary>Menu entry: starts the build after the current editor operation finishes.</summary>
        [MenuItem("Weather For Sale/Build WebGL")]
        public static void BuildFromMenu()
        {
            EditorApplication.delayCall += () => Build();
        }

        /// <summary>Batch-mode entry point. Exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            EditorApplication.Exit(Build() ? 0 : 1);
        }

        /// <summary>Applies the WebGL settings and builds. Returns true on success.</summary>
        public static bool Build()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebGlBuilder] No enabled scenes in the Build Settings.");
                return false;
            }

            Directory.CreateDirectory(OutputFolder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[WebGlBuilder] " + summary.result + " in " + summary.totalTime.TotalSeconds.ToString("0") + " s, "
                      + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalErrors + " errors -> " + OutputFolder);
            return summary.result == BuildResult.Succeeded;
        }
    }
}
