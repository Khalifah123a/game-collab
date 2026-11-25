#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static partial class EditorGenerator
{
    private static void PlaceSpecial(char tile, int col, int row, Transform parent)
    {
        if (tile == 'a' || tile == 'b' || tile == 'c')
        {
            var go = PlacePrefab(PrefabRoot + "/Firewall_Gate.prefab", col, row, parent);
            var gate = go.GetComponent<Gate_Firewall>();
            gate.requiredPort = tile == 'a' ? 80 : tile == 'b' ? 22 : 443;
            gate.requireEncrypted = tile != 'a';
            var collider = go.GetComponent<BoxCollider2D>();
            collider.isTrigger = false; collider.size = new Vector2(1, 18);
            collider.offset = new Vector2(0, 8);
            var label = new GameObject("Port").AddComponent<TextMesh>();
            label.transform.SetParent(go.transform, false); label.transform.localPosition = new Vector3(-3, 2.5f, 0);
            label.text = gate.requiredPort + (gate.requireEncrypted ? " ENC" : ""); label.fontSize = 40; label.characterSize = 0.08f;
            label.GetComponent<MeshRenderer>().sortingOrder = 20;
            if (gate.gateClosedVisual)
            {
                gate.gateClosedVisual.transform.localScale = Vector3.one;
                gate.gateClosedVisual.transform.localPosition = new Vector3(0, 8, 0);
                var closed = gate.gateClosedVisual.GetComponent<SpriteRenderer>();
                closed.drawMode = SpriteDrawMode.Sliced; closed.size = new Vector2(1, 18);
            }
            return;
        }
        if (tile == 'T')
        {
            var turret = PlacePrefab(PrefabRoot + "/Laser_Beam.prefab", col, row, parent);
            turret.transform.localScale = new Vector3(1, 1, 1);
            turret.transform.rotation = Quaternion.Euler(0, 0, 90);
            turret.GetComponent<BoxCollider2D>().size = new Vector2(0.3f, 7);
            turret.GetComponent<Laser_Beam>().onDuration = 1.2f;
            turret.GetComponent<Laser_Beam>().offDuration = 1.8f;
            turret.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced;
            turret.GetComponent<SpriteRenderer>().size = new Vector2(0.3f, 7);
            return;
        }
        var special = new GameObject(tile + "_" + col + "_" + row);
        special.transform.SetParent(parent); special.transform.position = new Vector3(col, -row, 0);
        var box = special.AddComponent<BoxCollider2D>();
        if (tile == 'Z')
        {
            box.isTrigger = true; box.size = new Vector2(36, 13); box.offset = new Vector2(17, 5);
            special.AddComponent<GravityZone>();
            return;
        }
        var sprite = special.AddComponent<SpriteRenderer>();
        sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/platform_moving.png");
        sprite.drawMode = SpriteDrawMode.Sliced; sprite.size = new Vector2(2.5f, 0.5f); sprite.sortingOrder = 4;
        box.size = sprite.size;
        if (tile == 'D')
        {
            box.isTrigger = true; box.size = new Vector2(1.5f, 2.5f); special.AddComponent<DockingPoint>();
            sprite.color = new Color(1, 0.8f, 0.3f);
        }
        else
        {
            var timed = special.AddComponent<TimedPlatform>(); timed.crumble = tile == 'R'; timed.phase = col % 3 * 0.3f;
            sprite.color = tile == 'H' ? new Color(0.4f,0.9f,1) : new Color(1,0.7f,0.35f);
        }
    }
}
#endif
