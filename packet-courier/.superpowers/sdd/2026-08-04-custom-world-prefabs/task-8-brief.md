### Task 8: Buat `EditorGenerator.cs` — kerangka + sprite + AnimatorController

**Files:**
- Create: `Assets/_Scripts/Editor/EditorGenerator.cs`
- Create (dihasilkan): `Assets/_Prefabs/_GeneratedArt/*.png` + `Assets/_Prefabs/PlayerAnimator.controller`

**Interfaces:**
- Produces (dipakai Task 9 & 10):
  - `EditorGenerator.GeneratePrefabs()` — menu item, titik masuk utama.
  - `EditorGenerator.SetupScene()` — menu item (diimplementasikan di Task 10).
  - Helper `EnsureFolder(string path)`, `LoadOrCreateSprite(string name, int w, int h, Color color, float ppu)`, `LoadWalkSprite(int index)`, `CreateSolidSpriteAsset(...)`, `CreatePlayerAnimatorController()`.
- Path konstanta: `PrefabRoot = "Assets/_Prefabs"`, `ArtRoot = PrefabRoot + "/_GeneratedArt"`, `WalkSpriteDir = "Assets/walk"`.

- [ ] **Step 1: Tulis kerangka generator + helper sprite**

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditorGenerator
{
    private const string PrefabRoot = "Assets/_Prefabs";
    private const string ArtRoot = PrefabRoot + "/_GeneratedArt";
    private const string WalkSpriteDir = "Assets/walk";

    [MenuItem("Tools/Packet Courier/Generate Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureFolder(PrefabRoot);
        EnsureFolder(ArtRoot);
        EnsureWalkSpriteImportSettings();

        // Implementasi pembuatan prefab ada di Task 9 & 10 (GeneratePlayer + teman-temannya dipanggil di sini).
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void EnsureWalkSpriteImportSettings()
    {
        if (!Directory.Exists(WalkSpriteDir)) return;

        foreach (string path in Directory.GetFiles(WalkSpriteDir, "*.png"))
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadWalkSprite(int index)
    {
        string name = index == 0 ? "walk" : $"walk{index}";
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{WalkSpriteDir}/{name}.png");
    }

    private static Sprite LoadOrCreateSprite(string name, int w, int h, Color color, float ppu)
    {
        string path = $"{ArtRoot}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
```

- [ ] **Step 2: Verify compile**

Buka Unity → Console. Expected: tidak ada error. Menu `Tools > Packet Courier > Generate Prefabs` muncul (belum melakukan apa-apa selain bikin folder + import settings).

---


---
PRE-FLIGHT DECISION (Finding A): This task must produce a partial class. Write "public static partial class EditorGenerator" (NOT "public static class EditorGenerator"). The SetupScene half (Task 10) lives in a separate file EditorGeneratorSetup.cs as the other half of the same partial class. Menu item stays "Tools > Packet Courier > ...". Both files stay under 500 lines.

