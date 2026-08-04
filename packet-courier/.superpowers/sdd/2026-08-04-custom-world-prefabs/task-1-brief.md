### Task 1: Tambah `sprintHeld` di InputManager

**Files:**
- Modify: `Assets/_Scripts/Core/InputManager.cs`

**Interfaces:**
- Produces: `InputManager.sprintHeld` (public bool, true selama Left Shift ditekan). Dibaca Task 3.

- [ ] **Step 1: Tambah field `sprintHeld`**

Di `InputManager.cs`, di blok `[Header("Buttons")]` (setelah baris `public bool powerUpPressed;`):

```csharp
    public bool sprintHeld;
```

- [ ] **Step 2: Set `sprintHeld` di keyboard handler**

Di `HandleKeyboardInput()`, tambahkan paling atas (sebelum `moveInput`):

```csharp
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
```

Catatan: `sprintHeld` TIDAK dibersihkan di `ClearPressedFlags()` karena ini state "sedang tahan", bukan "baru ditekan".

- [ ] **Step 3: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

