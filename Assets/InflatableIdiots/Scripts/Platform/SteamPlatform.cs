using System;
using Steamworks;
using UnityEngine;

namespace InflatableIdiots.Platform
{
    /// <summary>
    /// Steamworks.NET implementation of <see cref="IPlatform"/> for the networking spike.
    /// Converts CSteamID &lt;-&gt; ulong at this boundary only so the rest of the game stays SDK-free.
    ///
    /// We own the SteamAPI lifecycle here (Init / RunCallbacks / Shutdown) instead of using
    /// Steamworks.NET's bundled SteamManager MonoBehaviour, so the seam stays thin and swappable.
    /// IMPORTANT: only one owner may call Init/RunCallbacks/Shutdown — do NOT also add SteamManager
    /// to the scene. NetworkBootstrap is that single owner.
    /// </summary>
    public class SteamPlatform : IPlatform
    {
        public bool IsReady { get; private set; }
        public ulong LocalId { get; private set; }
        public string LocalName { get; private set; }

        public event Action<ulong> OnLobbyCreated;
        public event Action<ulong> OnInviteAcceptedJoinLobby;
        public event Action<ulong> OnLobbyEntered;

        CSteamID currentLobby = CSteamID.Nil;

        Callback<LobbyCreated_t> cbLobbyCreated;
        Callback<GameLobbyJoinRequested_t> cbJoinRequested;
        Callback<LobbyEnter_t> cbLobbyEnter;

        public void Initialize()
        {
            // InitEx gives the precise failure reason (Init() just discards it).
            ESteamAPIInitResult result = SteamAPI.InitEx(out string err);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Debug.LogError(
                    $"[Steam] SteamAPI.InitEx failed: {result} — \"{err}\"\n" +
                    $"Working dir (steam_appid.txt must be HERE): {System.IO.Directory.GetCurrentDirectory()}\n" +
                    "Checklist: Steam running + logged in (not Offline Mode); Unity and Steam at the SAME " +
                    "privilege level (if Unity is running as Administrator, Steam must be too — or neither); " +
                    "steam_appid.txt contains 480.");
                IsReady = false;
                return;
            }

            IsReady = true;
            LocalId = SteamUser.GetSteamID().m_SteamID;
            LocalName = SteamFriends.GetPersonaName();

            cbLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCb);
            cbJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequestedCb);
            cbLobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnterCb);

            Debug.Log($"[Steam] Init OK as {LocalName} ({LocalId}).");
        }

        public void Shutdown()
        {
            if (!IsReady) return;
            SteamAPI.Shutdown();
            IsReady = false;
        }

        public void RunCallbacks()
        {
            if (IsReady) SteamAPI.RunCallbacks();
        }

        public void CreateLobby(int maxMembers)
        {
            if (!IsReady) return;
            // Private = invite-only, which matches the "private party" flow. The result arrives in OnLobbyCreatedCb.
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, maxMembers);
        }

        public void LeaveLobby()
        {
            if (currentLobby.m_SteamID == 0) return;
            SteamMatchmaking.LeaveLobby(currentLobby);
            currentLobby = CSteamID.Nil;
        }

        public void OpenInviteOverlay()
        {
            if (currentLobby.m_SteamID == 0)
            {
                Debug.LogWarning("[Steam] Invite ignored — no lobby yet. Click Host first and wait for " +
                                 "status 'Hosting (lobby …). Click Invite'.");
                return;
            }
            // The overlay only exists in a BUILD with Steam running — it does NOT work in the Unity Editor,
            // which is the usual reason the invite dialog "does nothing, with no error."
            if (!SteamUtils.IsOverlayEnabled())
            {
                Debug.LogWarning("[Steam] Steam overlay unavailable, so the invite dialog can't open. " +
                                 "Expected in the Editor. In a build: launch with Steam running and enable " +
                                 "Steam → Settings → In Game → Steam Overlay.");
                return;
            }

            SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
            Debug.Log($"[Steam] Opened invite overlay for lobby {currentLobby.m_SteamID}.");
        }

        public void SetLobbyData(string key, string value)
        {
            if (currentLobby.m_SteamID != 0)
                SteamMatchmaking.SetLobbyData(currentLobby, key, value);
        }

        public string GetLobbyData(ulong lobbyId, string key)
        {
            return SteamMatchmaking.GetLobbyData(new CSteamID(lobbyId), key);
        }

        void OnLobbyCreatedCb(LobbyCreated_t e)
        {
            if (e.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError($"[Steam] CreateLobby failed: {e.m_eResult}");
                return;
            }
            currentLobby = new CSteamID(e.m_ulSteamIDLobby);
            OnLobbyCreated?.Invoke(e.m_ulSteamIDLobby);
        }

        void OnJoinRequestedCb(GameLobbyJoinRequested_t e)
        {
            // Friend accepted an overlay invite. Join the lobby; LobbyEnter follows.
            SteamMatchmaking.JoinLobby(e.m_steamIDLobby);
            OnInviteAcceptedJoinLobby?.Invoke(e.m_steamIDLobby.m_SteamID);
        }

        void OnLobbyEnterCb(LobbyEnter_t e)
        {
            currentLobby = new CSteamID(e.m_ulSteamIDLobby);
            OnLobbyEntered?.Invoke(e.m_ulSteamIDLobby);
        }
    }
}
