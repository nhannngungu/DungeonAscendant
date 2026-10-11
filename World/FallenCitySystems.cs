using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public sealed class CityAlarmSystem
{
    private const float CalmReductionSeconds = 6f;
    private readonly HashSet<string> _triggeredAlarmZones = new();
    private readonly HashSet<string> _disabledObjects = new();
    private readonly HashSet<string> _spawnedReinforcements = new();
    private float _calmTime;

    public int Level { get; private set; }
    public int MaximumLevel => 3;
    public float WatcherSignalFeedbackTimeRemaining { get; private set; }
    public IReadOnlyCollection<string> DisabledObjects => _disabledObjects;

    public void Reset()
    {
        Level = 0;
        _calmTime = 0f;
        WatcherSignalFeedbackTimeRemaining = 0f;
        _triggeredAlarmZones.Clear();
        _disabledObjects.Clear();
        _spawnedReinforcements.Clear();
    }

    public void Update(
        GameTime gameTime,
        DungeonMap map,
        Vector2 playerPosition,
        EnemyManager enemies,
        WildForestEncounterDirector encounters,
        int enemyLevel,
        int worldTier)
    {
        if (map?.IsFallenCity != true)
        {
            Reset();
            return;
        }

        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .1f);
        WatcherSignalFeedbackTimeRemaining = MathF.Max(
            0f,
            WatcherSignalFeedbackTimeRemaining - elapsed);

        foreach (FallenCityZoneDefinition zone in map.FallenCityZones)
        {
            if (!zone.RaisesAlarmOnEntry ||
                _triggeredAlarmZones.Contains(zone.ZoneId))
                continue;
            foreach (EncounterZone encounter in map.EncounterZones)
            {
                if (encounter.SectionId == zone.ZoneId &&
                    encounters.IsTriggered(encounter.ZoneId))
                {
                    _triggeredAlarmZones.Add(zone.ZoneId);
                    Raise(1);
                    break;
                }
            }
        }

        bool cityThreatAlive = false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!enemy.IsAlive || enemy is not FallenCityEnemy cityEnemy)
                continue;
            float playerDistanceSquared = Vector2.DistanceSquared(
                enemy.Position, playerPosition);
            cityThreatAlive |= playerDistanceSquared < 800f * 800f;
            if (cityEnemy.Type == EnemyType.BellWatcher)
            {
                cityEnemy.ConfigureAlarmTarget(
                    FindAvailableAlarmPosition(map, cityEnemy.Position));
            }
            if (cityEnemy.TryConsumeAlarmSignal())
            {
                Raise(1);
                WatcherSignalFeedbackTimeRemaining = .95f;
            }
            if (Level >= 2 &&
                playerDistanceSquared < 650f * 650f)
            {
                enemy.ApplyTemporaryBuff(
                    damagePercent: Level == 3 ? 12 : 6,
                    movementPercent: Level == 3 ? 8 : 3,
                    durationSeconds: .3f);
            }
        }

        SpawnEligibleReinforcements(map, enemies, enemyLevel, worldTier);
        if (cityThreatAlive)
        {
            _calmTime = 0f;
        }
        else if (Level > 0)
        {
            _calmTime += elapsed;
            if (_calmTime >= CalmReductionSeconds)
            {
                Level--;
                _calmTime = 0f;
            }
        }
    }

    public bool TryDisableNearest(Vector2 playerPosition, DungeonMap map)
    {
        if (map?.IsFallenCity != true)
            return false;
        foreach (CityAlarmObjectDefinition alarm in map.CityAlarmObjects)
        {
            if (_disabledObjects.Contains(alarm.ObjectId) ||
                Vector2.DistanceSquared(playerPosition, alarm.Position) >
                    alarm.DisableRadius * alarm.DisableRadius)
                continue;
            _disabledObjects.Add(alarm.ObjectId);
            Level = Math.Max(0, Level - 1);
            _calmTime = 0f;
            return true;
        }
        return false;
    }

    public bool IsDisabled(string objectId) =>
        _disabledObjects.Contains(objectId ?? string.Empty);

    private void Raise(int amount)
    {
        Level = Math.Clamp(Level + Math.Max(0, amount), 0, MaximumLevel);
        _calmTime = 0f;
    }

    private Vector2? FindAvailableAlarmPosition(
        DungeonMap map,
        Vector2 watcherPosition)
    {
        string zoneId = null;
        Point watcherPoint = watcherPosition.ToPoint();
        foreach (FallenCityZoneDefinition zone in map.FallenCityZones)
        {
            if (zone.Bounds.Contains(watcherPoint))
            {
                zoneId = zone.ZoneId;
                break;
            }
        }

        if (zoneId == null)
            return null;

        Vector2? nearest = null;
        float nearestDistanceSquared = float.MaxValue;
        foreach (CityAlarmObjectDefinition alarm in map.CityAlarmObjects)
        {
            if (alarm.ZoneId != zoneId ||
                _disabledObjects.Contains(alarm.ObjectId))
            {
                continue;
            }
            float distanceSquared = Vector2.DistanceSquared(
                watcherPosition,
                alarm.Position);
            if (distanceSquared >= nearestDistanceSquared)
                continue;
            nearestDistanceSquared = distanceSquared;
            nearest = alarm.Position;
        }
        return nearest;
    }

    private void SpawnEligibleReinforcements(
        DungeonMap map,
        EnemyManager enemies,
        int enemyLevel,
        int worldTier)
    {
        foreach (CityReinforcementDefinition reinforcement in map.CityReinforcements)
        {
            SpawnSocket socket = reinforcement.Socket;
            if (Level < reinforcement.MinimumAlarmLevel ||
                _spawnedReinforcements.Contains(socket.SocketId))
                continue;
            DungeonRoom room = map.FindRoomContaining(socket.Position);
            EnemyType type = socket.SuggestedEnemyTypes[0];
            Enemy spawned = enemies.SpawnAuthoredEnemy(
                type, socket, room, map, enemyLevel, worldTier);
            if (spawned != null)
                _spawnedReinforcements.Add(socket.SocketId);
        }
    }
}

