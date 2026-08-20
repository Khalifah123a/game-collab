# Task 1: Add air jump fields and initialization

**Goal:** Add variables for air jumps and initialize them properly.

**Files:**
- Modify: `Assets/_Scripts/Player/PlayerController.cs`

**Context:**
- Player starts with `airJumpsAvailable = 1` and `airJumpsLeft` set to that value.
- Reset air jumps when grounded.

**Instructions:**
- Add public fields `airJumpsAvailable` and `airJumpsLeft` in PlayerController.
- Initialize `airJumpsLeft = airJumpsAvailable` in `Start()`.
- In `CheckGrounded()`, when `isGrounded` becomes true, assign `airJumpsLeft = airJumpsAvailable`.

**Report:**
- Provide file paths modified and code snippets.
- State PASS/FAIL and commit hash.
- Note any concerns.
