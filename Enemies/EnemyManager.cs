using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns region-specific enemy population, room activation, and AI updates.
/// </summary>
public sealed class EnemyManager
{
    private const float ActivationDistance = 260f;
    private const float SpawnMargin = 44f;
    private const float MinimumEnemySpawnDistance = 64f;
    private const int SpawnAttempts = 20;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Goblin> _goblins = new();
    private readonly List<Enemy> _compatibilityDefeatedEnemies = new();
    private readonly ProjectileManager _compatibilityProjectiles = new();
    private readonly Random _random;

    public IReadOnlyList<Enemy> Enemies => _enemies;
    public IReadOnlyList<Goblin> Goblins => _goblins;

    public EnemyManager(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(randomSeed.Value)
            : new Random();
    }

    public void Reset(DungeonMap dungeon, int enemyLevel)
    {
        Reset(
            dungeon,
            enemyLevel,
            worldTier: 1,
            RegionType.WildForest);
    }

    public void Reset(
        DungeonMap dungeon,
        int enemyLevel,
        int worldTier)
    {
        Reset(
            dungeon,
            enemyLevel,
            worldTier,
            RegionType.WildForest);
    }

    public void Reset(
        DungeonMap dungeon,
        int enemyLevel,
        int worldTier,
        RegionType region)
    {
        _enemies.Clear();
        _goblins.Clear();
        _compatibilityProjectiles.Clear();
        int safeWorldTier = WorldProgression.ClampWorldTier(worldTier);
        DungeonRoom primaryEliteRoom = FindEliteRoom(dungeon);
        int additionalEliteChance = GetAdditionalEliteRoomChance(safeWorldTier);

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Type != RoomType.Enemy &&
                room.Type != RoomType.Normal)
            {
                continue;
            }

            int budget = GetSpawnBudget(room.Type);

            if (budget <= 0)
                continue;

