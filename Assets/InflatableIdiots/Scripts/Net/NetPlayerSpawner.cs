using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Server-side spawner for the 2-player spike. Mirrors FishNet's built-in PlayerSpawner pattern:
    /// it hooks <see cref="FishNet.Managing.Scened.SceneManager.OnClientLoadedStartScenes"/> (which fires
    /// once a connection — including the host's own clientHost — has loaded the start scenes) and calls
    /// AddOwnerToDefaultScene so the object is observed when no FishNet global scenes are used. On top of
    /// that it assigns each player a budget slot and, once both have joined, spawns the hose/air-budget link.
    ///
    /// Plain MonoBehaviour placed in the scene; only the server path runs (asServer guard).
    /// Remove/disable FishNet's built-in PlayerSpawner component on the NetworkManager — this replaces it.
    /// </summary>
    public class NetPlayerSpawner : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [Tooltip("Link prefab carrying HoseController + NetworkHose + NetworkAirBudget.")]
        [SerializeField] GameObject linkPrefab;
        [Tooltip("Spawn the hose/air-budget link once both players join. OFF for the spawn+movement test " +
                 "step (isolates two-body sync); turn ON when wiring up the hose in the next step.")]
        [SerializeField] bool spawnHoseLink = false;
        [SerializeField] Vector3[] spawnPoints = { new Vector3(-2f, 0f, 0f), new Vector3(2f, 0f, 0f) };

        NetworkManager networkManager;
        readonly List<NetworkInflatableController> players = new List<NetworkInflatableController>();
        bool linkSpawned;

        void Awake()
        {
            networkManager = GetComponentInParent<NetworkManager>();
            if (networkManager == null) networkManager = InstanceFinder.NetworkManager;
        }

        void Start()
        {
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        }

        void OnDestroy()
        {
            if (networkManager != null)
                networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        }

        void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer) return;
            if (players.Count >= spawnPoints.Length) return;   // spike is 2 players
            if (playerPrefab == null) { Debug.LogError("[Spike] NetPlayerSpawner.playerPrefab is not assigned."); return; }

            int slot = players.Count;
            GameObject go = Instantiate(playerPrefab, spawnPoints[slot], Quaternion.identity);
            networkManager.ServerManager.Spawn(go, conn);                                 // ownership -> this connection
            networkManager.SceneManager.AddOwnerToDefaultScene(go.GetComponent<NetworkObject>()); // make it observed

            var ctrl = go.GetComponent<NetworkInflatableController>();
            ctrl.SetSlotServer(slot);
            players.Add(ctrl);

            // Hose/air-budget link is deferred for the spawn+movement test step (see spawnHoseLink).
            if (spawnHoseLink && players.Count == 2 && !linkSpawned)
                SpawnLink();
        }

        void SpawnLink()
        {
            linkSpawned = true;
            if (linkPrefab == null) { Debug.LogError("[Spike] NetPlayerSpawner.linkPrefab is not assigned."); return; }

            GameObject linkGo = Instantiate(linkPrefab);

            linkGo.GetComponent<NetworkHose>().SetEndpointsServer(
                players[0].GetComponent<Rigidbody2D>(), players[1].GetComponent<Rigidbody2D>());
            var airBudget = linkGo.GetComponent<NetworkAirBudget>();
            airBudget.SetControllersServer(players[0], players[1]);

            // Run coordination (host-authoritative win/lose state + in-place restart). Optional component on the
            // link prefab; without it the networked scene falls back to RunController's scene-reload restart,
            // which drops the Steam/FishNet session.
            var run = linkGo.GetComponent<NetworkRunController>();
            if (run != null) run.ConfigureServer(this, airBudget);
            else Debug.LogWarning("[Spike] NetworkLink prefab has no NetworkRunController — add it for in-place " +
                                  "networked restart; restart currently falls back to a scene reload.");

            networkManager.ServerManager.Spawn(linkGo);   // server-owned; clients already in the default scene observe it
        }

        /// <summary>
        /// Server-only: send both tracked players back to their spawn points with zero velocity. Driven by
        /// <see cref="NetworkRunController"/> on an in-place restart; the moves are authoritative and propagate
        /// to clients via reconcile (owner) / NetworkTransform (spectator).
        /// </summary>
        public void ServerResetPlayers()
        {
            foreach (var ctrl in players)
            {
                if (ctrl == null) continue;
                int slot = Mathf.Clamp(ctrl.SlotValue, 0, spawnPoints.Length - 1);
                var rb = ctrl.GetComponent<Rigidbody2D>();
                if (rb == null) continue;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = spawnPoints[slot];
            }
        }
    }
}
