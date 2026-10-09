using System;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// Deterministic procedural chain paths shared by rendering and collision.
/// The handle leads, the head follows a delayed curve, and terrain validation
/// samples a fixed buffer rather than simulating rope bodies.
/// </summary>
public static class ChainFlailMotion
{
    private const int TerrainSamples = 14;
    private static readonly Vector2 HeadProbeSize = new(22f, 22f);

    public static Vector2 GetHeadOffset(
        AttackDefinition attack,
        float stateElapsed,
        float effectiveRange)
    {
        return GetHeadOffset(
            WeaponTechniqueEffect.None,
            0,
            attack,
            stateElapsed,
            effectiveRange,
            0f,
            0f,
            1);
    }

    public static Vector2 GetHeadOffset(
        WeaponTechniqueEffect effect,
        int stage,
        AttackDefinition attack,
        float stateElapsed,
        float effectiveRange,
        float momentumRatio,
        float orbitHeldSeconds,
        int curveSide)
    {
        if (attack == null)
            return new Vector2(42f, -30f);

        float radius = MathF.Max(45f, effectiveRange * .82f);
        float progress = Math.Clamp(
            stateElapsed / MathF.Max(.01f, attack.TotalTime),
            0f,
            1f);
        float activeProgress = Math.Clamp(
            (stateElapsed - attack.StartupTime) /
                MathF.Max(.01f, attack.ActiveTime),
            0f,
            1f);
        float delayed = Follow(activeProgress, .13f - momentumRatio * .045f);

        switch (effect)
        {
            case WeaponTechniqueEffect.ReckonerChainLash:
                if (stage == 2)
                {
                    float extension = Smooth(delayed);
                    return new Vector2(
                        30f + radius * extension,
                        -31f + MathF.Sin(extension * MathF.PI) * 9f);
                }

                float lashStart = stage == 0 ? -1.42f : .72f;
                float lashSweep = stage == 0 ? 2.55f : -2.65f;
                float lashAngle = lashStart + lashSweep * delayed;
                return Ellipse(lashAngle, radius, .64f, -31f);

            case WeaponTechniqueEffect.ReckonerOrbitingMaelstrom:
                int orbitStage = ReckonerTuning.GetOrbitStage(orbitHeldSeconds);
                if (stage > 0)
                {
                    float release = Smooth(progress);
                    float releaseAngle = stateElapsed * 8f + 1.2f * release;
                    return Ellipse(
                        releaseAngle,
                        MathHelper.Lerp(radius, 48f, release),
                        .70f,
                        -31f + release * 5f);
                }

                float orbitRadius = radius * (orbitStage switch
                {
                    3 => 1f,
                    2 => .84f,
                    _ => .66f
                });
                float orbitSpeed = (orbitStage switch
                {
                    3 => 10.2f,
                    2 => 8.6f,
                    _ => 6.8f
                }) * (1f + momentumRatio * .10f);
                float orbitAngle = stateElapsed * orbitSpeed - .32f;
                float variation = 1f + MathF.Sin(stateElapsed * 3.1f) * .035f;
                return Ellipse(
                    orbitAngle,
                    orbitRadius * variation,
                    .70f,
                    -31f);

            case WeaponTechniqueEffect.ReckonerChainHarpoon:
                if (stage == 0 && activeProgress <= 0f)
                {
                    float prepAngle = -1.6f + progress * MathHelper.TwoPi;
                    return Ellipse(prepAngle, 52f, .62f, -31f);
                }
                float travel = stage == 0
                    ? Smooth(delayed)
                    : 1f - Smooth(delayed);
                return new Vector2(
                    30f + radius * travel,
                    -31f + MathF.Sin(travel * MathF.PI) * 7f);

            case WeaponTechniqueEffect.ReckonerReapersPassage:
                float passageAngle = 2.52f - delayed * 4.65f;
                return Ellipse(passageAngle, radius, .70f, -27f);

            case WeaponTechniqueEffect.ReckonerCrescentRequiem:
                if (stage == 0)
                {
                    float tightAngle = progress * MathHelper.TwoPi * 1.15f - 1.4f;
                    return Ellipse(tightAngle, 54f, .72f, -34f);
                }
                float extensionRatio = Smooth(Math.Clamp(delayed * 1.42f, 0f, 1f));
                float crescentRadius = MathHelper.Lerp(58f, radius, extensionRatio);
                float crescentAngle = -1.82f + delayed * 3.45f;
                return Ellipse(crescentAngle, crescentRadius, .63f, -29f);

            case WeaponTechniqueEffect.ReckonerSerpentsFang:
                float curve = MathF.Sin(delayed * MathF.PI);
                return new Vector2(
                    28f + radius * delayed,
                    -31f - curve * 82f * MathF.Sign(curveSide == 0 ? 1 : curveSide));

            case WeaponTechniqueEffect.ReckonerVortexSnare:
                return GetVortexOffset(stage, progress, delayed, momentumRatio);

            case WeaponTechniqueEffect.ReckonerChainsOfJudgment:
                return GetJudgmentOffset(stage, progress, delayed, radius);
        }

        if (attack.HitboxShape == AttackHitboxShape.Thrust)
        {
            float extension = MathF.Sin(progress * MathF.PI);
            return new Vector2(
                38f + radius * extension,
                -28f + MathF.Sin(progress * MathF.PI * 2f) * 14f);
        }

        float sweep = attack.HitboxShape == AttackHitboxShape.WideArc
            ? MathF.PI * 2.15f
            : MathF.PI * 1.18f;
        float angle = -1.65f + Follow(progress, .12f) * sweep;
        return Ellipse(angle, radius, .62f, -34f);
    }

