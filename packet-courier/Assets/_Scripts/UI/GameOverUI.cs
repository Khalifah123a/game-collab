using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateLabel(canvas.transform, "PACKET LOST", new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), 34);
        CreateLabel(canvas.transform, "TTL depleted. Connection severed.", new Vector2(0.3f, 0.6f), new Vector2(0.7f, 0.68f), 16);
        CreateButton(canvas.transform, "Retry Segment", new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.5f), () => GameManager.Instance?.RetrySegment());
        CreateButton(canvas.transform, "Back to Menu", new Vector2(0.4f, 0.25f), new Vector2(0.6f, 0.35f), () => GameManager.Instance?.ReturnToMenu());
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("GameOver_Canvas");
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
        txt.color = new Color(1f, 0.6f, 0.6f);
    }

    private static void CreateButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.35f, 0.15f, 0.15f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        CreateLabel(go.transform, label, Vector2.zero, Vector2.one, 20);
    }
}
