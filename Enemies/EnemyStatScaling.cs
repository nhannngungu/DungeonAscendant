using System;
using DungeonAscendant.Progression;

namespace DungeonAscendant.Enemies;

internal static class EnemyStatScaling
{
    public static int Health(
        int baseValue,
        int perLevel,
        int level,
        int worldTier)
    {
        int value = baseValue + (Math.Max(1, level) - 1) * perLevel;
        return WorldProgression.ApplyPercent(
            value,
            WorldProgression.GetHealthMultiplierPercent(worldTier));
    }

    public static int Damage(
        int baseValue,
        int perLevel,
        int level,
        int worldTier)
    {
        int value = baseValue + (Math.Max(1, level) - 1) * perLevel;
        return WorldProgression.ApplyPercent(
            value,
            WorldProgression.GetDamageMultiplierPercent(worldTier));
    }
}
