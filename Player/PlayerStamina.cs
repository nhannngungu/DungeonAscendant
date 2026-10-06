using System;

namespace DungeonAscendant.Player;

public sealed class PlayerStamina
{
    public const float DefaultMaximum = 100f;
    public const float RegenerationDelaySeconds = 0.8f;
    public const float RegenerationPerSecond = 30f;

    private float _regenerationDelayRemaining;

    public float Maximum { get; }
    public float Current { get; private set; }
    public float Ratio => Current / Maximum;

    public PlayerStamina(float maximum = DefaultMaximum)
    {
        Maximum = MathF.Max(1f, maximum);
        Current = Maximum;
    }

    public bool TrySpend(float amount)
    {
        amount = MathF.Max(0f, amount);

        if (Current + 0.001f < amount)
            return false;

        SpendUpTo(amount);
        return true;
    }

    public float SpendUpTo(float amount)
    {
        float spent = MathF.Min(Current, MathF.Max(0f, amount));
        Current -= spent;

        if (spent > 0f)
            _regenerationDelayRemaining = RegenerationDelaySeconds;

        return spent;
    }

    public void Update(float elapsedSeconds, bool regenerationAllowed)
    {
        _regenerationDelayRemaining = MathF.Max(
            0f,
            _regenerationDelayRemaining - elapsedSeconds);

        if (!regenerationAllowed || _regenerationDelayRemaining > 0f)
            return;

        Current = MathF.Min(
            Maximum,
            Current + RegenerationPerSecond * elapsedSeconds);
    }

    public void Restore()
    {
        Current = Maximum;
        _regenerationDelayRemaining = 0f;
    }
}
