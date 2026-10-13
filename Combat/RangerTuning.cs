using System;

namespace DungeonAscendant.Combat;

/// <summary>Central tuning for Weapon 05: Bow / Phantom Archer.</summary>
public static class RangerTuning
{
    public const float FocusMaximum = 100f;
    public const float UltimateMaximum = 100f;
    public const float RangerFlowThreshold = 34f;
    public const float PhantomFlowThreshold = 67f;
    public const float RangerFlowSpeedMultiplier = 1.05f;
    public const float PhantomFlowSpeedMultiplier = 1.09f;
    public const float UltimateChordWindowSeconds = .12f;
    public const float PhantomHuntTargetRange = 650f;
    public const float PhantomHuntIntroSeconds = 1.75f;
    public const float PhantomHuntWorldSlowScale = .24f;
    public const float PhantomHuntCameraZoom = 1.48f;
    public const float PiercingDrawHoldThresholdSeconds = .34f;
    public const float ArrowStormHoldThresholdSeconds = .42f;
    public const float BackstepFollowupWindowSeconds = .48f;
    public const float PerfectDodgeWindowSeconds = .16f;
    public const float CounterWindowSeconds = .62f;
    public const float EvasiveStepCooldownSeconds = .46f;
    public const float EvasiveStepSpeed = 355f;
    public const float ConsistentHitGain = 4f;
    public const float MovingHitGain = 2f;
    public const float SafeSpacingHitGain = 2f;
    public const float FullDrawHitGain = 6f;
    public const float PerfectDrawGain = 8f;
    public const float EvasiveHitGain = 5f;
    public const float PerfectEvadeBonus = 14f;
    public const float CounterHitGain = 12f;
    public const float AerialHitGain = 5f;
    public const float WeakPointHitGain = 10f;
    public const float DamageFocusLoss = 24f;
    public const float MissFocusLoss = 4f;
    public const float FocusDecayDelaySeconds = 2.1f;
    public const float FocusDecayPerSecond = 7f;
    public const float SafeSpacingDistance = 185f;
    public const float ClosePressureDistance = 105f;
    public const float ClosePressureLossPerSecond = 0f;
    public const float HunterDrawMaximumSeconds = 1.72f;
    public const float FullDrawStartSeconds = 1.05f;
    public const float PerfectDrawStartSeconds = 1.45f;
    public const float PerfectDrawEndSeconds = 1.72f;
    public const float FullChargeStabilitySeconds = 1.85f;
    public const float HunterDrawBaseStaminaCost = 8f;
    public const float HunterDrawFullSurcharge = 4f;
    public const float HunterDrawPerfectSurcharge = 3f;
    public const float FullDrawDamageMultiplier = 1.30f;
    public const float PerfectDrawDamageMultiplier = 1.52f;
    public const float FullDrawSpeedMultiplier = 1.12f;
    public const float PerfectDrawSpeedMultiplier = 1.18f;
    public const float FullDrawPoiseMultiplier = 1.55f;
    public const float PerfectDrawPoiseMultiplier = 1.85f;
    public const int ArrowStormRainArrowCount = 7;
    public const int PhantomHuntArrowCount = 12;
    public const float PhantomHuntArrowDamageMultiplier = .20f;
    public const float PhantomHuntExecutionDamageMultiplier = 1.80f;
    public const float FlowGainPerTechniqueCap = 18f;
    public const float UltimateGainPerTechniqueCap = 16f;
    public const float UltimateGainPerHit = 5f;
    public const float UltimateMultiHitGain = 2f;

    // Retained names used by shared projectile helpers.
    public const int SkyfallRainArrowCount = ArrowStormRainArrowCount;
    public const float ThreefoldBaseAngleRadians = .16f;
    public const float ThreefoldFocusedAngleRadians = .10f;
    public const float ThreefoldSecondaryLargeTargetMultiplier = .58f;
    public const float ThreefoldFocusedCenterDamageBonus = .08f;
    public const float SkyfallTapThresholdSeconds = .20f;
    public const float SkyfallNearEndSeconds = .42f;
    public const float SkyfallMediumEndSeconds = .76f;
    public const float SkyfallNearDistance = 220f;
    public const float SkyfallMediumDistance = 360f;
    public const float SkyfallFarDistance = 500f;
    public const float DragonPiercerDamageLossPerTarget = .20f;
    public const float DragonPiercerMaximumFocusFalloffReduction = .08f;
    public const float DragonPiercerMinimumDamageMultiplier = .55f;
    public const float FallingStarMaximumDamageBonus = .20f;
    public const float FallingStarMaximumPoiseBonus = .50f;
    public const int PredatorFanArrowCount = 5;
    public const int PredatorFanHitBonusCap = 3;
    public const float PredatorPoiseBonusPerFanHit = .20f;
    public const float HeavensFuryChargeSeconds = PhantomHuntIntroSeconds;
    public const float HeavensFuryMaximumFocusDamageBonus = 0f;
    public const float HeavensFuryMaximumFocusPoiseBonus = 0f;
    public const float WindrunnerHitGain = EvasiveHitGain;
    public const float WindrunnerPerfectEvadeBonus = PerfectEvadeBonus;

    public static float FocusRatio(float value) =>
        Math.Clamp(value / FocusMaximum, 0f, 1f);

    public static int FocusTier(float value) => value >= PhantomFlowThreshold
        ? 2
        : value >= RangerFlowThreshold ? 1 : 0;

    public static float FlowSpeedMultiplier(float value) => FocusTier(value) switch
    {
        2 => PhantomFlowSpeedMultiplier,
        1 => RangerFlowSpeedMultiplier,
        _ => 1f
    };
}
