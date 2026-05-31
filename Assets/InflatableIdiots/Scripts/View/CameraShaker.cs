using InflatableIdiots.Player;
using UnityEngine;

namespace InflatableIdiots.View
{
    public class CameraShaker : MonoBehaviour
    {
        [SerializeField] AirPair pair;
        [SerializeField] float maxOffset = 0.08f;
        [SerializeField] float frequency = 35f;

        Vector3 basePosition;
        float seed;

        public void Initialize(AirPair airPair) => pair = airPair;

        void Awake()
        {
            basePosition = transform.position;
            seed = Random.value * 1000f;
        }

        void LateUpdate()
        {
            float intensity = pair != null ? pair.StalemateIntensity : 0f;
            if (intensity <= 0f)
            {
                transform.position = basePosition;
                return;
            }
            float t = Time.time * frequency;
            float ox = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * intensity * maxOffset;
            float oy = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * intensity * maxOffset;
            transform.position = basePosition + new Vector3(ox, oy, 0f);
        }
    }
}
