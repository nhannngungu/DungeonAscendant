using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class GoblinHunter : Enemy
{
    public const float PreferredMinimumRange = 145f;
    public const float PreferredMaximumRange = 220f;
    public const float ArrowRange = 300f;

    public GoblinHunter(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.GoblinHunter,
            position,
            new Vector2(36f, 46f),
            movementSpeed: 112f,
            detectionRange: 325f,
            GetScaledStat(85, 17, level, worldTier, health: true),
            GetScaledStat(14, 2, level, worldTier, health: false),
            experienceReward: 70,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: ArrowRange,
            attackCooldownSeconds: 1.4f)
    {
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

        Vector2 toPlayer = player.Position - Position;
        float distanceSquared = toPlayer.LengthSquared();
        float distance = MathF.Sqrt(distanceSquared);

        if (distance < PreferredMinimumRange)
        {
            MoveAway(gameTime, player.Position, dungeon);
        }
        else if (distance > PreferredMaximumRange)
        {
            MoveToward(
                gameTime,
                player.Position,
                PreferredMaximumRange,
                dungeon);
        }

        if (distance <= ArrowRange && Attack.TryStart())
        {
            projectiles.SpawnArrow(
                Position,
                player.Position - Position,
                EffectiveAttackDamage,
                RoomId);
        }
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
