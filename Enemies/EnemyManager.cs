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
/// Owns region-specific enemy population, room activation, deferred summons,
/// and AI updates.
/// </summary>
public sealed class EnemyManager
{
    public const int MaximumGlobalSpiderlings = 8;

    private const float ActivationDistance = 260f;
    private const float SpawnMargin = 190f;
    private const float MinimumEnemySpawnDistance = 96f;
    private const int SpawnAttempts = 20;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Goblin> _goblins = new();
    private readonly List<MotherSpider> _summonRequests = new(4);
    private readonly List<Enemy> _compatibilityDefeatedEnemies = new();
    private readonly ProjectileManager _compatibilityProjectiles = new();
    private readonly RootHazardManager _compatibilityRootHazards = new();
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
        Reset(dungeon, enemyLevel, 1, RegionType.WildForest);
    }

    public void Reset(DungeonMap dungeon, int enemyLevel, int worldTier)
    {
        Reset(dungeon, enemyLevel, worldTier, RegionType.WildForest);
    }

    public void Reset(
        DungeonMap dungeon,
        int enemyLevel,
        int worldTier,
        RegionType region)
    {
        _enemies.Clear();
        _goblins.Clear();
        _summonRequests.Clear();
        _compatibilityProjectiles.Clear();
        _compatibilityRootHazards.Clear();
        int safeWorldTier = WorldProgression.ClampWorldTier(worldTier);
        DungeonRoom primaryEliteRoom = FindEliteRoom(dungeon);
        int additionalEliteChance = GetAdditionalEliteRoomChance(safeWorldTier);

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Type != RoomType.Enemy && room.Type != RoomType.Normal)
                continue;

            int budget = GetSpawnBudget(room.Type);

            if (budget <= 0)
                continue;

            bool hasNamedElite = room == primaryEliteRoom ||
                room.Type == RoomType.Enemy &&
                _random.Next(100) < additionalEliteChance;
            SpawnRoomComposition(
                room,
                budget,
                enemyLevel,
                safeWorldTier,
                region,
                hasNamedElite,
                room.Type == RoomType.Enemy);
        }
    }

    public void UpdateCombatAndAi(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards)
    {
        int activeCount = _enemies.Count;

        for (int index = 0; index < activeCount; index++)
        {
            Enemy enemy = _enemies[index];

            if (!enemy.IsAlive ||
                !player.IsAlive ||
                !IsActive(enemy, player.Position, dungeon))
            {
                continue;
            }

            enemy.Update(
                gameTime,
                player,
                dungeon,
                projectiles,
                rootHazards,
                this);
        }

        ProcessSummonRequests(dungeon);
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
                    _compatibilityProjectiles,
                    _compatibilityRootHazards,
                    this);
            }
        }

        ProcessSummonRequests(dungeon);
        _compatibilityProjectiles.Update(gameTime, player, dungeon);
        _compatibilityRootHazards.Update(gameTime, player);
    }

    public bool RequestSpiderlingSummon(MotherSpider mother)
    {
        if (mother == null || !mother.IsAlive ||
            CountSpiderlings() + _summonRequests.Count >=
            MaximumGlobalSpiderlings ||
            CountSpiderlings(mother) + CountPendingSummons(mother) >=
            MotherSpider.MaximumActiveSpiderlings)
        {
            return false;
        }

        _summonRequests.Add(mother);
        return true;
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

            _enemies.RemoveAt(index);

            if (enemy is Goblin goblin)
                _goblins.Remove(goblin);

            if (enemy is GoblinChief)
                ClearGoblinFamilyBuffs(enemy.RoomId);

            if (!enemy.CountsForProgression)
                continue;

            defeatedCount++;
            experienceReward += enemy.ExperienceReward;
            defeatedEnemies.Add(enemy);
        }

        RemoveOrphanedSpiderlings();
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
        bool hasNamedElite,
        bool useTemplate)
    {
        WildForestEncounterType template = useTemplate
            ? SelectEncounterType(region, worldTier)
            : WildForestEncounterType.Mixed;

        if (hasNamedElite)
        {
            EnemyType eliteType = SelectNamedElite(template);
            SpawnEnemy(eliteType, room, enemyLevel, worldTier, isElite: true);
            budget -= GetSpawnCost(eliteType);
        }

        if (budget <= 0)
            return;

        switch (template)
        {
            case WildForestEncounterType.PackHunt:
                FillSingleType(room, ref budget, EnemyType.DireWolf, enemyLevel, worldTier, 4);
                FillSingleType(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier, 5);
                break;
            case WildForestEncounterType.GoblinPatrol:
                SpawnIfAffordable(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier);
                SpawnIfAffordable(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier);
                SpawnIfAffordable(room, ref budget, EnemyType.GoblinHunter, enemyLevel, worldTier);
                FillSingleType(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier, 5);
                break;
            case WildForestEncounterType.WebNest:
                SpawnIfAffordable(room, ref budget, EnemyType.GiantSpider, enemyLevel, worldTier);
                SpawnIfAffordable(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier);
                FillAlternating(
                    room,
                    ref budget,
                    EnemyType.GiantSpider,
                    EnemyType.Goblin,
                    enemyLevel,
                    worldTier);
                break;
            case WildForestEncounterType.CorruptedGrove:
                SpawnIfAffordable(room, ref budget, EnemyType.CorruptedTreant, enemyLevel, worldTier);
                SpawnIfAffordable(room, ref budget, EnemyType.ThornCrawler, enemyLevel, worldTier);
                FillSingleType(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier, 5);
                break;
            case WildForestEncounterType.BatSwarm:
                FillSingleType(room, ref budget, EnemyType.BloodBat, enemyLevel, worldTier, 4);
                FillSingleType(room, ref budget, EnemyType.Goblin, enemyLevel, worldTier, 5);
                break;
            default:
                FillWeighted(room, ref budget, enemyLevel, worldTier, region);
                break;
        }
    }

    private void FillWeighted(
        DungeonRoom room,
        ref int budget,
        int enemyLevel,
        int worldTier,
        RegionType region)
    {
        while (budget > 0)
        {
            EnemyType type = SelectEnemyType(region, worldTier, budget);
            SpawnEnemy(type, room, enemyLevel, worldTier, isElite: false);
            budget -= GetSpawnCost(type);
        }
    }

    private void FillSingleType(
        DungeonRoom room,
        ref int budget,
        EnemyType type,
        int enemyLevel,
        int worldTier,
        int maximumCount)
    {
        int count = 0;

        while (budget >= GetSpawnCost(type) && count < maximumCount)
        {
            SpawnEnemy(type, room, enemyLevel, worldTier, isElite: false);
            budget -= GetSpawnCost(type);
            count++;
        }
    }

    private void FillAlternating(
        DungeonRoom room,
        ref int budget,
        EnemyType first,
        EnemyType second,
        int enemyLevel,
        int worldTier)
    {
        bool useFirst = true;

        while (budget > 0)
        {
            EnemyType type = useFirst && GetSpawnCost(first) <= budget
                ? first
                : second;
            SpawnIfAffordable(room, ref budget, type, enemyLevel, worldTier);
            useFirst = !useFirst;
        }
    }

    private void SpawnIfAffordable(
        DungeonRoom room,
        ref int budget,
        EnemyType type,
        int enemyLevel,
        int worldTier)
    {
        int cost = GetSpawnCost(type);

        if (budget < cost)
            return;

        SpawnEnemy(type, room, enemyLevel, worldTier, isElite: false);
        budget -= cost;
    }

    private void SpawnEnemy(
        EnemyType type,
        DungeonRoom room,
        int enemyLevel,
        int worldTier,
        bool isElite)
    {
        Vector2 position = SelectSpawnPosition(room, type == EnemyType.BloodBat);
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
        enemy.SnapToGround(room);
        _enemies.Add(enemy);

        if (enemy is Goblin goblin)
            _goblins.Add(goblin);
    }

    private void ProcessSummonRequests(DungeonMap dungeon)
    {
        foreach (MotherSpider mother in _summonRequests)
        {
            if (!mother.IsAlive ||
                CountSpiderlings() >= MaximumGlobalSpiderlings ||
                CountSpiderlings(mother) >= MotherSpider.MaximumActiveSpiderlings)
            {
                continue;
            }

            DungeonRoom room = FindRoomById(dungeon, mother.RoomId);

            if (room == null)
                continue;

            Vector2 position = SelectSpawnPosition(room, isFlying: false);
            var spiderling = new Spiderling(
                position,
                mother.Level,
                mother.RoomId,
                mother.WorldTier,
                mother);
            spiderling.SnapToGround(room);
            _enemies.Add(spiderling);
        }

        _summonRequests.Clear();
    }

    private void RemoveOrphanedSpiderlings()
    {
        for (int index = _enemies.Count - 1; index >= 0; index--)
        {
            if (_enemies[index] is Spiderling spiderling &&
                !spiderling.Mother.IsAlive)
            {
                _enemies.RemoveAt(index);
            }
        }
    }

    private void ClearGoblinFamilyBuffs(int roomId)
    {
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.RoomId == roomId &&
                (enemy.Type == EnemyType.Goblin ||
                 enemy.Type == EnemyType.GoblinHunter))
            {
                enemy.ClearTemporaryBuff();
            }
        }
    }

    private int CountSpiderlings(MotherSpider mother = null)
    {
        int count = 0;

        foreach (Enemy enemy in _enemies)
        {
            if (enemy is Spiderling spiderling &&
                spiderling.IsAlive &&
                (mother == null || ReferenceEquals(spiderling.Mother, mother)))
            {
                count++;
            }
        }

        return count;
    }

    private int CountPendingSummons(MotherSpider mother)
    {
        int count = 0;

        foreach (MotherSpider pending in _summonRequests)
        {
            if (ReferenceEquals(pending, mother))
                count++;
        }

        return count;
    }

    private int GetSpawnBudget(RoomType roomType)
    {
        if (roomType == RoomType.Enemy)
            return _random.Next(5, 8);

        return _random.Next(100) < 38 ? _random.Next(1, 4) : 0;
    }

    private WildForestEncounterType SelectEncounterType(
        RegionType region,
        int worldTier)
    {
        if (region != RegionType.WildForest || _random.Next(100) < 15)
            return WildForestEncounterType.Mixed;

        return SelectEnemyType(region, worldTier, availableBudget: 3) switch
        {
            EnemyType.DireWolf => WildForestEncounterType.PackHunt,
            EnemyType.GiantSpider => WildForestEncounterType.WebNest,
            EnemyType.ThornCrawler => WildForestEncounterType.CorruptedGrove,
            EnemyType.CorruptedTreant => WildForestEncounterType.CorruptedGrove,
            EnemyType.BloodBat => WildForestEncounterType.BatSwarm,
            _ => WildForestEncounterType.GoblinPatrol
        };
    }

    private EnemyType SelectNamedElite(WildForestEncounterType template)
    {
        if (template == WildForestEncounterType.WebNest)
            return EnemyType.MotherSpider;

        if (template == WildForestEncounterType.GoblinPatrol)
            return EnemyType.GoblinChief;

        return _random.Next(2) == 0
            ? EnemyType.GoblinChief
            : EnemyType.MotherSpider;
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
        int threshold = weights.Goblin;
        EnemyType selected;

        if (roll < threshold)
            selected = EnemyType.Goblin;
        else if (roll < (threshold += weights.DireWolf))
            selected = EnemyType.DireWolf;
        else if (roll < (threshold += weights.GiantSpider))
            selected = EnemyType.GiantSpider;
        else if (roll < (threshold += weights.GoblinHunter))
            selected = EnemyType.GoblinHunter;
        else if (roll < (threshold += weights.ThornCrawler))
            selected = EnemyType.ThornCrawler;
        else if (roll < (threshold += weights.CorruptedTreant))
            selected = EnemyType.CorruptedTreant;
        else
            selected = EnemyType.BloodBat;

        return GetSpawnCost(selected) <= availableBudget
            ? selected
            : EnemyType.Goblin;
    }

    private Vector2 SelectSpawnPosition(DungeonRoom room, bool isFlying)
    {
        Rectangle roomBounds = room.Bounds;
        float spawnY = isFlying ? room.GroundY - 180f : room.GroundY - 80f;
        Vector2 bestCandidate = new(roomBounds.Center.X, spawnY);
        float bestNearestDistanceSquared = -1f;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            Vector2 candidate = new(
                MathHelper.Lerp(
                    roomBounds.Left + SpawnMargin,
                    roomBounds.Right - SpawnMargin,
                    (float)_random.NextDouble()),
                spawnY);

            if (!isFlying && !IsGroundSpawnClear(candidate.X, room))
                continue;

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

        if (!isFlying && !IsGroundSpawnClear(bestCandidate.X, room))
        {
            for (float x = roomBounds.Left + SpawnMargin;
                x <= roomBounds.Right - SpawnMargin;
                x += 64f)
            {
                if (IsGroundSpawnClear(x, room))
                    return new Vector2(x, spawnY);
            }
        }

        return bestCandidate;
    }

    private static bool IsGroundSpawnClear(float x, DungeonRoom room)
    {
        foreach (Platform platform in room.Platforms)
        {
            if (platform.Kind == PlatformKind.Ground ||
                platform.Kind == PlatformKind.Transition)
            {
                continue;
            }

            if (x >= platform.Bounds.Left - 56f &&
                x <= platform.Bounds.Right + 56f)
            {
                return false;
            }
        }

        return true;
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
        GetVariantProbabilities(worldTier, out int normal, out int fast, out _);
        int roll = _random.Next(100);
        return roll < normal
            ? GoblinVariant.Normal
            : roll < normal + fast
                ? GoblinVariant.Fast
                : GoblinVariant.Brute;
    }

    public static EnemySpawnWeights GetWildForestSpawnWeights(int worldTier)
    {
        return WorldProgression.ClampWorldTier(worldTier) switch
        {
            2 => new EnemySpawnWeights(28, 22, 15, 11, 11, 6, 7),
            3 => new EnemySpawnWeights(24, 21, 16, 12, 12, 7, 8),
            4 => new EnemySpawnWeights(21, 20, 17, 13, 13, 8, 8),
            5 => new EnemySpawnWeights(18, 19, 18, 14, 14, 9, 8),
            _ => new EnemySpawnWeights(32, 22, 14, 10, 10, 5, 7)
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
            case 2: normalChance = 52; fastChance = 28; bruteChance = 20; break;
            case 3: normalChance = 44; fastChance = 31; bruteChance = 25; break;
            case 4: normalChance = 36; fastChance = 34; bruteChance = 30; break;
            case 5: normalChance = 30; fastChance = 35; bruteChance = 35; break;
            default: normalChance = 60; fastChance = 25; bruteChance = 15; break;
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

    public static int GetSpawnCost(EnemyType type)
    {
        return type switch
        {
            EnemyType.GiantSpider => 2,
            EnemyType.GoblinHunter => 2,
            EnemyType.ThornCrawler => 2,
            EnemyType.CorruptedTreant => 3,
            EnemyType.GoblinChief => 3,
            EnemyType.MotherSpider => 4,
            _ => 1
        };
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

    private static float DistanceSquaredToRectangle(Vector2 point, Rectangle rectangle)
    {
        float horizontal = MathF.Max(
            rectangle.Left - point.X,
            MathF.Max(0f, point.X - rectangle.Right));
        float vertical = MathF.Max(
            rectangle.Top - point.Y,
            MathF.Max(0f, point.Y - rectangle.Bottom));
        return horizontal * horizontal + vertical * vertical;
    }
}
