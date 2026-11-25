using UnityEngine;

/// <summary>
/// World 3 (Fiber Deep) environmental mechanic.
/// An interactive terminal switch that can be triggered by either a melee blade slash
/// or a ranged Cyber Blaster projectile. Toggles connected DataLaserBarriers.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DataBlasterSwitch : MonoBehaviour, IEnemyDamageable
{
    [Header("Switch State")]
    public bool isActivated;
    public DataLaserBarrier linkedBarrier;

    [Header("Visual Colors")]
    public Color activeColor = new Color(0.1f, 1f, 0.85f, 1f);
    public Color inactiveColor = new Color(1f, 0.25f, 0.35f, 1f);

    public int HitPoints => 999;
    public bool IsDead => false;
    public Transform HitTransform => transform;

    private SpriteRenderer _renderer;
    private BoxCollider2D _collider;
    private float _lastToggleTime;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();

        if (_renderer == null)
        {
            var go = new GameObject("SwitchVisual", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            _renderer = go.GetComponent<SpriteRenderer>();
        }

        _renderer.sprite = Resources.Load<Sprite>("GeneratedArt/data_switch");
        _renderer.sortingOrder = 5;
        UpdateVisual();
    }

    public void TakeHit(int damage)
    {
        if (Time.time < _lastToggleTime + 0.25f) return;
        _lastToggleTime = Time.time;

        isActivated = !isActivated;
        UpdateVisual();

        AudioManager.PlayInteract();
        JuiceManager.Hitstop(0.04f);
        JuiceManager.Shake(0.15f);
        JuiceManager.SpawnImpactSparks(transform.position, isActivated ? activeColor : inactiveColor);

        if (linkedBarrier != null)
        {
            linkedBarrier.SetBarrierOpen(isActivated);
        }
    }

    private void UpdateVisual()
    {
        if (_renderer != null)
        {
            _renderer.color = isActivated ? activeColor : inactiveColor;
        }
    }
}
