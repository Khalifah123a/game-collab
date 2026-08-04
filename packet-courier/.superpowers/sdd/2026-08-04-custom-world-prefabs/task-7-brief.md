### Task 7: Pindahkan `CameraFollow` ke file sendiri

**Files:**
- Create: `Assets/_Scripts/Core/CameraFollow.cs`
- Modify: `Assets/_Scripts/PrototypeBootstrapper.cs` (hapus class `CameraFollow`, baris 391–409)

**Interfaces:**
- Produces: `CameraFollow` (public class, API tidak berubah) — dipakai Task 9 (Setup Scene). Setelah Task 10 menghapus bootstrapper, camera tetap tersedia.

- [ ] **Step 1: Buat `CameraFollow.cs`**

```csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(4, 2, -10);
    public float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player) target = player.transform;
            else return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
```

- [ ] **Step 2: Hapus class `CameraFollow` dari `PrototypeBootstrapper.cs`**

Hapus seluruh blok mulai `public class CameraFollow : MonoBehaviour` sampai akhir file (baris 391–409). File bootstrapper berakhir setelah `SetupTiledSprite` method (kurung tutup `}` baris 389).

- [ ] **Step 3: Verify compile**

Buka Unity → Console. Expected: tidak ada error kompilasi (class `CameraFollow` kini satu-satunya di file barunya).

---

