using System;

namespace DungeonAscendant.Combat;

/// <summary>
/// Central tuning for the Duelist's continuous Momentum economy and bounded
/// tier scaling. Gameplay code should use these values instead of embedding
/// resource constants in controllers or renderers.
/// </summary>
public static class DuelistTuning
{
    public const float MomentumMaximum = 100f;
    public const float MediumTierThreshold = 34f;
    public const float HighTierThreshold = 67f;
    public const float DecayDelaySeconds = 1.75f;
    public const float DecayPerSecond = 16f;
    public const float DamageMomentumLoss = 28f;

    public const float BasicHitGain = 5f;
    public const float TwinFangFinisherBonus = 13f;
    public const float MovementAttackHitGain = 10f;
    public const float AggressiveRepositionGain = 4f;
    public const float PerfectPhantomStepGain = 24f;

    public const float MaximumAttackSpeedBonus = .12f;
    public const float MaximumUltimateDamageBonus = .20f;
    public const float MinimumUltimateDamageMultiplier = .90f;

    public static int GetTier(float momentum)
    {
        if (momentum >= HighTierThreshold)
            return 2;
        return momentum >= MediumTierThreshold ? 1 : 0;
    }

    public static float GetRatio(float momentum)
    {
        return Math.Clamp(momentum / MomentumMaximum, 0f, 1f);
    }

    public static int GetBladeTempestHitCount(float momentum) =>
        GetTier(momentum) switch { 2 => 6, 1 => 5, _ => 4 };

    public static int GetHundredFangsHitCount(float momentum) =>
        GetTier(momentum) switch { 2 => 8, 1 => 7, _ => 6 };

    public static int GetFinalWaltzHitCount(float momentum) =>
        GetTier(momentum) switch { 2 => 10, 1 => 8, _ => 6 };
}