            bool roomHasElite = room == primaryEliteRoom ||
                room.Type == RoomType.Enemy &&
                _random.Next(100) < additionalEliteChance;
            SpawnRoomComposition(
                room,
                budget,
                enemyLevel,
                safeWorldTier,
                region,
                roomHasElite);
        }
    }

    public void UpdateCombatAndAi(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles)
    {
        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsAlive ||
                !player.IsAlive ||
                !IsActive(enemy, player.Position, dungeon))
            {
                continue;
            }

            enemy.Update(gameTime, player, dungeon, projectiles);
        }
    }

    public void UpdateTimers(
        GameTime gameTime,
        Vector2 playerPosition,
        DungeonMap dungeon)
    {
        foreach (Enemy enemy in _enemies)
        {
            if (IsActive(enemy, playerPosition, dungeon))
                enemy.UpdateTimers(gameTime);
        }
    }

    public void UpdateCombatAndAi(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        foreach (Enemy enemy in _enemies)
        {
            if (IsActive(enemy, player.Position, dungeon))
            {
                enemy.UpdateBehaviorOnly(
                    gameTime,
                    player,
                    dungeon,
                    _compatibilityProjectiles);
            }
        }

        _compatibilityProjectiles.Update(gameTime, player, dungeon);
    }

    public int RemoveDefeated(
        List<Enemy> defeatedEnemies,
        out int experienceReward)
    {
        defeatedEnemies.Clear();
        int defeatedCount = 0;
        experienceReward = 0;

        for (int index = _enemies.Count - 1; index >= 0; index--)
        {
            Enemy enemy = _enemies[index];

            if (enemy.IsAlive)
                continue;

            defeatedCount++;
            experienceReward += enemy.ExperienceReward;
            defeatedEnemies.Add(enemy);
            _enemies.RemoveAt(index);

            if (enemy is Goblin goblin)
                _goblins.Remove(goblin);
        }

        return defeatedCount;
    }

    public int RemoveDefeated(
        List<Goblin> defeatedGoblins,
        out int experienceReward)
    {
        int defeatedCount = RemoveDefeated(
            _compatibilityDefeatedEnemies,
            out experienceReward);
        defeatedGoblins.Clear();

        foreach (Enemy enemy in _compatibilityDefeatedEnemies)
        {
            if (enemy is Goblin goblin)
                defeatedGoblins.Add(goblin);
        }

        return defeatedCount;
    }

    public bool IsActive(
        Enemy enemy,
        Vector2 playerPosition,
        DungeonMap dungeon)
    {
        DungeonRoom playerRoom = dungeon.FindRoomContaining(playerPosition);

        if (playerRoom != null && playerRoom.Id == enemy.RoomId)
            return true;

        DungeonRoom enemyRoom = FindRoomById(dungeon, enemy.RoomId);
        return enemyRoom != null &&
            DistanceSquaredToRectangle(playerPosition, enemyRoom.Bounds) <=
            ActivationDistance * ActivationDistance;
    }

    private void SpawnRoomComposition(
        DungeonRoom room,
        int budget,
        int enemyLevel,
        int worldTier,
        RegionType region,
        bool roomHasElite)
    {
        EnemyType anchor = SelectEnemyType(region, worldTier, budget);
        SpawnEnemy(
            anchor,
            room,
            enemyLevel,
            worldTier,
            roomHasElite);
        budget -= GetSpawnCost(anchor);

        EnemyType? companion = GetCompositionCompanion(anchor, budget);

        if (companion.HasValue)
        {
            SpawnEnemy(
                companion.Value,
                room,
                enemyLevel,
                worldTier,
                isElite: false);
            budget -= GetSpawnCost(companion.Value);
        }

        while (budget > 0)
        {
            EnemyType type = SelectEnemyType(region, worldTier, budget);
            SpawnEnemy(
                type,
                room,
                enemyLevel,
                worldTier,
                isElite: false);
            budget -= GetSpawnCost(type);
        }
    }

    private EnemyType? GetCompositionCompanion(
        EnemyType anchor,
        int remainingBudget)
    {
        if (remainingBudget <= 0)
            return null;

        if (anchor == EnemyType.GiantSpider ||
            anchor == EnemyType.GoblinHunter)
        {
            return EnemyType.Goblin;
        }

        if (anchor == EnemyType.DireWolf &&
            remainingBudget >= GetSpawnCost(EnemyType.DireWolf) &&
            _random.Next(100) < 55)
        {
            return EnemyType.DireWolf;
        }

        return null;
    }

    private void SpawnEnemy(
        EnemyType type,
        DungeonRoom room,
        int enemyLevel,
        int worldTier,
        bool isElite)
    {
        Vector2 position = SelectSpawnPosition(room.Bounds);
        var context = new EnemySpawnContext(
            position,
            enemyLevel,
            worldTier,
            room.Id,
            isElite,
            type == EnemyType.Goblin
                ? SelectGoblinVariant(worldTier)
                : GoblinVariant.Normal);
        Enemy enemy = EnemyFactory.Create(type, context);

        _enemies.Add(enemy);

        if (enemy is Goblin goblin)
            _goblins.Add(goblin);
    }

    private int GetSpawnBudget(RoomType roomType)
    {
        if (roomType == RoomType.Enemy)
            return _random.Next(4, 6);

        return _random.Next(100) < 38
            ? _random.Next(1, 3)
            : 0;
    }

    private EnemyType SelectEnemyType(
        RegionType region,
        int worldTier,
        int availableBudget)
    {
        if (region != RegionType.WildForest)
            return EnemyType.Goblin;

        EnemySpawnWeights weights = GetWildForestSpawnWeights(worldTier);
        int roll = _random.Next(100);

        EnemyType selected;

        if (roll < weights.Goblin)
            selected = EnemyType.Goblin;
        else
        {
            int threshold = weights.Goblin + weights.DireWolf;

            if (roll < threshold)
                selected = EnemyType.DireWolf;
            else
            {
                threshold += weights.GiantSpider;
                selected = roll < threshold
                    ? EnemyType.GiantSpider
                    : EnemyType.GoblinHunter;
            }
        }

        return GetSpawnCost(selected) <= availableBudget
            ? selected
            : EnemyType.Goblin;
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
            float nearestDistanceSquared =
                GetNearestEnemyDistanceSquared(candidate);

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

        foreach (Enemy enemy in _enemies)
        {
            nearestDistanceSquared = MathF.Min(
                nearestDistanceSquared,
                Vector2.DistanceSquared(candidate, enemy.Position));
        }

        return nearestDistanceSquared;
    }

    private GoblinVariant SelectGoblinVariant(int worldTier)
    {
        GetVariantProbabilities(
            worldTier,
            out int normalChance,
            out int fastChance,
            out _);
        int roll = _random.Next(100);

        if (roll < normalChance)
            return GoblinVariant.Normal;

        return roll < normalChance + fastChance
            ? GoblinVariant.Fast
            : GoblinVariant.Brute;
    }

    public static EnemySpawnWeights GetWildForestSpawnWeights(int worldTier)
    {
        return WorldProgression.ClampWorldTier(worldTier) switch
        {
            2 => new EnemySpawnWeights(37, 26, 21, 16),
            3 => new EnemySpawnWeights(34, 27, 22, 17),
            4 => new EnemySpawnWeights(31, 28, 23, 18),
            5 => new EnemySpawnWeights(28, 29, 24, 19),
            _ => new EnemySpawnWeights(40, 25, 20, 15)
        };
    }

    public static void GetVariantProbabilities(
        int worldTier,
        out int normalChance,
        out int fastChance,
        out int bruteChance)
    {
        switch (WorldProgression.ClampWorldTier(worldTier))
        {
            case 2:
                normalChance = 52;
                fastChance = 28;
                bruteChance = 20;
                break;
            case 3:
                normalChance = 44;
                fastChance = 31;
                bruteChance = 25;
                break;
            case 4:
                normalChance = 36;
                fastChance = 34;
                bruteChance = 30;
                break;
            case 5:
                normalChance = 30;
                fastChance = 35;
                bruteChance = 35;
                break;
            default:
                normalChance = 60;
                fastChance = 25;
                bruteChance = 15;
                break;
        }
    }

    public static int GetAdditionalEliteRoomChance(int worldTier)
    {
        return WorldProgression.ClampWorldTier(worldTier) switch
        {
            2 => 10,
            3 => 18,
            4 => 26,
            5 => 34,
            _ => 0
        };
    }

    private static int GetSpawnCost(EnemyType type)
    {
        return type == EnemyType.GiantSpider ||
            type == EnemyType.GoblinHunter
            ? 2
            : 1;
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
