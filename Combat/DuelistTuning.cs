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
    public const float MediumTierThreshold = 35f;
    public const float HighTierThreshold = 75f;
    public const float DecayDelaySeconds = 3f;
    public const float DecayPerSecond = 12f;
    public const float DamageMomentumLoss = 15f;
    public const float ChordWindowSeconds = .13f;

    public const float BasicHitGain = 5f;
    public const float StarburstCompletionBonus = 10f;
    public const float PerfectParryGain = 15f;
    public const float CrossRequiemGain = 12f;
    public const float ThousandEdgeWaltzGain = 15f;
    public const float DodgeCounterGain = 8f;
    public const float PerfectParryWindowSeconds = .14f;
    public const float RiposteWindowSeconds = .8f;
    public const float OverdriveDurationSeconds = 9f;
    public const float OverdriveCooldownSeconds = 18f;
    public const float OverdriveSpeedBonus = .15f;
    public const float OverdriveDamageBonus = .10f;
    public const float HeavenSplitterLevelTwoSeconds = .45f;
    public const float HeavenSplitterLevelThreeSeconds = .90f;
    public const float UltimateTargetRange = 650f;
    public const float UltimateIntroSeconds = .62f;
    public const int UltimateStrikeCount = 32;

    public const float MaximumAttackSpeedBonus = .10f;

    public const float AggressiveRepositionGain = DodgeCounterGain;

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

    public static float GetTierSpeedMultiplier(float momentum) =>
        GetTier(momentum) switch { 2 => 1.10f, 1 => 1.05f, _ => 1f };

}
