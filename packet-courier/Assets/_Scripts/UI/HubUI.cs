using UnityEngine;

public class HubUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateLabel(canvas.transform, "RUN COMPLETE", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 34);
        CreateLabel(canvas.transform, HubSubtitle(), new Vector2(0.3f, 0.58f), new Vector2(0.7f, 0.66f), 16);
        CreateButton(canvas.transform, "Back to Menu", new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.4f), () => GameManager.Instance?.ReturnToMenu());
    }

    private static string HubSubtitle()
    {
        var gm = GameManager.Instance;
        if (gm == null) return "Run complete.";

        int cleared = gm.currentWorldIndex + 1;
        if (cleared + 1 <= gm.worldsUnlocked.Length)
            return $"World {cleared} cleared. World {cleared + 1} unlocked.";
        return $"World {cleared} cleared. All worlds unlocked.";
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Hub_Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private static void CreateLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, int fontSize)
    {
        var go = new GameObject("Label_" + text.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<UnityEngine.UI.Text>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    private static void CreateButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.3f, 0.2f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        CreateLabel(go.transform, label, Vector2.zero, Vector2.one, 20);
    }
}
