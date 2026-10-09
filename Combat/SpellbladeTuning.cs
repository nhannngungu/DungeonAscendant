using System;

namespace DungeonAscendant.Combat;

public static class SpellbladeTuning
{
    public const int MaximumImprints = 3;
    public const float ImprintLifetimeSeconds = 8f;
    public const float ImprintRefreshSeconds = 3f;
    public const float MaximumRefreshedLifetimeSeconds = 10f;
    public const int ImprintDamagePerStack = 18;
    public const float ImprintPoisePerStack = 9f;
    public const float TwoStackSlowDurationSeconds = 1.05f;
    public const float TwoStackSlowMultiplier = .62f;
    public const float ThreeStackStunDurationSeconds = .48f;
    public const float BossSlowDurationSeconds = .55f;
    public const float BossSlowMultiplier = .82f;
    public const float BossPoisePerStack = 14f;

    public const int MaximumGroundRunes = 3;
    public const float RuneLifetimeSeconds = 12f;
    public const float RuneWidth = 34f;
    public const float RuneBrandTapThresholdSeconds = .20f;
    public const float RuneBrandNearEndSeconds = .42f;
    public const float RuneBrandMediumEndSeconds = .76f;
    public const float RuneNearDistance = 175f;
    public const float RuneMediumDistance = 300f;
    public const float RuneFarDistance = 430f;

    public const float ArcaneDetonationRadius = 150f;
    public const float RuneBlastRadius = 58f;
    public const int RuneBlastDamage = 22;
    public const float RuneBlastPoise = 16f;
    public const float SecondRuneDamageMultiplier = .70f;
    public const float ThirdRuneDamageMultiplier = .45f;

    public const float RunicSpearMarkedDamageBonus = .24f;
    public const int RunicSpearMaximumTargets = 5;
    public const float ResonanceRearConeRange = 132f;
    public const float ResonanceRearConeHeight = 86f;
    public const float ResonanceRearConeDamageMultiplier = .58f;

    public const float ConvergenceMaximumRange = 520f;
    public const float ConvergenceDurationSeconds = 1.20f;
    public const float ConvergenceTickIntervalSeconds = .30f;
    public const float ConvergenceLineThickness = 20f;
    public const int ConvergenceTickDamage = 13;
    public const float ConvergenceTickPoise = 7f;
    public const float ConvergenceImprintDamagePerStack = 3f;

    public const float DominionRadius = 245f;
    public const float DominionBindingDurationSeconds = 1.05f;
    public const float DominionNormalSlowMultiplier = .25f;
    public const float DominionBossSlowMultiplier = .75f;
    public const float DominionRuneBonusPerRune = .12f;
    public const float DominionImprintBonusPerStack = .13f;
    public const float DominionMaximumSetupBonus = .75f;

    public static float GetPlacementDistance(RunePlacementDistance distance) =>
        distance switch
        {
            RunePlacementDistance.Near => RuneNearDistance,
            RunePlacementDistance.Far => RuneFarDistance,
            _ => RuneMediumDistance
        };

    public static int GetImprintDamage(int stacks, bool boss)
    {
        stacks = Math.Clamp(stacks, 0, MaximumImprints);
        float curve = stacks switch { 1 => 1f, 2 => 2.25f, 3 => 3.65f, _ => 0f };
        return (int)MathF.Round(ImprintDamagePerStack * curve *
            (boss ? 1.18f : 1f));
    }
}
