# Task 3: Integrate upgrade for air jump count

**Goal:** Allow upgrade to increase air jumps.

**Files:**
- Modify: `Assets/_Scripts/Core/GameManager.cs` (UpgradeType, maxLevels, costs)
- Modify: `Assets/_Scripts/Player/PlayerController.cs` (GetUpgradeBonus)

**Context:**
- Add `AirJump` enum value.
- Extend `upgradeLevels` to include new enum.
- Define max level 3 and cost array.
- Modify `GetUpgradeBonus()` in PlayerController to return upgrade level for `AirJump`.

**Instructions:**
- Add `AirJump` after existing enum values in GameManager.
- Update `maxLevels` array length.
- Add cost array for AirJump.
- In PlayerController, inside `GetUpgradeBonus`, handle `AirJump`:
```csharp
case GameManager.UpgradeType.AirJump:
    return GameManager.Instance?.GetUpgradeLevel(type) ?? 0;
```
(Actually current method just calls generic GetUpgradeLevel, so no extra case needed if enum exists. But ensure code compiles.)

**Report:**
- File paths, snippets, PASS/FAIL, commit hash, concerns.
