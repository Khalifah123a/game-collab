# Floating Platform Campaign: Implementation Report

Date: 2026-09-24
Project: `D:/unity/game-collab/packet-courier`
Status: implemented, awaiting full human route playtest.

## Scope

The approved design was recorded first in `C:/Users/khaleefa/Documents/Obsidian Vault/Packet Courier/Platform Melayang dan Pos Istirahat.md`. The other study vault was not used.

- Regenerated all 17 segments across Worlds 1-4 with mixed ground and floating routes, elevated rewards and permanent floating checkpoint islands.
- Each segment contains 51-70 grouped permanent one-way platforms with rewards, plus dynamic platforms in later worlds; 3-4 floating checkpoint islands are 11-15 units wide.
- Added two visible bonus ledges near spawn and rescue floors under early World 1 practice sections.
- Grouped adjacent one-way tiles into one continuous collider and sliced renderer.
- Fixed wall checks treating one-way platforms as solid walls during drop-through.
- Configured ground/one-way/moving sprite importers to Full Rect for sliced rendering; generator preserves this setting.
- Added a scoped editor regeneration command that leaves player/enemy prefabs and meta scenes untouched.

## Verification

| Check | Result |
| --- | --- |
| Check-FloatingLayouts.ps1 | All 17 pass; initially all 17 failed before implementation |
| FloatingPlatformSmoke | 26 runtime checks pass, repeated September 24 |
| GameplayValidation.ValidateAssets | Animation/combat/stamina configuration and all 17 layouts/build scenes pass |
| ProgressionSmoke | 17 ordered segments, four Hub completions, unlocks, exactly-once banking and Menu pass September 24 |
| CombatSmoke | 22 checks pass September 24 |
| WorldMechanicsSmoke | Gate rules, zero-gravity entry/exit and docking refill pass September 24 |
| UnitySmoke / CampaignSmoke | Movement/retry and all 17 scene spawn/TTL/HUD checks passed September 20 after regeneration |
| CapturePreview | HUD bounds pass 1280x720, 1920x1080 and 2560x1080; floating route and checkpoint renders visually inspected |
| Editor state | Compilation successful; Play Mode stopped; clean Menu scene open |

Runtime floating tests cover permanent checkpoint support, landing, snapshot/retry in all four worlds, jump through underside, landing on top, collectible pickup, drop-through and jump/double-jump to a raised ledge. World 3-4 checkpoint activation invokes the activation method directly; their hold-button interaction is not covered. Progression tests invoke completion, not physical traversal.

## Limits And Maintenance

Full traversal of all new floating layouts with hazards enabled, human difficulty/TTL balancing, final art/animations and bosses remain outstanding. September 17 traversal timings apply only to the preceding terrain layouts.

Use `Tools > Packet Courier > Apply Floating Platform Campaign` only after backing up manual layout/scene edits: it replaces all 17 layouts and level scenes. Pre-change backup: `D:/unity/game-collab/artifacts/pre-floating-platforms-20260919`.

Preview images are retained in the Obsidian note's `media` folder as `floating-platforms-1280.png` and `floating-checkpoint-1280.png`; wide versions are in workspace `artifacts`.
