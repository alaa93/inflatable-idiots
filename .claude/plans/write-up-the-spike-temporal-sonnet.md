# Inflatable Idiots — Thin Networking Spike Plan

## Context

The local-2P MVP passed its validation gate (~2026-06-01): friends got the concept fast, the
inflation mechanic and the co-op air-share feel landed. The user is ~40% confident in the concept
and is moving to **scope-expansion #1: online play for a Steam-first launch.**

Rather than build full netcode blind, this is a **thin vertical spike** that proves the riskiest 20%
of online play *before* porting any real level. Three architecture forks were locked (see memory
`project_networking_architecture.md`): Steam-first behind a thin `IPlatform` wrapper, **FishNet**
host-authoritative (no client-side prediction yet — we want to *measure* the unpredicted feel),
**FishySteamworks** transport over Steam Datagram Relay, tested for free on **Spacewar App ID 480**.
The user chose to **include the air-share** in the spike (not just movement+hose), because measuring
whether the core differentiator survives latency is the highest-value signal.

**Intended outcome:** two friends on two PCs in two locations connect via a Steam invite, each
control their own inflatable, see the hose between them, and feel the zero-sum air-share work over
the network — enough data to decide whether prediction is needed before building the full feature.

### What the spike must prove (success criteria)
1. Steam inits on 480; host creates a private lobby; invites a friend via the Steam overlay; friend accepts and joins.
2. FishNet connects host+client over FishySteamworks/SDR across two PCs.
3. Two bodies synced host-authoritatively: each machine controls its own; the other is network-driven.
4. Hose simulates on the host and **renders** on the client from networked node positions.
5. Air-share works host-authoritatively: client's trigger → host integrates → `P1Budget` replicates back → both bodies inflate/deflate on both screens, zero-sum.

### Out of scope (do NOT touch/port)
Fan Trap, RisingHazard, GoalZone, RunController, voice/Dissonance, client-side prediction, asmdef
split, Epic/EOS. Keep `PlayerTest.unity` (validated local scene) working and unchanged.

---

## Approach: non-destructive, additive

Local play must keep working. All edits to existing scripts are **additive opt-in** (new modes/setters,
default values preserve current behavior). Netcode lives in **new** files; the spike runs in a **new**
scene `NetworkSpikeTest.unity`. Players spawn from a **prefab** per connection (FishNet's idiomatic
PlayerSpawner pattern — matches how the full game will work).

---

## Step 1 — Packages & setup (order matters)

1. Pre-flight: Steam installed, running, logged in on both PCs; the two accounts are **friends**.
2. Create `C:\Unity Projects\inflatable-idiots\steam_appid.txt` containing exactly `480` (ASCII, no BOM).
   Read by the editor at Play. Must also be copied next to the built `.exe` (Step 6). It is NOT gitignored — commit it.
3. **Install Steamworks.NET first** (FishySteamworks depends on it) into `Assets/Third-Party/`. **Match the
   Steamworks SDK version FishySteamworks' README specifies** — version mismatch is the #1 setup failure.
4. **Install FishNet** (free; Asset Store or GitHub release) into `Assets/Third-Party/FishNet/`. Let code-gen
   finish; verify `Tools > Fish-Networking` menu appears and there are no compile errors.
5. **Install FishySteamworks** into `Assets/Third-Party/`. If it shows errors referencing `Steamworks`, the
   cause is install order or a missing scripting define — verify against its README before continuing.
6. Keep the **single assembly** (no asmdefs) for the spike — lowest friction with FishNet code-gen.

