using UnityEngine;

/// <summary>
/// Exit door to the next world, standing on the departure safe-zone pad.
/// Pure placeholder frame (two posts + lintel), no collider: world progression
/// still runs through ServerB_Goal. Replace visuals with final art later.
/// </summary>
public class WorldDoor : MonoBehaviour
{
    public Color frameColor = new Color(0.4f, 1f, 0.6f, 1f);
    public Vector2 doorSize = new Vector2(1.8f, 2.8f);
    public float beamThick = 0.35f;
    public int sortingOrder = 3;

    private bool _built;
    private static Sprite _box;

    private void Awake()
    {
        BuildFrame();
    }

    private void BuildFrame()
    {
        if (_built) return;
        _built = true;
        float postH = Mathf.Max(0.5f, doorSize.y);
        AddBar("Post West", new Vector3(-doorSize.x * 0.5f, postH * 0.5f, 0f), new Vector2(beamThick * 1.4f, postH));
        AddBar("Post East", new Vector3(doorSize.x * 0.5f, postH * 0.5f, 0f), new Vector2(beamThick * 1.4f, postH));
        AddBar("Lintel", new Vector3(0f, postH + beamThick * 0.6f, 0f), new Vector2(doorSize.x + beamThick * 2.8f, beamThick * 1.2f));

        // Neon Sign
        var textGo = new GameObject("AirlockSign");
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0f, postH + beamThick * 1.35f, 0f);
        var tm = textGo.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 28;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.text = "[ AIRLOCK // ZONE BOUNDARY ]";
        tm.color = new Color(0.2f, 0.95f, 1f, 0.95f);
        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = sortingOrder + 1;

        // Laser Curtain Partition (Soft glowing energy boundary)
        var laserGo = new GameObject("LaserCurtain");
        laserGo.transform.SetParent(transform, false);
        laserGo.transform.localPosition = new Vector3(0f, postH * 0.5f, 0f);
        var laserSr = laserGo.AddComponent<SpriteRenderer>();
        laserSr.sprite = BoxSprite();
        laserSr.color = new Color(0.1f, 0.95f, 1f, 0.22f);
        laserSr.sortingOrder = sortingOrder - 1;
        var unit = BoxSprite().bounds.size;
        laserGo.transform.localScale = new Vector3((doorSize.x - beamThick) / unit.x, postH / unit.y, 1f);
    }

    private void AddBar(string name, Vector3 localPos, Vector2 size)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = localPos;
        var sprite = child.AddComponent<SpriteRenderer>();
        sprite.sprite = BoxSprite();
        var color = frameColor;
        color.a = 0.95f;
        sprite.color = color;
        sprite.sortingOrder = sortingOrder;
        var unit = BoxSprite().bounds.size;
        child.transform.localScale = new Vector3(size.x / unit.x, size.y / unit.y, 1f);
    }

    private static Sprite BoxSprite()
    {
        if (_box != null) return _box;
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = "Door_Box" };
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        _box = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        _box.name = "Door_Box";
        return _box;
    }
}
