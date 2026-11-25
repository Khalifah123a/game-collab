using UnityEngine;

/// <summary>
/// Holographic incoming arrival data portal placed on the far-left wall of the start safe zone chamber.
/// Displays an active incoming server gateway link with rotating energy ring and cyan vortex.
/// </summary>
public class SafeZoneArrivalPortal : MonoBehaviour
{
    private SpriteRenderer _portalRenderer;
    private Transform _ringTransform;
    private TextMesh _statusText;
    private float _pulseTimer;

    public static SafeZoneArrivalPortal Spawn(Vector3 position, Transform parent = null)
    {
        var go = new GameObject("ArrivalPortal");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.AddComponent<SafeZoneArrivalPortal>();
    }

    private void Awake()
    {
        BuildVisuals();
    }

    private void BuildVisuals()
    {
        // 1. Base Portal Arch & Vortex
        var portalGo = new GameObject("PortalArch", typeof(SpriteRenderer));
        portalGo.transform.SetParent(transform, false);
        portalGo.transform.localPosition = Vector3.zero;
        _portalRenderer = portalGo.GetComponent<SpriteRenderer>();
        _portalRenderer.sprite = Resources.Load<Sprite>("GeneratedArt/portal_warp");
        _portalRenderer.color = new Color(0.1f, 0.95f, 1f, 0.95f);
        _portalRenderer.sortingOrder = 5;

        // 2. Swirling Energy Ring
        var ringGo = new GameObject("PortalRing", typeof(SpriteRenderer));
        ringGo.transform.SetParent(transform, false);
        ringGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        _ringTransform = ringGo.transform;
        var ringSr = ringGo.GetComponent<SpriteRenderer>();
        ringSr.sprite = Resources.Load<Sprite>("GeneratedArt/portal_ring");
        ringSr.color = new Color(0.2f, 1f, 0.9f, 0.9f);
        ringSr.sortingOrder = 6;
        ringGo.transform.localScale = Vector3.one * 1.25f;

        // 3. Status Hologram Text
        var textGo = new GameObject("PortalStatus", typeof(TextMesh));
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        _statusText = textGo.GetComponent<TextMesh>();
        _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _statusText.fontSize = 28;
        _statusText.characterSize = 0.045f;
        _statusText.anchor = TextAnchor.MiddleCenter;
        _statusText.alignment = TextAlignment.Center;
        _statusText.text = "< ARRIVAL GATEWAY >\nSERVER LINK ONLINE";
        _statusText.color = new Color(0.3f, 1f, 0.85f, 0.9f);

        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 15;
    }

    private void Update()
    {
        _pulseTimer += Time.deltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * 3.5f);

        if (_ringTransform != null)
        {
            _ringTransform.Rotate(0f, 0f, -40f * Time.deltaTime);
            float scale = 1.15f + pulse * 0.12f;
            _ringTransform.localScale = new Vector3(scale, scale, 1f);
        }

        if (_statusText != null)
        {
            _statusText.color = new Color(0.3f, 1f, 0.85f, 0.75f + pulse * 0.25f);
        }
    }
}