> Verify-at-install (don't guess): exact Steamworks.NET↔FishySteamworks version pairing & any required
> define; FishySteamworks' client-address setter member name; FishySteamworks host self-connect (listen-server) semantics.

## Step 2 — Platform abstraction (Steam behind a thin wrapper)

- **`Assets/InflatableIdiots/Scripts/Platform/IPlatform.cs`** — interface with **zero Steamworks types**
  (IDs as `ulong`): `Initialize()/Shutdown()`, `IsReady`, `LocalId`, `LocalName`, `CreateLobby(int max)`,
  `LeaveLobby()`, `OpenInviteOverlay()`, `SetLobbyData/GetLobbyData`, `RunCallbacks()`, and events
  `OnLobbyCreated(ulong)`, `OnInviteAcceptedJoinLobby(ulong)`, `OnLobbyEntered(ulong)`.
- **`Assets/InflatableIdiots/Scripts/Platform/SteamPlatform.cs`** — `IPlatform` via Steamworks.NET.
  `SteamAPI.Init()`; cache `SteamUser.GetSteamID()` / `SteamFriends.GetPersonaName()`. Register
  `Callback<LobbyCreated_t>`, `Callback<GameLobbyJoinRequested_t>` (→ `SteamMatchmaking.JoinLobby`),
  `Callback<LobbyEnter_t>`. `CreateLobby` → `SteamMatchmaking.CreateLobby(k_ELobbyTypePrivate, max)`.
  `OpenInviteOverlay` → `SteamFriends.ActivateGameOverlayInviteDialog(lobbyId)`. Convert
  `CSteamID`↔`ulong` only at this boundary. (Wrap it ourselves instead of Steamworks.NET's sample
  `SteamManager`, so the seam stays thin/swappable for a future `EpicPlatform`.)

## Step 3 — Networking scripts (new, under `Assets/InflatableIdiots/Scripts/Net/`)

- **`NetworkBootstrap.cs`** (scene MonoBehaviour) — constructs `SteamPlatform`, calls `Initialize()`,
  pumps `RunCallbacks()` once per frame (single pump point). Owns the OnGUI harness (Host / Invite /
  status). Drives the handshake (Step 5). Gets the `FishySteamworks` transport via
  `NetworkManager.TransportManager.Transport`.
- **`NetPlayerSpawner.cs`** (server-only; subscribes to `ServerManager.OnRemoteConnectionState`) —
  on each connection, `Instantiate` NetworkPlayer prefab and `ServerManager.Spawn(go, connection)` to grant
  ownership. Spawn points from a small `List<Vector3>` (`(-2,0,0)`,`(2,0,0)`) — index in, don't hardcode "2".
  After the 2nd player exists, spawns + wires the hose.
- **`NetworkInflatableController.cs`** (NetworkBehaviour on the prefab) — input struct
  `NetInput { Vector2 move; bool jump; float trigger; }`. **Owner** reads PlayerInput and sends
  `[ServerRpc] SubmitInput(NetInput)` (move/trigger last-wins; jump reliable/consumed-once). **Server**
  applies via `InflatableCharacter` setters, then lets its `FixedUpdate` run. **Non-owner client**:
  disable local `PlayerInput`, set body kinematic, `SimulatePhysics=false` (NetworkTransform drives it).
- **`NetworkAirBudget.cs`** (NetworkBehaviour; the server-authoritative `AirPair` replacement) — refs both
  controllers; `[SyncVar] float p1Budget = 0.5f`. Server runs the ported `AirPair` flow
  (`flow=(t1-t2)*transferSpeed*dt; clamp 0.1..0.9`) from both last-known triggers; SyncVar replicates.
  Exposes `P1Budget/P2Budget`; feeds each `InflatableCharacter.BudgetSource`. (Do **not** reuse `AirPair` —
  its `Start` assumes a single shared keyboard and it isn't a NetworkBehaviour.)
- **`NetworkHose.cs`** (NetworkBehaviour on the hose prefab) — **Host** reuses `HoseController`
  (`Initialize(rbA,rbB)` + real sim); every ~20–30 Hz reads chain world positions and sends
  `[ObserversRpc] RenderRope(Vector2[] pts)` (~8 points; interval, not every tick; not a SyncList).
  **Client** spawns hose in `RenderOnly` mode and calls `HoseController.RenderFromPoints(pts)`.

## Step 4 — Additive edits to existing scripts (defaults preserve local play)

- **`Assets/InflatableIdiots/Scripts/Player/InflatableCharacter.cs`**:
  - Add setters mirroring the existing callbacks: `SetMoveInput(Vector2)`, `QueueJump()`, `SetTrigger(float)`.
  - Add `public bool SimulatePhysics = true;` — guard `CheckGround/ApplyWalk/ApplyJump` behind it; keep
    `UpdatePhysicsState()` running everywhere so visuals track budget. (Client sets it false.)
  - Add `public System.Func<float> BudgetSource = null;` — when set, `UpdatePhysicsState` reads it instead
    of `pair`. Null default → existing `pair` path byte-for-byte unchanged.
  - Leave `LateUpdate` (visual scale/jitter) running on both machines.
- **`Assets/InflatableIdiots/Scripts/Hose/HoseController.cs`**:
  - Add `public bool RenderOnly = false;` — in `Start` skip `BuildRope()` (still grab LineRenderer); in
    `LateUpdate` skip when RenderOnly.
  - Add `public void RenderFromPoints(IReadOnlyList<Vector2> pts)` (client render) and a chain-position
    read accessor (e.g. `SnapshotChain()` / `NodeCount`) so the host can broadcast node positions.
- **No edits** to `AirPair`, `RunController`, `GoalZone`, `RisingHazard`, `FanZone`, `CameraShaker`,
  `TestSceneBuilder`, `BuildWindows`.

## Step 5 — Prefabs, scene, build (reproducible from code where practical)

- **`Assets/InflatableIdiots/Editor/NetworkSpikeSceneBuilder.cs`** — menu
  `Tools/Inflatable Idiots/Build Network Spike Scene` builds `NetworkSpikeTest.unity` (Camera + Ground only,
  mirroring `TestSceneBuilder` helpers), instantiates **FishNet's bundled NetworkManager prefab** (building
  it from scratch is fiddly — this is the one place "from code" is awkward), selects **FishySteamworks** as
  the transport, adds the `NetworkBootstrap`/`NetPlayerSpawner` GameObject, registers prefabs in the
  NetworkManager spawnable collection, and adds the scene to Build Settings (reuse `EnsureSceneInBuildSettings`
  — do not disturb `PlayerTest.unity`'s entry).
- **`NetworkPlayer.prefab`** (`Assets/InflatableIdiots/Prefabs/`) — same components `TestSceneBuilder.CreatePlayer`
  adds (SpriteRenderer 9-slice, Rigidbody2D freezeRotation, CapsuleCollider2D, InflatableCharacter +
  PlayerInput configured: SendMessages, action map "Player", `neverAutoSwitchControlSchemes`, the inputactions
  asset) **plus** `NetworkObject`, `NetworkInflatableController`, FishNet `NetworkTransform`. Build via a
  `Tools/Inflatable Idiots/Build Network Prefabs` editor routine (`PrefabUtility.SaveAsPrefabAsset`) to stay reproducible.
- **`NetworkHose.prefab`** — LineRenderer + HoseController + NetworkObject + NetworkHose (endpoints injected at runtime).
- **State replication:** server runs real Rigidbody2D physics on both bodies; `NetworkTransform` replicates to
  clients; client bodies kinematic so physics doesn't fight NetworkTransform. `P1Budget` via SyncVar. No
  prediction component (deliberate — measure the raw feel).
- **Standalone:** after `Build Windows (x64)`, copy `steam_appid.txt` (`480`) next to the exe in
  `Builds/Windows/` (gitignored → manual; optional follow-up: auto-copy in `BuildWindows.cs`).

### Connect handshake
**Host (A):** Init → click Host → `CreateLobby(2)` → `LobbyCreated_t` → `SetLobbyData("HostSteamID", localId)`
→ start FishNet **server** (+ own client for listen-server) → spawner spawns host player → click **Invite**
→ overlay invite dialog. **Joiner (B):** Init → accept overlay invite → `GameLobbyJoinRequested_t` →
`JoinLobby` → `LobbyEnter_t` → read `HostSteamID` → set FishySteamworks client address to it → start FishNet
**client** → server spawns joiner player. **Then:** server spawns the hose, `Initialize(p1.rb, p2.rb)`,
begins broadcasting; clients render it RenderOnly. `RunCallbacks()` pumped every frame on both.

---

## Verification (two PCs; Discord for voice)

1. **Steam init** — both launch; logs show `SteamAPI.Init` success + each persona name/SteamID. *(crit 1a)*
2. **Lobby + invite** — A hosts (lobby created, HostSteamID set); A invites; B accepts; B log shows
   JoinRequested→JoinLobby→LobbyEnter→reads HostSteamID. *(crit 1)*
3. **FishNet connect** — A server up; B client Connected; A logs remote connection Started. *(crit 2)*
4. **Two bodies spawn** on both screens.
5. **Ownership** — A's keyboard moves only A's body; B's only B's; the other mirrors the remote; non-owner
   body ignores local input. *(crit 3 ownership)*
6. **Host authority** — client bodies kinematic, moved only by NetworkTransform (no double-sim jitter);
   walk/jump correct on both screens (latency expected, unpredicted). *(crit 3 host-auth)*
7. **Hose** — drapes/sways on host (real physics), renders on client following both bodies at broadcast rate;
   move apart/together, client tracks. *(crit 4)*
8. **Air-share** — client holds AirTransfer → trigger reaches server → server integrates → `p1Budget` SyncVar
   replicates → both bodies inflate/deflate on both screens; verify zero-sum (P1 up ⇒ P2 down). *(crit 5)*
9. **Measure the feel** — record subjective input-to-motion latency and any rubber-banding on the
   remote-controlled body and the air-share. **This is the data the spike exists to gather** (informs the prediction decision).
10. **Non-destruction** — open/play `PlayerTest.unity`; confirm local 2-keyboard co-op is unchanged.

---

## Key risks / gotchas
- **Steamworks.NET ↔ FishySteamworks version mismatch** (most likely failure) — install Steamworks.NET first, match README.
- **`steam_appid.txt`** missing/wrong location → init fails; need it at project root (editor) AND beside exe (build).
- **`AirPair.ForceKeyboardScheme`** single-keyboard assumption — why we use `NetworkAirBudget`, not `AirPair`, for net play.
- **`InflatableCharacter.FixedUpdate` drives physics directly** — on client it fights NetworkTransform → `SimulatePhysics=false` + kinematic body.
- **PlayerInput device grabbing** with two PlayerInputs — disable on non-owner; owner may need an explicit `SwitchCurrentControlScheme` to its local device.
- **Hose endpoints exist only after both connect** — server spawns/wires hose after 2nd player; client never calls `BuildRope`.
- **Jump edge-trigger** must not be dropped — send reliably / consume-once on server.
- **No prediction = visible remote-body latency** — expected; it's the measurement, not a bug.

---

## Critical files
- `Assets/InflatableIdiots/Scripts/Player/InflatableCharacter.cs` — additive setters + `SimulatePhysics` + `BudgetSource`
- `Assets/InflatableIdiots/Scripts/Hose/HoseController.cs` — `RenderOnly` + `RenderFromPoints` + chain accessor
- `Assets/InflatableIdiots/Scripts/Player/AirPair.cs` — flow logic to port into `NetworkAirBudget` (reference only)
- `Assets/InflatableIdiots/Editor/TestSceneBuilder.cs` — pattern reference for the new spike scene/prefab builders
- New: `Scripts/Platform/{IPlatform,SteamPlatform}.cs`, `Scripts/Net/{NetworkBootstrap,NetPlayerSpawner,NetworkInflatableController,NetworkAirBudget,NetworkHose}.cs`, `Editor/NetworkSpikeSceneBuilder.cs`, `Prefabs/{NetworkPlayer,NetworkHose}.prefab`, `Scenes/NetworkSpikeTest.unity`, `steam_appid.txt`
