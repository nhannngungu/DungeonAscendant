using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class CorruptedTreant : Enemy
{
    public const float RootStrikeCooldownSeconds = 5f;
    public const float RootTelegraphSeconds = 0.85f;
    public const float RootStrikeVisualSeconds = 0.18f;
    public const float RootRecoveryVisualSeconds = 0.42f;

    private float _rootCooldownRemaining = 1.8f;
    private float _rootVisualTimeRemaining;
    private float _rootVisualDuration;

    public CorruptedTreantRootVisualState RootVisualState { get; private set; }
    public float RootVisualProgress => _rootVisualDuration <= 0f
        ? 0f
        : Math.Clamp(
            1f - _rootVisualTimeRemaining / _rootVisualDuration,
            0f,
            1f);

    public CorruptedTreant(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.CorruptedTreant,
            position,
            new Vector2(64f, 78f),
            movementSpeed: 58f,
            detectionRange: 215f,
            EnemyStatScaling.Health(240, 45, level, worldTier),
            EnemyStatScaling.Damage(24, 4, level, worldTier),
            experienceReward: 120,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 65f,
            attackCooldownSeconds: 1.45f)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        _rootCooldownRemaining = MathF.Max(
            0f,
            _rootCooldownRemaining -
            (float)gameTime.ElapsedGameTime.TotalSeconds);
        UpdateRootVisualState(
            (float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (!IsPlayerDetected(player.Position))
            return;

        if (_rootCooldownRemaining <= 0f && rootHazards != null)
        {
            bool added = rootHazards.TryAdd(
                player.Position,
                new Vector2(62f, 62f),
                RootTelegraphSeconds,
                activeSeconds: 0.9f,
                damage: Math.Max(1, EffectiveAttackDamage * 3 / 4),
                slowMultiplier: 0.72f,
                slowDurationSeconds: 0.55f,
                RoomId,
                this,
                isTerrainRoot: false,
                dungeon);

            if (added)
                EnterRootVisualState(
                    CorruptedTreantRootVisualState.Windup,
                    RootTelegraphSeconds);

            _rootCooldownRemaining = RootStrikeCooldownSeconds;
        }

        float distanceSquared = Vector2.DistanceSquared(
            Position,
            player.Position);

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            TryMeleeAttack(player);
        }
        else
        {
            MoveToward(gameTime, player.Position, Attack.Range, dungeon);
        }
    }

    private void UpdateRootVisualState(float elapsedSeconds)
    {
        if (RootVisualState == CorruptedTreantRootVisualState.Ready)
            return;

        _rootVisualTimeRemaining -= elapsedSeconds;

        if (_rootVisualTimeRemaining > 0f)
            return;

        switch (RootVisualState)
        {
            case CorruptedTreantRootVisualState.Windup:
                EnterRootVisualState(
                    CorruptedTreantRootVisualState.Strike,
                    RootStrikeVisualSeconds);
                break;
            case CorruptedTreantRootVisualState.Strike:
                EnterRootVisualState(
                    CorruptedTreantRootVisualState.Recovery,
                    RootRecoveryVisualSeconds);
                break;
            default:
                EnterRootVisualState(
                    CorruptedTreantRootVisualState.Ready,
                    0f);
                break;
        }
    }

    private void EnterRootVisualState(
        CorruptedTreantRootVisualState state,
        float durationSeconds)
    {
        RootVisualState = state;
        _rootVisualTimeRemaining = durationSeconds;
        _rootVisualDuration = durationSeconds;
    }
}

public enum CorruptedTreantRootVisualState
{
    Ready,
    Windup,
    Strike,
    Recovery
}
