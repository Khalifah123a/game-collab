using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public readonly StaminaState state = new StaminaState();
    public float Current => state.Current;
    public float Capacity => state.Capacity;
    [Tooltip("Set while wall-clinging: skips regen so cling time is strictly limited.")]
    public bool suppressRegen;
    private PlayerHealth health;
    private void Awake() { health = GetComponent<PlayerHealth>(); }
    private void Update()
    {
        if (suppressRegen || isClinging) return;
        if (health != null && !health.isDead && Time.timeScale > 0) state.Tick(Time.deltaTime * OverclockManager.StaminaRegenMult);
    }
    public bool TryDash() => state.TrySpend(25f * OverclockManager.StaminaCostMult);
    public bool TrySprint(float dt) => state.TrySpend(15f * dt);
    public void AddStamina(float amount) => state.Restore(amount);

    [Header("Wall Cling")]
    [Tooltip("Cling stamina spent per second of sliding on a wall. Drained continuously, not in one chunk.")]
    public float clingDrainRate = 28f;

    /// <summary>True while the player is actively clinging to a wall.</summary>
    public bool isClinging { get; private set; }

    /// <summary>Latched after a cling ran out of stamina; cleared by a successful cling or on landing.</summary>
    public bool clingExhausted { get; private set; }

    /// <summary>Effective drain applied by the last TryCling call (upgrade adjusted).</summary>
    public float ClingDrainPerSecond { get; private set; }

    public bool TryCling(float dt) => TryCling(dt, clingDrainRate);

    /// <summary>
    /// Drains cling stamina for one physics tick. The pool is shared with dash and sprint,
    /// so grabbing a wall after a dash starts from whatever is left.
    /// </summary>
    /// <returns>False when stamina is empty: the caller must peel the player off the wall.</returns>
    public bool TryCling(float dt, float ratePerSecond)
    {
        isClinging = true;
        suppressRegen = true;
        ClingDrainPerSecond = ratePerSecond;

        if (!state.TryDrain(ratePerSecond, dt))
        {
            isClinging = false;
            suppressRegen = false;
            clingExhausted = true;
            return false;
        }

        clingExhausted = false;
        return true;
    }

    /// <summary>Stops the cling without spending stamina (walljump, dash, hurt, landing).</summary>
    public void EndCling()
    {
        isClinging = false;
        suppressRegen = false;
        ClingDrainPerSecond = 0f;
    }

    public void ResetClingExhausted() { clingExhausted = false; }
}
