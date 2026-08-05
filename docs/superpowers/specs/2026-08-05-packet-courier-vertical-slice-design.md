# Packet Courier — Vertical Slice Design

Date: 2026-08-05
Status: Approved by user

## Goal

Make Packet Courier playable end-to-end: `Menu → World1_Segment1 → 2 → 3 → Hub`, with `GameOver` on death and retry. Levels are generated from text-layout definitions so they are quick to iterate on, and all scenes are auto-registered in `EditorBuildSettings` so the slice builds out of the box.

## Background (why this exists)

Playtest exposed two root causes that are already fixed (uncommitted):

1. **Player animation never showed**: clips were created as runtime objects without `AssetDatabase.AddObjectToAsset`, so `m_Motion` was `{fileID: 0}` and the `== null` guard blocked regeneration. Fix: `CreatePlayerAnimatorController` is now idempotent, reuses the existing state machine/states (guid preserved), and persists clips as sub-assets.
2. **No GameManager, no scenes, empty Build Settings**: `SetupScene` never created GameManager; `GameOver`/`Hub`/`World1_SegmentX` didn't exist; `EditorBuildSettings.asset` had `m_Scenes: []`. Fix: `SetupScene` calls `EnsureGameManager()`; `GameManager.LoadOrReload(sceneName)` reloads the active scene when the target is missing (dev fallback, keeps production behavior).

This design adds the missing playable content: real levels, menu, game-over, hub, and build-settings wiring.

## Architecture

### Level generator (text-layout driven)

- New menu item: **`Tools > Packet Courier > Generate Level Scenes`**.
- Level definitions are plain-text files in `Assets/_Levels/`, one per level, e.g. `World1_Segment1.txt`. Each line is one row of the grid; each character is one tile cell of 1 world unit. Cell `(col, row)` maps to world position `(col, -row)`.
- On generate, `EditorGenerator` (existing partial class) parses every `*.txt` in `Assets/_Levels/`, instantiates prefabs via `PrefabUtility.InstantiatePrefab` (keeps the original prefab reference — no duplication), parents them under a `LevelContent` root, and composes the scene.
- Every generated scene contains: `InputManager`, orthographic `Camera` + `CameraFollow`, `LevelManager` (with `worldIndex`, `segmentIndex`, `playerSpawn`, `serverBPoint` wired), `LevelContent` root. `GameManager` is NOT added — it is a `DontDestroyOnLoad` singleton carried over from the first scene.
- Generated scenes are written to `Assets/_Scenes/` and appended to `EditorBuildSettings.scenes` (idempotent: regenerate skips already-registered scenes).
- Regeneration is destructive: the scene file is recreated from the layout text, so levels are edited by editing the `.txt` and re-running the menu item.

### Tile symbol set

| Char | Prefab | Notes |
|------|--------|-------|
| `#` | `Ground` | static solid ground |
| `-` | `Platform_OneWay` | one-way platform |
| `=` | `Moving_Platform` | moving platform |
| `S` | `Spike` | InstaKill unless encrypted |
| `E` | `Enemy_Patrol` | patrol enemy |
| `L` | `Laser_Beam` | laser hazard |
| `P` | `Player` | exactly one per level; sets `LevelManager.playerSpawn` |
| `G` | `ServerB_Goal` | exactly one per level; sets `LevelManager.serverBPoint` |
| `B` | `DataBit` | collectible |
| `N` | `Snippet` | TTL heal |
| `C` | `Checkpoint_TCP` | interact-to-activate, TTL bonus |
| `F<port>` | `Gate_Firewall` | `requiredPort = <port>`, plaintext ok; e.g. `F80`, `F443` |
| ` ` | — | empty cell |

Gate ports are encoded in the symbol (`F80`, `F443`). Other hazard/prop configuration (enemy patrol speed, laser length) uses the prefab's existing defaults; tuning lives in the `.txt` layout, not new inspector knobs.

### Gameplay loop / flow

1. `Menu` — title + **Start Run** + **Quit**. `Start Run` calls `GameManager.StartRun(0)` which loads `World1_Segment1`.
2. `World1_Segment1 → 2 → 3` — each `LevelManager.CompleteSegment()` → `GameManager.OnSegmentComplete()` → `LoadOrReload` next segment. `currentWorldIndex`/`currentSegmentIndex` are already persisted and drive the flow.
3. Death anywhere → `PlayerHealth.Die()` → `GameManager.OnPlayerDeath()` → `GameOver`.
4. `GameOver` — "Packet Lost" + **Retry Segment** (reloads current segment) + **Back to Menu**.
5. After `World1_Segment3` completes → `Hub` (run complete). `Hub` — minimal: run summary text + **Back to Menu**. (Next-world content is future work; "World 2" shows locked.)

### New scripts (small)

- `MenuUI` — Start Run / Quit buttons.
- `GameOverUI` — Retry / Menu buttons; reads current segment from GameManager.
- `HubUI` — Back to Menu; shows bits collected (from GameManager).
- `EditorLevelGenerator` — the scene-builder logic (called by the new menu item in `EditorGenerator`).

All three UI scripts follow existing patterns (singleton access to `GameManager.Instance`, no new input bindings — menu uses `OnMouseDown`/UI Buttons + legacy input).

## Scenes generated

- `Assets/_Scenes/Menu.unity`
- `Assets/_Scenes/GameOver.unity`
- `Assets/_Scenes/Hub.unity`
- `Assets/_Scenes/World1_Segment1.unity`
- `Assets/_Scenes/World1_Segment2.unity`
- `Assets/_Scenes/World1_Segment3.unity`

Build settings order: `Menu` (index 0) first so builds boot into the menu, then segments in order, then `Hub`, `GameOver`.

## World 1 content (tutorial world)

- **Segment 1 — "Handshake"**: gentle platforms, databits along the route, one checkpoint, goal at the end. No hazards. Teaches move / jump / dash.
- **Segment 2 — "Firewall"**: introduces spikes (plaintext = death, encrypted = survivable with TTL damage), a checkpoint before the spike cluster, a couple of patrol enemies, snippets as rewards. Teaches the mode toggle (Fire2).
- **Segment 3 — "Root Access"**: two firewall gates (F80 and F443), lasers, patrols, databit cluster, final ServerB goal. Teaches port cycling (Q/E) combined with earlier mechanics.

Each segment targets roughly 30–60 seconds of play at plaintext speed (8 u/s) with TTL drain 1/s.

## Out of scope (this slice)

- World 2+ content, upgrade/power-up shop integration.
- Persistent save/load; session state only.
- Touch/mobile UI for menu scenes.
- Audio.
- Balancing beyond the tutorial-intent values above.

## Open questions (decided)

- Generator approach: **text-layout grid** (chosen over hand-placed scenes or noise generation) — fast iteration, deterministic, readable diffs.
- Scope: **full playable slice** Menu→Goal (chosen over bare 3 test segments).

## Verification

- Open project → `Tools > Packet Courier > Generate Level Scenes` completes with no errors; 6 scenes exist in `Assets/_Scenes/`; `EditorBuildSettings` has 6 entries.
- Play from `Menu`: start run → play Segment 1→2→3 (spikes kill in plaintext, gates open on correct port, goal advances) → land in `Hub`.
- Die in any segment → `GameOver` → Retry reloads same segment; Back to Menu works.
- Player animation plays (regression check for the animator fix).
