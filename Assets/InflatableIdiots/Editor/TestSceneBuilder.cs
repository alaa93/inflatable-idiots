using System.IO;
using InflatableIdiots.Hose;
using InflatableIdiots.Level;
using InflatableIdiots.Player;
using InflatableIdiots.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InflatableIdiots.EditorTools
{
    public static class TestSceneBuilder
    {
        const string ScenePath = "Assets/InflatableIdiots/Scenes/PlayerTest.unity";
        const string InputAssetPath = "Assets/InflatableIdiots/Input/InflatableIdiots.inputactions";

        // Shared arena geometry (camera, ground, Fan Trap, goal, hazard, run controller) lives in
        // SceneBuildHelpers so this local scene and NetworkSpikeSceneBuilder stay in sync. Only the
        // local-play pieces — keyboard players, the hose, the AirPair, the offline physics driver — are
        // built here (the networked flow spawns players + hose from prefabs instead).
        [MenuItem("Inflatable Idiots/Build Test Scene", false, 0)]
        public static void Build()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Build Test Scene",
                $"This will generate a fresh test scene and save it to:\n{ScenePath}\n\n" +
                "Any existing scene at that path is overwritten, and your currently open scene is replaced.",
                "Build",
                "Cancel");
            if (!confirmed) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null)
            {
                Debug.LogError($"[InflatableIdiots] Input asset missing at {InputAssetPath}. Aborting.");
                return;
            }

            int groundLayer = SceneBuildHelpers.EnsureLayer(SceneBuildHelpers.GroundLayerName);
            LayerMask groundMask = 1 << groundLayer;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                var cameraGo = SceneBuildHelpers.CreateCamera();
                SceneBuildHelpers.CreateGround(groundLayer);
                SceneBuildHelpers.CreateFanTrap(groundLayer);
                SceneBuildHelpers.CreateGoal();
                SceneBuildHelpers.CreateRisingHazard();
                SceneBuildHelpers.CreateRunController();
                CreatePhysicsDriver();
                var pair = CreatePair();

                var p1 = CreatePlayer("Player 1", new Vector3(-2f, 0f, 0f), new Color(0.4f, 0.6f, 1f), "Keyboard P1", inputAsset);
                var p2 = CreatePlayer("Player 2", new Vector3( 2f, 0f, 0f), new Color(1f, 0.5f, 0.4f), "Keyboard P2", inputAsset);

                var c1 = p1.GetComponent<InflatableCharacter>();
                var c2 = p2.GetComponent<InflatableCharacter>();
                c1.Initialize(pair, playerOne: true,  ground: groundMask);
                c2.Initialize(pair, playerOne: false, ground: groundMask);
                pair.Initialize(c1, c2);

                var shaker = cameraGo.AddComponent<CameraShaker>();
                shaker.Initialize(pair);

                var hose = CreateHose(p1, p2);

                EditorUtility.SetDirty(c1);
                EditorUtility.SetDirty(c2);
                EditorUtility.SetDirty(pair);
                EditorUtility.SetDirty(shaker);
                EditorUtility.SetDirty(hose);
                EditorSceneManager.MarkSceneDirty(scene);

                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
                if (!ok)
                {
                    Debug.LogError($"[InflatableIdiots] SaveScene returned false for {ScenePath}.");
                    return;
                }
                SceneBuildHelpers.EnsureSceneInBuildSettings(ScenePath);
                Debug.Log($"[InflatableIdiots] Built test scene at {ScenePath}. P1: WASD / LeftCtrl / LeftShift. P2: Arrows / RightCtrl / RightShift. Or plug in two gamepads.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InflatableIdiots] Test scene build failed mid-construction. See exception below; scene NOT saved.");
                Debug.LogException(e);
            }
        }

        static AirPair CreatePair()
        {
            var go = new GameObject("AirPair");
            return go.AddComponent<AirPair>();
        }

        static void CreatePhysicsDriver()
        {
            // Keeps physics simulating in this non-networked scene even when a prior in-editor networked run
            // left Physics2D in Script mode (FishNet's prediction requirement, which leaks across the editor
            // session). Without it every Rigidbody2D hangs frozen — the "PlayerTest physics freeze". Plain
            // GameObject, no transform needs.
            var go = new GameObject("Physics Driver");
            go.AddComponent<LocalPhysicsDriver>();
        }

        static HoseController CreateHose(GameObject p1, GameObject p2)
        {
            var go = new GameObject("Hose");
            var hose = go.AddComponent<HoseController>(); // [RequireComponent] adds the LineRenderer
            hose.Initialize(p1.GetComponent<Rigidbody2D>(), p2.GetComponent<Rigidbody2D>());
            // Suppress the default 2-point line at the origin in edit mode; the rope/positions are
            // rebuilt at play. A Gizmo on HoseController previews the straight tether in the Scene view.
            go.GetComponent<LineRenderer>().positionCount = 0;
            return hose;
        }

        static GameObject CreatePlayer(string name, Vector3 pos, Color color, string scheme, InputActionAsset inputAsset)
        {
            // Build the GameObject inactive so PlayerInput.OnEnable (auto-added by
            // InflatableCharacter's [RequireComponent]) doesn't fire before we configure it.
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
            col.edgeRadius = 0.05f;   // rounded corners so the box slides over ledges instead of snagging

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
