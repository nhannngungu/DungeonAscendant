using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum DemonicCorruptionState
{
    Stable,
    Tainted,
    Corrupted,
    Overrun
}

public sealed class DemonicCorruptionSystem
{
    public const float Maximum = 100f;
    public const float PassiveDecayPerSecond = 4.5f;
    public const float ShrineDecayPerSecond = 30f;
    public const float OverrunDamageIntervalSeconds = 4f;

    private float _damageClock = OverrunDamageIntervalSeconds;

    public float Value { get; private set; }
    public float Ratio => Value / Maximum;
    public DemonicCorruptionState State => Value < 25f
        ? DemonicCorruptionState.Stable
        : Value < 50f
            ? DemonicCorruptionState.Tainted
            : Value < 75f
                ? DemonicCorruptionState.Corrupted
                : DemonicCorruptionState.Overrun;
    public float HealingMultiplier => State switch
    {
        DemonicCorruptionState.Tainted => .95f,
        DemonicCorruptionState.Corrupted => .88f,
        DemonicCorruptionState.Overrun => .80f,
        _ => 1f
    };
    public float DemonPressureMultiplier => State switch
    {
        DemonicCorruptionState.Tainted => 1.04f,
        DemonicCorruptionState.Corrupted => 1.08f,
        DemonicCorruptionState.Overrun => 1.12f,
        _ => 1f
    };

    public void Reset()
    {
        Value = 0f;
        _damageClock = OverrunDamageIntervalSeconds;
    }

    public void Add(float amount) =>
        Value = MathF.Min(Maximum, Value + MathF.Max(0f, amount));

    public void Reduce(float amount) =>
        Value = MathF.Max(0f, Value - MathF.Max(0f, amount));

    public void Update(
        GameTime gameTime,
        Player.Player player,
        DungeonMap map,
        CorruptionAnchorSystem anchors)
    {
        if (map?.IsDemonLands != true)
        {
            Reset();
            return;
        }

        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        bool purifying = false;
        foreach (PurificationShrineDefinition shrine in map.PurificationShrines)
        {
            if (Vector2.DistanceSquared(player.Position, shrine.Position) <=
                shrine.Radius * shrine.Radius)
            {
                purifying = true;
                break;
            }
        }

        float gain = 0f;
        foreach (CorruptionFieldDefinition field in map.CorruptionFields)
        {
            if (!field.Bounds.Contains(player.Position.ToPoint()) ||
                (field.AnchorId.Length > 0 &&
                 anchors?.IsActive(field.AnchorId) == false))
            {
                continue;
            }
            gain = MathF.Max(gain, field.GainPerSecond);
        }

        if (purifying)
            Reduce(ShrineDecayPerSecond * elapsed);
        else if (gain > 0f)
            Add(gain * elapsed);
        else
            Reduce(PassiveDecayPerSecond * elapsed);

        if (State == DemonicCorruptionState.Overrun)
        {
            _damageClock -= elapsed;
            if (_damageClock <= 0f)
            {
                player.ReceiveDamage(Math.Max(1, player.MaxHealth / 100));
                _damageClock = OverrunDamageIntervalSeconds;
            }
        }
        else
        {
            _damageClock = OverrunDamageIntervalSeconds;
        }
    }
}

public sealed class CorruptionAnchorRuntime
{
    public CorruptionAnchorDefinition Definition { get; }
    public int Health { get; set; }
    public bool ReinforcementSpawned { get; set; }
    public bool IsActive => Health > 0;
    public float HealthRatio => Health / (float)Definition.MaxHealth;
    public float BlessingTimeRemaining { get; set; }
    public DemonLandsEnemy BlessingSource { get; set; }
    public bool IsBlessed => IsActive && BlessingTimeRemaining > 0f;
    public bool IsTemporaryBossAnchor { get; }
    public DemonLandsEnemy BossSource { get; }
    public float LifetimeRemaining { get; set; }
    public float RemovalTimeRemaining { get; set; }

    public CorruptionAnchorRuntime(
        CorruptionAnchorDefinition definition,
        bool isTemporaryBossAnchor = false,
        DemonLandsEnemy bossSource = null,
        float lifetimeSeconds = 0f)
    {
        Definition = definition;
        Health = definition.MaxHealth;
        IsTemporaryBossAnchor = isTemporaryBossAnchor;
        BossSource = bossSource;
        LifetimeRemaining = MathF.Max(0f, lifetimeSeconds);
    }
}

