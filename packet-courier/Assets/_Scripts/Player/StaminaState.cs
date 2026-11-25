using System;

public sealed class StaminaState
{
    public float Current { get; private set; }
    public float Capacity { get; private set; }
    private float delay;
    public StaminaState() { Capacity = 100f; Current = Capacity; }

    public bool IsEmpty { get { return Current <= 0f; } }

    public bool TrySpend(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0 || amount > Current) return false;
        Current -= amount;
        delay = 0.75f;
        return true;
    }

    /// <summary>
    /// Continuous drain used by wall cling: spends up to <paramref name="rate"/> * dt,
    /// clamping to whatever is left instead of rejecting the whole tick.
    /// </summary>
    /// <returns>False when stamina is empty, i.e. the cling must break.</returns>
    public bool TryDrain(float rate, float dt)
    {
        if (float.IsNaN(rate) || float.IsInfinity(rate) || float.IsNaN(dt) || float.IsInfinity(dt)) return false;
        if (rate <= 0 || dt <= 0) return !IsEmpty;
        float amount = rate * dt;
        Current = Math.Max(0f, Current - amount);
        delay = 0.75f;
        return !IsEmpty;
    }

    public void Tick(float dt)
    {
        if (dt <= 0) return;
        float recoveryTime = Math.Max(0, dt - delay);
        delay = Math.Max(0, delay - dt);
        Current = Math.Min(Capacity, Current + recoveryTime * 35f);
    }

    public void Reset() { Current = Capacity; delay = 0; }

    public void Restore(float amount)
    {
        if (amount <= 0) return;
        Current = Math.Min(Capacity, Current + amount);
    }
}
