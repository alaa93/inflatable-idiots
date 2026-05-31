using System.IO;
using InflatableIdiots.Hazards;
using InflatableIdiots.Hose;
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
        const string GroundLayerName = "Ground";

        // Default package sprites (both ~1 unit native @ 256 PPU). 9-Sliced is a rounded rect — fits the
        // capsule body and reads as "inflatable"; Square is a solid fill for the ground. Loaded as assets
        // (not runtime Sprite.Create) so the reference actually serializes into the saved scene.
        const string PlayerSpritePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/9-Sliced.png";
        const string GroundSpritePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Square.png";

        [MenuItem("Tools/Inflatable Idiots/Build Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null)
            {
                Debug.LogError($"[InflatableIdiots] Input asset missing at {InputAssetPath}. Aborting.");
                return;
            }

            int groundLayer = EnsureLayer(GroundLayerName);
            LayerMask groundMask = 1 << groundLayer;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                var cameraGo = CreateCamera();
                CreateGround(groundLayer);
                CreateFanTrap(groundLayer);
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
                Debug.Log($"[InflatableIdiots] Built test scene at {ScenePath}. P1: WASD / LeftCtrl / LeftShift. P2: Arrows / RightCtrl / RightShift. Or plug in two gamepads.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InflatableIdiots] Test scene build failed mid-construction. See exception below; scene NOT saved.");
                Debug.LogException(e);
            }
        }

        static GameObject CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0f, 2f, -10f);
            go.AddComponent<AudioListener>();
            return go;
        }

        static void CreateGround(int groundLayer)
        {
            var go = CreateColoredQuad("Ground", new Vector3(0f, -3f, 0f), new Vector3(20f, 1f, 1f), new Color(0.3f, 0.6f, 0.3f), LoadSprite(GroundSpritePath));
            go.layer = groundLayer;
            go.AddComponent<BoxCollider2D>();
        }

        static void CreateFanTrap(int groundLayer)
        {
            // Fan housing — a solid pedestal the updraft rises from (Ground layer so players can stand on it).
            var housing = CreateColoredQuad("Fan Housing", new Vector3(5f, -2.25f, 0f), new Vector3(2.5f, 0.5f, 1f),
                new Color(0.25f, 0.25f, 0.3f), LoadSprite(GroundSpritePath));
            housing.layer = groundLayer;
            housing.AddComponent<BoxCollider2D>();

            // Updraft column — translucent trigger zone carrying the FanZone force. Rises from the housing top
            // (y -2.25) to y 4.75. Default layer (NOT Ground) so it never registers as standable ground.
            var column = CreateColoredQuad("Fan Updraft", new Vector3(5f, 1.25f, 0f), new Vector3(2.5f, 7f, 1f),
                new Color(0.45f, 0.8f, 1f, 0.18f), LoadSprite(GroundSpritePath));
            column.GetComponent<SpriteRenderer>().sortingOrder = -1; // behind the players
            var trigger = column.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            column.AddComponent<FanZone>();

            // Perch — a ledge offset to the side (so it doesn't ceiling the column) for the lifted, air-steered
            // player to land on. Reaching it is the kite moment; no win logic yet, just a target to aim at.
            var perch = CreateColoredQuad("Perch", new Vector3(8f, 4.25f, 0f), new Vector3(3f, 0.5f, 1f),
                new Color(0.5f, 0.5f, 0.55f), LoadSprite(GroundSpritePath));
            perch.layer = groundLayer;
            perch.AddComponent<BoxCollider2D>();
        }

        static AirPair CreatePair()
        {
            var go = new GameObject("AirPair");
            return go.AddComponent<AirPair>();
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
            sr.sprite = LoadSprite(PlayerSpritePath);
            sr.color = color;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.direction = CapsuleDirection2D.Vertical;

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

        static GameObject CreateColoredQuad(string name, Vector3 pos, Vector3 scale, Color color, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            return go;
        }

        static Sprite LoadSprite(string path)
        {
            // These textures import as 'Multiple', so the Sprite is a sub-asset — LoadAllAssetsAtPath +
            // first-Sprite is robust to that (plain LoadAssetAtPath<Sprite> can return null here).
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) return sprite;
            Debug.LogWarning($"[InflatableIdiots] No Sprite found at '{path}'; that renderer will be left empty.");
            return null;
        }

        static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                Debug.LogWarning("[InflatableIdiots] Could not access TagManager; ground will use Default layer.");
                return 0;
            }
            var so = new SerializedObject(tagManagerAsset[0]);
            var layers = so.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    so.ApplyModifiedProperties();
                    return i;
                }
            }
            Debug.LogWarning($"[InflatableIdiots] No empty layer slot for '{layerName}'; ground will use Default.");
            return 0;
        }
    }
}
