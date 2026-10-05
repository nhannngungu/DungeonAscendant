using System;

namespace DungeonAscendant.Progression;

public static class WorldProgression
{
    public const int MaximumWorldTier = 5;

    public static int GetWorldTier(int dungeonDepth)
    {
        int safeDepth = Math.Max(1, dungeonDepth);
        return Math.Min(MaximumWorldTier, (safeDepth + 1) / 2);
    }

    public static int GetEnemyLevel(
        int playerLevel,
        int dungeonDepth,
        int worldTier)
    {
        int safePlayerLevel = Math.Max(1, playerLevel);
        int safeDepth = Math.Max(1, dungeonDepth);
        int safeWorldTier = ClampWorldTier(worldTier);
        return safePlayerLevel +
            (safeDepth - 1) / 2 +
            safeWorldTier - 1;
    }

    public static int GetHealthMultiplierPercent(int worldTier)
    {
        return 100 + (ClampWorldTier(worldTier) - 1) * 20;
    }

    public static int GetDamageMultiplierPercent(int worldTier)
    {
        return 100 + (ClampWorldTier(worldTier) - 1) * 12;
    }

    public static int ApplyPercent(int value, int percent)
    {
        return Math.Max(1, (value * percent + 50) / 100);
    }

    public static int ClampWorldTier(int worldTier)
    {
        return Math.Clamp(worldTier, 1, MaximumWorldTier);
    }
}
