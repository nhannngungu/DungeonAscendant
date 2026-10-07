using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class BloodBat : Enemy
{
    private float _stateTimeRemaining = 1.1f;
    private float _stateDuration = 1.1f;
    private float _flightTime;
    private readonly float _circleDirection;
    private float _lastKnownGroundY = float.NaN;

    public BloodBatState State { get; private set; } = BloodBatState.Circling;
    public float StateProgress => _stateDuration <= 0f
        ? 0f
        : Math.Clamp(
            1f - _stateTimeRemaining / _stateDuration,
            0f,
            1f);
    public float DeathLandingY { get; private set; } = float.NaN;
    public bool IsLowAltitude => State == BloodBatState.Diving;
    public float FlightVisualOffset => IsLowAltitude
        ? 3f
        : 12f + MathF.Sin(_flightTime * 5f) * 3f;
    public override float LootChanceMultiplier => 0.35f;
    public override bool IsFlying => true;
    public override Rectangle MeleeTargetBounds => IsLowAltitude
        ? Bounds
        : new Rectangle(
            (int)Position.X - 8,
            (int)(Position.Y - FlightVisualOffset) - 6,
            16,
            12);

    public BloodBat(
        Vector2 position,
        int level,
        bool isElite,
        int roomId,
        int worldTier)
        : base(
            EnemyType.BloodBat,
            position,
            new Vector2(38f, 24f),
            movementSpeed: 190f,
            detectionRange: 285f,
            EnemyStatScaling.Health(45, 9, level, worldTier),
            EnemyStatScaling.Damage(9, 2, level, worldTier),
            experienceReward: 30,
            level,
            worldTier,
            isElite,
            roomId,
            attackRange: 38f,
            attackCooldownSeconds: 0.9f)
    {
        _circleDirection = ((int)(position.X + position.Y) & 1) == 0
            ? 1f
            : -1f;
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _flightTime += elapsedSeconds;
        _stateTimeRemaining = MathF.Max(
            0f,
            _stateTimeRemaining - elapsedSeconds);
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);

        if (room != null)
            _lastKnownGroundY = room.GroundY;

        if (!IsPlayerDetected(player.Position))
            return;

        Vector2 toPlayer = player.Position - Position;
        float distance = MathF.Max(1f, toPlayer.Length());
        Vector2 radial = toPlayer / distance;
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (State == BloodBatState.Circling)
        {
            Vector2 tangent = new(-radial.Y, radial.X);
            tangent *= _circleDirection;
            float correction = distance > 125f ? 0.55f : -0.28f;
            Vector2 direction = tangent + radial * correction;
            direction.Normalize();
            MoveWithinRoom(
                direction * EffectiveMovementSpeed * 0.72f * elapsedSeconds,
                dungeon);

            if (_stateTimeRemaining <= 0f)
                EnterState(BloodBatState.Diving, 0.8f);
            return;
        }

        if (State == BloodBatState.Diving)
        {
            MoveWithinRoom(
                radial * EffectiveMovementSpeed * 1.4f * elapsedSeconds,
                dungeon);

            if (distance <= Attack.Range && TryMeleeAttack(player))
            {
                EnterState(BloodBatState.Retreating, 0.8f);
            }
            else if (_stateTimeRemaining <= 0f)
            {
                EnterState(BloodBatState.Retreating, 0.8f);
            }

            return;
        }

        MoveWithinRoom(
            -radial * EffectiveMovementSpeed * 1.05f * elapsedSeconds,
            dungeon);

        if (_stateTimeRemaining <= 0f)
            EnterState(BloodBatState.Circling, 1.2f);
    }

    private void EnterState(BloodBatState state, float durationSeconds)
    {
        State = state;
        _stateTimeRemaining = durationSeconds;
        _stateDuration = durationSeconds;
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
        {
            DeathLandingY = float.IsNaN(_lastKnownGroundY)
                ? Position.Y + 72f
                : _lastKnownGroundY;
        }
    }
}
