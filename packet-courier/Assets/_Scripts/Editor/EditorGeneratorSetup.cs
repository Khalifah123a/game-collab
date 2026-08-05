#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static partial class EditorGenerator
{
    private static void CreateFirewallGatePrefab()
    {
        string path = PrefabRoot + "/Firewall_Gate.prefab";
        GameObject go = new GameObject("Firewall_Gate");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 2f);
        col.isTrigger = true;

        var gate = go.AddComponent<Gate_Firewall>();
        gate.requiredPort = 80;
        gate.requireEncrypted = false;

        var closed = new GameObject("Visual_Closed");
        closed.transform.SetParent(go.transform);
        var srClosed = closed.AddComponent<SpriteRenderer>();
        srClosed.sprite = LoadOrCreateSprite("gate_closed", 10, 20, new Color(1f, 0.5f, 0f), 32f);
        srClosed.sortingOrder = 6;

        var open = new GameObject("Visual_Open");
        open.transform.SetParent(go.transform);
        var srOpen = open.AddComponent<SpriteRenderer>();
        srOpen.sprite = LoadOrCreateSprite("gate_open", 10, 20, new Color(0.4f, 1f, 0.4f), 32f);
        srOpen.sortingOrder = 6;

        gate.gateOpenVisual = open;
        gate.gateClosedVisual = closed;
        open.SetActive(false);

        SavePrefab(go, path);
    }

    private static void CreateCheckpointPrefab()
    {
        string path = PrefabRoot + "/Checkpoint_TCP.prefab";
        GameObject go = new GameObject("Checkpoint_TCP");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1.5f);
        col.isTrigger = true;

        var cp = go.AddComponent<Checkpoint_TCP>();

        var inactive = new GameObject("Visual_Inactive");
        inactive.transform.SetParent(go.transform);
        var srInactive = inactive.AddComponent<SpriteRenderer>();
        srInactive.sprite = LoadOrCreateSprite("checkpoint_off", 8, 12, Color.gray, 32f);
        srInactive.sortingOrder = 6;

        var active = new GameObject("Visual_Active");
        active.transform.SetParent(go.transform);
        var srActive = active.AddComponent<SpriteRenderer>();
        srActive.sprite = LoadOrCreateSprite("checkpoint_on", 8, 12, new Color(0f, 1f, 0.6f), 32f);
        srActive.sortingOrder = 6;

        cp.visualActive = active;
        cp.visualInactive = inactive;
        active.SetActive(false);

        SavePrefab(go, path);
    }

    private static void CreateDataBitPrefab()
    {
        string path = PrefabRoot + "/DataBit.prefab";
        GameObject go = new GameObject("DataBit");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.5f);
        col.isTrigger = true;

        go.AddComponent<Collectible_DataBit>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("databit", 8, 8, new Color(0f, 1f, 0.3f), 32f);
        sr.sortingOrder = 7;

        SavePrefab(go, path);
    }

    private static void CreateSnippetPrefab()
    {
        string path = PrefabRoot + "/Snippet.prefab";
        GameObject go = new GameObject("Snippet");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.isTrigger = true;

        go.AddComponent<Collectible_Snippet>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("snippet", 10, 10, Color.magenta, 32f);
        sr.sortingOrder = 7;

        SavePrefab(go, path);
    }

    private static void CreateServerBGoalPrefab()
    {
        string path = PrefabRoot + "/ServerB_Goal.prefab";
        GameObject go = new GameObject("ServerB_Goal");
        go.layer = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 4f);
        col.isTrigger = true;

        go.AddComponent<ServerB_Goal>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadOrCreateSprite("serverb_goal", 20, 40, Color.yellow, 32f);
        sr.sortingOrder = 8;

        SavePrefab(go, path);
    }

    [MenuItem("Tools/Packet Courier/Setup Scene")]
    public static void SetupScene()
    {
        EnsureInputManager();
        EnsureCamera();
        EnsureLevelManager();
        EnsureGameManager();
        EnsureHUD();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Packet Courier",
            "Scene disiapkan: InputManager, Main Camera (CameraFollow), LevelManager, GameManager, dan HUD.\nDrag prefab dari Assets/_Prefabs/ untuk membangun level.",
            "OK"
        );
    }

    private static void EnsureInputManager()
    {
        if (Object.FindObjectOfType<InputManager>() != null) return;

        var go = new GameObject("InputManager");
        go.AddComponent<InputManager>();
    }

    private static void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        cam.transform.position = new Vector3(0, 0, -10);

        if (cam.GetComponent<CameraFollow>() == null)
            cam.gameObject.AddComponent<CameraFollow>();
    }

    private static void EnsureLevelManager()
    {
        if (Object.FindObjectOfType<LevelManager>() != null) return;

        var go = new GameObject("LevelManager");
        var lm = go.AddComponent<LevelManager>();
        lm.worldIndex = 0;
        lm.segmentIndex = 0;
        lm.levelDrainRate = 1f;
    }

    private static void EnsureGameManager()
    {
        if (Object.FindObjectOfType<GameManager>() != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private static void EnsureHUD()
    {
        if (Object.FindObjectOfType<HUD_TTL>() != null) return;

        var canvasObj = new GameObject("HUD_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var hudTTL = new GameObject("HUD_TTL");
        hudTTL.transform.SetParent(canvasObj.transform);
        var ttlCanvas = hudTTL.AddComponent<HUD_TTL>();

        var ttlBar = new GameObject("TTL_Bar");
        ttlBar.transform.SetParent(hudTTL.transform);
        var img = ttlBar.AddComponent<UnityEngine.UI.Image>();
        ttlCanvas.ttlFillBar = img;
        var rt = ttlBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.01f, 0.94f);
        rt.anchorMax = new Vector2(0.35f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.color = Color.green;
        img.type = UnityEngine.UI.Image.Type.Filled;
        img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

        var ttlTxt = new GameObject("TTL_Text");
        ttlTxt.transform.SetParent(hudTTL.transform);
        var tmp = ttlTxt.AddComponent<UnityEngine.UI.Text>();
        ttlCanvas.ttlText = tmp;
        var txtRt = ttlTxt.GetComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0.01f, 0.89f);
        txtRt.anchorMax = new Vector2(0.35f, 0.94f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        tmp.text = "TTL: 100/100";
        tmp.fontSize = 14;
        tmp.alignment = TextAnchor.MiddleLeft;
        tmp.color = Color.white;
        tmp.font = font;

        var hudModeObj = new GameObject("HUD_Mode");
        hudModeObj.transform.SetParent(canvasObj.transform);
        var modeComp = hudModeObj.AddComponent<HUD_Mode>();

        var modeText = new GameObject("Mode_Label");
        modeText.transform.SetParent(hudModeObj.transform);
        var mTmp = modeText.AddComponent<UnityEngine.UI.Text>();
        modeComp.modeText = mTmp;
        var modeRt = modeText.GetComponent<RectTransform>();
        modeRt.anchorMin = new Vector2(0.75f, 0.94f);
        modeRt.anchorMax = new Vector2(0.99f, 0.98f);
        modeRt.offsetMin = Vector2.zero;
        modeRt.offsetMax = Vector2.zero;
        mTmp.text = "PLAINTEXT";
        mTmp.fontSize = 14;
        mTmp.alignment = TextAnchor.MiddleRight;
        mTmp.color = Color.green;
        mTmp.font = font;

        var bitsText = new GameObject("Bits_Label");
        bitsText.transform.SetParent(hudModeObj.transform);
        var bTmp = bitsText.AddComponent<UnityEngine.UI.Text>();
        modeComp.bitsText = bTmp;
        var bitsRt = bitsText.GetComponent<RectTransform>();
        bitsRt.anchorMin = new Vector2(0.55f, 0.94f);
        bitsRt.anchorMax = new Vector2(0.74f, 0.98f);
        bitsRt.offsetMin = Vector2.zero;
        bitsRt.offsetMax = Vector2.zero;
        bTmp.text = "BITS \u00A5 0";
        bTmp.fontSize = 12;
        bTmp.alignment = TextAnchor.MiddleRight;
        bTmp.color = Color.yellow;
        bTmp.font = font;

        var portText = new GameObject("Port_Label");
        portText.transform.SetParent(hudModeObj.transform);
        var pTmp = portText.AddComponent<UnityEngine.UI.Text>();
        modeComp.portText = pTmp;
        var portRt = portText.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.55f, 0.89f);
        portRt.anchorMax = new Vector2(0.74f, 0.94f);
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;
        pTmp.text = "PORT 80";
        pTmp.fontSize = 12;
        pTmp.alignment = TextAnchor.MiddleRight;
        pTmp.color = Color.yellow;
        pTmp.font = font;
    }
}
#endif
