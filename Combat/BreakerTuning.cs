using System;

namespace DungeonAscendant.Combat;

/// <summary>Central tuning for Breaker Inertia, commitment, and impact.</summary>
public static class BreakerTuning
{
    public const float MaximumInertia = 100f;
    public const float InertiaDecayDelaySeconds = 2.6f;
    public const float InertiaDecayPerSecond = 8f;
    public const float InterruptedAttackLoss = 24f;
    public const float PoorRhythmLoss = 7f;
    public const float SwingCompletionGain = 4f;

    public const float IronCrushHitOneGain = 9f;
    public const float IronCrushHitTwoGain = 13f;
    public const float IronCrushHitThreeGain = 19f;
    public const float RhythmGainMultiplier = 1.25f;
    public const float PoorRhythmGainMultiplier = .65f;

    public const float PendulumStageTwoSeconds = .68f;
    public const float PendulumStageThreeSeconds = 1.24f;
    public const float PendulumMaximumSeconds = 1.85f;
    public const float PendulumStaminaDrainPerSecond = 5f;
    public const float PendulumInertiaGainPerSecond = 12f;
    public const float PendulumStageOneHitGain = 9f;
    public const float PendulumStageTwoHitGain = 16f;
    public const float PendulumStageThreeHitGain = 24f;

    public const float EarthbreakerConsumption = 20f;
    public const float TitanBackhandConsumption = 25f;
    public const float AnvilFallConsumption = 34f;
    public const float CataclysmConsumptionRatio = .72f;

    public const float EarthbreakerMaximumDamageBonus = .18f;
    public const float EarthbreakerMaximumPoiseBonus = .42f;
    public const float EarthbreakerShockwaveRadius = 92f;
    public const int EarthbreakerShockwaveDamage = 20;
    public const float EarthbreakerShockwavePoise = 36f;

    public const float BatteringRushHitGain = 20f;
    public const float BatteringRushLaunchVelocity = -165f;
    public const float UnstablePoiseRatio = .40f;

    public const float TitanBackhandMaximumDamageBonus = .16f;
    public const float TitanBackhandMaximumPoiseBonus = .34f;
    public const float AnvilFallMaximumDamageBonus = .24f;
    public const float AnvilFallMaximumPoiseBonus = .46f;
    public const float AnvilStaggeredDamageBonus = .38f;

    public const float CataclysmWaveRange = 335f;
    public const float CataclysmWaveSpeed = 520f;
    public const float CataclysmWaveWidth = 46f;
    public const float CataclysmWaveHeight = 62f;
    public const int CataclysmNearDamage = 56;
    public const int CataclysmFarDamage = 24;
    public const float CataclysmNearPoise = 78f;
    public const float CataclysmFarPoise = 30f;

    public const float WorldbreakerMaximumDamageBonus = .70f;
    public const float WorldbreakerMaximumPoiseBonus = .90f;
    public const float WorldbreakerBaseShockwaveRadius = 132f;
    public const float WorldbreakerMaximumShockwaveRadiusBonus = 38f;
    public const int WorldbreakerShockwaveDamage = 58;
    public const float WorldbreakerShockwavePoise = 96f;
    public const float WorldbreakerNormalLaunchVelocity = -230f;

    public static float InertiaRatio(float inertia) =>
        Math.Clamp(inertia / MaximumInertia, 0f, 1f);

    public static BreakerPendulumStage GetPendulumStage(float holdSeconds) =>
        holdSeconds >= PendulumStageThreeSeconds
            ? BreakerPendulumStage.StageThree
            : holdSeconds >= PendulumStageTwoSeconds
                ? BreakerPendulumStage.StageTwo
                : BreakerPendulumStage.StageOne;
}
