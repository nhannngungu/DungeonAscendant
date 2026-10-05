using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Balanced Wild Forest melee enemy with Fast and Brute variants.
/// </summary>
public sealed class Goblin : Enemy
{
    private const int BaseMaxHealth = 100;
    private const int HealthIncreasePerLevel = 20;
    private const int BaseAttackDamage = 10;
    private const int DamageIncreasePerLevel = 2;

    public GoblinVariant Variant { get; }

    public Goblin(
        Vector2 position,
        int level = 1,
        GoblinVariant variant = GoblinVariant.Normal,
        bool isElite = false,
        float detectionRange = 200f,
        int roomId = -1,
        int worldTier = 1)
        : base(
            EnemyType.Goblin,
            position,
            GetSize(variant),
            GetMovementSpeed(variant),
            detectionRange,
            GetMaxHealth(level, worldTier, variant),
            GetAttackDamage(level, worldTier, variant),
            GetExperienceReward(variant),
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 50f,
            attackCooldownSeconds: 1f)
    {
        Variant = variant;
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

        float distanceSquared = Vector2.DistanceSquared(
            Position,
            player.Position);

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(EffectiveAttackDamage);

            return;
        }

        MoveToward(gameTime, player.Position, Attack.Range, dungeon);
    }

    public void UpdateMovement(
        GameTime gameTime,
        Vector2 playerPosition,
        float stoppingRange,
        DungeonMap dungeon)
    {
        if (IsAlive && IsPlayerDetected(playerPosition))
            MoveToward(gameTime, playerPosition, stoppingRange, dungeon);
    }

    private static int GetMaxHealth(
        int level,
        int worldTier,
        GoblinVariant variant)
    {
        int health = WorldProgression.ApplyPercent(
            BaseMaxHealth + (Math.Max(1, level) - 1) * HealthIncreasePerLevel,
            WorldProgression.GetHealthMultiplierPercent(worldTier));
        return variant switch
        {
            GoblinVariant.Fast => health * 3 / 4,
            GoblinVariant.Brute => health * 3 / 2,
            _ => health
        };
    }

    private static int GetAttackDamage(
        int level,
        int worldTier,
        GoblinVariant variant)
    {
        int damage = WorldProgression.ApplyPercent(
            BaseAttackDamage + (Math.Max(1, level) - 1) * DamageIncreasePerLevel,
            WorldProgression.GetDamageMultiplierPercent(worldTier));
        return variant switch
        {
            GoblinVariant.Fast => Math.Max(1, damage - 2),
            GoblinVariant.Brute => damage + 5,
            _ => damage
        };
    }

    private static int GetExperienceReward(GoblinVariant variant)
    {
        return variant switch
        {
            GoblinVariant.Fast => 45,
            GoblinVariant.Brute => 75,
            _ => 50
        };
    }

    private static float GetMovementSpeed(GoblinVariant variant)
    {
        return variant switch
        {
            GoblinVariant.Fast => 155f,
            GoblinVariant.Brute => 75f,
            _ => 110f
        };
    }

    private static Vector2 GetSize(GoblinVariant variant)
    {
        return variant switch
        {
            GoblinVariant.Fast => new Vector2(32f, 40f),
            GoblinVariant.Brute => new Vector2(44f, 52f),
            _ => new Vector2(36f, 44f)
        };
    }
}
