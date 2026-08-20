# Double Jump Implementation Plan

**Goal:** Implement air jump feature with upgrade scaling, direction upward, reset on ground.
**Architecture:** Extend PlayerController with air jump fields and logic, add AirJump upgrade in GameManager.
**Tech Stack:** Unity 2022.3.20, C#, Rigidbody2D.

## Global Constraints
- Start with 1 air jump.
- Air jumps increase via `GameManager.UpgradeType.AirJump`.
- Air jump direction is straight up (Vector2.up) with same force as ground jump.
- Air jumps reset when player is grounded.
- Air jump cannot be used during dash or wall cling.

### Task 1: Add air jump fields and initialization
- [x] Add `public int airJumpsAvailable = 1;` and `public int airJumpsLeft;` to PlayerController.
- [x] In `Start()`, set `airJumpsLeft = airJumpsAvailable;`.
- [x] In `CheckGrounded()`, when `isGrounded` becomes true, set `airJumpsLeft = airJumpsAvailable;`.

### Task 2: Implement air jump logic in HandleJumpInput
- [x] Insert air jump branch after ground jump check and before wall jump check:
```csharp
else if (airJumpsLeft > 0 && !isGrounded && !isOnWall)
{
    DoJump(Vector2.up, jumpForce);
    airJumpsLeft--;
    _coyoteTimer = 0;
}
```

### Task 3: Integrate upgrade for air jump count
- [x] Add `AirJump` to `GameManager.UpgradeType`.
- [x] Extend `maxLevels` and `costs` arrays.
- [x] `PlayerController.GetUpgradeBonus()` already handles generically.

### Task 4: Test in Unity
- [ ] Verified single air jump works.
- [ ] Verified reset after landing.
- [ ] Verified air jump blocked during dash/wall.

### Task 5: Commit changes
- [ ] Committed implementation (commit pending push).

> **Catatan 2026-08-12:** Task 1-3 ternyata BELUM pernah diimplementasikan di kode
> meski dicentang (plan ditulis, kode kosong — grep `airjump` = 0). Sudah diimplementasikan
> sekarang di PlayerController.cs + GameManager.cs. Task 4 (test Unity) & 5 (commit) menunggu
> playtest di editor.
