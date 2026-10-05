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

    private float _rootCooldownRemaining = 1.8f;

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
            rootHazards.TryAdd(
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
            _rootCooldownRemaining = RootStrikeCooldownSeconds;
        }

        float distanceSquared = Vector2.DistanceSquared(
            Position,
            player.Position);

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(EffectiveAttackDamage);
        }
        else
        {
            MoveToward(gameTime, player.Position, Attack.Range, dungeon);
        }
    }
}
