namespace InflatableIdiots.Level
{
    /// <summary>
    /// Central allocation of the 0..63 co-op puzzle-flag ids carried by <see cref="RunController"/>'s
    /// host-authoritative bitmask (see <see cref="RunController.GetPuzzleFlag"/>). Sensors, gates and the
    /// PlaytestLayout builder all reference these constants so the wiring reads clearly and ids never collide.
    ///
    /// Convention per chamber: one flag per producer (a switch held / plate pressed / "both reached the ledge")
    /// plus one gate flag the chamber's FlagGate writes from those inputs.
    /// </summary>
    public static class PuzzleFlags
    {
        // Tutorial chamber — single-kite, take turns over the wall.
        public const int TutorialBothUp   = 0;  // both players reached the upper exit ledge
        public const int TutorialGate     = 1;  // hatch up out of the tutorial

        // Vertical-climb chamber — float one up / anchor the other, then swap.
        public const int ClimbBothUp      = 2;
        public const int ClimbGate        = 3;

        // Dual-switch chamber — high switch (inflated) + low plate (deflated), held together.
        public const int DualHighHeld     = 4;
        public const int DualLowHeld      = 5;
        public const int DualGate         = 6;

        // Elevator chamber — both board the column, ride the gated lift fan up.
        public const int LiftBothAboard   = 7;  // both players in the column -> the lift fan blows
        public const int LiftBothUp       = 8;  // both reached the top exit ledge
        public const int LiftGate         = 9;

        // Kite + anchor chamber — heavy anchor on the plate powers the fan; light kite rides to the lever.
        public const int KitePlatePressed = 10; // deflated anchor on the weight plate -> powered fan blows
        public const int KiteLeverHeld    = 11; // inflated kite reached the high lever
        public const int KiteGate         = 12;

        // Climax chamber — same dual-switch skill, but the lava does NOT relent here (no StationZone).
        public const int ClimaxHighHeld   = 13;
        public const int ClimaxLowHeld    = 14;
        public const int ClimaxGate       = 15;
    }
}
