#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class EditorGenerator
{
    private const string SafeZonesGroup = "SafeZones";
    private static readonly Vector2 SafeZoneSize = new Vector2(11f, 9f);

    [MenuItem("Tools/Packet Courier/Install Safe Zones")]
    public static void InstallSafeZones()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scene before installing safe zones.");

        var report = new List<string>();
        for (int world = 0; world < WorldRules.Count; world++)
        {
            int segments = WorldRules.Segments(world);
            for (int seg = 1; seg <= segments; seg++)
            {
                string segPath = $"{ScenesRoot}/World{world + 1}_Segment{seg}.unity";
                if (!File.Exists(segPath))
                {
                    report.Add($"SKIP: {segPath} does not exist.");
                    continue;
                }

                report.Add(InstallSafeZoneAt(segPath, "Arrival", FindPlayerSpawn, "player spawn", true));
                report.Add(InstallSafeZoneAt(segPath, "Departure", FindGoal, "server B portal", false));
            }
        }

        EditorSceneManager.OpenScene(ScenesRoot + "/Menu.unity", OpenSceneMode.Single);
        Debug.Log("[SafeZones] " + string.Join("\n[SafeZones] ", report));
    }

    private static string InstallSafeZoneAt(string scenePath, string label, Func<Transform> anchorFinder, string anchorName, bool isArrival)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var anchor = anchorFinder();
        if (anchor == null)
            return $"SKIP: {Path.GetFileNameWithoutExtension(scenePath)} has no {anchorName} to anchor a {label.ToLowerInvariant()} safe zone.";

        // Idempotent: rebuilding the group keeps repeat runs free of duplicates.
        var existing = GameObject.Find(SafeZonesGroup);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        float platformW = SafeZoneSize.x + 4f;
        const float padThick = 1.5f;

        // Match the existing ground layer so pads, ground checks and snaps all agree.
        int groundLayer = 0;
        float edgeMin = anchor.position.x;
        if (TryLevelBounds(out float bMin, out _, out int minLayer, out int maxLayer))
        {
            edgeMin = bMin;
            groundLayer = isArrival ? minLayer : maxLayer;
        }

        var root = new GameObject(SafeZonesGroup);

        Vector3 zoneCenter;
        string spawnNote = "";
        const float roomHeight = 5.5f;

        if (isArrival)
        {
            float feetY = anchor.position.y - 0.5f;
            float edgeX = edgeMin;
            float padLeft = edgeX - (platformW - 2f);
            var padCenter = new Vector3(padLeft + platformW * 0.5f, feetY - padThick * 0.5f, 0f);

            // Construct full enclosed Cyber Chamber Room with corner exit boundary door
            BuildSafeChamber(root.transform, "SafeChamber_Start", padCenter, platformW, padThick, roomHeight, groundLayer, doorOnRight: true);
            zoneCenter = new Vector3(padCenter.x, anchor.position.y + 1.5f, 0f);

            // Snap the spawn cleanly onto the pad inside the chamber
            var body = anchor.GetComponent<Collider2D>();
            float halfH = body != null ? body.bounds.extents.y : 0.5f;
            float padTop = padCenter.y + padThick * 0.5f;
            var spawnPos = new Vector3(padCenter.x - 2.5f, padTop + halfH, anchor.position.z);
            var level = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            if (level != null && level.playerSpawn != null) level.playerSpawn.position = spawnPos;
            anchor.position = spawnPos;
            spawnNote = " spawn relocated inside the cyber chamber";
        }
        else
        {
            Vector3 goalOld = anchor.position;
            float topY = goalOld.y - 1f;
            float padLeft = goalOld.x - 3f;
            var padCenter = new Vector3(padLeft + platformW * 0.5f, topY - padThick * 0.5f, 0f);

            // Construct departure chamber
            BuildSafeChamber(root.transform, "SafeChamber_End", padCenter, platformW, padThick, roomHeight, groundLayer, doorOnRight: false);

            Vector3 goalNew = new Vector3(padCenter.x, topY + 1.2f, goalOld.z);
            anchor.position = goalNew;
            var level = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            if (level != null && level.serverBPoint != null) level.serverBPoint.position = goalNew;
            zoneCenter = new Vector3(goalNew.x, goalNew.y + 1.5f, 0f);
            spawnNote = " goal relocated inside the departure chamber";
        }

        var zoneObject = new GameObject($"SafeZone_{label}");
        zoneObject.transform.SetParent(root.transform, false);
        zoneObject.transform.position = zoneCenter;
        var box = zoneObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = SafeZoneSize;
        zoneObject.AddComponent<SafeZone>();

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        return $"PASS: {scene.name} gained a {label.ToLowerInvariant()} cyber chamber room with pad at x={zoneCenter.x:0.#}.{spawnNote}";
    }

    private static void BuildSafeChamber(Transform parent, string prefix, Vector3 floorCenter, float width, float floorThick, float roomHeight, int layer, bool doorOnRight)
    {
        // 1. Solid Floor
        BuildPad(parent, $"{prefix}_Floor", floorCenter, width, floorThick, layer);

        float padTop = floorCenter.y + floorThick * 0.5f;
        float ceilingY = padTop + roomHeight + floorThick * 0.5f;
        var ceilingCenter = new Vector3(floorCenter.x, ceilingY, floorCenter.z);

        // 2. Solid Ceiling
        BuildPad(parent, $"{prefix}_Ceiling", ceilingCenter, width, floorThick, layer);

        // 3. Solid Back Wall (encloses the safe room)
        float wallThick = 1.4f;
        float wallX = doorOnRight ? (floorCenter.x - width * 0.5f + wallThick * 0.5f) : (floorCenter.x + width * 0.5f - wallThick * 0.5f);
        float wallY = padTop + roomHeight * 0.5f;
        var wallCenter = new Vector3(wallX, wallY, floorCenter.z);
        BuildPad(parent, $"{prefix}_BackWall", wallCenter, wallThick, roomHeight, layer);

        // 4. Chamber Dark Cyber Backdrop
        var backdrop = new GameObject($"{prefix}_Backdrop");
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3(floorCenter.x, wallY, 0.2f);
        var bgSr = backdrop.AddComponent<SpriteRenderer>();
        bgSr.sprite = PadSubSprite();
        bgSr.drawMode = SpriteDrawMode.Tiled;
        bgSr.tileMode = SpriteTileMode.Continuous;
        bgSr.size = new Vector2(width - wallThick, roomHeight);
        bgSr.color = new Color(0.26f, 0.32f, 0.42f, 0.96f);
        bgSr.sortingOrder = -5;

        // 5. Ceiling Neon Light Strip
        var lightBar = new GameObject($"{prefix}_CeilingLight");
        lightBar.transform.SetParent(parent, false);
        lightBar.transform.position = new Vector3(floorCenter.x, padTop + roomHeight - 0.1f, 0f);
        var lightSr = lightBar.AddComponent<SpriteRenderer>();
        var boxSpr = NPC_Base.BoxSprite();
        lightSr.sprite = boxSpr;
        lightSr.color = new Color(0.1f, 0.95f, 1f, 0.85f);
        lightSr.sortingOrder = 2;
        var unit = boxSpr.bounds.size;
        lightBar.transform.localScale = new Vector3((width - 3.5f) / unit.x, 0.12f / unit.y, 1f);

        // 6. Corner Boundary Door dividing Safe Zone and outside world
        float doorX = doorOnRight ? (floorCenter.x + width * 0.5f - 1.2f) : (floorCenter.x - width * 0.5f + 1.2f);
        var doorPos = new Vector3(doorX, padTop, 0f);
        var doorGo = new GameObject("WorldDoor");
        doorGo.transform.SetParent(parent, false);
        doorGo.transform.position = doorPos;
        var doorComp = doorGo.AddComponent<WorldDoor>();
        doorComp.doorSize = new Vector2(2.0f, roomHeight * 0.65f);
    }

    private static void BuildPad(Transform parent, string name, Vector3 center, float width, float thick, int layer)
    {
        var pad = new GameObject(name);
        pad.transform.SetParent(parent, false);
        pad.transform.position = center;
        pad.layer = layer;
        pad.transform.localScale = Vector3.one;

        var padBox = pad.AddComponent<BoxCollider2D>();
        padBox.size = new Vector2(width, thick);
        padBox.offset = Vector2.zero;
        padBox.isTrigger = false;

        var visual = pad.AddComponent<SpriteRenderer>();
        bool isBackWall = name.EndsWith("_BackWall");
        visual.sprite = isBackWall ? PadSubSprite() : PadSprite();
        visual.drawMode = SpriteDrawMode.Tiled;
        visual.tileMode = SpriteTileMode.Continuous;
        visual.size = new Vector2(width, thick);
        visual.color = Color.white;
        visual.sortingOrder = 0;

        if (!isBackWall && thick > 2.1f)
        {
            WorldThemeApplier.ApplySegmentedGround(visual, PadSprite(), PadSubSprite(), 2.0f);
        }
    }

    /// <summary>Horizontal extremes of the solid level geometry plus the layers they sit on.</summary>
    private static bool TryLevelBounds(out float minX, out float maxX, out int minLayer, out int maxLayer)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minLayer = 0;
        maxLayer = 0;
        bool any = false;
        foreach (var col in UnityEngine.Object.FindObjectsByType<Collider2D>())
        {
            if (col == null || col.isTrigger) continue;
            var go = col.gameObject;
            if (go.name.StartsWith("SafePad_") || go.name.StartsWith("SafeChamber_")) continue;
            if (go.GetComponentInParent<PlayerController>() != null) continue;
            if (go.GetComponentInParent<SafeZone>() != null) continue;
            var b = col.bounds;
            if (b.min.x < minX) { minX = b.min.x; minLayer = go.layer; }
            if (b.max.x > maxX) { maxX = b.max.x; maxLayer = go.layer; }
            any = true;
        }
        return any;
    }

    private static Sprite _padSprite;
    private static Sprite _padSubSprite;

    private static Sprite PadSubSprite()
    {
        if (_padSubSprite != null) return _padSubSprite;
        _padSubSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/safepad_sub.png");
        return _padSubSprite != null ? _padSubSprite : PadSprite();
    }

    private static Sprite PadSprite()
    {
        if (_padSprite != null) return _padSprite;
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/safepad.png");
        if (existing != null)
        {
            _padSprite = existing;
            return _padSprite;
        }

        const int w = 32;
        const int h = 16;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "SafePad",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (y >= h - 2)
                    pixels[y * w + x] = new Color(0f, 1f, 0.85f, 1f); // Neon top strip
                else if (y == h - 3)
                    pixels[y * w + x] = new Color(0f, 0.7f, 0.6f, 1f); // Secondary glow
                else if (y < 2)
                    pixels[y * w + x] = new Color(0.06f, 0.08f, 0.12f, 1f); // Base chassis
                else if (x == 0 || x == w - 1)
                    pixels[y * w + x] = new Color(0.12f, 0.16f, 0.22f, 1f); // Panel edge
                else if (x % 8 == 0)
                    pixels[y * w + x] = new Color(0.15f, 0.20f, 0.28f, 1f); // Vertical groove
                else
                    pixels[y * w + x] = new Color(0.10f, 0.13f, 0.18f, 1f); // Metallic chassis
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        _padSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        _padSprite.name = "SafePad";
        return _padSprite;
    }

    private static Transform FindPlayerSpawn()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        return player != null ? player.transform : null;
    }

    private static Transform FindGoal()
    {
        var goal = UnityEngine.Object.FindAnyObjectByType<ServerB_Goal>();
        return goal != null ? goal.transform : null;
    }
}
#endif
