using System.IO;
using InflatableIdiots.Hose;
using InflatableIdiots.Level;
using InflatableIdiots.Net;
using InflatableIdiots.Player;
using InflatableIdiots.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InflatableIdiots.EditorTools
{
    /// <summary>
    /// Generates the full "Ascent" graybox playtest — the whole co-op puzzle ladder (tutorial → vertical climb →
    /// dual-switch → elevator → kite+anchor → climax → goal), code-driven so one menu click rebuilds it. Two
    /// flavours from the same <see cref="PlaytestLayout"/>:
    ///
    /// • <b>Local</b> (<c>PlaytestLocal.unity</c>) — keyboard 2P like <see cref="TestSceneBuilder"/>'s PlayerTest.
    ///   The fast-iteration scene for tuning puzzle feel/solvability solo before the wire.
    /// • <b>Net</b> (<c>PlaytestNet.unity</c>) — assembled from the FishNet prefabs like
    ///   <see cref="NetworkSpikeSceneBuilder"/>; players + hose/air/run-link spawn at runtime. The 2-PC test.
    ///
    /// Both share the new host-authoritative puzzle-flag layer (see <see cref="RunController"/> /
    /// <see cref="NetworkRunController"/>), the gated <see cref="Hazards.FanZone"/>, the station-relenting
    /// <see cref="Hazards.RisingHazard"/>, and the two-player <see cref="FollowCamera"/>.
    /// </summary>
    public static class PlaytestSceneBuilder
    {
        const string LocalScenePath = "Assets/InflatableIdiots/Scenes/PlaytestLocal.unity";
        const string NetScenePath = "Assets/InflatableIdiots/Scenes/PlaytestNet.unity";
        const string InputAssetPath = "Assets/InflatableIdiots/Input/InflatableIdiots.inputactions";
        const string ManagerPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkManager.prefab";
        const string PlayerPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkPlayer.prefab";
        const string LinkPrefabPath = "Assets/InflatableIdiots/Prefabs/NetworkLink.prefab";

        // When true (headless -batchmode entry below), skip the confirm/result dialogs so generation can't hang.
        static bool batch;

        /// <summary>
        /// Headless entry point for `Unity -batchmode -executeMethod`: builds BOTH playtest scenes without any
        /// interactive dialogs. Used to regenerate + compile-verify the scenes while the editor is closed.
        /// </summary>
        public static void BuildBothBatch()
        {
            batch = true;
            try { BuildLocal(); BuildNet(); }
            finally { batch = false; }
        }

        // ---------------------------------------------------------------- LOCAL ----------------------------------------------------------------

        [MenuItem("Inflatable Idiots/Build Playtest - Local (keyboard 2P)", false, 5)]
        public static void BuildLocal()
        {
            if (!Confirm(LocalScenePath, "local keyboard 2P")) return;
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null) { Debug.LogError($"[InflatableIdiots] Input asset missing at {InputAssetPath}. Aborting."); return; }

            int groundLayer = SceneBuildHelpers.EnsureLayer(SceneBuildHelpers.GroundLayerName);
            LayerMask groundMask = 1 << groundLayer;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                var cameraGo = CreateFollowCamera();
                Vector3[] spawns = PlaytestLayout.Build(groundLayer);
                SceneBuildHelpers.CreateRunController();
                CreatePhysicsDriver();
                var pair = CreatePair();

                var p1 = CreatePlayer("Player 1", spawns[0], new Color(0.4f, 0.6f, 1f), "Keyboard P1", inputAsset);
                var p2 = CreatePlayer("Player 2", spawns[1], new Color(1f, 0.5f, 0.4f), "Keyboard P2", inputAsset);

                var c1 = p1.GetComponent<InflatableCharacter>();
                var c2 = p2.GetComponent<InflatableCharacter>();
                c1.Initialize(pair, playerOne: true, ground: groundMask);
                c2.Initialize(pair, playerOne: false, ground: groundMask);
                pair.Initialize(c1, c2);

                CreateHose(p1, p2);

                EditorSceneManager.MarkSceneDirty(scene);
                Directory.CreateDirectory(Path.GetDirectoryName(LocalScenePath));
                if (!EditorSceneManager.SaveScene(scene, LocalScenePath))
                { Debug.LogError($"[InflatableIdiots] SaveScene returned false for {LocalScenePath}."); return; }

                SceneBuildHelpers.EnsureSceneInBuildSettings(LocalScenePath);
                Debug.Log($"[InflatableIdiots] Built LOCAL playtest at {LocalScenePath}. " +
                          "P1: WASD / LeftCtrl / LeftShift. P2: Arrows / RightCtrl / RightShift. L toggles the lava.");
                if (!batch) EditorUtility.DisplayDialog("Build Playtest - Local", "Done — open PlaytestLocal.unity and press Play.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InflatableIdiots] Local playtest build failed mid-construction; scene NOT saved.");
                Debug.LogException(e);
            }
        }

        // ----------------------------------------------------------------- NET -----------------------------------------------------------------

        [MenuItem("Inflatable Idiots/Build Playtest - Net (FishNet 2-PC)", false, 6)]
        public static void BuildNet()
        {
            if (!Confirm(NetScenePath, "networked (FishNet, assembled from the existing prefabs)")) return;
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var linkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LinkPrefabPath);
            if (managerPrefab == null || playerPrefab == null || linkPrefab == null)
            {
                Debug.LogError($"[InflatableIdiots] Missing a required prefab. Found: NetworkManager={managerPrefab != null}, " +
                               $"NetworkPlayer={playerPrefab != null}, NetworkLink={linkPrefab != null}. Aborting.");
                return;
            }

            int groundLayer = SceneBuildHelpers.EnsureLayer(SceneBuildHelpers.GroundLayerName);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                CreateFollowCamera();
                Vector3[] spawns = PlaytestLayout.Build(groundLayer);
                SceneBuildHelpers.CreateRunController();

                // NetworkManager prefab instance carries all the FishNet config (TimeManager PhysicsMode,
                // FishySteamworks P2P, DefaultPrefabObjects). Instantiate as a prefab instance so edits propagate.
                PrefabUtility.InstantiatePrefab(managerPrefab, scene);

                // NetSpike harness: Steam lifecycle + host/Join-by-ID GUI + per-connection spawner.
                var netSpike = new GameObject("NetSpike");
                netSpike.AddComponent<NetworkBootstrap>();
                var spawner = netSpike.AddComponent<NetPlayerSpawner>();
                var so = new SerializedObject(spawner);
                so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
                so.FindProperty("linkPrefab").objectReferenceValue = linkPrefab;
                so.FindProperty("spawnHoseLink").boolValue = true; // spawn the hose/air/run link once both join
                var sp = so.FindProperty("spawnPoints");
                sp.arraySize = spawns.Length;
                for (int i = 0; i < spawns.Length; i++) sp.GetArrayElementAtIndex(i).vector3Value = spawns[i];
                so.ApplyModifiedProperties();

                var hudGo = new GameObject("FPSCounter");
                var fps = hudGo.AddComponent<FpsCounter>();
                var fpsSo = new SerializedObject(fps);
                fpsSo.FindProperty("corner").enumValueIndex = 1; // TopRight
                fpsSo.FindProperty("fontSize").intValue = 24;
                fpsSo.ApplyModifiedProperties();

                EditorUtility.SetDirty(spawner);
                EditorUtility.SetDirty(fps);
                EditorSceneManager.MarkSceneDirty(scene);
                Directory.CreateDirectory(Path.GetDirectoryName(NetScenePath));
                if (!EditorSceneManager.SaveScene(scene, NetScenePath))
                { Debug.LogError($"[InflatableIdiots] SaveScene returned false for {NetScenePath}."); return; }

                SceneBuildHelpers.EnsureSceneInBuildSettings(NetScenePath, asStartup: true);
                Debug.Log($"[InflatableIdiots] Built NET playtest at {NetScenePath}. Play → Host; partner uses " +
                          "'Join by ID' with the host's SteamID. For a 2-PC build: Inflatable Idiots → Build Windows (x64). L toggles lava.");
                if (!batch) EditorUtility.DisplayDialog("Build Playtest - Net",
                    "Done — PlaytestNet.unity is the startup scene. Play → Host on one PC, Join by ID on the other " +
                    "(or Build Windows for a standalone 2-PC test).", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InflatableIdiots] Net playtest build failed mid-construction; scene NOT saved.");
                Debug.LogException(e);
            }
        }

        // ---------------------------------------------------------------- shared ----------------------------------------------------------------

        static bool Confirm(string path, string flavour)
        {
            if (batch) return true; // headless: no interactive dialog
            return EditorUtility.DisplayDialog("Build Playtest",
                $"Generate the fresh {flavour} playtest scene and save it to:\n{path}\n\n" +
                "Any existing scene at that path is overwritten, and your currently open scene is replaced.",
                "Build", "Cancel");
        }

        static GameObject CreateFollowCamera()
        {
            var go = SceneBuildHelpers.CreateCamera(); // ortho camera + AudioListener (FollowCamera drives it at runtime)
            go.AddComponent<FollowCamera>();
            return go;
        }

        static AirPair CreatePair() => new GameObject("AirPair").AddComponent<AirPair>();

        static void CreatePhysicsDriver() => new GameObject("Physics Driver").AddComponent<LocalPhysicsDriver>();

        static HoseController CreateHose(GameObject p1, GameObject p2)
        {
            var go = new GameObject("Hose");
            var hose = go.AddComponent<HoseController>(); // [RequireComponent] adds the LineRenderer
            hose.Initialize(p1.GetComponent<Rigidbody2D>(), p2.GetComponent<Rigidbody2D>());
            go.GetComponent<LineRenderer>().positionCount = 0; // suppress the stray origin line in edit mode
            return hose;
        }

        static GameObject CreatePlayer(string name, Vector3 pos, Color color, string scheme, InputActionAsset inputAsset)
        {
            // Build inactive so PlayerInput.OnEnable doesn't fire before configuration (matches TestSceneBuilder).
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SceneBuildHelpers.LoadSprite(SceneBuildHelpers.PlayerSpritePath);
            sr.color = color;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            col.edgeRadius = 0.05f;

            go.AddComponent<InflatableCharacter>();
            var pi = go.GetComponent<PlayerInput>();
            pi.notificationBehavior = PlayerNotifications.SendMessages;
            pi.defaultControlScheme = scheme;
            pi.defaultActionMap = "Player";
            pi.neverAutoSwitchControlSchemes = true;
            pi.actions = inputAsset;

            go.SetActive(true);
            return go;
        }
    }
}
