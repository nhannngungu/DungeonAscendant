using System;

namespace DungeonAscendant.Combat;

/// <summary>Central tuning for Raider stagger and displacement behavior.</summary>
public static class RaiderTuning
{
    public const float LightDisplacementMultiplier = 1.20f;
    public const float NormalDisplacementMultiplier = 1f;
    public const float HeavyDisplacementMultiplier = .28f;

    public const float HookPullDistance = 66f;
    public const float HookSafeDistance = 52f;
    public const float HeavyHookPlayerAdvance = 18f;
    public const float BossHookPlayerAdvance = 20f;

    public const float BucklerRamPushDistance = 74f;
    public const float RavagersRushPushDistance = 58f;
    public const float ExecutionerRepositionDistance = 34f;
    public const float CrowdCrusherThrowDistance = 112f;
    public const float GatheringPullDistance = 78f;
    public const float DominionPushDistance = 104f;

    public const float WallCollisionPoise = 30f;
    public const int WallCollisionDamage = 4;
    public const float EnemyCollisionPoise = 22f;
    public const float EnemyCollisionNudge = 18f;
    public const float DisplacementImpactCooldownSeconds = .38f;

    public const float UnstablePoiseRatio = .38f;
    public const float ExecutionerUnstableDamageBonus = .25f;
    public const float ExecutionerUnstablePoiseBonus = .30f;
    public const float SkullbreakerUnstableDamageBonus = .22f;
    public const float DominionFinalUnstableDamageBonus = .30f;
    public const float DominionWallDamageBonus = .15f;

    public const float WarCryRadius = 118f;
    public const float WarCryHesitationSeconds = .48f;
    public const float WarCryMovementMultiplier = .55f;
    public const float DominionGatherZoneDistance = 62f;

    public static EnemyWeightClass ClassifyWeight(
        float maximumPoise,
        bool isFlying)
    {
        if (isFlying || maximumPoise <= 40f)
            return EnemyWeightClass.Light;
        return maximumPoise <= 95f
            ? EnemyWeightClass.Normal
            : EnemyWeightClass.Heavy;
    }

    public static float GetDisplacementMultiplier(EnemyWeightClass weight) =>
        weight switch
        {
            EnemyWeightClass.Light => LightDisplacementMultiplier,
            EnemyWeightClass.Heavy => HeavyDisplacementMultiplier,
            EnemyWeightClass.Boss => 0f,
            _ => NormalDisplacementMultiplier
        };
}
