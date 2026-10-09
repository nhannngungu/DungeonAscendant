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
    public const float WildForestShowcaseActivationDistance = 650f;
    public const float WildForestShowcaseSummonRadius = 180f;
    // Temporary Combat 2.0 test override. Disable to restore normal first-room spawning.
    public static bool DebugForceFirstEnemyGoblinHunter { get; set; } = true;

    private const float ActivationDistance = 260f;
    private const float SpawnMargin = 190f;
    private const float MinimumEnemySpawnDistance = 96f;
    private const float DebugHunterDistanceFromRoomEntrance = 650f;
    private const int SpawnAttempts = 20;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Goblin> _goblins = new();
    private readonly List<Enemy> _defeatPresentations = new(8);
    private readonly List<MotherSpider> _summonRequests = new(4);
    private readonly List<Enemy> _compatibilityDefeatedEnemies = new();
    private readonly ProjectileManager _compatibilityProjectiles = new();
    private readonly RootHazardManager _compatibilityRootHazards = new();
    private readonly Random _random;
    private bool _wildForestShowcaseMode;

    public IReadOnlyList<Enemy> Enemies => _enemies;
    public IReadOnlyList<Goblin> Goblins => _goblins;
    public IReadOnlyList<Enemy> DefeatPresentations => _defeatPresentations;
    public bool IsWildForestShowcaseMode => _wildForestShowcaseMode;

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
        RegionType region,
        bool isFirstDungeonOfRun = false,
        bool suppressProceduralSpawns = false)
    {
        _enemies.Clear();
        _goblins.Clear();
        _defeatPresentations.Clear();
        _summonRequests.Clear();
        _compatibilityProjectiles.Clear();
        _compatibilityRootHazards.Clear();
        _wildForestShowcaseMode = suppressProceduralSpawns;

        if (suppressProceduralSpawns)
            return;

        int safeWorldTier = WorldProgression.ClampWorldTier(worldTier);
        DungeonRoom primaryEliteRoom = FindEliteRoom(dungeon);
        DungeonRoom debugHunterRoom =
            DebugForceFirstEnemyGoblinHunter && isFirstDungeonOfRun
                ? FindFirstEnemyRoom(dungeon)
                : null;
        DungeonRoom debugMeleeGoblinRoom = debugHunterRoom != null
            ? FindNextCombatRoom(dungeon, debugHunterRoom)
            : null;
        int additionalEliteChance = GetAdditionalEliteRoomChance(safeWorldTier);

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Type != RoomType.Enemy && room.Type != RoomType.Normal)
                continue;

            if (debugHunterRoom != null)
            {
                if (room == debugHunterRoom)
                {
                    SpawnDebugFirstHunter(
                        dungeon,
                        room,
                        enemyLevel,
                        safeWorldTier);
                    continue;
                }

                if (room == debugMeleeGoblinRoom)
                {
                    SpawnDebugMeleeGoblin(
                        room,
                        enemyLevel,
                        safeWorldTier);
                    continue;
                }

                if (room.Bounds.Left < debugHunterRoom.Bounds.Left)
                    continue;
            }

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

    public Enemy SpawnSequentialTestEnemy(
        EnemyType type,
        DungeonRoom room,
        Vector2 playerPosition,
        int enemyLevel,
        int worldTier,
        DungeonMap dungeon)
    {
        if (room == null || dungeon == null || type == EnemyType.Spiderling)
            return null;

        bool isFlying = type == EnemyType.BloodBat;
        bool isRanged = type == EnemyType.GoblinHunter;
        float desiredDistance = type switch
        {
            EnemyType.GoblinHunter => 650f,
            EnemyType.MotherSpider => 550f,
            EnemyType.BloodBat => 500f,
            EnemyType.CorruptedTreant => 500f,
            EnemyType.ThornCrawler => 475f,
            _ => 425f
        };
        Vector2 safeSize = isFlying
            ? new Vector2(48f, 36f)
            : new Vector2(110f, 126f);
        Vector2 position = FindSequentialTestPosition(
            room,
            playerPosition,
            desiredDistance,
            isRanged ? 500f : 350f,
            isFlying,
            safeSize,
            dungeon);
        bool isNamedElite = type == EnemyType.GoblinChief ||
            type == EnemyType.MotherSpider;
        var context = new EnemySpawnContext(
            position,
            enemyLevel,
            worldTier,
            room.Id,
            isNamedElite,
            GoblinVariant.Normal);
        Enemy enemy = EnemyFactory.Create(type, context);
        enemy.SnapToGround(room);
        _enemies.Add(enemy);

        if (enemy is Goblin goblin)
            _goblins.Add(goblin);

        return enemy;
    }

    public Enemy SpawnWildForestShowcaseEnemy(
        EnemyType type,
        DungeonRoom room,
        float x,
        int enemyLevel,
        int worldTier)
    {
        if (room == null || type == EnemyType.Spiderling)
            return null;

        bool isFlying = type == EnemyType.BloodBat;
        Vector2 position = new(
            MathHelper.Clamp(
                x,
                room.Bounds.Left + SpawnMargin,
                room.Bounds.Right - SpawnMargin),
            isFlying ? room.GroundY - 180f : room.GroundY - 80f);
        bool isNamedElite = type == EnemyType.GoblinChief ||
            type == EnemyType.MotherSpider;
        var context = new EnemySpawnContext(
            position,
            enemyLevel,
            worldTier,
            room.Id,
            isNamedElite,
            GoblinVariant.Normal);
        Enemy enemy = EnemyFactory.Create(type, context);

        if (!isFlying)
            enemy.SnapToGround(room);

        _enemies.Add(enemy);

        if (enemy is Goblin goblin)
            _goblins.Add(goblin);

        return enemy;
    }

    public Enemy SpawnAuthoredEnemy(
        EnemyType type,
        SpawnSocket socket,
        DungeonRoom room,
        DungeonMap dungeon,
        int enemyLevel,
        int worldTier)
    {
        if (socket == null || room == null || dungeon == null ||
            type == EnemyType.Spiderling)
            return null;

        bool aerial = type is EnemyType.BloodBat or EnemyType.GraveBat ||
            socket.Role == SpawnSocketRole.Flying;
        bool elite = type is EnemyType.GoblinChief or EnemyType.MotherSpider or
            EnemyType.CursedKnight or EnemyType.SoulCollector or EnemyType.DeathKnight or EnemyType.FallenKnight;
        var context = new EnemySpawnContext(
            socket.Position,
            enemyLevel,
            worldTier,
            room.Id,
            elite,
            GoblinVariant.Normal);
        Enemy enemy = EnemyFactory.Create(type, context);
        float[] offsets = { 0f, -28f, 28f, -56f, 56f };
        bool placed = false;
        foreach (float offset in offsets)
        {
            float x = socket.Position.X + offset;
            float footY = aerial
                ? socket.Position.Y
                : FindAuthoredSupportTop(
                    room, x, enemy.Size.X, socket.Position.Y);
            Vector2 foot = new(x, footY);
            enemy.PlaceAtAuthoredSocket(foot, aerial);
            if (!socket.PlacementBounds.Contains(enemy.Position.ToPoint()) ||
                !SideScrollingCollision.IsPositionFree(
                    enemy.Position, enemy.Size, dungeon))
                continue;
            if (!aerial && !SideScrollingCollision.IsSupported(
                    enemy.Position, enemy.Size, dungeon))
                continue;
            placed = true;
            break;
        }

        if (!placed)
            return null;

        _enemies.Add(enemy);
        if (enemy is Goblin goblin)
            _goblins.Add(goblin);
        return enemy;
    }

    private static float FindAuthoredSupportTop(
        DungeonRoom room,
        float x,
        float enemyWidth,
        float desiredTop)
    {
        float halfWidth = enemyWidth / 2f;
        float bestTop = desiredTop;
        float bestDistance = 34f;
        foreach (Platform platform in room.Platforms)
        {
            if (platform.Kind is not
                    (PlatformKind.Ground or PlatformKind.Transition or
                     PlatformKind.Raised) ||
                x - halfWidth < platform.Bounds.Left ||
                x + halfWidth > platform.Bounds.Right)
                continue;
            float distance = MathF.Abs(platform.Bounds.Top - desiredTop);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            bestTop = platform.Bounds.Top;
        }
        return bestTop;
    }

    public void ClearSequentialTestEnemies(
        bool clearDefeatPresentations = true)
    {
        _enemies.Clear();
        _goblins.Clear();

        if (clearDefeatPresentations)
            _defeatPresentations.Clear();

        _summonRequests.Clear();
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
        UpdateDeathPresentations(gameTime);
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
        UpdateDeathPresentations(gameTime);
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

            if (enemy.Type == EnemyType.Goblin ||
                enemy.Type == EnemyType.GoblinHunter ||
                enemy.Type == EnemyType.DireWolf ||
                enemy.Type == EnemyType.GiantSpider ||
                enemy.Type == EnemyType.BloodBat ||
                enemy.Type == EnemyType.ThornCrawler ||
                enemy.Type == EnemyType.CorruptedTreant ||
                enemy.Type == EnemyType.GoblinChief ||
                enemy.Type == EnemyType.MotherSpider)
            {
                _defeatPresentations.Add(enemy);
            }

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

    public void RemoveForDebug(IReadOnlyList<Enemy> enemies)
    {
        if (enemies == null)
            return;

        foreach (Enemy enemy in enemies)
        {
            _enemies.Remove(enemy);
            if (enemy is Goblin goblin)
                _goblins.Remove(goblin);
            _defeatPresentations.Remove(enemy);
            if (enemy is GoblinChief)
                ClearGoblinFamilyBuffs(enemy.RoomId);
        }

        RemoveOrphanedSpiderlings();
    }

    private void UpdateDeathPresentations(GameTime gameTime)
    {
        for (int index = _defeatPresentations.Count - 1;
             index >= 0;
             index--)
        {
            Enemy enemy = _defeatPresentations[index];
            enemy.UpdateDeathPresentation(gameTime);

            if (enemy.IsDeathPresentationComplete)
                _defeatPresentations.RemoveAt(index);
        }
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
        if (_wildForestShowcaseMode)
        {
            return Vector2.DistanceSquared(playerPosition, enemy.Position) <=
                WildForestShowcaseActivationDistance *
                WildForestShowcaseActivationDistance;
        }

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

    private void SpawnDebugFirstHunter(
        DungeonMap dungeon,
        DungeonRoom room,
        int enemyLevel,
        int worldTier)
    {
        Vector2 hunterSize = new(36f, 46f);
        float desiredX = room.Entrance.X +
            DebugHunterDistanceFromRoomEntrance;
        float spawnX = MathHelper.Clamp(
            desiredX,
            room.Bounds.Left + SpawnMargin,
            room.Bounds.Right - SpawnMargin);
        Vector2 position = SideScrollingCollision.PlaceOnGround(
            spawnX,
            hunterSize,
            room);

        if (!SideScrollingCollision.IsPositionFree(position, hunterSize, dungeon))
        {
            position = SideScrollingCollision.PlaceOnGround(
                room.Bounds.Center.X,
                hunterSize,
                room);
        }

        var context = new EnemySpawnContext(
            position,
            enemyLevel,
            worldTier,
            room.Id,
            isElite: false,
            GoblinVariant.Normal);
        Enemy hunter = EnemyFactory.Create(EnemyType.GoblinHunter, context);
        hunter.SnapToGround(room);
        _enemies.Add(hunter);
    }

    private void SpawnDebugMeleeGoblin(
        DungeonRoom room,
        int enemyLevel,
        int worldTier)
    {
        Vector2 position = SelectSpawnPosition(room, isFlying: false);
        var context = new EnemySpawnContext(
            position,
            enemyLevel,
            worldTier,
            room.Id,
            isElite: false,
            GoblinVariant.Normal);
        Enemy goblin = EnemyFactory.Create(EnemyType.Goblin, context);
        goblin.SnapToGround(room);
        _enemies.Add(goblin);
        _goblins.Add((Goblin)goblin);
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

            Vector2 position = _wildForestShowcaseMode
                ? SelectShowcaseSummonPosition(room, mother.Position, dungeon)
                : SelectSpawnPosition(room, isFlying: false);
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

    private static Vector2 SelectShowcaseSummonPosition(
        DungeonRoom room,
        Vector2 motherPosition,
        DungeonMap dungeon)
    {
        Vector2 spiderlingSize = new(28f, 20f);

        for (int step = 1; step <= 6; step++)
        {
            float direction = step % 2 == 1 ? -1f : 1f;
            float distance = 60f + ((step - 1) / 2) * 48f;
            float x = MathHelper.Clamp(
                motherPosition.X + direction * distance,
                motherPosition.X - WildForestShowcaseSummonRadius,
                motherPosition.X + WildForestShowcaseSummonRadius);
            Vector2 candidate = SideScrollingCollision.PlaceOnGround(
                x,
                spiderlingSize,
                room);

            if (SideScrollingCollision.IsPositionFree(
                candidate,
                spiderlingSize,
                dungeon))
            {
                return candidate;
            }
        }

        return SideScrollingCollision.PlaceOnGround(
            motherPosition.X,
            spiderlingSize,
            room);
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

    private static Vector2 FindSequentialTestPosition(
        DungeonRoom room,
        Vector2 playerPosition,
        float desiredDistance,
        float minimumDistance,
        bool isFlying,
        Vector2 safeSize,
        DungeonMap dungeon)
    {
        float minimumX = room.Bounds.Left + SpawnMargin;
        float maximumX = room.Bounds.Right - SpawnMargin;
        float rightSpace = maximumX - playerPosition.X;
        float leftSpace = playerPosition.X - minimumX;
        float direction = rightSpace >= desiredDistance
            ? 1f
            : leftSpace >= desiredDistance
                ? -1f
                : rightSpace >= leftSpace ? 1f : -1f;
        float desiredX = MathHelper.Clamp(
            playerPosition.X + direction * desiredDistance,
            minimumX,
            maximumX);
        float bestX = desiredX;
        float bestDistance = MathF.Abs(desiredX - playerPosition.X);

        for (int step = 0; step <= 8; step++)
        {
            float offset = step == 0
                ? 0f
                : ((step + 1) / 2) * 64f * (step % 2 == 1 ? 1f : -1f);
            float candidateX = MathHelper.Clamp(
                desiredX + offset,
                minimumX,
                maximumX);

            if (!isFlying && !IsGroundSpawnClear(candidateX, room))
                continue;

            Vector2 candidate = isFlying
                ? new Vector2(candidateX, room.GroundY - 180f)
                : SideScrollingCollision.PlaceOnGround(
                    candidateX,
                    safeSize,
                    room);

            if (!SideScrollingCollision.IsPositionFree(candidate, safeSize, dungeon))
                continue;

            float distance = MathF.Abs(candidateX - playerPosition.X);

            if (distance >= minimumDistance)
                return candidate;

            if (distance > bestDistance)
            {
                bestX = candidateX;
                bestDistance = distance;
            }
        }

        return isFlying
            ? new Vector2(bestX, room.GroundY - 180f)
            : SideScrollingCollision.PlaceOnGround(bestX, safeSize, room);
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

    private static DungeonRoom FindFirstEnemyRoom(DungeonMap dungeon)
    {
        DungeonRoom result = null;

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Type == RoomType.Enemy &&
                (result == null || room.Bounds.Left < result.Bounds.Left))
            {
                result = room;
            }
        }

        return result;
    }

    private static DungeonRoom FindNextCombatRoom(
        DungeonMap dungeon,
        DungeonRoom previousRoom)
    {
        DungeonRoom result = null;

        foreach (DungeonRoom room in dungeon.Rooms)
        {
            bool supportsCombat = room.Type == RoomType.Enemy ||
                room.Type == RoomType.Normal;

            if (!supportsCombat || room.Bounds.Left <= previousRoom.Bounds.Left)
                continue;

            if (result == null || room.Bounds.Left < result.Bounds.Left)
                result = room;
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
