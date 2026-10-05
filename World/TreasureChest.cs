using DungeonAscendant.Dungeon;
using DungeonAscendant.Items;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public sealed class TreasureChest
{
    public const float InteractionRadius = 72f;

    public Vector2 Position { get; }
    public int RoomId { get; }
    public bool IsOpen { get; private set; }
    public Rectangle Bounds => new(
        (int)Position.X - 28,
        (int)Position.Y - 20,
        56,
        40);

    public TreasureChest(Vector2 position, int roomId)
    {
        Position = position;
        RoomId = roomId;
    }

    public bool IsPlayerInRange(Vector2 playerPosition)
    {
        return Vector2.DistanceSquared(Position, playerPosition) <=
            InteractionRadius * InteractionRadius;
    }

    public bool TryOpen(
        Vector2 playerPosition,
        LootManager loot,
        int playerLevel,
        int dungeonDepth,
        DungeonMap dungeon)
    {
        return TryOpen(
            playerPosition,
            loot,
            playerLevel,
            dungeonDepth,
            WorldProgression.GetWorldTier(dungeonDepth),
            dungeon);
    }

    public bool TryOpen(
        Vector2 playerPosition,
        LootManager loot,
        int playerLevel,
        int dungeonDepth,
        int worldTier,
        DungeonMap dungeon)
    {
        if (IsOpen || !IsPlayerInRange(playerPosition))
            return false;

        loot.CreateTreasureChestDrops(
            Position,
            playerLevel,
            dungeonDepth,
            worldTier,
            dungeon);
        IsOpen = true;
        return true;
    }
}
