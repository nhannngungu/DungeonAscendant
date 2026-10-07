using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class GoblinChief : Enemy
{
    public const int WarCryDamagePercent = 20;
    public const int WarCryMovementPercent = 18;
    public const float WarCryDurationSeconds = 5f;
    public const float WarCryRadius = 190f;
    public const float WarCryCooldownSeconds = 8f;
    public const float WarCryWindupSeconds = 0.32f;
    public const float WarCryActiveSeconds = 0.18f;
    public const float WarCryRecoverySeconds = 0.30f;

    private float _warCryCooldownRemaining = 2.5f;
    private float _warCryStateTimeRemaining;
    private float _warCryStateDuration;

    public GoblinChiefWarCryState WarCryState { get; private set; }
    public bool IsWarCryActive => WarCryState != GoblinChiefWarCryState.Ready;
    public float WarCryProgress => _warCryStateDuration <= 0f
        ? 0f
        : Math.Clamp(
            1f - _warCryStateTimeRemaining / _warCryStateDuration,
            0f,
            1f);

    public GoblinChief(
        Vector2 position,
        int level,
        int roomId,
        int worldTier)
        : base(
            EnemyType.GoblinChief,
            position,
            new Vector2(48f, 58f),
            movementSpeed: 105f,
            detectionRange: 235f,
            EnemyStatScaling.Health(160, 30, level, worldTier),
            EnemyStatScaling.Damage(16, 3, level, worldTier),
            experienceReward: 160,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: 55f,
            attackCooldownSeconds: 1f,
            applyEliteModifiers: false)
    {
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _warCryCooldownRemaining = MathF.Max(
            0f,
            _warCryCooldownRemaining - elapsedSeconds);
        _warCryStateTimeRemaining = MathF.Max(
            0f,
            _warCryStateTimeRemaining - elapsedSeconds);
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

        if (WarCryState != GoblinChiefWarCryState.Ready)
        {
            UpdateWarCry(enemies);
            return;
        }

        if (_warCryCooldownRemaining <= 0f && enemies != null)
        {
            EnterWarCryState(
                GoblinChiefWarCryState.Windup,
                WarCryWindupSeconds);
            _warCryCooldownRemaining = WarCryCooldownSeconds;
            return;
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

    private void UpdateWarCry(EnemyManager enemies)
    {
        if (_warCryStateTimeRemaining > 0f)
            return;

        switch (WarCryState)
        {
            case GoblinChiefWarCryState.Windup:
                BuffNearbyGoblins(enemies);
                EnterWarCryState(
                    GoblinChiefWarCryState.Cry,
                    WarCryActiveSeconds);
                break;
            case GoblinChiefWarCryState.Cry:
                EnterWarCryState(
                    GoblinChiefWarCryState.Recovery,
                    WarCryRecoverySeconds);
                break;
            default:
                EnterWarCryState(GoblinChiefWarCryState.Ready, 0f);
                break;
        }
    }

    private void EnterWarCryState(
        GoblinChiefWarCryState state,
        float durationSeconds)
    {
        WarCryState = state;
        _warCryStateTimeRemaining = durationSeconds;
        _warCryStateDuration = durationSeconds;
    }

    private void BuffNearbyGoblins(EnemyManager enemies)
    {
        float radiusSquared = WarCryRadius * WarCryRadius;

        foreach (Enemy ally in enemies.Enemies)
        {
            if (ReferenceEquals(ally, this) ||
                !ally.IsAlive ||
                ally.RoomId != RoomId ||
                (ally.Type != EnemyType.Goblin &&
                 ally.Type != EnemyType.GoblinHunter) ||
                Vector2.DistanceSquared(Position, ally.Position) > radiusSquared)
            {
                continue;
            }

            ally.ApplyTemporaryBuff(
                WarCryDamagePercent,
                WarCryMovementPercent,
                WarCryDurationSeconds);
        }
    }
}

public enum GoblinChiefWarCryState
{
    Ready,
    Windup,
    Cry,
    Recovery
}
