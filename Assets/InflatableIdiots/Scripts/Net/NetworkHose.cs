using FishNet.Object;
using UnityEngine;
using InflatableIdiots.Hose;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Networks the air hose host-authoritatively: the SERVER runs the real spring-rope simulation; clients
    /// don't simulate it at all — they receive node positions and render them through the LineRenderer.
    ///
    /// Drives <see cref="HoseController"/> via its Managed mode so lifecycle timing fits FishNet's spawn
    /// callbacks (Unity Start runs before OnStartServer/OnStartClient, which is why HoseController.Managed
    /// is set in Awake to suppress its own Start).
    /// </summary>
    [RequireComponent(typeof(HoseController))]
    public class NetworkHose : NetworkBehaviour
    {
        [Tooltip("How often (seconds) the server broadcasts rope node positions to clients. ~25 Hz.")]
        [SerializeField] float sendInterval = 0.04f;

        HoseController hose;
        Rigidbody2D endA, endB;   // server-only, set before Spawn
        float timer;
        bool clientEndpointsResolved;

        void Awake()
        {
            hose = GetComponent<HoseController>();
            // Must be set before HoseController.Start runs (all Awakes precede all Starts), so its Start no-ops
            // and we drive it from the network callbacks below.
            hose.Managed = true;
        }

        /// <summary>Server-only: inject the two player rigidbodies before Spawn.</summary>
        public void SetEndpointsServer(Rigidbody2D a, Rigidbody2D b)
        {
            endA = a;
            endB = b;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            hose.BuildAsHost(endA, endB);   // real simulated rope on the host
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsServerInitialized) return;   // host already built the real rope in OnStartServer
            hose.InitAsClientRenderer();
            ResolveClientEndpoints();
        }

        void Update()
        {
            if (base.IsServerInitialized)
            {
                timer += Time.deltaTime;
                if (timer < sendInterval) return;
                timer = 0f;
                RenderRope(hose.SnapshotChain());
            }
            else if (!clientEndpointsResolved)
            {
                ResolveClientEndpoints();   // players may spawn / slot-sync a frame or two after the link
            }
        }

        // Client: find the two players by budget slot (0 -> endA, 1 -> endB) and anchor the rope ends to them.
        void ResolveClientEndpoints()
        {
            var controllers = FindObjectsByType<NetworkInflatableController>(FindObjectsSortMode.None);
            NetworkInflatableController a = null, b = null;
            foreach (var c in controllers)
            {
                if (c.SlotValue == 0) a = c;
                else if (c.SlotValue == 1) b = c;
            }
            if (a == null || b == null) return;   // slots not both synced yet; retry next frame

            hose.SetClientEndpoints(a.GetComponent<Rigidbody2D>(), b.GetComponent<Rigidbody2D>());
            clientEndpointsResolved = true;
        }

        // ExcludeServer: the host draws its own simulated rope via HoseController.LateUpdate, so it doesn't
        // need (and shouldn't fight) this snapshot.
        [ObserversRpc(ExcludeServer = true)]
        void RenderRope(Vector2[] points)
        {
            hose.RenderFromPoints(points);
        }
    }
}