public sealed class CorruptionAnchorSystem
{
    public const float DestroyedFeedbackSeconds = 1.2f;
    public const float LocalCorruptionPerSecond = 3f;
    public const int BuffDamagePercent = 12;
    public const int BuffMovementPercent = 8;
    public const float PriestBlessingDurationSeconds = 5f;
    public const float PriestBlessingCorruptionMultiplier = 1.65f;
    public const float BossAnchorLifetimeSeconds = 12f;
    public const int MaximumTemporaryBossAnchors = 2;

    private readonly Dictionary<string, CorruptionAnchorRuntime> _states = new();
    private int _lastAttackId = -1;
    private int _bossAnchorSerial;

    public IReadOnlyDictionary<string, CorruptionAnchorRuntime> States => _states;
    public string LastDestroyedAnchorId { get; private set; } = string.Empty;
    public float DestroyedFeedbackTimeRemaining { get; private set; }

    public void Reset(DungeonMap map)
    {
        _states.Clear();
        _lastAttackId = -1;
        _bossAnchorSerial = 0;
        LastDestroyedAnchorId = string.Empty;
        DestroyedFeedbackTimeRemaining = 0f;
        if (map?.IsDemonLands != true)
            return;
        foreach (CorruptionAnchorDefinition definition in map.CorruptionAnchors)
            _states[definition.AnchorId] = new CorruptionAnchorRuntime(definition);
    }

    public bool IsActive(string anchorId) =>
        _states.TryGetValue(anchorId ?? string.Empty, out CorruptionAnchorRuntime state) &&
        state.IsActive;

    public bool IsBlessed(string anchorId) =>
        _states.TryGetValue(anchorId ?? string.Empty, out CorruptionAnchorRuntime state) &&
        state.IsBlessed;

