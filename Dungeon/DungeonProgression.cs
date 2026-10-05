using System;

namespace DungeonAscendant.Dungeon;

public static class DungeonProgression
{
    public static int GetEnemyLevel(int playerLevel, int dungeonDepth)
    {
        return Math.Max(1, playerLevel) + Math.Max(1, dungeonDepth) - 1;
    }
}
