using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.View
{
    /// <summary>
    /// Single-screen two-player follow camera for the vertical climb: centers on the midpoint of both idiots and
    /// zooms so both stay framed (with margin), clamped and smoothed. Each machine runs its own (cameras aren't
    /// networked); it polls for players because in networked play they spawn over time. The graybox answer to the
    /// long-deferred "two-player single-screen framing" question — dynamic zoom, not split-screen.
    ///
    /// Uses unscaled time so it keeps framing even when local play freezes the scene (timeScale 0) on win/lose.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] float minSize = 5.5f;
        [SerializeField] float maxSize = 13f;
        [Tooltip("World-unit padding kept around the players when framing.")]
        [SerializeField] float margin = 3.5f;
        [SerializeField] float moveSmoothing = 6f;
        [SerializeField] float zoomSmoothing = 4f;
        [Tooltip("The climb shaft is centered on this x; locking x keeps the camera column-stable.")]
        [SerializeField] float fixedX = 0f;
        [SerializeField] bool lockX = true;

        Camera cam;
        readonly List<InflatableCharacter> players = new List<InflatableCharacter>();
        float nextScan;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void Rescan()
        {
            players.Clear();
            players.AddRange(FindObjectsByType<InflatableCharacter>(FindObjectsSortMode.None));
        }

        bool NeedsRescan()
        {
            if (players.Count == 0) return true;
            foreach (var p in players)
                if (p == null) return true;
            return false;
        }

        void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan || NeedsRescan())
            {
                nextScan = Time.unscaledTime + 0.5f;
                Rescan();
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            int n = 0;
            foreach (var p in players)
            {
                if (p == null) continue;
                Vector2 pos = p.transform.position;
                min = Vector2.Min(min, pos);
                max = Vector2.Max(max, pos);
                n++;
            }
            if (n == 0) return;

            Vector2 center = (min + max) * 0.5f;
            float halfH = (max.y - min.y) * 0.5f + margin;
            float halfW = (max.x - min.x) * 0.5f + margin;
            float targetSize = Mathf.Clamp(Mathf.Max(halfH, halfW / Mathf.Max(0.1f, cam.aspect)), minSize, maxSize);

            float tMove = 1f - Mathf.Exp(-moveSmoothing * Time.unscaledDeltaTime);
            float tZoom = 1f - Mathf.Exp(-zoomSmoothing * Time.unscaledDeltaTime);

            float targetX = lockX ? fixedX : center.x;
            Vector3 cur = transform.position;
            transform.position = Vector3.Lerp(cur, new Vector3(targetX, center.y, cur.z), tMove);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, tZoom);
        }
    }
}