    public void ClearPriestBlessings()
    {
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            state.BlessingTimeRemaining = 0f;
            state.BlessingSource = null;
        }
    }

    public int CountActiveTemporaryBossAnchors(DemonLandsEnemy source = null)
    {
        int count = 0;
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            if (state.IsActive && state.IsTemporaryBossAnchor &&
                (source == null || ReferenceEquals(state.BossSource, source)))
            {
                count++;
            }
        }
        return count;
    }

    public bool TrySummonBossAnchor(
        DemonLandsEnemy boss,
        Vector2 requestedPosition,
        DungeonMap map,
        Player.Player player,
        out string anchorId)
    {
        anchorId = string.Empty;
        if (boss?.IsAlive != true || boss.Type != EnemyType.HornedGeneral ||
            map?.IsDemonLands != true ||
            CountActiveTemporaryBossAnchors(boss) >= MaximumTemporaryBossAnchors)
        {
            return false;
        }

        Vector2 anchorSize = new(52f, 82f);
        Rectangle playerExclusion = player.Bounds;
        playerExclusion.Inflate(42, 12);
        for (int index = 0; index < 8; index++)
        {
            float offset = index switch
            {
                0 => 0f,
                1 => -72f,
                2 => 72f,
                3 => -128f,
                4 => 128f,
                5 => -184f,
                6 => 184f,
                _ => boss.Position.X < requestedPosition.X ? -236f : 236f
            };
            float x = requestedPosition.X + offset;
            foreach (Platform platform in map.Platforms)
            {
                if (x - anchorSize.X * .5f < platform.Bounds.Left + 8f ||
                    x + anchorSize.X * .5f > platform.Bounds.Right - 8f)
                {
                    continue;
                }
                Vector2 center = new(
                    x,
                    platform.Bounds.Top - anchorSize.Y * .5f);
                Rectangle bounds = DungeonCollision.CreateBounds(center, anchorSize);
                if (!map.WorldBounds.Contains(bounds) ||
                    bounds.Intersects(playerExclusion) ||
                    !SideScrollingCollision.IsPositionFree(center, anchorSize, map) ||
                    !SideScrollingCollision.IsSupported(center, anchorSize, map))
                {
                    continue;
                }

                bool overlapsAnchor = false;
                foreach (CorruptionAnchorRuntime state in _states.Values)
                {
                    if (state.IsActive && MathF.Abs(
                        state.Definition.Position.X - x) < 96f)
                    {
                        overlapsAnchor = true;
                        break;
                    }
                }
                if (overlapsAnchor)
                    continue;

                DungeonRoom room = map.FindRoomContaining(center);
                if (room == null || room.Id != boss.RoomId)
                    continue;
                anchorId = $"horned-general-anchor-{++_bossAnchorSerial}";
                var definition = new CorruptionAnchorDefinition(
                    anchorId,
                    "infernal-heart",
                    new Vector2(x, platform.Bounds.Top),
                    influenceRadius: 190f,
                    maxHealth: 135,
                    enablesReinforcement: false,
                    isBossAnchor: true);
                _states[anchorId] = new CorruptionAnchorRuntime(
                    definition,
                    isTemporaryBossAnchor: true,
                    bossSource: boss,
                    lifetimeSeconds: BossAnchorLifetimeSeconds);
                return true;
            }
        }
        return false;
    }

    public void ClearBossAnchors(
        DemonLandsEnemy boss = null,
        bool includeAuthoredBossAnchors = false)
    {
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            bool ownedTemporary = state.IsTemporaryBossAnchor &&
                (boss == null || ReferenceEquals(state.BossSource, boss));
            bool authoredBossAnchor = includeAuthoredBossAnchors &&
                state.Definition.IsBossAnchor;
            if (!ownedTemporary && !authoredBossAnchor)
                continue;
            state.Health = 0;
            state.BlessingTimeRemaining = 0f;
            state.BlessingSource = null;
            state.LifetimeRemaining = 0f;
            state.RemovalTimeRemaining = 0f;
        }
    }

    public bool TryBlessNearestActive(
        DemonLandsEnemy priest,
        Vector2 source,
        float maximumDistance,
        out string anchorId,
        out Vector2 anchorPosition)
    {
        anchorId = string.Empty;
        anchorPosition = Vector2.Zero;
        CorruptionAnchorRuntime nearest = null;
        float nearestDistance = maximumDistance * maximumDistance;
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            if (!state.IsActive)
                continue;
            float distance = Vector2.DistanceSquared(
                source,
                state.Definition.Position);
            if (distance >= nearestDistance)
                continue;
            nearest = state;
            nearestDistance = distance;
        }
        if (nearest == null)
            return false;
        if (priest?.IsAlive != true)
            return false;
        nearest.BlessingTimeRemaining = MathF.Max(
            nearest.BlessingTimeRemaining,
            PriestBlessingDurationSeconds);
        nearest.BlessingSource = priest;
        anchorId = nearest.Definition.AnchorId;
        anchorPosition = nearest.Definition.Position;
        return true;
    }

    public void Update(
        GameTime gameTime,
        DungeonMap map,
        Player.Player player,
        EnemyManager enemies,
        DemonicCorruptionSystem corruption,
        int enemyLevel,
        int worldTier)
    {
        DestroyedFeedbackTimeRemaining = MathF.Max(
            0f,
            DestroyedFeedbackTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (map?.IsDemonLands != true)
            return;

        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        string removeFirst = null;
        string removeSecond = null;
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            if (state.IsTemporaryBossAnchor)
            {
                if (state.BossSource?.IsAlive != true)
                {
                    state.Health = 0;
                    state.LifetimeRemaining = 0f;
                }
                if (state.IsActive)
                {
                    state.LifetimeRemaining = MathF.Max(
                        0f,
                        state.LifetimeRemaining - elapsed);
                    if (state.LifetimeRemaining <= 0f)
                    {
                        state.Health = 0;
                        state.RemovalTimeRemaining = .45f;
                    }
                }
                else
                {
                    state.RemovalTimeRemaining = MathF.Max(
                        0f,
                        state.RemovalTimeRemaining - elapsed);
                    if (state.RemovalTimeRemaining <= 0f)
                    {
                        if (removeFirst == null)
                            removeFirst = state.Definition.AnchorId;
                        else
                            removeSecond = state.Definition.AnchorId;
                    }
                }
            }
            if (state.BlessingSource?.IsAlive != true)
            {
                state.BlessingTimeRemaining = 0f;
                state.BlessingSource = null;
            }
            state.BlessingTimeRemaining = MathF.Max(
                0f,
                state.BlessingTimeRemaining - elapsed);
            if (state.BlessingTimeRemaining <= 0f)
                state.BlessingSource = null;
            if (!state.IsActive)
                continue;
            float radiusSquared = state.Definition.InfluenceRadius *
                state.Definition.InfluenceRadius;
            if (Vector2.DistanceSquared(player.Position, state.Definition.Position) <=
                radiusSquared)
            {
                corruption.Add(LocalCorruptionPerSecond * elapsed *
                    (state.IsBlessed
                        ? PriestBlessingCorruptionMultiplier
                        : 1f));
            }
            foreach (Enemy enemy in enemies.Enemies)
            {
                if (enemy.IsAlive &&
                    Vector2.DistanceSquared(enemy.Position, state.Definition.Position) <=
                    radiusSquared)
                {
                    enemy.ApplyTemporaryBuff(
                        state.IsBlessed ? BuffDamagePercent + 5 : BuffDamagePercent,
                        state.IsBlessed ? BuffMovementPercent + 3 : BuffMovementPercent,
                        .25f);
                }
            }
            if (state.Definition.EnablesReinforcement &&
                !state.ReinforcementSpawned &&
                Vector2.DistanceSquared(player.Position, state.Definition.Position) <=
                    radiusSquared * .70f)
            {
                TrySpawnReinforcement(state, map, enemies, enemyLevel, worldTier);
            }
        }
        if (removeFirst != null)
            _states.Remove(removeFirst);
        if (removeSecond != null)
            _states.Remove(removeSecond);
    }

    public bool TryDamage(
        Rectangle attackArea,
        int attackId,
        int damage,
        DemonicCorruptionSystem corruption)
    {
        if (attackId == _lastAttackId || damage <= 0)
            return false;
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            Rectangle bounds = new(
                (int)state.Definition.Position.X - 26,
                (int)state.Definition.Position.Y - 82,
                52,
                82);
            if (!state.IsActive || !attackArea.Intersects(bounds))
                continue;
            _lastAttackId = attackId;
            state.Health = Math.Max(0, state.Health - damage);
            if (!state.IsActive)
            {
                state.BlessingTimeRemaining = 0f;
                state.BlessingSource = null;
                corruption?.Reduce(20f);
                LastDestroyedAnchorId = state.Definition.AnchorId;
                DestroyedFeedbackTimeRemaining = DestroyedFeedbackSeconds;
                if (state.IsTemporaryBossAnchor)
                    state.RemovalTimeRemaining = DestroyedFeedbackSeconds;
            }
            return true;
        }
        return false;
    }

    public void PulseBossAnchor(
        Vector2 bossPosition,
        EnemyManager enemies,
        DemonicCorruptionSystem corruption)
    {
        CorruptionAnchorRuntime nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (CorruptionAnchorRuntime state in _states.Values)
        {
            if (!state.IsActive || !state.Definition.IsBossAnchor)
                continue;
            float distance = Vector2.DistanceSquared(
                bossPosition,
                state.Definition.Position);
            if (distance < nearestDistance)
            {
                nearest = state;
                nearestDistance = distance;
            }
        }
        if (nearest == null)
            return;
        corruption?.Add(7f);
        foreach (Enemy enemy in enemies.Enemies)
            if (enemy.IsAlive && Vector2.DistanceSquared(
                    enemy.Position,
                    nearest.Definition.Position) <=
                nearest.Definition.InfluenceRadius *
                nearest.Definition.InfluenceRadius)
                enemy.ApplyTemporaryBuff(18, 10, 4f);
    }

    private static void TrySpawnReinforcement(
        CorruptionAnchorRuntime state,
        DungeonMap map,
        EnemyManager enemies,
        int enemyLevel,
        int worldTier)
    {
        DungeonRoom room = map.FindRoomContaining(state.Definition.Position);
        if (room == null)
            return;
        var position = state.Definition.Position + new Vector2(-130f, 0f);
        var socket = new SpawnSocket(
            state.Definition.AnchorId + "-reinforcement",
            state.Definition.ZoneId,
            SpawnSocketRole.Minion,
            position,
            new Rectangle((int)position.X - 100, (int)position.Y - 130, 200, 160),
            new[] { EnemyType.ImpRaider },
            false);
        if (enemies.SpawnAuthoredEnemy(
                EnemyType.ImpRaider,
                socket,
                room,
                map,
                enemyLevel,
                worldTier) != null)
        {
            state.ReinforcementSpawned = true;
        }
    }
}
