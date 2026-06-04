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

        [MenuItem("Inflatable Idiots/Build Windows (x64)", false, 20)]
        public static void BuildWindows64()
        {
            Build(BuildTarget.StandaloneWindows64, "Windows", $"{ProductName}.exe");
        }

        // --- Other desktop targets, stubbed for later. MVP ships Windows only; mobile stays unwired. ---
        // [MenuItem("Inflatable Idiots/Build macOS", false, 21)]
        // public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "macOS", $"{ProductName}.app");
        //
        // [MenuItem("Inflatable Idiots/Build Linux (x64)", false, 22)]
        // public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Linux", $"{ProductName}.x86_64");

        static void Build(BuildTarget target, string platformFolder, string executableName)
        {
            string destination = Path.Combine(BuildRoot, platformFolder, executableName);
            bool confirmed = EditorUtility.DisplayDialog(
                "Build Player",
                $"Build a {target} player to:\n{destination}\n\n" +
                "This overwrites any previous build at that path and can take a while.",
                "Build",
                "Cancel");
            if (!confirmed) return;

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[InflatableIdiots] No enabled scenes in Build Settings. " +
                               "Run Inflatable Idiots → Build Test Scene first.");
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
            {
                CopySteamAppId(dir);
                Debug.Log($"[InflatableIdiots] Build succeeded: {summary.totalSize / (1024 * 1024)} MB at {summary.outputPath}");
            }
            else
                Debug.LogError($"[InflatableIdiots] Build {summary.result} with {summary.totalErrors} error(s). See log above.");
        }

        /// <summary>
        /// Steamworks.NET reads steam_appid.txt from the working directory at launch. The editor uses the
        /// project root (where the file lives), but a fresh standalone build has no copy next to the .exe —
        /// so SteamAPI.Init fails in builds unless we place it here. This is the #1 "Steam works in editor
        /// but not in the build" cause. (Do NOT ship this file in a real Steam release — the Steam client
        /// injects the real App ID; it's only for testing builds outside Steam, e.g. on App ID 480.)
        /// </summary>
        static void CopySteamAppId(string buildDir)
        {
            const string fileName = "steam_appid.txt";
            string src = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (!File.Exists(src))
            {
                Debug.LogWarning($"[InflatableIdiots] {fileName} not found at project root — Steam will fail to " +
                                 "init in this build. Create it (containing 480 for the spike) and rebuild.");
                return;
            }

            string dst = Path.Combine(buildDir, fileName);
            File.Copy(src, dst, overwrite: true);
            Debug.Log($"[InflatableIdiots] Copied {fileName} → {dst} (so SteamAPI.Init works in the build).");
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
