using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns dungeon enemy placement, room activation, and enemy updates.
/// </summary>
public sealed class EnemyManager
{
    private const float ActivationDistance = 260f;
    private const float SpawnMargin = 44f;
    private const float MinimumEnemySpawnDistance = 64f;
    private const int SpawnAttempts = 20;

    private readonly List<Goblin> _goblins = new();
    private readonly Random _random;

    public IReadOnlyList<Goblin> Goblins => _goblins;

    public EnemyManager(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
    }

    public void Reset(DungeonMap dungeon, int playerLevel)
    {
        _goblins.Clear();
        DungeonRoom eliteRoom = FindEliteRoom(dungeon);

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            int enemyCount = GetEnemyCount(room.Type);

            for (int index = 0; index < enemyCount; index++)
            {
                bool isElite = room == eliteRoom && index == 0;
                SpawnGoblin(room, playerLevel, isElite);
            }
        }
    }

    public void UpdateTimers(
        GameTime gameTime,
        Vector2 playerPosition,
        DungeonMap dungeon)
    {
        foreach (Goblin goblin in _goblins)
        {
            if (IsActive(goblin, playerPosition, dungeon))
                goblin.UpdateTimers(gameTime);
        }
    }

    public void UpdateCombatAndAi(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        foreach (Goblin goblin in _goblins)
        {
            if (!goblin.IsAlive ||
                !player.IsAlive ||
                !IsActive(goblin, player.Position, dungeon))
            {
                continue;
            }

            float distanceSquared = Vector2.DistanceSquared(
                goblin.Position,
                player.Position);

            if (distanceSquared <= goblin.Attack.Range * goblin.Attack.Range)
            {
                if (goblin.Attack.TryStart())
                    player.ReceiveDamage(goblin.AttackDamage);
            }
            else
            {
                goblin.UpdateMovement(
                    gameTime,
                    player.Position,
                    goblin.Attack.Range,
                    dungeon);
            }
        }
    }

    public int RemoveDefeated(
        List<Goblin> defeatedGoblins,
        out int experienceReward)
    {
        defeatedGoblins.Clear();
        int defeatedCount = 0;
        experienceReward = 0;

        for (int index = _goblins.Count - 1; index >= 0; index--)
        {
            Goblin goblin = _goblins[index];

            if (goblin.IsAlive)
                continue;

            defeatedCount++;
            experienceReward += goblin.ExperienceReward;
            defeatedGoblins.Add(goblin);
            _goblins.RemoveAt(index);
        }

        return defeatedCount;
    }

    public bool IsActive(
        Goblin goblin,
        Vector2 playerPosition,
        DungeonMap dungeon)
    {
        DungeonRoom playerRoom = dungeon.FindRoomContaining(playerPosition);

        if (playerRoom != null && playerRoom.Id == goblin.RoomId)
            return true;

        DungeonRoom enemyRoom = FindRoomById(dungeon, goblin.RoomId);
        return enemyRoom != null &&
            DistanceSquaredToRectangle(playerPosition, enemyRoom.Bounds) <=
            ActivationDistance * ActivationDistance;
    }

    private int GetEnemyCount(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => 0,
            RoomType.Enemy => _random.Next(2, 5),
            RoomType.Exit => _random.Next(1, 3),
            _ => _random.Next(0, 3)
        };
    }

    private void SpawnGoblin(
        DungeonRoom room,
        int playerLevel,
        bool isElite)
    {
        Vector2 spawnPosition = SelectSpawnPosition(room.Bounds);
        GoblinVariant variant = SelectVariant(playerLevel);
        _goblins.Add(new Goblin(
            spawnPosition,
            playerLevel,
            variant,
            isElite,
            roomId: room.Id));
    }

    private Vector2 SelectSpawnPosition(Rectangle roomBounds)
    {
        Vector2 bestCandidate = roomBounds.Center.ToVector2();
        float bestNearestDistanceSquared = -1f;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            Vector2 candidate = new(
                MathHelper.Lerp(
                    roomBounds.Left + SpawnMargin,
                    roomBounds.Right - SpawnMargin,
                    (float)_random.NextDouble()),
                MathHelper.Lerp(
                    roomBounds.Top + SpawnMargin,
                    roomBounds.Bottom - SpawnMargin,
                    (float)_random.NextDouble()));
            float nearestDistanceSquared = GetNearestEnemyDistanceSquared(candidate);

            if (nearestDistanceSquared > bestNearestDistanceSquared)
            {
                bestCandidate = candidate;
                bestNearestDistanceSquared = nearestDistanceSquared;
            }

            if (nearestDistanceSquared >=
                MinimumEnemySpawnDistance * MinimumEnemySpawnDistance)
            {
                return candidate;
            }
        }

        return bestCandidate;
    }

    private float GetNearestEnemyDistanceSquared(Vector2 candidate)
    {
        float nearestDistanceSquared = float.MaxValue;

        foreach (Goblin goblin in _goblins)
        {
            nearestDistanceSquared = MathF.Min(
                nearestDistanceSquared,
                Vector2.DistanceSquared(candidate, goblin.Position));
        }

        return nearestDistanceSquared;
    }

    private GoblinVariant SelectVariant(int playerLevel)
    {
        int levelsAboveOne = Math.Max(0, playerLevel - 1);
        int bruteChance = Math.Min(35, 15 + levelsAboveOne * 2);
        int fastChance = Math.Min(30, 25 + levelsAboveOne);
        int roll = _random.Next(100);

        if (roll < bruteChance)
            return GoblinVariant.Brute;

        if (roll < bruteChance + fastChance)
            return GoblinVariant.Fast;

        return GoblinVariant.Normal;
    }

    private static DungeonRoom FindEliteRoom(DungeonMap dungeon)
    {
        DungeonRoom result = dungeon.ExitRoom;
        float farthestDistanceSquared = -1f;

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Type != RoomType.Enemy)
                continue;

            float distanceSquared = Vector2.DistanceSquared(
                room.Center,
                dungeon.StartRoom.Center);

            if (distanceSquared > farthestDistanceSquared)
            {
                result = room;
                farthestDistanceSquared = distanceSquared;
            }
        }

        return result;
    }

    private static DungeonRoom FindRoomById(DungeonMap dungeon, int roomId)
    {
        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Id == roomId)
                return room;
        }

        return null;
    }

    private static float DistanceSquaredToRectangle(
        Vector2 point,
        Rectangle rectangle)
    {
        float horizontalDistance = MathF.Max(
            rectangle.Left - point.X,
            MathF.Max(0f, point.X - rectangle.Right));
        float verticalDistance = MathF.Max(
            rectangle.Top - point.Y,
            MathF.Max(0f, point.Y - rectangle.Bottom));
        return horizontalDistance * horizontalDistance +
            verticalDistance * verticalDistance;
    }
}
