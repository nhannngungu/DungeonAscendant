using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class DireWolf : Enemy
{
    public const float RetreatHealthThreshold = 0.28f;
    public const float RetreatDurationSeconds = 1.4f;

    private float _retreatTimeRemaining;
    private bool _hasRetreated;

    public bool IsRetreating => _retreatTimeRemaining > 0f;

    public DireWolf(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.DireWolf,
            position,
            new Vector2(52f, 32f),
            movementSpeed: 178f,
            detectionRange: 245f,
            GetScaledStat(80, 16, level, worldTier, health: true),
            GetScaledStat(12, 2, level, worldTier, health: false),
            experienceReward: 60,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 44f,
            attackCooldownSeconds: 0.85f)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);

        if (_retreatTimeRemaining > 0f)
        {
            _retreatTimeRemaining = MathF.Max(
                0f,
                _retreatTimeRemaining -
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (IsRetreating)
        {
            MoveAway(
                gameTime,
                player.Position,
                dungeon,
                speedMultiplier: 1.12f);
            return;
        }

        if (!IsPlayerDetected(player.Position))
            return;

        float distanceSquared = Vector2.DistanceSquared(
            Position,
            player.Position);

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            TryMeleeAttack(player);

            return;
        }

        MoveToward(gameTime, player.Position, Attack.Range, dungeon);
    }

    protected override void OnDamaged()
    {
        if (_hasRetreated || !IsAlive ||
            CurrentHealth > MaxHealth * RetreatHealthThreshold)
        {
            return;
        }

        _hasRetreated = true;
        _retreatTimeRemaining = RetreatDurationSeconds;
        Attack.Cancel();
    }

    private static int GetScaledStat(
        int baseValue,
        int perLevel,
        int level,
        int worldTier,
        bool health)
    {
        int value = baseValue + (Math.Max(1, level) - 1) * perLevel;
        int percent = health
            ? WorldProgression.GetHealthMultiplierPercent(worldTier)
            : WorldProgression.GetDamageMultiplierPercent(worldTier);
        return WorldProgression.ApplyPercent(value, percent);
    }
}