    public static Vector2 ResolveHeadPosition(
        Vector2 playerPosition,
        float facingSign,
        Vector2 localOffset,
        DungeonMap dungeon,
        out bool terrainClipped)
    {
        Vector2 anchor = playerPosition + new Vector2(facingSign * 12f, -30f);
        Vector2 desired = playerPosition + new Vector2(
            localOffset.X * facingSign,
            localOffset.Y);
        terrainClipped = false;

        if (dungeon == null)
            return desired;

        Vector2 lastValid = anchor;
        for (int index = 1; index <= TerrainSamples; index++)
        {
            float t = index / (float)TerrainSamples;
            Vector2 sample = Vector2.Lerp(anchor, desired, t);
            Rectangle probe = DungeonCollision.CreateBounds(sample, HeadProbeSize);
            if (!dungeon.WorldBounds.Contains(probe) ||
                !DungeonCollision.IsWalkable(probe, dungeon))
            {
                terrainClipped = true;
                return lastValid;
            }
            lastValid = sample;
        }

        return desired;
    }

    public static Rectangle CreateHeadHitbox(
        Vector2 playerPosition,
        float facingSign,
        Vector2 localOffset,
        AttackDefinition attack,
        DungeonMap dungeon,
        out bool terrainClipped)
    {
        Vector2 center = ResolveHeadPosition(
            playerPosition,
            facingSign,
            localOffset,
            dungeon,
            out terrainClipped);
        int size = (int)MathF.Round(Math.Clamp(
            attack?.Thickness ?? 34f,
            30f,
            112f));
        return new Rectangle(
            (int)MathF.Round(center.X) - size / 2,
            (int)MathF.Round(center.Y) - size / 2,
            size,
            size);
    }

    private static Vector2 GetVortexOffset(
        int stage,
        float progress,
        float delayed,
        float momentumRatio)
    {
        float center = ReckonerTuning.VortexPlacementDistance *
            (1f + momentumRatio * .05f);
        if (stage == 0)
            return new Vector2(24f + (center - 24f) * Smooth(delayed), -31f);
        if (stage >= 6)
            return new Vector2(MathHelper.Lerp(center, 28f, Smooth(delayed)), -31f);

        float direction = stage % 2 == 0 ? -1f : 1f;
        float angle = direction * (progress * MathHelper.TwoPi + stage * .82f);
        float radius = ReckonerTuning.VortexOrbitRadius *
            (1f + momentumRatio * .08f);
        return new Vector2(
            center + MathF.Cos(angle) * radius,
            -30f + MathF.Sin(angle) * radius * .62f);
    }

    private static Vector2 GetJudgmentOffset(
        int stage,
        float progress,
        float delayed,
        float radius)
    {
        if (stage == 0)
        {
            float awakenRadius = MathHelper.Lerp(62f, radius * .78f, Smooth(progress));
            return Ellipse(progress * MathHelper.TwoPi - 1.4f, awakenRadius, .68f, -30f);
        }
        if (stage is >= 1 and <= 5)
        {
            float direction = stage % 2 == 0 ? -1f : 1f;
            float angle = direction * (delayed * MathHelper.TwoPi + stage * .72f);
            float verticalScale = stage % 3 == 0 ? .88f : .60f;
            return Ellipse(angle, radius, verticalScale, -28f);
        }
        if (stage == 6)
        {
            float contraction = Smooth(delayed);
            float angle = delayed * MathHelper.TwoPi * 1.2f;
            return Ellipse(
                angle,
                MathHelper.Lerp(radius, 68f, contraction),
                .68f,
                -28f);
        }

        float slam = Smooth(delayed);
        return new Vector2(
            MathHelper.Lerp(44f, 116f, slam),
            MathHelper.Lerp(-155f, 16f, slam));
    }

    private static Vector2 Ellipse(
        float angle,
        float radius,
        float verticalScale,
        float yCenter)
    {
        return new Vector2(
            10f + MathF.Cos(angle) * radius,
            yCenter + MathF.Sin(angle) * radius * verticalScale);
    }

    private static float Follow(float progress, float lag)
    {
        return Smooth(Math.Clamp(
            (progress - MathF.Max(0f, lag)) /
                MathF.Max(.01f, 1f - MathF.Max(0f, lag)),
            0f,
            1f));
    }

    private static float Smooth(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
