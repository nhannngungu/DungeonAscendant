using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class MotherSpider : Enemy
{
    public const int MaximumActiveSpiderlings = 3;
    public const float SummonCooldownSeconds = 6.5f;

    private float _webShotCooldownRemaining = 0.9f;
    private float _webPatchCooldownRemaining = 2.2f;
    private float _summonCooldownRemaining = 3f;

    public MotherSpider(
        Vector2 position,
        int level,
        int roomId,
        int worldTier)
        : base(
            EnemyType.MotherSpider,
            position,
            new Vector2(72f, 54f),
            movementSpeed: 82f,
            detectionRange: 285f,
            EnemyStatScaling.Health(260, 45, level, worldTier),
            EnemyStatScaling.Damage(16, 3, level, worldTier),
            experienceReward: 220,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: 60f,
            attackCooldownSeconds: 1.2f,
            applyEliteModifiers: false)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _webShotCooldownRemaining = MathF.Max(0f, _webShotCooldownRemaining - elapsedSeconds);
        _webPatchCooldownRemaining = MathF.Max(0f, _webPatchCooldownRemaining - elapsedSeconds);
        _summonCooldownRemaining = MathF.Max(0f, _summonCooldownRemaining - elapsedSeconds);
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
        float distance = MathF.Sqrt(toPlayer.LengthSquared());

        if (_summonCooldownRemaining <= 0f && enemies != null)
        {
            _summonCooldownRemaining = enemies.RequestSpiderlingSummon(this)
                ? SummonCooldownSeconds
                : 1f;
        }

        if (_webShotCooldownRemaining <= 0f)
        {
            projectiles.SpawnWebShot(Position, toPlayer, RoomId);
            _webShotCooldownRemaining = 2.8f;
        }

        if (_webPatchCooldownRemaining <= 0f)
        {
            bool created = projectiles.TryCreateWebPatch(
                player.Position,
                RoomId,
                dungeon);
            _webPatchCooldownRemaining = created ? 5.4f : 1f;
        }

        if (distance <= Attack.Range)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(EffectiveAttackDamage);
        }
        else if (distance < 95f)
        {
            MoveAway(gameTime, player.Position, dungeon);
        }
        else if (distance > 170f)
        {
            MoveToward(gameTime, player.Position, 170f, dungeon);
        }
    }
}
