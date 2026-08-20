# Packet Courier — Sprint 2: World 1 Playtest

## Sprint 2 Fixes & Changes

| Fix | File | Detail |
|---|---|---|
| Checkpoint dual input | `Level/Checkpoint_TCP.cs` | Removed raw `Input.GetButton("Submit")`, now uses only InputManager |
| HUD dash text | `UI/HUD_Cooldown.cs` | Fixed corrupted character, now shows "···" while cooling |
| Fan Vortex enemy | `Level/Fan_Vortex.cs` + Prefab | New push-field enemy (World 1 Seg 3) |
| Movement tuned | `Player/PlayerController.cs` | acceleration 100→1200, jump 22→400, dash 45→700 |
| Speed tuned | `Player/PlayerMode.cs` | plaintextSpeed 8→10, encryptedSpeed 5→6 |
| Level layouts | `Assets/_Levels/` | Added more platforms, checkpoint, fan vortex |

## How to Test in Unity

### Step 1: Regenerate Prefabs (one-time)
1. Open project in Unity 2022.3.x
2. **Tools > Packet Courier > Generate Prefabs**
   - Creates `Fan_Vortex.prefab` and all other prefabs
3. **Tools > Packet Courier > Generate Level Scenes**
   - Rebuilds World1_Segment1-3 from updated layouts

### Step 2: Play Test
1. Open **World1_Segment1** scene
2. Press **Play**

### Controls
| Key | Action |
|---|---|
| A/D or ←/→ | Move |
| Space or W/↑ | Jump (walljump on contact) |
| Left Ctrl or Mouse0 | Dash (8-directional) |
| Left Alt or Mouse1 | Toggle Plaintext/Encrypted |
| Q | Port backward |
| E | Port forward |
| Enter | Interact (hold at checkpoint) |
| Left Shift | Sprint (while moving on ground) |

### World 1 Segment Overview

**Seg 1 — Recycle Bin** (tutorial)
- Flat ground, 3 Data Bits, 1 Checkpoint, 1 Snippet
- Introduces: move, jump, dash, platform timing

**Seg 2 — File Explorer** (vertical climb)
- Spikes (SSS), Enemy patrol (E), Checkpoint (C), Snippet (N)
- Introduces: enemy avoidance, spike hazards

**Seg 3 — Network Tray** (puzzle + danger)
- Firewall gates: F22 (port 22), F443 (port 443)
- Laser beam (L), Moving platform (=), Fan Vortex (V)
- Introduces: port matching, timed hazards, push fields

## Known Test Items

- [ ] Player movement feels snappy (not floaty)
- [ ] Dash has satisfying impact
- [ ] Walljump direction is intuitive
- [ ] TTL drain creates urgency without frustration
- [ ] Firewall gates give clear feedback on wrong port
- [ ] Checkpoint hold-to-activate feels fair (0.5s)
- [ ] Fan Vortex push is noticeable but not deadly
- [ ] Laser timing is readable (on/off cycle)
- [ ] Camera follows smoothly without jitter

## Next: Sprint 3
- World 2: LAN Party (first boss: Censor)
- Enemy variants: Cursor, Process Zombie
- Upgrade system polish (Hub UI)
