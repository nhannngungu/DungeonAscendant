using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Shared combat, movement, reward, and temporary-effect state for enemies.
/// Concrete enemies own their behavior and special mechanics.
/// </summary>
public abstract class Enemy
{
    private const float HitFeedbackDurationSeconds = 0.14f;

    private float _hitFeedbackTimeRemaining;
    private float _buffTimeRemaining;
    private int _damageBuffPercent;
    private int _movementBuffPercent;

    public EnemyType Type { get; }
    public Vector2 Position { get; protected set; }
    public Vector2 Size { get; protected set; }
    public float MovementSpeed { get; protected set; }
    public float EffectiveMovementSpeed => MovementSpeed *
        (100 + (IsBuffed ? _movementBuffPercent : 0)) / 100f;
    public float DetectionRange { get; }
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public int Level { get; }
    public int WorldTier { get; }
    public int AttackDamage { get; protected set; }
    public int EffectiveAttackDamage => AttackDamage *
        (100 + (IsBuffed ? _damageBuffPercent : 0)) / 100;
    public int ExperienceReward { get; protected set; }
    public bool IsElite { get; }
    public bool IsBuffed => _buffTimeRemaining > 0f;
    public virtual bool CanBeTargeted => IsAlive;
    public virtual bool CountsForProgression => true;
    public virtual bool CanDropLoot => true;
    public virtual float LootChanceMultiplier => 1f;
    public int RoomId { get; protected set; }
    public MeleeAttack Attack { get; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));
    public virtual Rectangle MeleeTargetBounds => Bounds;

    protected Enemy(
        EnemyType type,
        Vector2 position,
        Vector2 size,
        float movementSpeed,
        float detectionRange,
        int maxHealth,
        int attackDamage,
        int experienceReward,
        int level,
        int worldTier,
        bool isElite,
        int roomId,
        float attackRange,
        float attackCooldownSeconds,
        bool applyEliteModifiers = true)
    {
        Type = type;
        Position = position;
        Size = size;
        MovementSpeed = movementSpeed;
        DetectionRange = detectionRange;
        MaxHealth = Math.Max(1, maxHealth);
        AttackDamage = Math.Max(1, attackDamage);
        ExperienceReward = Math.Max(0, experienceReward);
        Level = Math.Max(1, level);
        WorldTier = Progression.WorldProgression.ClampWorldTier(worldTier);
        IsElite = isElite;
        RoomId = roomId;

        if (IsElite && applyEliteModifiers)
        {
            MaxHealth = (MaxHealth * 7 + 3) / 4;
            AttackDamage = (AttackDamage * 3 + 1) / 2;
            ExperienceReward *= 2;
            Size *= 1.15f;
        }

        CurrentHealth = MaxHealth;
        Attack = new MeleeAttack(attackRange, attackCooldownSeconds);
    }

    public void Update(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards = null,
        EnemyManager enemies = null)
    {
        if (!IsAlive)
            return;

        UpdateTimers(gameTime);

        if (player.IsAlive)
        {
            UpdateBehavior(
                gameTime,
                player,
                dungeon,
                projectiles,
                rootHazards,
                enemies);
        }
    }

    internal void UpdateBehaviorOnly(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards = null,
        EnemyManager enemies = null)
    {
        if (IsAlive && player.IsAlive)
        {
            UpdateBehavior(
                gameTime,
                player,
                dungeon,
                projectiles,
                rootHazards,
                enemies);
        }
    }

    public virtual void UpdateTimers(GameTime gameTime)
    {
        Attack.Update(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);
        _buffTimeRemaining = MathF.Max(
            0f,
            _buffTimeRemaining - elapsedSeconds);

        if (_buffTimeRemaining <= 0f)
        {
            _damageBuffPercent = 0;
            _movementBuffPercent = 0;
        }
    }

    protected abstract void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies);

    public virtual void ReceiveDamage(int damage)
    {
        if (!CanBeTargeted || damage <= 0)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;
        OnDamaged();
    }

    public virtual void ApplyKnockback(
        Vector2 sourcePosition,
        float distance,
        DungeonMap dungeon)
    {
        if (!CanBeTargeted || distance <= 0f)
            return;

        Vector2 direction = Position - sourcePosition;

        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;
        else
            direction.Normalize();

        MoveBy(direction * distance, dungeon);
    }

    public void ApplyTemporaryBuff(
        int damagePercent,
        int movementPercent,
        float durationSeconds)
    {
        if (!IsAlive || durationSeconds <= 0f)
            return;

        _damageBuffPercent = Math.Max(
            _damageBuffPercent,
            Math.Clamp(damagePercent, 0, 100));
        _movementBuffPercent = Math.Max(
            _movementBuffPercent,
            Math.Clamp(movementPercent, 0, 100));
        _buffTimeRemaining = MathF.Max(
            _buffTimeRemaining,
            durationSeconds);
    }

    public void ClearTemporaryBuff()
    {
        _buffTimeRemaining = 0f;
        _damageBuffPercent = 0;
        _movementBuffPercent = 0;
    }

    protected virtual void OnDamaged()
    {
    }

    protected bool IsPlayerDetected(Vector2 playerPosition)
    {
        return Vector2.DistanceSquared(Position, playerPosition) <=
            DetectionRange * DetectionRange;
    }

    protected void MoveToward(
        GameTime gameTime,
        Vector2 target,
        float stoppingRange,
        DungeonMap dungeon,
        float speedMultiplier = 1f)
    {
        Vector2 offset = target - Position;
        float distanceSquared = offset.LengthSquared();

        if (distanceSquared <= stoppingRange * stoppingRange ||
            distanceSquared <= 0f)
        {
            return;
        }

        float distance = MathF.Sqrt(distanceSquared);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementDistance = MathF.Min(
            EffectiveMovementSpeed * speedMultiplier * elapsedSeconds,
            distance - stoppingRange);
        MoveBy(offset / distance * movementDistance, dungeon);
    }

    protected void MoveAway(
        GameTime gameTime,
        Vector2 threat,
        DungeonMap dungeon,
        float speedMultiplier = 1f)
    {
        Vector2 direction = Position - threat;

        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;
        else
            direction.Normalize();

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MoveBy(
            direction * EffectiveMovementSpeed * speedMultiplier * elapsedSeconds,
            dungeon);
    }

    protected void MoveWithinRoom(Vector2 movement, DungeonMap dungeon)
    {
        DungeonRoom room = FindRoomById(dungeon, RoomId);
        Rectangle area = room?.Bounds ?? dungeon.WorldBounds;
        float halfWidth = Size.X / 2f;
        float halfHeight = Size.Y / 2f;
        Vector2 desired = Position + movement;
        Position = new Vector2(
            MathHelper.Clamp(
                desired.X,
                area.Left + halfWidth,
                area.Right - halfWidth),
            MathHelper.Clamp(
                desired.Y,
                area.Top + halfHeight,
                area.Bottom - halfHeight));
    }

    protected void MoveBy(Vector2 movement, DungeonMap dungeon)
    {
        Position = DungeonCollision.ResolveMovement(
            Position,
            Position + movement,
            Size,
            dungeon);
        DungeonRoom room = dungeon.FindRoomContaining(Position);

        if (room != null)
            RoomId = room.Id;
    }

    private static DungeonRoom FindRoomById(DungeonMap dungeon, int roomId)
    {
        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (room.Id == roomId)
                return room;
        }

        return null;
    }
}
