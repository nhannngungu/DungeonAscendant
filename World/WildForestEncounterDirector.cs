using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

/// <summary>
/// Activates authored, finite encounters. Terrain generation only supplies
/// intent; this director owns warning time, spawn-once state, and clear hooks.
/// </summary>
public sealed class WildForestEncounterDirector
{
    public const float WarningDurationSeconds = .48f;

    private readonly Dictionary<string, ZoneState> _states = new();
    private readonly List<Enemy> _spawned = new(8);

    public string WarningZoneId { get; private set; } = string.Empty;
    public string ActiveZoneId { get; private set; } = string.Empty;
    public bool WarCampRewardUnlocked => IsCleared("war-camp-chief");
    public bool MotherSpiderDefeated => IsCleared("mothers-nest");

    public void Reset(DungeonMap map)
    {
        _states.Clear();
        WarningZoneId = string.Empty;
        ActiveZoneId = string.Empty;
        if (map == null || (!map.IsAuthoredWildForest && !map.IsAncientCatacombs))
            return;
        foreach (EncounterZone zone in map.EncounterZones)
            _states[zone.ZoneId] = new ZoneState(zone);
    }

    public void Update(
        GameTime gameTime,
        Vector2 playerPosition,
        DungeonMap map,
        EnemyManager enemies,
        int enemyLevel,
        int worldTier)
    {
        if (map == null || (!map.IsAuthoredWildForest && !map.IsAncientCatacombs))
            return;

        WarningZoneId = string.Empty;
        ActiveZoneId = string.Empty;
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);

        foreach (ZoneState state in _states.Values)
        {
            if (state.Spawned && !state.Cleared)
            {
                state.Cleared = AreAllDefeated(state.Enemies);
                if (!state.Cleared)
                    ActiveZoneId = state.Zone.ZoneId;
            }

            if ((state.Zone.IsBossZone && map.IsAuthoredWildForest) || state.Spawned ||
                !state.Zone.ActivationBounds.Contains(playerPosition.ToPoint()))
                continue;

            if (!state.WarningStarted)
            {
                state.WarningStarted = true;
                state.WarningRemaining = WarningDurationSeconds;
            }

            state.WarningRemaining = MathF.Max(0f, state.WarningRemaining - elapsed);
            WarningZoneId = state.Zone.ZoneId;
            if (state.WarningRemaining > 0f)
                continue;

            SpawnZone(
                state,
                playerPosition,
                map,
                enemies,
                enemyLevel,
                worldTier);
            WarningZoneId = string.Empty;
            ActiveZoneId = state.Zone.ZoneId;
        }
    }

    public bool IsTriggered(string zoneId) =>
        _states.TryGetValue(zoneId, out ZoneState state) && state.Spawned;

    public bool IsCleared(string zoneId) =>
        _states.TryGetValue(zoneId, out ZoneState state) && state.Cleared;

    public bool ResetZone(string zoneId, EnemyManager enemies)
    {
        if (!_states.TryGetValue(zoneId ?? string.Empty, out ZoneState state))
            return false;

        enemies?.RemoveForDebug(state.Enemies);
        state.Enemies.Clear();
        state.WarningStarted = false;
        state.WarningRemaining = 0f;
        state.Spawned = false;
        state.Cleared = false;
        if (WarningZoneId == zoneId)
            WarningZoneId = string.Empty;
        if (ActiveZoneId == zoneId)
            ActiveZoneId = string.Empty;
        return true;
    }

    public void EnforceArenaBounds()
    {
        foreach (ZoneState state in _states.Values)
        {
            if ((!state.Zone.IsEliteZone && !state.Zone.IsBossZone) || !state.Spawned || state.Cleared)
                continue;
            foreach (Enemy enemy in state.Enemies)
                enemy.ConstrainToAuthoredArena(state.Zone.ActivationBounds);
        }
    }

    private void SpawnZone(
        ZoneState state,
        Vector2 playerPosition,
        DungeonMap map,
        EnemyManager enemies,
        int enemyLevel,
        int worldTier)
    {
        _spawned.Clear();
        float minimumDistance = GetMinimumPlayerDistance(
            map, state.Zone.SectionId);
        float minimumDistanceSquared = minimumDistance * minimumDistance;
        foreach (SpawnSocket socket in map.SpawnSockets)
        {
            if (socket.ZoneId != state.Zone.ZoneId ||
                _spawned.Count >= state.Zone.MaxConcurrentEnemies)
                continue;
            if (Vector2.DistanceSquared(playerPosition, socket.Position) <
                minimumDistanceSquared)
                continue;
            EnemyType? type = SelectSocketType(socket);
            if (!type.HasValue)
                continue;
            DungeonRoom room = map.FindRoomContaining(socket.Position);
            Enemy enemy = enemies.SpawnAuthoredEnemy(
                type.Value, socket, room, map, enemyLevel, worldTier);
            if (enemy != null)
                _spawned.Add(enemy);
        }
        if (_spawned.Count == 0)
        {
            state.WarningRemaining = .15f;
            return;
        }
        state.Spawned = true;
        state.Enemies.AddRange(_spawned);
    }

    private static float GetMinimumPlayerDistance(
        DungeonMap map,
        string sectionId)
    {
        foreach (WildForestSpawnProfile profile in map.SpawnProfiles)
            if (profile.SectionId == sectionId)
                return profile.MinimumPlayerDistance;
        return 250f;
    }

    private static EnemyType? SelectSocketType(SpawnSocket socket)
    {
        foreach (EnemyType type in socket.SuggestedEnemyTypes)
            if (type != EnemyType.Spiderling)
                return type;
        return null;
    }

    private static bool AreAllDefeated(IReadOnlyList<Enemy> enemies)
    {
        if (enemies.Count == 0)
            return true;
        foreach (Enemy enemy in enemies)
            if (enemy.IsAlive)
                return false;
        return true;
    }

    private sealed class ZoneState
    {
        public EncounterZone Zone { get; }
        public List<Enemy> Enemies { get; } = new();
        public bool WarningStarted { get; set; }
        public float WarningRemaining { get; set; }
        public bool Spawned { get; set; }
        public bool Cleared { get; set; }

        public ZoneState(EncounterZone zone)
        {
            Zone = zone;
        }
    }
}
