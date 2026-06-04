using System.IO;
using InflatableIdiots.Net;
using InflatableIdiots.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InflatableIdiots.EditorTools
{
    /// <summary>
    /// Regenerates the networked spike scene <c>NetworkSpikeTest.unity</c> from code — the sibling of
    /// <see cref="TestSceneBuilder"/>, codifying the previously-hand-wired scene so it stops drifting and
    /// stays a one-click rebuild.
    ///
    /// The fragile FishNet wiring (prediction, state-forwarding, NetworkTransform config, TimeManager
    /// PhysicsMode, FishySteamworks P2P) lives in inspector-authored PREFABS — `NetworkManager`,
    /// `NetworkPlayer`, `NetworkLink` — many of whose settings are private serialized fields with no public
    /// C# setter. So this builder does NOT regenerate the prefabs; it ASSEMBLES the scene from them: the same
    /// Fan Trap arena as the local scene (<see cref="SceneBuildHelpers"/>) plus the networking objects. Networked
    /// players + the hose/air-budget link are spawned at runtime from the prefabs by NetPlayerSpawner — there
    /// are no scene-placed players (contrast TestSceneBuilder's local players).
    ///
    /// NOTE: this lays out the full Fan Trap arena, but the hazards' NETCODE correctness (server-authoritative
    /// FanZone forces, replicated GoalZone/RunController win-state) is a separate port still to be validated —
    /// the spike was proven on a bare ground+wall arena. See the Roadmap / Networking Spike vault notes.
    /// </summary>
    public static class NetworkSpikeSceneBuilder
    {
        const string ScenePath = "Assets/InflatableIdiots/Scenes/NetworkSpikeTest.unity";
        const string ManagerPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkManager.prefab";
        const string PlayerPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkPlayer.prefab";
        const string LinkPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkLink.prefab";

        [MenuItem("Inflatable Idiots/Build Network Spike Scene", false, 1)]
        public static void Build()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Build Network Spike Scene",
                $"This will generate a fresh networked test scene and save it to:\n{ScenePath}\n\n" +
                "Any existing scene at that path is overwritten, and your currently open scene is replaced. " +
                "The NetworkManager / NetworkPlayer / NetworkLink prefabs are reused as-is (not regenerated).",
                "Build",
                "Cancel");
            if (!confirmed) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Load the inspector-authored prefabs this scene is assembled from. Abort loudly if any is missing
            // rather than saving a half-wired scene.
            var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var linkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LinkPrefabPath);
            if (managerPrefab == null || playerPrefab == null || linkPrefab == null)
            {
                Debug.LogError($"[InflatableIdiots] Missing a required prefab. Found: " +
                               $"NetworkManager={(managerPrefab != null)}, NetworkPlayer={(playerPrefab != null)}, " +
                               $"NetworkLink={(linkPrefab != null)}. Aborting (none of {ScenePath} written).");
                return;
            }

            int groundLayer = SceneBuildHelpers.EnsureLayer(SceneBuildHelpers.GroundLayerName);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                // --- Arena geometry (shared with the local scene) ---
                var cameraGo = SceneBuildHelpers.CreateCamera();
                // Camera shake is driven by a local AirPair's stalemate intensity, which networked play has no
                // scene instance of — added to match the arena but left unwired (CameraShaker no-ops on null).
                cameraGo.AddComponent<CameraShaker>();
                SceneBuildHelpers.CreateGround(groundLayer);
                SceneBuildHelpers.CreateFanTrap(groundLayer);
                SceneBuildHelpers.CreateGoal();
                SceneBuildHelpers.CreateRisingHazard();
                SceneBuildHelpers.CreateRunController();

                // --- Networking ---
                // NetworkManager carries all the FishNet config (TimeManager PhysicsMode, FishySteamworks P2P,
                // ObserverManager, the DefaultPrefabObjects collection). Instantiate it as a PREFAB INSTANCE so
                // future prefab edits keep propagating into this scene.
                PrefabUtility.InstantiatePrefab(managerPrefab, scene);

                // NetSpike: the spike harness. NetworkBootstrap owns the Steam lifecycle + host/join GUI;
                // NetPlayerSpawner replaces FishNet's built-in PlayerSpawner (server-spawns one NetworkPlayer
                // per connection, then the NetworkLink once both have joined). Both self-init at runtime, so
                // there is nothing to wire on NetworkBootstrap. NetPlayerSpawner's references are private
                // [SerializeField]s — set them through SerializedObject.
                var netSpike = new GameObject("NetSpike");
                netSpike.AddComponent<NetworkBootstrap>();
                var spawner = netSpike.AddComponent<NetPlayerSpawner>();

                var so = new SerializedObject(spawner);
                so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
                so.FindProperty("linkPrefab").objectReferenceValue = linkPrefab;
                so.FindProperty("spawnHoseLink").boolValue = true; // spawn the hose/air-budget link once both join
                var spawnPoints = so.FindProperty("spawnPoints");
                spawnPoints.arraySize = 2;
                spawnPoints.GetArrayElementAtIndex(0).vector3Value = new Vector3(-2f, 0f, 0f);
                spawnPoints.GetArrayElementAtIndex(1).vector3Value = new Vector3(2f, 0f, 0f);
                so.ApplyModifiedProperties();

                // Perf HUD (FPS + role + tick + RTT). Top-right to stay clear of NetworkBootstrap's top-left GUI.
                var hudGo = new GameObject("FPSCounter");
                var fps = hudGo.AddComponent<FpsCounter>();
                var fpsSo = new SerializedObject(fps);
                fpsSo.FindProperty("corner").enumValueIndex = 1; // TopRight
                fpsSo.FindProperty("fontSize").intValue = 24;
                fpsSo.ApplyModifiedProperties();

                EditorUtility.SetDirty(spawner);
                EditorUtility.SetDirty(fps);
                EditorSceneManager.MarkSceneDirty(scene);

                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
                if (!ok)
                {
                    Debug.LogError($"[InflatableIdiots] SaveScene returned false for {ScenePath}.");
                    return;
                }
                SceneBuildHelpers.EnsureSceneInBuildSettings(ScenePath, asStartup: true);
                Debug.Log($"[InflatableIdiots] Built network spike scene at {ScenePath}. " +
                          "Play → Host to spawn the host player; a 2nd connection spawns the partner + hose. " +
                          "For a 2-PC build run Inflatable Idiots → Build Windows (x64).");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InflatableIdiots] Network spike scene build failed mid-construction. See exception below; scene NOT saved.");
                Debug.LogException(e);
            }
        }
    }
}
