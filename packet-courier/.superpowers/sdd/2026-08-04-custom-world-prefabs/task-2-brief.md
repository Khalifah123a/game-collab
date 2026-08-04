### Task 2: Buat `PlayerAnimator.cs`

**Files:**
- Create: `Assets/_Scripts/Player/PlayerAnimator.cs`

**Interfaces:**
- Consumes: `Animator` + `SpriteRenderer` (dari GameObject yang sama).
- Produces: `PlayerAnimator.UpdateState(bool isMoving, float speedMultiplier, float facing)` — dipanggil Task 3. Men-set param animator `isMoving` / `speedMultiplier` / `facing`, set `animator.speed = speedMultiplier`, dan `spriteRenderer.flipX = facing < 0`.

- [ ] **Step 1: Tulis file baru**

```csharp
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private bool _warned;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateState(bool isMoving, float speedMultiplier, float facing)
    {
        if (animator == null)
        {
            if (!_warned)
            {
                Debug.LogWarning("[PlayerAnimator] Animator belum di-set. Skip animasi.", this);
                _warned = true;
            }
            return;
        }

        animator.speed = speedMultiplier;
        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("speedMultiplier", speedMultiplier);
        animator.SetFloat("facing", facing);

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;
    }
}
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi.

---

