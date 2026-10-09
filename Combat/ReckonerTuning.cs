using System;

namespace DungeonAscendant.Combat;

/// <summary>
/// Central, bounded tuning for the Reckoner's deterministic chain motion and
/// 0-100 Momentum economy.  Nothing here depends on a physics engine.
/// </summary>
public static class ReckonerTuning
{
    public const float MomentumMaximum = 100f;
    public const float MomentumDecayDelaySeconds = 1.75f;
    public const float MomentumDecayPerSecond = 9f;
    public const float HeavyDamageMomentumLoss = 25f;
    public const float InterruptedMomentumLoss = 10f;
    public const float PoorRhythmMomentumLoss = 12f;

    public const float LashHitGain = 7f;
    public const float LashCompletionBonus = 9f;
    public const float OrbitHitGain = 3f;
    public const float OrbitBuildPerSecond = 8f;
    public const float OrbitUninterruptedBonusPerSecond = 4f;
    public const float OrbitActivationStamina = 12f;
    public const float OrbitStaminaDrainPerSecond = 8f;
    public const float OrbitMaximumSeconds = 4.20f;
    public const float OrbitStageTwoSeconds = .85f;
    public const float OrbitStageThreeSeconds = 1.85f;
    public const float OrbitHitIntervalStageOne = .52f;
    public const float OrbitHitIntervalStageTwo = .42f;
    public const float OrbitHitIntervalStageThree = .34f;

    public const float HarpoonMaximumDistance = 250f;
    public const float HarpoonPierceThreshold = 60f;
    public const int HarpoonMaximumLightTargets = 3;
    public const float ReapersPassageHitGain = 8f;

    public const float MaximumRadiusBonus = .15f;
    public const float MaximumHeadSpeedBonus = .18f;
    public const float CrescentOuterEdgeStartRatio = .72f;
    public const float CrescentOuterEdgeDamageBonus = .35f;
    public const float CrescentMomentumDamageBonus = .20f;

    public const float VortexPlacementDistance = 210f;
    public const float VortexOrbitRadius = 70f;
    public const float VortexMomentumDrainPerPass = 6f;
    public const float VortexMaximumDurationSeconds = 2.65f;
    public const float VortexSlowDurationSeconds = .38f;
    public const float VortexSlowMultiplier = .62f;
    public const float VortexHeavySlowMultiplier = .82f;

    public const float UltimateMaximumDamageBonus = .35f;
    public const float UltimateMaximumControlBonus = .40f;
    public const int UltimateMinimumCirclePasses = 3;
    public const int UltimateMaximumCirclePasses = 5;

    public const int ChainSegmentCount = 12;

    public static float MomentumRatio(float momentum) =>
        Math.Clamp(momentum / MomentumMaximum, 0f, 1f);

    public static int GetOrbitStage(float heldSeconds)
    {
        if (heldSeconds >= OrbitStageThreeSeconds)
            return 3;
        return heldSeconds >= OrbitStageTwoSeconds ? 2 : 1;
    }

    public static float GetOrbitHitInterval(int stage) => stage switch
    {
        3 => OrbitHitIntervalStageThree,
        2 => OrbitHitIntervalStageTwo,
        _ => OrbitHitIntervalStageOne
    };

    public static int GetVortexPassCount(float momentum)
    {
        if (momentum >= 75f)
            return 5;
        return momentum >= 35f ? 4 : 3;
    }

    public static int GetUltimateCirclePassCount(float momentum)
    {
        if (momentum >= 75f)
            return UltimateMaximumCirclePasses;
        return momentum >= 35f ? 4 : UltimateMinimumCirclePasses;
    }
}
