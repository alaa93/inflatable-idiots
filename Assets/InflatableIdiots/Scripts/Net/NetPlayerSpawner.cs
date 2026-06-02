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
            linkGo.GetComponent<NetworkAirBudget>().SetControllersServer(players[0], players[1]);

            networkManager.ServerManager.Spawn(linkGo);   // server-owned; clients already in the default scene observe it
        }
    }
}