public sealed class CollapsingCitySystem
{
    private const float BreakFeedbackSeconds = .72f;
    private readonly HashSet<string> _broken = new();
    private readonly HashSet<Enemy> _processedEliteAttacks = new();

    public IReadOnlyCollection<string> BrokenStructureIds => _broken;
    public string RecentlyBrokenStructureId { get; private set; } = string.Empty;
    public float BreakFeedbackTimeRemaining { get; private set; }

    public void Reset(DungeonMap map = null)
    {
        _broken.Clear();
        _processedEliteAttacks.Clear();
        RecentlyBrokenStructureId = string.Empty;
        BreakFeedbackTimeRemaining = 0f;
        map?.ResetCollapsingStructureCollision();
    }

    public void Update(GameTime gameTime)
    {
        BreakFeedbackTimeRemaining = MathF.Max(0f,
            BreakFeedbackTimeRemaining - MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds, .1f));
        if (BreakFeedbackTimeRemaining <= 0f)
            RecentlyBrokenStructureId = string.Empty;
    }

    public bool TryBreakWithHeavyAttack(Rectangle attackArea, DungeonMap map)
    {
        if (map?.IsFallenCity != true)
            return false;
        bool changed = false;
        foreach (CollapsingStructureDefinition structure in map.CollapsingStructures)
        {
            if (!structure.IsMandatorySupport &&
                !_broken.Contains(structure.StructureId) &&
                attackArea.Intersects(structure.Bounds))
            {
                _broken.Add(structure.StructureId);
                map.SetCollapsingStructureBroken(structure.StructureId);
                RecentlyBrokenStructureId = structure.StructureId;
                BreakFeedbackTimeRemaining = BreakFeedbackSeconds;
                changed = true;
            }
        }
        return changed;
    }

    public void UpdateEliteAttacks(DungeonMap map, IReadOnlyList<Enemy> enemies)
    {
        if (map?.IsFallenCity != true)
            return;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive || !enemy.IsElite || !enemy.IsCombatAttackActive)
            {
                _processedEliteAttacks.Remove(enemy);
                continue;
            }
            if (_processedEliteAttacks.Add(enemy))
                TryBreakWithHeavyAttack(enemy.ActiveAttackArea, map);
        }
    }

    public bool IsBroken(string structureId) =>
        _broken.Contains(structureId ?? string.Empty);
}
