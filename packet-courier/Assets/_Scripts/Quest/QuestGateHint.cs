using UnityEngine;

/// <summary>
/// World-space quest label shown by the final goal while the mandatory package quest is
/// still blocking world progress. Self-hides again after a few seconds; the goal refreshes
/// it for as long as the player stands at the locked exit.
/// </summary>
public class QuestGateHint : MonoBehaviour
{
    [TextArea]
    public string message = "";
    public float visibleSeconds = 2.5f;
    public float fadeSeconds = 0.4f;
    public float worldHeight = 2.8f;

    private TextMesh _text;
    private MeshRenderer _renderer;
    private float _hideAt;
    private bool _showing;

    /// <summary>Builds a hint above <paramref name="anchor"/>, or reuses one already there.</summary>
    public static QuestGateHint Create(Transform anchor, string message)
    {
        var existing = FindAnyObjectByType<QuestGateHint>();
        if (existing != null)
        {
            existing.AttachTo(anchor);
            return existing;
        }

        var go = new GameObject("QuestGateHint");
        var hint = go.AddComponent<QuestGateHint>();
        hint.AttachTo(anchor);
        hint.Show(message);
        return hint;
    }

    public void AttachTo(Transform anchor)
    {
        if (anchor != null) transform.position = anchor.position + Vector3.up * worldHeight;
    }

    /// <summary>Builds a hint at a world position (e.g. the end safe zone anchor point).</summary>
    public static QuestGateHint Create(Vector3 position, string message)
    {
        var existing = FindAnyObjectByType<QuestGateHint>();
        if (existing != null)
        {
            existing.AttachTo(position);
            return existing;
        }

        var go = new GameObject("QuestGateHint");
        var hint = go.AddComponent<QuestGateHint>();
        hint.AttachTo(position);
        hint.Show(message);
        return hint;
    }

    public void AttachTo(Vector3 position)
    {
        transform.position = position + Vector3.up * worldHeight;
    }

    public bool IsShowing
    {
        get { return _showing; }
    }

    public void Show(string text)
    {
        EnsureLabel();
        _hideAt = Time.unscaledTime + visibleSeconds;
        _showing = true;
        if (_text.text == text)
        {
            RefreshColor();
            return;
        }
        _text.text = text;
        Debug.Log("[QuestGate] " + text, this);
    }

    public void Hide()
    {
        _showing = false;
        _hideAt = 0f;
        if (_text != null) _text.color = new Color(1f, 0.85f, 0.35f, 0f);
    }

    private void Update()
    {
        if (!_showing || _text == null) return;
        float remaining = _hideAt - Time.unscaledTime;
        if (remaining <= 0f)
        {
            Hide();
            return;
        }
        var color = new Color(1f, 0.85f, 0.35f, Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeSeconds)));
        _text.color = color;
        if (_renderer != null) _renderer.sortingOrder = 20;
    }

    private void EnsureLabel()
    {
        if (_text != null) return;
        _text = gameObject.AddComponent<TextMesh>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 64;
        _text.characterSize = 0.05f;
        _text.anchor = TextAnchor.LowerCenter;
        _text.alignment = TextAlignment.Center;
        _text.color = new Color(1f, 0.85f, 0.35f, 1f);
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null) _renderer.sortingOrder = 20;
    }

    private void RefreshColor()
    {
        if (_text != null) _text.color = new Color(1f, 0.85f, 0.35f, 1f);
    }
}
