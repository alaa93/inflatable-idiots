using InflatableIdiots.Hazards;
using InflatableIdiots.Level;
using InflatableIdiots.View;
using UnityEngine;

namespace InflatableIdiots.EditorTools
{
    /// <summary>
    /// Builds the whole "Ascent" graybox playtest geometry into the currently-open scene — the shared layout used
    /// by both the local and networked PlaytestSceneBuilder, the same split as <see cref="SceneBuildHelpers"/>.
    ///
    /// One enclosed vertical shaft, lava rising from the bottom, six stacked chambers connected by puzzle-gated
    /// hatches (bottom → top):
    ///   1. TUTORIAL      — single-kite, take turns over a wall (the existing Fan Trap, repurposed as the intro)
    ///   2. VERTICAL CLIMB— float one up / anchor the other, then swap air to bring the anchor up
    ///   3. DUAL-SWITCH   — high switch (inflated) + low plate (deflated) held together (the identity hook)
    ///   4. ELEVATOR      — both board the column; a gated lift fan carries them up together
    ///   5. KITE + ANCHOR — a heavy anchor on the weight plate powers the fan; the light kite rides to the lever
    ///   6. CLIMAX        — the dual-switch again, but with NO station-relent: the lava keeps chasing (pressure)
    /// Then a Goal room at the very top (both players inside = win).
    ///
    /// Everything is ceiling-enclosed on purpose: a fully-inflated jump apex is ~34u, so without ceilings the
    /// light player would simply leap past every puzzle. Each chamber's only exit is its puzzle-gated hatch, so
    /// the air-share is mandatory. Coordinates are first-pass and meant to be tuned from the playtest.
    /// </summary>
    internal static class PlaytestLayout
    {
        // Shaft interior spans x ∈ [-6.5, 6.5]; walls sit at ±7.
        const float L = -6.5f;
        const float R = 6.5f;

        // Chamber floor TOP surfaces (y). Heights vary; the elevator/kite chambers are taller.
        const float F0 = 0f;    // tutorial floor (spawn)
        const float F1 = 9f;    // tutorial ceiling = vertical-climb floor
        const float F2 = 19f;   // climb ceiling = dual-switch floor
        const float F3 = 28f;   // dual ceiling = elevator floor
        const float F4 = 41f;   // elevator ceiling = kite floor
        const float F5 = 53f;   // kite ceiling = climax floor
        const float F6 = 63f;   // climax ceiling = goal floor

        static readonly Color FloorColor  = new Color(0.30f, 0.32f, 0.34f);
        static readonly Color WallColor   = new Color(0.22f, 0.22f, 0.28f);
        static readonly Color LedgeColor  = new Color(0.40f, 0.42f, 0.46f);
        static readonly Color FanColor    = new Color(0.45f, 0.80f, 1.00f, 0.18f);
        static readonly Color LiftColor   = new Color(0.55f, 0.90f, 1.00f, 0.22f);
        static readonly Color SensorIdle  = new Color(0.70f, 0.32f, 0.32f, 0.95f);
        static readonly Color SensorOn    = new Color(0.40f, 0.95f, 0.50f, 1.00f);
        static readonly Color GateClosed  = new Color(0.34f, 0.30f, 0.40f, 1.00f);
        static readonly Color GateOpen    = new Color(0.34f, 0.30f, 0.40f, 0.10f);
        static readonly Color GoalColor   = new Color(0.95f, 0.85f, 0.20f, 0.28f);

        /// <summary>Build the arena; returns the two player spawn points (bottom of the tutorial chamber).</summary>
        public static Vector3[] Build(int groundLayer)
        {
            // --- Shaft shell ---
            Solid("Wall L", new Vector2(-7f, 30f), new Vector2(1f, 110f), WallColor, groundLayer); // y -25..85
            Solid("Wall R", new Vector2( 7f, 30f), new Vector2(1f, 110f), WallColor, groundLayer);
            Solid("Floor 0", new Vector2(0f, F0 - 0.5f), new Vector2(13f, 1f), FloorColor, groundLayer);
            CreateLava(startY: -10f);

            BuildTutorial(groundLayer);
            BuildVerticalClimb(groundLayer);
            BuildDualSwitch(groundLayer);
            BuildElevator(groundLayer);
            BuildKiteAnchor(groundLayer);
            BuildClimax(groundLayer);
            BuildGoal(groundLayer);
            BuildLabels();

            return new[] { new Vector3(-4f, 0.7f, 0f), new Vector3(-2.5f, 0.7f, 0f) };
        }

