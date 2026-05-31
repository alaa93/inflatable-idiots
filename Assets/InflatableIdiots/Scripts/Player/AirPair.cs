using UnityEngine;
using UnityEngine.InputSystem;

namespace InflatableIdiots.Player
{
    public class AirPair : MonoBehaviour
    {
        [SerializeField] float transferSpeed = 1.5f;
        [SerializeField] float minBudget = 0.1f;
        [SerializeField] float maxBudget = 0.9f;

        [SerializeField] InflatableCharacter p1;
        [SerializeField] InflatableCharacter p2;

        public float P1Budget { get; private set; } = 0.5f;
        public float P2Budget => 1f - P1Budget;

        // Both pressing their triggers = stalemate. Magnitude is the smaller of the two
        // trigger values, so it scales with how hard both sides are pushing.
        public float StalemateIntensity =>
            (p1 == null || p2 == null) ? 0f : Mathf.Min(p1.TriggerValue, p2.TriggerValue);

        public void Initialize(InflatableCharacter player1, InflatableCharacter player2)
        {
            p1 = player1;
            p2 = player2;
        }

        void Start()
        {
            // Force-pair the same Keyboard device to both PlayerInputs whose default
            // scheme is a Keyboard scheme. Without this, Input System's "one PlayerInput
            // = one device" auto-pairing only assigns the keyboard to one of them.
            if (Keyboard.current == null) return;
            ForceKeyboardScheme(p1);
            ForceKeyboardScheme(p2);
        }

        static void ForceKeyboardScheme(InflatableCharacter c)
        {
            if (c == null) return;
            var pi = c.GetComponent<PlayerInput>();
            if (pi == null) return;
            var scheme = pi.defaultControlScheme;
            if (string.IsNullOrEmpty(scheme) || !scheme.StartsWith("Keyboard")) return;
            pi.SwitchCurrentControlScheme(scheme, Keyboard.current);
        }

        void FixedUpdate()
        {
            if (p1 == null || p2 == null) return;

            // PDF formula, sign chosen so pressing the trigger pulls air TOWARD the presser.
            float flow = (p1.TriggerValue - p2.TriggerValue) * transferSpeed * Time.fixedDeltaTime;
            P1Budget = Mathf.Clamp(P1Budget + flow, minBudget, maxBudget);
        }
    }
}
