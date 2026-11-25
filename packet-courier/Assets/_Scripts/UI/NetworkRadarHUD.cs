using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holographic Network Radar & Compass HUD.
/// Provides real-time navigation telemetry for high-speed parkour:
/// 1. Direction and distance to the Exit Gateway (Server B).
/// 2. Direction and distance to the active or nearest TCP Checkpoint.
/// 3. Radar sweep ping when a Secret Memory Core or hidden cache is within 38m.
/// </summary>
public class NetworkRadarHUD : MonoBehaviour
{
    private Text _telemetryText;
    private Image _backdrop;
    private ServerB_Goal _cachedGoal;
    private Checkpoint_TCP[] _checkpoints;
    private SecretMemoryCore _cachedSecret;
    private HiddenHermitNPC _cachedHermit;

    private float _scanTimer;
    private float _pulseTimer;

    public static NetworkRadarHUD Attach(Transform parent)
    {
        var go = new GameObject("NetworkRadarHUD", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<NetworkRadarHUD>();
    }

    private void Start()
    {
        BuildUI();
        ScanTargets();
    }

    private void BuildUI()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.58f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, -96f);
        rt.offsetMax = new Vector2(-16f, -68f);

        _backdrop = gameObject.AddComponent<Image>();
        _backdrop.color = new Color(0.03f, 0.05f, 0.09f, 0.85f);
        _backdrop.raycastTarget = false;

        // Cyan right-edge accent line matching quest banner style
        var lineGo = new GameObject("RadarAccent", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(transform, false);
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(1f, 0f);
        lineRt.anchorMax = new Vector2(1f, 1f);
        lineRt.offsetMin = new Vector2(-3f, 0f);
        lineRt.offsetMax = Vector2.zero;
        lineGo.GetComponent<Image>().color = new Color(0.1f, 0.85f, 0.95f, 0.9f);

        var textGo = new GameObject("RadarLabel", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 0f);
        textRt.offsetMax = new Vector2(-12f, 0f);

        _telemetryText = textGo.GetComponent<Text>();
        _telemetryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _telemetryText.fontSize = 12;
        _telemetryText.alignment = TextAnchor.MiddleRight;
        _telemetryText.color = new Color(0.3f, 0.95f, 1f, 0.95f);
        _telemetryText.raycastTarget = false;
    }

    private void ScanTargets()
    {
        _cachedGoal = FindAnyObjectByType<ServerB_Goal>();
        _checkpoints = FindObjectsByType<Checkpoint_TCP>();
        _cachedSecret = FindAnyObjectByType<SecretMemoryCore>();
        _cachedHermit = FindAnyObjectByType<HiddenHermitNPC>();
    }

    private void Update()
    {
        _scanTimer += Time.deltaTime;
        if (_scanTimer > 2.0f)
        {
            _scanTimer = 0f;
            ScanTargets();
        }

        var player = FindAnyObjectByType<PlayerController>();
        if (player == null || _telemetryText == null) return;

        Vector2 pPos = player.transform.position;
        _pulseTimer += Time.deltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * 4f);

        // 1. Gateway Telemetry
        string gatewayStr = "";
        if (_cachedGoal != null)
        {
            Vector2 gPos = _cachedGoal.transform.position;
            float dist = Vector2.Distance(pPos, gPos);
            string arrow = GetDirectionArrow(pPos, gPos);

            if (dist <= 6f)
            {
                gatewayStr = "<color=#00FFAA>GATEWAY IN REACH</color>";
            }
            else
            {
                gatewayStr = string.Format("<color=#00F0FF>GATEWAY {0} {1}m</color>", arrow, Mathf.RoundToInt(dist));
            }
        }

        // 2. Checkpoint Telemetry
        string cpStr = "";
        var gm = GameManager.Instance;
        if (gm != null && gm.hasCheckpoint)
        {
            float cpDist = Vector2.Distance(pPos, (Vector2)gm.checkpointPosition);
            string arrow = GetDirectionArrow(pPos, (Vector2)gm.checkpointPosition);
            cpStr = string.Format("<color=#FFD700>TCP {0} {1}m</color>", arrow, Mathf.RoundToInt(cpDist));
        }
        else if (_checkpoints != null && _checkpoints.Length > 0)
        {
            // Find closest ahead checkpoint
            Checkpoint_TCP nearest = null;
            float minAheadDist = float.MaxValue;
            foreach (var cp in _checkpoints)
            {
                if (cp == null) continue;
                float d = cp.transform.position.x - pPos.x;
                if (d > 0 && d < minAheadDist)
                {
                    minAheadDist = d;
                    nearest = cp;
                }
            }
            if (nearest != null)
            {
                float totalDist = Vector2.Distance(pPos, nearest.transform.position);
                cpStr = string.Format("<color=#FFB030>NEXT TCP ► {0}m</color>", Mathf.RoundToInt(totalDist));
            }
        }

        // 3. Secret Cache Radar Ping (Within 38m)
        string cacheStr = "";
        Vector2? secretPos = null;
        if (_cachedSecret != null) secretPos = _cachedSecret.transform.position;
        else if (_cachedHermit != null) secretPos = _cachedHermit.transform.position;

        if (secretPos.HasValue)
        {
            float sDist = Vector2.Distance(pPos, secretPos.Value);
            if (sDist <= 38f)
            {
                string sArrow = GetDirectionArrow(pPos, secretPos.Value);
                cacheStr = string.Format("<color=#FF44FF>⚡ CACHE {0} {1}m</color>", sArrow, Mathf.RoundToInt(sDist));
            }
        }

        // Combine into unified telemetry ribbon
        string text = string.Format("RADAR: {0}   |   {1}", gatewayStr, cpStr);
        if (!string.IsNullOrEmpty(cacheStr))
        {
            text += string.Format("   |   {0}", cacheStr);
        }

        _telemetryText.text = text;
    }

    private static string GetDirectionArrow(Vector2 origin, Vector2 target)
    {
        float dx = target.x - origin.x;
        float dy = target.y - origin.y;

        if (Mathf.Abs(dx) > Mathf.Abs(dy) * 1.5f)
        {
            return dx > 0 ? "►" : "◄";
        }
        else if (Mathf.Abs(dy) > Mathf.Abs(dx) * 1.5f)
        {
            return dy > 0 ? "▲" : "▼";
        }
        else
        {
            if (dx > 0 && dy > 0) return "▲►";
            if (dx > 0 && dy < 0) return "▼►";
            if (dx < 0 && dy > 0) return "◄▲";
            return "◄▼";
        }
    }
}
