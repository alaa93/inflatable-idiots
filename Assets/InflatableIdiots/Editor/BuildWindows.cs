using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace InflatableIdiots.EditorTools
{
    /// <summary>
    /// One-click Windows Standalone (x64) build. The MVP ships Windows only; macOS/Linux are stubbed as
    /// commented placeholders below, and mobile is intentionally NOT wired (deferred). Scenes come from
    /// Build Settings, which TestSceneBuilder populates — so build the test scene first.
    /// </summary>
    public static class BuildWindows
    {
        const string ProductName = "InflatableIdiots";
        const string BuildRoot = "Builds";

        [MenuItem("Tools/Inflatable Idiots/Build Windows (x64)")]
        public static void BuildWindows64()
        {
            Build(BuildTarget.StandaloneWindows64, "Windows", $"{ProductName}.exe");
        }

        // --- Other desktop targets, stubbed for later. MVP ships Windows only; mobile stays unwired. ---
        // [MenuItem("Tools/Inflatable Idiots/Build macOS")]
        // public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "macOS", $"{ProductName}.app");
        //
        // [MenuItem("Tools/Inflatable Idiots/Build Linux (x64)")]
        // public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Linux", $"{ProductName}.x86_64");

        static void Build(BuildTarget target, string platformFolder, string executableName)
        {
            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[InflatableIdiots] No enabled scenes in Build Settings. " +
                               "Run Tools → Inflatable Idiots → Build Test Scene first.");
                return;
            }

            string dir = Path.Combine(BuildRoot, platformFolder);
            Directory.CreateDirectory(dir);
            string outputPath = Path.Combine(dir, executableName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            };

            Debug.Log($"[InflatableIdiots] Building {target} → {outputPath} ({scenes.Length} scene(s))...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[InflatableIdiots] Build succeeded: {summary.totalSize / (1024 * 1024)} MB at {summary.outputPath}");
            else
                Debug.LogError($"[InflatableIdiots] Build {summary.result} with {summary.totalErrors} error(s). See log above.");
        }

        static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }
    }
}
