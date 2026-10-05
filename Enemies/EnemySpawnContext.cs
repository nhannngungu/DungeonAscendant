using Microsoft.Xna.Framework;

namespace DungeonAscendant.Enemies;

public readonly struct EnemySpawnContext
{
    public Vector2 Position { get; }
    public int Level { get; }
    public int WorldTier { get; }
    public int RoomId { get; }
    public bool IsElite { get; }
    public GoblinVariant GoblinVariant { get; }

    public EnemySpawnContext(
        Vector2 position,
        int level,
        int worldTier,
        int roomId,
        bool isElite,
        GoblinVariant goblinVariant)
    {
        Position = position;
        Level = level;
        WorldTier = worldTier;
        RoomId = roomId;
        IsElite = isElite;
        GoblinVariant = goblinVariant;
    }
}
