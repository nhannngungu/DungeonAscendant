using System;

namespace DungeonAscendant.Combat;

/// <summary>Central tuning for Hunter's Focus and facing-based Ranger skills.</summary>
public static class RangerTuning
{
    public const float FocusMaximum = 100f;
    public const float ConsistentHitGain = 5f;
    public const float SafeSpacingHitGain = 6f;
    public const float FullDrawHitGain = 10f;
    public const float PerfectDrawGain = 18f;
    public const float WindrunnerHitGain = 14f;
    public const float WindrunnerPerfectEvadeBonus = 12f;
    public const float DamageFocusLoss = 30f;
    public const float ClosePressureLossPerSecond = 10f;
    public const float ClosePressureDistance = 105f;
    public const float SafeSpacingDistance = 185f;
    public const float FocusDecayDelaySeconds = 1.6f;
    public const float FocusDecayPerSecond = 11f;

    public const float HunterDrawMaximumSeconds = 1.05f;
    public const float FullDrawStartSeconds = .55f;
    public const float PerfectDrawStartSeconds = .82f;
    public const float PerfectDrawEndSeconds = 1.04f;
    public const float HunterDrawBaseStaminaCost = 6f;
    public const float HunterDrawFullSurcharge = 3f;
    public const float HunterDrawPerfectSurcharge = 4f;
    public const float FullDrawDamageMultiplier = 1.45f;
    public const float PerfectDrawDamageMultiplier = 1.78f;
    public const float FullDrawSpeedMultiplier = 1.22f;
    public const float PerfectDrawSpeedMultiplier = 1.36f;
    public const float FullDrawPoiseMultiplier = 1.45f;
    public const float PerfectDrawPoiseMultiplier = 1.85f;

    public const float ThreefoldBaseAngleRadians = .23f;
    public const float ThreefoldFocusedAngleRadians = .14f;
    public const float ThreefoldSecondaryLargeTargetMultiplier = .52f;
    public const float ThreefoldFocusedCenterDamageBonus = .20f;

    public const float SkyfallTapThresholdSeconds = .20f;
    public const float SkyfallNearEndSeconds = .42f;
    public const float SkyfallMediumEndSeconds = .76f;
    public const float SkyfallNearDistance = 220f;
    public const float SkyfallMediumDistance = 360f;
    public const float SkyfallFarDistance = 500f;
    public const int SkyfallRainArrowCount = 6;

    public const float DragonPiercerDamageLossPerTarget = .20f;
    public const float DragonPiercerMaximumFocusFalloffReduction = .08f;
    public const float DragonPiercerMinimumDamageMultiplier = .55f;

    public const float FallingStarMaximumDamageBonus = .20f;
    public const float FallingStarMaximumPoiseBonus = .50f;

    public const int PredatorFanArrowCount = 5;
    public const int PredatorFanHitBonusCap = 3;
    public const float PredatorPoiseBonusPerFanHit = .20f;

    public const float HeavensFuryChargeSeconds = 1.25f;
    public const float HeavensFuryMaximumFocusDamageBonus = .50f;
    public const float HeavensFuryMaximumFocusPoiseBonus = .60f;

    public static float FocusRatio(float value) =>
        Math.Clamp(value / FocusMaximum, 0f, 1f);
}
