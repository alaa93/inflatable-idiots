using System;

namespace InflatableIdiots.Platform
{
    /// <summary>
    /// Thin, store-agnostic platform-services seam (identity / lobby / invite) for the networking spike.
    ///
    /// Deliberately exposes ZERO Steamworks types — IDs are plain <see cref="ulong"/> — so a future
    /// EpicPlatform (or a DirectPlatform for dev/mobile) can implement the same surface without leaking
    /// SDK details into game code. This is "abstract by extraction": only the calls the spike actually
    /// uses live here, and the shape is taken from a real Steam implementation rather than guessed.
    ///
    /// Lifecycle: Initialize() once at startup, RunCallbacks() every frame (single pump point), Shutdown()
    /// on quit. The concrete SteamPlatform converts CSteamID <-> ulong at its own boundary only.
    /// </summary>
    public interface IPlatform
    {
        /// <summary>True once the underlying SDK initialized successfully.</summary>
        bool IsReady { get; }

        /// <summary>The local user's platform id (SteamID as a ulong).</summary>
        ulong LocalId { get; }

        /// <summary>The local user's display name (Steam persona name).</summary>
        string LocalName { get; }

        /// <summary>Fired when our own <see cref="CreateLobby"/> succeeds; carries the new lobby id.</summary>
        event Action<ulong> OnLobbyCreated;

        /// <summary>Fired when the user accepts an invite from the platform overlay; carries the lobby id to join.</summary>
        event Action<ulong> OnInviteAcceptedJoinLobby;

        /// <summary>Fired once we have actually entered a lobby (host or joiner); carries the lobby id.</summary>
        event Action<ulong> OnLobbyEntered;

        void Initialize();
        void Shutdown();

        /// <summary>Pump platform callbacks. Call exactly once per frame from a single owner.</summary>
        void RunCallbacks();

        void CreateLobby(int maxMembers);
        void LeaveLobby();

        /// <summary>Open the platform's "invite a friend" overlay for the current lobby.</summary>
        void OpenInviteOverlay();

        void SetLobbyData(string key, string value);
        string GetLobbyData(ulong lobbyId, string key);
    }
}
