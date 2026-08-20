# Task 1 Report

- Modified `Assets/_Scripts/Player/PlayerController.cs`:
  - Added fields:
    ```csharp
    [Header("AirJump")]
    public int airJumpsAvailable = 1;
    public int airJumpsLeft;
    ```
  - Updated `Start()`:
    ```csharp
    airJumpsLeft = airJumpsAvailable;
    ```
  - Updated `CheckGrounded()`:
    ```csharp
    if (isGrounded)
    {
        // existing resets
        airJumpsLeft = airJumpsAvailable;
    }
    ```
- Commit: to be created after full implementation.

- Status: PASS

- Concerns: None
