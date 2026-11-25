using UnityEngine;

public class ServerB_Goal : MonoBehaviour
{
    [Header("Config")]
    public LevelManager levelManager;

    private void Start()
    {
        EnsurePortal();
    }

    private void EnsurePortal()
    {
        if (GetComponentInChildren<SafeZonePortal>() == null)
        {
            var pGo = new GameObject("SafeZonePortal");
            pGo.transform.SetParent(transform, false);
            pGo.transform.localPosition = Vector3.zero;
            var portal = pGo.AddComponent<SafeZonePortal>();
            portal.goal = this;
            portal.levelManager = ResolveManager();
        }
    }

    /// <summary>True while the mandatory package quest still blocks this exit.</summary>
    public bool GateLocked
    {
        get { return ResolveManager() != null && levelManager.IsGoalLocked; }
    }

    /// <summary>Locked-goal quest hint, for HUD and dialog layers to surface.</summary>
    public string GateHint
    {
        get { return levelManager != null ? levelManager.LastGateHint : ""; }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLivingPlayer(other)) return;
        if (ResolveManager() == null) return;

        if (levelManager.IsGoalLocked) levelManager.ShowGateHint();
        else
        {
            AudioManager.DuckBGMForWarp(0.12f, 0.45f);
            AudioManager.Play("sfx_ui_open", 1.0f, 1.2f);
            levelManager.CompleteSegment();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsLivingPlayer(other) || ResolveManager() == null || !levelManager.IsGoalLocked) return;
        levelManager.ShowGateHint();
    }

    private bool IsLivingPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player")) return false;
        var health = other.GetComponent<PlayerHealth>();
        return health != null && !health.isDead;
    }

    private LevelManager ResolveManager()
    {
        if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();
        return levelManager;
    }
}
