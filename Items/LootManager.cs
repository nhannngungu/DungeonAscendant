using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using DungeonAscendant.Player;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Items;

public sealed class LootManager
{
    public const float NormalDropChance = 0.35f;
    public const float PickupRadius = 72f;

    private readonly List<WorldLoot> _worldLoot = new();
    private readonly Random _random;
    private readonly ItemGenerator _itemGenerator;

    public IReadOnlyList<WorldLoot> Drops => _worldLoot;

    public LootManager(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
        _itemGenerator = new ItemGenerator(
            randomSeed.HasValue
                ? unchecked(randomSeed.Value ^ 0x51ED270B)
                : null);
    }

    public bool TryCreateDrop(
        Goblin goblin,
        int playerLevel,
        DungeonMap dungeon)
    {
        if (goblin == null || dungeon == null)
            return false;

        if (!goblin.IsElite && _random.NextDouble() >= NormalDropChance)
            return false;

        EquipmentItem item = _itemGenerator.Generate(
            goblin.Level,
            playerLevel,
            goblin.IsElite
                ? LootSource.EliteEnemy
                : LootSource.NormalEnemy);
        Vector2 position = FindSafeDropPosition(goblin.Position, dungeon);
        _worldLoot.Add(new WorldLoot(item, position));
        return true;
    }

    public int CreateTreasureChestDrops(
        Vector2 chestPosition,
        int playerLevel,
        int dungeonDepth,
        DungeonMap dungeon)
    {
        int itemCount = _random.Next(1, 3);
        int sourceLevel = DungeonProgression.GetEnemyLevel(
            playerLevel,
            dungeonDepth);

        for (int index = 0; index < itemCount; index++)
        {
            EquipmentItem item = _itemGenerator.Generate(
                sourceLevel,
                playerLevel,
                LootSource.TreasureChest);
            Vector2 position = FindSafeDropPosition(chestPosition, dungeon);
            _worldLoot.Add(new WorldLoot(item, position));
        }

        return itemCount;
    }

    public void CreateBossDrop(
        Vector2 bossPosition,
        int bossLevel,
        int playerLevel,
        DungeonMap dungeon)
    {
        EquipmentItem item = _itemGenerator.Generate(
            bossLevel,
            playerLevel,
            LootSource.Boss);
        Vector2 position = FindSafeDropPosition(bossPosition, dungeon);
        _worldLoot.Add(new WorldLoot(item, position));
    }

    public bool TryCollectNearest(Vector2 playerPosition, Inventory inventory)
    {
        if (inventory == null || inventory.IsFull)
            return false;

        int nearestIndex = -1;
        float nearestDistanceSquared = PickupRadius * PickupRadius;

        for (int index = 0; index < _worldLoot.Count; index++)
        {
            float distanceSquared = Vector2.DistanceSquared(
                playerPosition,
                _worldLoot[index].Position);

            if (distanceSquared > nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearestIndex = index;
        }

        if (nearestIndex < 0)
            return false;

        WorldLoot loot = _worldLoot[nearestIndex];

        if (!inventory.TryAdd(loot.Item))
            return false;

        _worldLoot.RemoveAt(nearestIndex);
        return true;
    }

    public void Reset()
    {
        _worldLoot.Clear();
    }

    private Vector2 FindSafeDropPosition(
        Vector2 deathPosition,
        DungeonMap dungeon)
    {
        Vector2 offset = new(
            _random.Next(-16, 17),
            _random.Next(-16, 17));
        Vector2 candidate = deathPosition + offset;

        if (IsValidDropPosition(candidate, dungeon))
            return candidate;

        if (IsValidDropPosition(deathPosition, dungeon))
            return deathPosition;

        DungeonRoom room = dungeon.FindRoomContaining(deathPosition);

        if (room != null)
        {
            Vector2 clamped = ClampInside(room.Bounds, deathPosition);

            if (IsValidDropPosition(clamped, dungeon))
                return clamped;
        }

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (!corridor.Contains(deathPosition.ToPoint()))
                continue;

            Vector2 clamped = ClampInside(corridor, deathPosition);

            if (IsValidDropPosition(clamped, dungeon))
                return clamped;
        }

        for (int radius = 16; radius <= 96; radius += 16)
        {
            for (int horizontal = -radius; horizontal <= radius; horizontal += radius)
            {
                for (int vertical = -radius; vertical <= radius; vertical += radius)
                {
                    if (Math.Abs(horizontal) != radius &&
                        Math.Abs(vertical) != radius)
                    {
                        continue;
                    }

                    Vector2 nearby = deathPosition + new Vector2(
                        horizontal,
                        vertical);

                    if (IsValidDropPosition(nearby, dungeon))
                        return nearby;
                }
            }
        }

        return dungeon.StartRoom.Center;
    }

    private static bool IsValidDropPosition(
        Vector2 position,
        DungeonMap dungeon)
    {
        return DungeonCollision.IsWalkable(
            DungeonCollision.CreateBounds(position, WorldLoot.Size),
            dungeon);
    }

    private static Vector2 ClampInside(Rectangle floor, Vector2 position)
    {
        float halfWidth = WorldLoot.Size.X / 2f;
        float halfHeight = WorldLoot.Size.Y / 2f;
        return new Vector2(
            MathHelper.Clamp(position.X, floor.Left + halfWidth, floor.Right - halfWidth),
            MathHelper.Clamp(position.Y, floor.Top + halfHeight, floor.Bottom - halfHeight));
    }
}
