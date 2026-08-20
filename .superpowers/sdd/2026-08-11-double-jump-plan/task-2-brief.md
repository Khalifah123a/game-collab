# Task 2: Implement air jump logic in HandleJumpInput

**Goal:** Allow player to perform air jumps when available.

**Files:**
- Modify: `Assets/_Scripts/Player/PlayerController.cs`

**Context:**
- Air jump triggers when `airJumpsLeft > 0`, not grounded, not on wall.
- Use `DoJump(Vector2.up, jumpForce)` and decrement `airJumpsLeft`.
- Ensure air jump doesn't interfere with dash or wall jump.

**Instructions:**
- Insert after ground jump block:
```csharp
else if (airJumpsLeft > 0 && !isGrounded && !isOnWall)
{
    DoJump(Vector2.up, jumpForce);
    airJumpsLeft--;
    _coyoteTimer = 0;
}
```

**Report:**
- File paths modified, code snippet, PASS/FAIL, commit hash, concerns.
