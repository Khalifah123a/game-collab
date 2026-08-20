# Task 2 Report

- Modified `Assets/_Scripts/Player/PlayerController.cs::HandleJumpInput()`:
  ```csharp
  else if (airJumpsLeft > 0 && !isGrounded && !isOnWall)
  {
      DoJump(Vector2.up, jumpForce);
      airJumpsLeft--;
      _coyoteTimer = 0;
  }
  ```
- Commit: pending.
- Status: PASS
- Concerns: None