        // ===== Chamber 1: TUTORIAL (y 0..9) — single-kite, take turns over the wall =====
        static void BuildTutorial(int groundLayer)
        {
            Station("Station Tutorial", new Vector2(0f, 4.5f), new Vector2(13f, 9f));
            // Mid-chamber wall (top y≈4) — too tall to clear deflated; the fan + air-share is the way over.
            Solid("Tut Wall", new Vector2(1.5f, 1.75f), new Vector2(0.7f, 4.5f), WallColor, groundLayer);
            // Updraft left of the wall: lifts the light player up and over like a kite (always on).
            Fan("Tut Fan", new Vector2(-1.5f, 3.5f), new Vector2(2.6f, 7f), force: 10f, maxRise: 4f, flag: -1);
            // Exit ledge on the right; both must gather here (take turns) to open the hatch above.
            Solid("Tut Ledge", new Vector2(4.75f, 0.75f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Sensor("Tut BothUp", new Vector2(4.75f, 1.8f), new Vector2(3.4f, 2f),
                PuzzleFlags.TutorialBothUp, ZoneSensor.Qualifier.AnyPlayer, count: 2);
            // Ceiling at F1 with a hatch over the right ledge.
            FloorWithGap("Ceil Tutorial", F1, gapCenterX: 4.75f, gapWidth: 3f, groundLayer);
            Gate("Tut Hatch", new Vector2(4.75f, F1 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.TutorialGate, new[] { PuzzleFlags.TutorialBothUp }, latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Chamber 2: VERTICAL CLIMB (y 9..19) — float up / anchor / swap =====
        static void BuildVerticalClimb(int groundLayer)
        {
            Station("Station Climb", new Vector2(0f, 14f), new Vector2(13f, 10f));
            // Staging ledge mid-chamber + a high gather ledge just under the hatch.
            Solid("Climb Ledge A", new Vector2(-3f, 13f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Solid("Climb Ledge Top", new Vector2(2.5f, 17f), new Vector2(4f, 0.5f), LedgeColor, groundLayer);
            // Both must reach the high ledge (only by floating up one at a time and swapping air).
            Sensor("Climb BothUp", new Vector2(2.5f, 18f), new Vector2(3.8f, 2f),
                PuzzleFlags.ClimbBothUp, ZoneSensor.Qualifier.AnyPlayer, count: 2);
            FloorWithGap("Ceil Climb", F2, gapCenterX: 2.5f, gapWidth: 3f, groundLayer);
            Gate("Climb Hatch", new Vector2(2.5f, F2 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.ClimbGate, new[] { PuzzleFlags.ClimbBothUp }, latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Chamber 3: DUAL-SWITCH (y 19..28) — high switch (inflated) + low plate (deflated) =====
        static void BuildDualSwitch(int groundLayer)
        {
            Station("Station Dual", new Vector2(0f, 23.5f), new Vector2(13f, 9f));
            // HIGH switch on a high-left perch — reachable only by the floating (inflated) player.
            Solid("Dual High Perch", new Vector2(-4.25f, 24f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Sensor("Dual High Switch", new Vector2(-4.25f, 25f), new Vector2(3.2f, 1.6f),
                PuzzleFlags.DualHighHeld, ZoneSensor.Qualifier.Inflated, count: 1);
            // LOW plate on the floor — only a heavy (deflated) player can hold it down.
            Solid("Dual Plate Base", new Vector2(4f, F2 + 0.15f), new Vector2(3f, 0.3f), LedgeColor, groundLayer);
            Sensor("Dual Low Plate", new Vector2(4f, F2 + 0.7f), new Vector2(3f, 1.2f),
                PuzzleFlags.DualLowHeld, ZoneSensor.Qualifier.Deflated, count: 1);
            // Hatch opens while BOTH are held (latched permanent here so the playtest is guaranteed traversable;
            // flip to latchSeconds for the "who-holds-the-door" tension once the layout is confirmed).
            FloorWithGap("Ceil Dual", F3, gapCenterX: 0f, gapWidth: 3f, groundLayer);
            Gate("Dual Hatch", new Vector2(0f, F3 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.DualGate, new[] { PuzzleFlags.DualHighHeld, PuzzleFlags.DualLowHeld },
                latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Chamber 4: ELEVATOR (y 28..41) — both board, gated lift fan carries them up =====
        static void BuildElevator(int groundLayer)
        {
            Station("Station Lift", new Vector2(0f, 34.5f), new Vector2(13f, 13f));
            // The lift column: a strong, speed-capped fan that only blows while BOTH players are aboard, so even
            // a heavy body rises and the pair travels up together at the cap (~3 m/s) regardless of the air split.
            int liftFlag = PuzzleFlags.LiftBothAboard;
            CreateLiftFan("Lift Column", new Vector2(0f, 34.5f), new Vector2(4.5f, 12f), force: 90f, maxRise: 3f, flag: liftFlag);
            Sensor("Lift Aboard", new Vector2(0f, 34.5f), new Vector2(4.5f, 12f),
                liftFlag, ZoneSensor.Qualifier.AnyPlayer, count: 2);
            // Top exit ledges either side of the column.
            Solid("Lift Ledge L", new Vector2(-4.5f, 39f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Solid("Lift Ledge R", new Vector2(4.5f, 39f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Sensor("Lift BothUp", new Vector2(0f, 40f), new Vector2(13f, 1.6f),
                PuzzleFlags.LiftBothUp, ZoneSensor.Qualifier.AnyPlayer, count: 2);
            FloorWithGap("Ceil Lift", F4, gapCenterX: -4.5f, gapWidth: 3f, groundLayer);
            Gate("Lift Hatch", new Vector2(-4.5f, F4 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.LiftGate, new[] { PuzzleFlags.LiftBothUp }, latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Chamber 5: KITE + ANCHOR (y 41..53) — anchor powers the fan, kite rides to the lever =====
        static void BuildKiteAnchor(int groundLayer)
        {
            Station("Station Kite", new Vector2(0f, 47f), new Vector2(13f, 12f));
            // Weight plate (left): a deflated anchor standing here powers the fan.
            int plateFlag = PuzzleFlags.KitePlatePressed;
            Solid("Kite Plate Base", new Vector2(-4f, F4 + 0.15f), new Vector2(3f, 0.3f), LedgeColor, groundLayer);
            Sensor("Kite Weight Plate", new Vector2(-4f, F4 + 0.7f), new Vector2(3f, 1.2f),
                plateFlag, ZoneSensor.Qualifier.Deflated, count: 1);
            // Powered fan (right column): only blows while the plate is pressed; the light kite rides it up.
            Fan("Kite Fan", new Vector2(3f, 47f), new Vector2(3f, 11f), force: 10f, maxRise: 4f, flag: plateFlag);
            // High lever at the top of the column — the floating kite reaches it (momentary touch latches the gate).
            Solid("Kite Lever Perch", new Vector2(3f, 51f), new Vector2(3f, 0.5f), LedgeColor, groundLayer);
            Sensor("Kite Lever", new Vector2(3f, 51.9f), new Vector2(3f, 1.4f),
                PuzzleFlags.KiteLeverHeld, ZoneSensor.Qualifier.Inflated, count: 1);
            FloorWithGap("Ceil Kite", F5, gapCenterX: 3f, gapWidth: 3f, groundLayer);
            Gate("Kite Hatch", new Vector2(3f, F5 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.KiteGate, new[] { PuzzleFlags.KiteLeverHeld }, latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Chamber 6: CLIMAX (y 53..63) — dual-switch with NO station relent (lava keeps chasing) =====
        static void BuildClimax(int groundLayer)
        {
            // Deliberately NO StationZone here: the lava chases at full transition speed = the pressure finale.
            Solid("Climax High Perch", new Vector2(-4.25f, 58f), new Vector2(3.5f, 0.5f), LedgeColor, groundLayer);
            Sensor("Climax High Switch", new Vector2(-4.25f, 59f), new Vector2(3.2f, 1.6f),
                PuzzleFlags.ClimaxHighHeld, ZoneSensor.Qualifier.Inflated, count: 1);
            Solid("Climax Plate Base", new Vector2(4f, F5 + 0.15f), new Vector2(3f, 0.3f), LedgeColor, groundLayer);
            Sensor("Climax Low Plate", new Vector2(4f, F5 + 0.7f), new Vector2(3f, 1.2f),
                PuzzleFlags.ClimaxLowHeld, ZoneSensor.Qualifier.Deflated, count: 1);
            FloorWithGap("Ceil Climax", F6, gapCenterX: 0f, gapWidth: 3f, groundLayer);
            Gate("Climax Hatch", new Vector2(0f, F6 - 0.5f), new Vector2(3f, 1f),
                PuzzleFlags.ClimaxGate, new[] { PuzzleFlags.ClimaxHighHeld, PuzzleFlags.ClimaxLowHeld },
                latchOnce: true, latchSeconds: 0f, groundLayer);
        }

        // ===== Goal room (above F6) — its floor IS the climax ceiling (FloorWithGap at F6) =====
        static void BuildGoal(int groundLayer)
        {
            // Top cap so a 34u inflated jump can't fly off the top of the shaft.
            Solid("Top Cap", new Vector2(0f, F6 + 5f), new Vector2(13f, 1f), WallColor, groundLayer);
            // Win zone: both players inside = run won (reuses GoalZone, requiredPlayers 2).
            var goal = TriggerBox("Goal", new Vector2(0f, F6 + 2f), new Vector2(11f, 3f), GoalColor, sortingOrder: -1, out _);
            goal.AddComponent<GoalZone>();
        }

        // ===== Per-puzzle world labels (2-word title + ≤4-word how-to), drawn by WorldLabel =====
        static void BuildLabels()
        {
            Label("Fan Trap",       "Take turns up",         new Vector2(0f, 5.5f));
            Label("Vertical Climb", "Float, anchor, swap",   new Vector2(0f, 15f));
            Label("Dual Switch",    "One high, one low",     new Vector2(0f, 25.5f));
            Label("Co-op Lift",     "Both aboard to rise",   new Vector2(0f, 35.5f));
            Label("Kite Anchor",    "Anchor powers the fan", new Vector2(0f, 49f));
            Label("Final Push",     "Lava won't relent",     new Vector2(0f, 60f));
            Label("The Goal",       "Both reach it",         new Vector2(0f, F6 + 2.8f));
        }

        static void Label(string title, string desc, Vector2 pos)
        {
            var go = new GameObject($"Label {title}");
            go.transform.position = pos;
            go.AddComponent<WorldLabel>().Configure(title, desc);
        }

        // ---------------- primitives ----------------

        static GameObject Solid(string name, Vector2 center, Vector2 size, Color color, int groundLayer)
        {
            var go = SceneBuildHelpers.CreateColoredQuad(name, center, new Vector3(size.x, size.y, 1f), color,
                SceneBuildHelpers.LoadSprite(SceneBuildHelpers.GroundSpritePath));
            go.layer = groundLayer;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        static GameObject TriggerBox(string name, Vector2 center, Vector2 size, Color color, int sortingOrder, out SpriteRenderer sr)
        {
            var go = SceneBuildHelpers.CreateColoredQuad(name, center, new Vector3(size.x, size.y, 1f), color,
                SceneBuildHelpers.LoadSprite(SceneBuildHelpers.GroundSpritePath));
            sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            return go;
        }

        static void Sensor(string name, Vector2 center, Vector2 size, int flag, ZoneSensor.Qualifier q, int count)
        {
            var go = TriggerBox(name, center, size, SensorIdle, sortingOrder: 1, out var sr);
            go.AddComponent<ZoneSensor>().Configure(flag, q, count, sr, SensorIdle, SensorOn);
        }

        static void Fan(string name, Vector2 center, Vector2 size, float force, float maxRise, int flag)
        {
            var go = TriggerBox(name, center, size, FanColor, sortingOrder: -1, out _);
            go.AddComponent<FanZone>().Configure(force, maxRise, Vector2.up, flag);
        }

        static void CreateLiftFan(string name, Vector2 center, Vector2 size, float force, float maxRise, int flag)
        {
            var go = TriggerBox(name, center, size, LiftColor, sortingOrder: -1, out _);
            go.AddComponent<FanZone>().Configure(force, maxRise, Vector2.up, flag);
        }

        static void Gate(string name, Vector2 center, Vector2 size, int gateFlag, int[] inputs, bool latchOnce, float latchSeconds, int groundLayer)
        {
            var go = SceneBuildHelpers.CreateColoredQuad(name, center, new Vector3(size.x, size.y, 1f), GateClosed,
                SceneBuildHelpers.LoadSprite(SceneBuildHelpers.GroundSpritePath));
            go.layer = groundLayer;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            go.AddComponent<BoxCollider2D>(); // solid; FlagGate flips collider.enabled when open
            go.AddComponent<FlagGate>().Configure(gateFlag, inputs, latchSeconds, latchOnce, sr, GateClosed, GateOpen);
        }

        static void Station(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<BoxCollider2D>().isTrigger = true; // BoxCollider2D default 1x1 → scaled to size
            go.AddComponent<StationZone>();
        }

        // A horizontal floor at top-surface y with a hatch gap centered on gapCenterX.
        static void FloorWithGap(string namePrefix, float topY, float gapCenterX, float gapWidth, int groundLayer)
        {
            float y = topY - 0.5f;
            float gapL = gapCenterX - gapWidth * 0.5f;
            float gapR = gapCenterX + gapWidth * 0.5f;

            float leftW = gapL - L;
            if (leftW > 0.05f)
                Solid($"{namePrefix} L", new Vector2((L + gapL) * 0.5f, y), new Vector2(leftW, 1f), FloorColor, groundLayer);

            float rightW = R - gapR;
            if (rightW > 0.05f)
                Solid($"{namePrefix} R", new Vector2((gapR + R) * 0.5f, y), new Vector2(rightW, 1f), FloorColor, groundLayer);
        }

        static void CreateLava(float startY)
        {
            var go = SceneBuildHelpers.CreateColoredQuad("Rising Hazard", new Vector3(0f, startY, 0f),
                new Vector3(13f, 1.5f, 1f), new Color(0.9f, 0.3f, 0.15f, 0.85f),
                SceneBuildHelpers.LoadSprite(SceneBuildHelpers.GroundSpritePath));
            go.GetComponent<SpriteRenderer>().sortingOrder = 2; // engulf, drawn in front
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            go.AddComponent<RisingHazard>();
        }
    }
}
