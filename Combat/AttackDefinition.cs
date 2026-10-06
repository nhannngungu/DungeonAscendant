using System;

namespace DungeonAscendant.Combat;

/// <summary>
/// Immutable move data. Future weapon movesets can provide different definitions
/// without changing the player combat controller.
/// </summary>
public sealed class AttackDefinition
{
    public AttackKind Kind { get; }
    public float DamageMultiplier { get; }
    public float PoiseDamage { get; }
    public float StaminaCost { get; }
    public float StartupTime { get; }
    public float ActiveTime { get; }
    public float RecoveryTime { get; }
    public float Knockback { get; }
    public float Range { get; }
    public float Thickness { get; }
    public bool IsHeavy => Kind == AttackKind.Heavy;
    public float TotalTime => StartupTime + ActiveTime + RecoveryTime;

    public AttackDefinition(
        AttackKind kind,
        float damageMultiplier,
        float poiseDamage,
        float staminaCost,
        float startupTime,
        float activeTime,
        float recoveryTime,
        float knockback,
        float range,
        float thickness)
    {
        Kind = kind;
        DamageMultiplier = MathF.Max(0f, damageMultiplier);
        PoiseDamage = MathF.Max(0f, poiseDamage);
        StaminaCost = MathF.Max(0f, staminaCost);
        StartupTime = MathF.Max(0f, startupTime);
        ActiveTime = MathF.Max(0.01f, activeTime);
        RecoveryTime = MathF.Max(0f, recoveryTime);
        Knockback = MathF.Max(0f, knockback);
        Range = MathF.Max(1f, range);
        Thickness = MathF.Max(1f, thickness);
    }

    public int CalculateDamage(int baseDamage)
    {
        return Math.Max(1, (int)MathF.Round(baseDamage * DamageMultiplier));
    }
}
