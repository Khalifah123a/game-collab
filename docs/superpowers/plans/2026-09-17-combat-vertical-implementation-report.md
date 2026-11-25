# Combat And Vertical Campaign

Approved in chat on 17 September 2026 after the user requested less flat/easy worlds, earlier enemy detection, left-click attacks, a closer camera, and smoother movement. The approved design was saved to `C:/Users/khaleefa/Documents/Obsidian Vault/Packet Courier/Revisi Combat dan Level Vertikal.md` before implementation.

## Changes

- Left mouse uses PlayerCombat: directional melee, 0.08s windup, 0.13s active, 0.21s recovery, one hit per target, line-of-sight checks, visible arc and Attack animation. Ctrl retains dash independently. Ordinary enemies have two hit points and stagger on hit.
- Enemy detection 10 units, chase limit 16 from home, chase speed 4.5, wall/ledge guards. Patrol and chase bounds no longer share the old four-unit leash.
- Player Rigidbody interpolation; acceleration/deceleration 45/55, air acceleration 32. ConsumeJump clears its pending flag immediately.
- Camera size 4.5, facing-aware lookahead 1.8, vertical offset 1.2, SmoothDamp follow. Manual zoom retained.
- All 17 layout sources and scenes regenerated with solid ascending/descending terraces, 10-12 units of elevation range, gaps, timed/moving platforms, encounters, gates, and checkpoints. World 4 includes an optional elevated zero-gravity route. Modular layouts remain placeholder art, not final handcrafted levels.
- Editor menu: `Tools/Packet Courier/Apply Combat And Vertical Campaign`. This overwrites layout sources before rebuilding; manual authoring must be backed up first. Ordinary `Apply Gameplay Rework` preserves layout text sources.

## Verification

- GameplayFeelChecks initially failed all targeted configuration/elevation checks, then passed for all 17 layouts and updated prefabs.
- CombatSmoke: 22 checks passed (direction, once per swing, recovery, wall obstruction, animation/arc, attack input independent of dash, pause/death/frozen/dash guards, motion acceleration/braking, distant detection and chase).
- Run-Checks.ps1: 14 model assertions passed.
- UnitySmoke: original movement, dash collision, double jump, resource immunity/pause, enemy telegraph, checkpoint/TTL/bits tests passed.
- CampaignSmoke: all 17 scenes loaded with grounded spawn, expected TTL, and HUD.
- WorldMechanicsSmoke: gates, zero-gravity entry/exit, timed-platform availability and docking passed. Dock test now selects a separate actual dock rather than a stale fixed coordinate.
- ProgressionSmoke: 17 ordered transitions, four world completions, unlocks, exactly-once rewards, return to Menu passed. This calls completion directly, not traversal.
- TraversalSmoke: first segment of each world reached the goal using player physics/input. Recorded simulation times approximately 66.8/75.1/83.5/95.2 seconds. Enemies, spikes, lasers and gates are disabled for this geometry test; moving/timed platforms remain active. World 4 needed a repeat due to timed-platform phase. Bot runs are diagnostic and can fail with different timing; these are not human difficulty benchmarks.
- CapturePreview: HUD bounds passed at 1280x720, 1920x1080, 2560x1080; 1280 and ultrawide combat screenshots inspected visually.

## Remaining Limits

Final sprites/attack art and boss encounters are not part of this revision. Full human playthroughs of all 17 routes with hazards enabled, shortcut testing, and TTL/difficulty balance are still outstanding. No claim that bots prove the game feels smooth or that every timing challenge is balanced.

Backup before this revision: `D:/unity/game-collab/artifacts/pre-combat-vertical-20260917`. No user changes were reverted and no commit was made.
