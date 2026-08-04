### Task 3: Sprint + wiring animator di PlayerController

**Files:**
- Modify: `Assets/_Scripts/Player/PlayerController.cs`

**Interfaces:**
- Consumes: `_input.sprintHeld` (Task 1), `PlayerAnimator.UpdateState` (Task 2).
- Produces: `PlayerController.isSprinting` (public bool). Kecepatan lari = `GetSpeed() * sprintSpeedMultiplier` (8 → 12.8), animator `speedMultiplier` = `sprintAnimMultiplier` (1.8) saat sprint.

- [ ] **Step 1: Tambah header + field Sprint**

Setelah blok `[Header("Dash")]` (setelah `public LayerMask dashBlockMask;`):

```csharp
    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.6f;
    public float sprintAnimMultiplier = 1.8f;
    public bool isSprinting;
```

- [ ] **Step 2: Tambah field `_animator`**

Di antara field private (setelah `private PlayerHealth _health;`):

```csharp
    private PlayerAnimator _animator;
```

- [ ] **Step 3: Assign `_animator` di Awake**

Di `Awake()` (setelah `_health = GetComponent<PlayerHealth>();`):

```csharp
        _animator = GetComponent<PlayerAnimator>();
```

- [ ] **Step 4: Null-guard `_input` di HandleJumpInput & HandleDashInput**

Di awal `HandleJumpInput()`:

```csharp
        if (_input == null) return;
```

Di awal `HandleDashInput()`:

```csharp
        if (_input == null) return;
```

- [ ] **Step 5: Panggil HandleSprintInput di Update**

Di `Update()`, paling akhir (setelah `UpdateFacing();`):

```csharp
        HandleSprintInput();
```

- [ ] **Step 6: Terapkan multiplier sprint di FixedMove**

Ubah baris di `FixedMove()`:

```csharp
        float targetSpeed = _mode.GetSpeed() * _moveInput.x;
```

menjadi:

```csharp
        float targetSpeed = _mode.GetSpeed() * (isSprinting ? sprintSpeedMultiplier : 1f) * _moveInput.x;
```

- [ ] **Step 7: Tambah metode HandleSprintInput**

Tempatkan sebelum `UpdateFacing()`:

```csharp
    private void HandleSprintInput()
    {
        isSprinting = _input != null && _input.sprintHeld && Mathf.Abs(_moveInput.x) > 0.01f && isGrounded;

        if (_animator == null) return;
        float mult = isSprinting ? sprintAnimMultiplier : 1f;
        _animator.UpdateState(Mathf.Abs(_rb.velocity.x) > 0.1f, mult, FacingDir);
    }
```

- [ ] **Step 8: Verify compile + playtest**

1. Buka Unity → Console, expected tidak ada error.
2. Play scene yang sudah ada `InputManager` → gerak kiri/kanan, tahan Left Shift → player lari lebih cepat.

---

