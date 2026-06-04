using System.Collections.Generic;
using InflatableIdiots.Hazards;
using InflatableIdiots.Level;
using UnityEditor;
using UnityEngine;

namespace InflatableIdiots.EditorTools
{
    /// <summary>
    /// Shared scene-construction primitives used by both <see cref="TestSceneBuilder"/> (local PlayerTest)
    /// and <see cref="NetworkSpikeSceneBuilder"/> (networked NetworkSpikeTest). Keeping the camera, ground, and
    /// Fan Trap geometry in one place means both scenes stay in sync — change the arena once, rebuild either.
    ///
    /// Only the genuinely shared pieces live here. Player/hose/pair construction is local-only (players spawn
    /// from prefabs in the networked flow) and stays in TestSceneBuilder; the networking objects are
    /// NetworkSpikeSceneBuilder's job.
    /// </summary>
    internal static class SceneBuildHelpers
    {
        internal const string GroundLayerName = "Ground";

        // Default package sprites (both ~1 unit native @ 256 PPU). 9-Sliced is a rounded rect — fits the
        // capsule body and reads as "inflatable"; Square is a solid fill for the ground. Loaded as assets
        // (not runtime Sprite.Create) so the reference actually serializes into the saved scene.
        internal const string PlayerSpritePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/9-Sliced.png";
        internal const string GroundSpritePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Square.png";

        internal static GameObject CreateCamera()
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

        internal static void CreateGround(int groundLayer)
        {
            var go = CreateColoredQuad("Ground", new Vector3(0f, -3f, 0f), new Vector3(40f, 1f, 1f), new Color(0.3f, 0.6f, 0.3f), LoadSprite(GroundSpritePath));
            go.layer = groundLayer;
            go.AddComponent<BoxCollider2D>();
        }

        internal static void CreateFanTrap(int groundLayer)
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

            // Blocking wall — sits just right of the updraft, too tall to clear from the ground. Riding the
            // fan up and drifting over it is the showcased way past (top y 3.0 < updraft top y 4.75). Because
            // air is zero-sum, only one player can be light enough to cross at a time → forces taking turns.
            var wall = CreateColoredQuad("Wall", new Vector3(7f, 0.25f, 0f), new Vector3(0.6f, 5.5f, 1f),
                new Color(0.22f, 0.2f, 0.28f), LoadSprite(GroundSpritePath));
            wall.layer = groundLayer;
            wall.AddComponent<BoxCollider2D>();
        }

        internal static void CreateGoal()
        {
            // Objective on the far side of the wall. Translucent trigger over the right-hand ground; the
            // GoalZone latches a win once BOTH players are inside at once. Default layer + isTrigger so it
            // neither blocks movement nor registers as standable ground.
            var go = CreateColoredQuad("Goal", new Vector3(9f, -1.25f, 0f), new Vector3(2.5f, 2.5f, 1f),
                new Color(0.95f, 0.85f, 0.2f, 0.25f), LoadSprite(GroundSpritePath));
            go.GetComponent<SpriteRenderer>().sortingOrder = -1; // behind the players
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            go.AddComponent<GoalZone>();
        }

        internal static void CreateRisingHazard()
        {
            // Full-width "lava" bar that starts below the view and climbs. Kinematic + trigger so it engulfs
            // (doesn't push) the players; RisingHazard ends the run on contact. Drawn on top so it reads as
            // engulfing. Starts at y -6 (off-screen bottom) and rises into the play area.
            var go = CreateColoredQuad("Rising Hazard", new Vector3(0f, -6f, 0f), new Vector3(42f, 1.5f, 1f),
                new Color(0.9f, 0.3f, 0.15f, 0.85f), LoadSprite(GroundSpritePath));
            go.GetComponent<SpriteRenderer>().sortingOrder = 2; // in front of players/geometry
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            go.AddComponent<RisingHazard>();
        }

        internal static void CreateRunController()
        {
            // Owns win/lose state, the freeze-on-end, and R-to-restart. Plain GameObject, no transform needs.
            var go = new GameObject("Run Controller");
            go.AddComponent<RunController>();
        }

        internal static GameObject CreateColoredQuad(string name, Vector3 pos, Vector3 scale, Color color, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            return go;
        }

        internal static Sprite LoadSprite(string path)
        {
            // These textures import as 'Multiple', so the Sprite is a sub-asset — LoadAllAssetsAtPath +
            // first-Sprite is robust to that (plain LoadAssetAtPath<Sprite> can return null here).
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) return sprite;
            Debug.LogWarning($"[InflatableIdiots] No Sprite found at '{path}'; that renderer will be left empty.");
            return null;
        }

        /// <summary>
        /// Ensures <paramref name="path"/> is an enabled scene in Build Settings (so runtime scene-reload and
        /// standalone builds include it). When <paramref name="asStartup"/> is true the scene is moved to
        /// index 0 — <see cref="BuildWindows"/> builds every enabled scene in order, and index 0 is what loads on launch
        /// (the networked spike must boot straight into NetworkSpikeTest). Otherwise it's appended if missing.
        /// </summary>
        internal static void EnsureSceneInBuildSettings(string path, bool asStartup = false)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existing = scenes.FindIndex(s => s.path == path);

            if (asStartup)
            {
                if (existing == 0) return; // already the startup scene
                if (existing > 0) scenes.RemoveAt(existing);
                scenes.Insert(0, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[InflatableIdiots] Set {path} as the startup scene (Build Settings index 0).");
                return;
            }

            if (existing >= 0) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[InflatableIdiots] Added {path} to Build Settings (enables runtime scene reload / standalone build).");
        }

        internal static int EnsureLayer(string layerName)
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
