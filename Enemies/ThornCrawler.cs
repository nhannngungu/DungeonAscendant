using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class ThornCrawler : Enemy
{
    public const float WarningDurationSeconds = 0.65f;

    private float _stateTimeRemaining;
    private float _stateDuration;

    public ThornCrawlerState State { get; private set; } =
        ThornCrawlerState.Hidden;
    public float StateProgress => _stateDuration <= 0f
        ? 0f
        : Math.Clamp(
            1f - _stateTimeRemaining / _stateDuration,
            0f,
            1f);
    public bool IsUnderground =>
        State == ThornCrawlerState.Hidden ||
        State == ThornCrawlerState.TrackingUnderground ||
        State == ThornCrawlerState.Warning ||
        State == ThornCrawlerState.Burrowing;
    public override bool CanBeTargeted => IsAlive && !IsUnderground;
    public override Rectangle MeleeTargetBounds => IsUnderground
        ? Bounds
        : new Rectangle(
            (int)Position.X - 27,
            Bounds.Bottom - 30,
            54,
            30);

    public ThornCrawler(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.ThornCrawler,
            position,
            new Vector2(42f, 44f),
            movementSpeed: 116f,
            detectionRange: 250f,
            EnemyStatScaling.Health(95, 18, level, worldTier),
            EnemyStatScaling.Damage(15, 2, level, worldTier),
            experienceReward: 70,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 46f,
            attackCooldownSeconds: 1.05f)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        _stateTimeRemaining = MathF.Max(
            0f,
            _stateTimeRemaining -
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
        float distanceSquared = Vector2.DistanceSquared(
            Position,
            player.Position);

        switch (State)
        {
            case ThornCrawlerState.Hidden:
                if (IsPlayerDetected(player.Position))
                    EnterState(ThornCrawlerState.TrackingUnderground, 0.9f);
                break;
            case ThornCrawlerState.TrackingUnderground:
                MoveToward(
                    gameTime,
                    player.Position,
                    stoppingRange: 72f,
                    dungeon,
                    speedMultiplier: 1.18f);

                if (_stateTimeRemaining <= 0f || distanceSquared <= 82f * 82f)
                    EnterState(ThornCrawlerState.Warning, WarningDurationSeconds);
                break;
            case ThornCrawlerState.Warning:
                if (_stateTimeRemaining <= 0f)
                    EnterState(ThornCrawlerState.Emerging, 0.35f);
                break;
            case ThornCrawlerState.Emerging:
                if (_stateTimeRemaining <= 0f)
                    EnterState(ThornCrawlerState.Attacking, 1.35f);
                break;
            case ThornCrawlerState.Attacking:
                if (distanceSquared <= Attack.Range * Attack.Range)
                {
                    TryMeleeAttack(player);
                }
                else
                {
                    MoveToward(
                        gameTime,
                        player.Position,
                        Attack.Range,
                        dungeon);
                }

                if (_stateTimeRemaining <= 0f)
                    EnterState(ThornCrawlerState.Recovering, 0.9f);
                break;
            case ThornCrawlerState.Recovering:
                if (_stateTimeRemaining <= 0f)
                    EnterState(ThornCrawlerState.Burrowing, 0.45f);
                break;
            case ThornCrawlerState.Burrowing:
                if (_stateTimeRemaining <= 0f)
                {
                    EnterState(
                        IsPlayerDetected(player.Position)
                            ? ThornCrawlerState.TrackingUnderground
                            : ThornCrawlerState.Hidden,
                        IsPlayerDetected(player.Position) ? 0.9f : 0f);
                }
                break;
        }
    }

    private void EnterState(ThornCrawlerState state, float durationSeconds)
    {
        State = state;
        _stateTimeRemaining = durationSeconds;
        _stateDuration = durationSeconds;
    }
}
