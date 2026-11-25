using UnityEngine;

/// <summary>
/// Owner of the mandatory package-delivery quest and the extension point for optional quests.
///
/// Contract for the optional-quest sibling: drive optional missions through
/// <see cref="QuestService"/> only. <see cref="IsPackageDelivered"/> is the single
/// condition the world gate reads, so optional progress can never block or re-open it,
/// and no optional code needs to edit <see cref="LevelManager"/> or <see cref="ServerB_Goal"/>.
///
/// The service is a component on the <see cref="GameManager"/> object, so it inherits that
/// object's <c>DontDestroyOnLoad</c>: package state survives every scene load and
/// <see cref="GameManager.RetrySegment"/>. No second persistent object is ever created.
/// </summary>
public class QuestService : MonoBehaviour
{
    public static QuestService Instance { get; private set; }

    [Header("Wiring")]
    public GameManager owner;

    [Header("Mandatory Quest")]
    [Tooltip("Data bits paid once when the package reaches the receiver.")]
    public int packageRewardBits = 250;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        var gm = GameManager.Instance;
        if (gm != null && gm.gameObject != gameObject)
        {
            owner = gm;
            transform.SetParent(gm.transform, false);
        }
    }

    /// <summary>
    /// Guarantees a quest service on the persistent GameManager object. Never creates a
    /// second <c>DontDestroyOnLoad</c> object.
    /// </summary>
    public static QuestService Attach(GameManager gm)
    {
        if (gm == null) return Instance;
        if (Instance != null && Instance.gameObject == gm.gameObject)
        {
            Instance.owner = gm;
            return Instance;
        }
        var service = gm.gameObject.GetComponent<QuestService>();
        if (service == null) service = gm.gameObject.AddComponent<QuestService>();
        if (Instance == null) Instance = service;
        service.owner = gm;
        return service;
    }

    public static QuestService Ensure()
    {
        if (Instance != null) return Instance;
        return Attach(GameManager.Ensure());
    }

    // ---- mandatory package quest -------------------------------------------------------

    /// <summary>Stable ledger id for the one-shot hand-in reward.</summary>
    public static string RewardId(int world)
    {
        return "quest:package:" + world;
    }

    public PackageState GetPackageState(int world)
    {
        var gm = Owner;
        if (gm == null || world < 0 || world >= gm.packageStates.Length) return PackageState.None;
        return gm.packageStates[world];
    }

    /// <summary>None -> Offered. Idempotent: an already-offered package is left alone.</summary>
    public bool OfferPackage(int world)
    {
        var gm = Owner;
        if (gm == null || !InRange(gm, world)) return false;
        if (!PackageQuestRules.TryOffer(gm.packageStates[world], out var next)) return false;
        gm.packageStates[world] = next;
        return true;
    }

    /// <summary>Offered -> Active. The giver is the only normal caller.</summary>
    public bool AcceptPackage(int world)
    {
        var gm = Owner;
        if (gm == null || !InRange(gm, world)) return false;
        if (!PackageQuestRules.TryAccept(gm.packageStates[world], out var next)) return false;
        gm.packageStates[world] = next;
        PacketIntegrityManager.Ensure()?.ResetIntegrity();
        return true;
    }

    /// <summary>
    /// Active -> Delivered and pays the reward exactly once. A second hand-in is rejected,
    /// so the reward can never be granted twice, however often the receiver is used.
    /// </summary>
    public bool DeliverPackage(int world)
    {
        var gm = Owner;
        if (gm == null || !InRange(gm, world)) return false;
        if (!PackageQuestRules.TryDeliver(gm.packageStates[world], out var next)) return false;
        gm.packageStates[world] = next;
        PayReward(gm, world);
        return true;
    }

    /// <summary>Delivered -> Claimed. Bookkeeping only; the reward is already paid.</summary>
    public bool ClaimPackage(int world)
    {
        var gm = Owner;
        if (gm == null || !InRange(gm, world)) return false;
        if (!PackageQuestRules.TryClaim(gm.packageStates[world], out var next)) return false;
        gm.packageStates[world] = next;
        return true;
    }

    /// <summary>The single condition the world-progress gate reads.</summary>
    public bool IsPackageDelivered(int world)
    {
        return PackageQuestRules.IsDelivered(GetPackageState(world));
    }

    public string GetQuestLabel(int world)
    {
        return PackageQuestRules.Label(GetPackageState(world));
    }

    public string GetLockedHint(int world)
    {
        return PackageQuestRules.LockedHint(GetPackageState(world));
    }

    private void PayReward(GameManager gm, int world)
    {
        if (gm.packageRewardsPaid[world]) return;
        gm.packageRewardsPaid[world] = true;
        if (gm.ledger.CollectRun(RewardId(world), packageRewardBits) && gm.isInRun)
            gm.dataBitsCollectedThisRun += packageRewardBits;
    }

    // ---- world flow ---------------------------------------------------------------------

    /// <summary>
    /// Called by <see cref="LevelManager"/> on every segment start. Guarantees the package is
    /// always reachable on the main path: the world-start safe zone gets a giver, the final
    /// segment gets a receiver, and a package that was never handed out is offered again.
    /// Endpoints are only spawned when the scene does not already contain one, so authored
    /// NPCs always win and nothing is duplicated across reloads or retries.
    /// </summary>
    public void BeginWorldSegment(int worldIndex, int segmentIndex, int segmentCount, Vector2 startAnchor, Vector2 endAnchor)
    {
        var gm = Owner;
        if (gm == null || !InRange(gm, worldIndex)) return;

        if (segmentIndex <= 0)
        {
            PackageQuestRules.TryOffer(gm.packageStates[worldIndex], out var offered);
            gm.packageStates[worldIndex] = offered;
            OptionalQuestService.PayoutSink = gm.AddDataBits;
            OptionalQuestService.Roll(worldIndex, new[] { "enemy:patrol", "enemy:flying" }, new[] { "bit:recycle" });
        }
        else
        {
            // If package was already delivered in a previous stage, offer a fresh packet for current stage
            if (PackageQuestRules.IsDelivered(gm.packageStates[worldIndex]))
            {
                gm.packageStates[worldIndex] = PackageState.Offered;
            }
        }

        // Safe Zone 1 (Arrival Chamber at start of stage):
        // [Arrival Portal -9.2] -> [Spawn -7.2] -> [Overclock -3.8] -> [Quest +1.2] -> [Protocol +3.6] -> [Shop +6.2] -> [Door +10.8]
        EnsureOverclockTerminal(startAnchor + new Vector2(-3.8f, 0f), "OverclockTerminal_Arrival");
        EnsureEndpoint<PackageGiverNPC>(worldIndex, startAnchor + new Vector2(1.2f, 0f), new Color(0.3f, 0.95f, 1f));
        EnsureProtocolTerminal(startAnchor + new Vector2(3.6f, 0f));
        EnsureEndpoint<ShopNPC>(worldIndex, startAnchor + new Vector2(6.2f, 0f), new Color(0.5f, 1f, 0.5f), "Shop_Arrival");

        // Safe Zone 2 (Departure Chamber at end of stage - REVERSED / MIRRORED):
        // [Door -10.8] -> [Shop -6.2] -> [Receiver -1.2] -> [Overclock +3.8] -> [Departure Portal +9.2]
        EnsureEndpoint<ShopNPC>(worldIndex, endAnchor + new Vector2(-6.2f, 0f), new Color(0.5f, 1f, 0.5f), "Shop_Departure");
        EnsureEndpoint<PackageReceiverNPC>(worldIndex, endAnchor + new Vector2(-1.2f, 0f), new Color(1f, 0.8f, 0.3f));
        EnsureOverclockTerminal(endAnchor + new Vector2(3.8f, 0f), "OverclockTerminal_Departure");
    }

    private void EnsureProtocolTerminal(Vector2 position)
    {
        var existing = GameObject.Find("ProtocolTerminal_Arrival");
        if (existing != null)
        {
            existing.transform.position = position;
            var termExisting = existing.GetComponent<ProtocolTerminal>();
            if (termExisting != null) termExisting.SnapToGround();
            return;
        }
        var root = GameObject.Find("PackageQuest");
        if (root == null) root = new GameObject("PackageQuest");

        var go = new GameObject("ProtocolTerminal_Arrival", typeof(SpriteRenderer), typeof(ProtocolTerminal));
        go.transform.SetParent(root.transform, false);
        go.transform.position = position;
        var term = go.GetComponent<ProtocolTerminal>();
        if (term != null) term.SnapToGround();
    }

    private void EnsureOverclockTerminal(Vector2 position, string slot)
    {
        var existing = GameObject.Find(slot);
        if (existing != null)
        {
            existing.transform.position = position;
            var term = existing.GetComponent<OverclockTerminal>();
            if (term != null) term.SnapToGround();
            return;
        }
        var root = GameObject.Find("PackageQuest");
        if (root == null) root = new GameObject("PackageQuest");

        var terminal = OverclockTerminal.Spawn(position, root.transform);
        terminal.gameObject.name = slot;
        terminal.SnapToGround();
    }

    private void EnsureEndpoint<T>(int worldIndex, Vector2 position, Color color, string slot = null) where T : NPC_Base
    {
        string goName = slot ?? typeof(T).Name;
        if (slot != null)
        {
            var existing = GameObject.Find(goName);
            if (existing != null)
            {
                existing.transform.position = position;
                var npcExisting = existing.GetComponent<NPC_Base>();
                if (npcExisting != null) npcExisting.SnapToGround();
                return;
            }
        }
        else
        {
            var existing = FindAnyObjectByType<T>();
            if (existing != null)
            {
                existing.transform.position = position;
                var npcExisting = existing.GetComponent<NPC_Base>();
                if (npcExisting != null) npcExisting.SnapToGround();
                return;
            }
        }

        var root = GameObject.Find("PackageQuest");
        if (root == null) root = new GameObject("PackageQuest");

        var go = new GameObject(goName);
        go.layer = 0;
        go.transform.SetParent(root.transform, false);
        go.transform.position = position;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.6f, 2.2f);
        collider.offset = new Vector2(0f, 1.1f);
        collider.isTrigger = true;

        var npc = go.AddComponent<T>();
        npc.npcId = goName + ":" + worldIndex;
        npc.worldIndex = worldIndex;
        npc.SnapToGround();
    }

    private static Sprite endpointSprite;

    /// <summary>Shared solid marker sprite, built once so spawned endpoints stay cheap.</summary>
    private static Sprite EndpointSprite()
    {
        if (endpointSprite != null) return endpointSprite;
        var texture = new Texture2D(8, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[8 * 16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        endpointSprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), new Vector2(0.5f, 0f), 8f);
        return endpointSprite;
    }

    private GameManager Owner
    {
        get
        {
            if (owner == null) owner = GameManager.Instance;
            return owner;
        }
    }

    private static bool InRange(GameManager gm, int world)
    {
        return world >= 0 && world < WorldRules.Count && world < gm.packageStates.Length && world < gm.packageRewardsPaid.Length;
    }
}
