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
    private const float TargetFacingDeadzone = 2f;
    private const float MovementFacingDeadzone = 0.5f;

    private float _hitFeedbackTimeRemaining;
    private float _buffTimeRemaining;
    private int _damageBuffPercent;
    private int _movementBuffPercent;
    private float _verticalVelocity;
    private float _poiseRecoveryDelayRemaining;
    private float _staggerTimeRemaining;
    private EnemyFacingDirection _attackFacing = EnemyFacingDirection.Right;

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
    public float MaxPoise { get; }
    public float CurrentPoise { get; private set; }
    public bool IsStaggered => _staggerTimeRemaining > 0f;
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
    public bool IsGrounded { get; private set; }
    public virtual bool IsFlying => false;
    public EnemyFacingDirection Facing { get; private set; } =
        EnemyFacingDirection.Right;
    public MeleeAttack Attack { get; }
    public Rectangle AttackArea => CreateAttackArea(_attackFacing);
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
        MaxPoise = GetMaximumPoise(type) * (IsElite ? 1.35f : 1f);
        CurrentPoise = MaxPoise;
        Attack = CreateAttack(type, attackRange, attackCooldownSeconds);
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

        if (player.IsAlive && !IsStaggered)
        {
            if (IsPlayerDetected(player.Position))
                UpdateFacing(player.Position.X - Position.X, TargetFacingDeadzone);

            UpdateBehavior(
                gameTime,
                player,
                dungeon,
                projectiles,
                rootHazards,
                enemies);

            if (Type != EnemyType.GoblinHunter && Attack.IsActive)
                TryResolveMeleeContact(player);
        }

        ApplySideViewPhysics(gameTime, dungeon);
    }

    internal void UpdateBehaviorOnly(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards = null,
        EnemyManager enemies = null)
    {
        if (IsAlive && player.IsAlive && !IsStaggered)
        {
            if (IsPlayerDetected(player.Position))
                UpdateFacing(player.Position.X - Position.X, TargetFacingDeadzone);

            UpdateBehavior(
                gameTime,
                player,
                dungeon,
                projectiles,
                rootHazards,
                enemies);

            if (Type != EnemyType.GoblinHunter && Attack.IsActive)
                TryResolveMeleeContact(player);
            ApplySideViewPhysics(gameTime, dungeon);
        }
    }

    public virtual void UpdateTimers(GameTime gameTime)
    {
        Attack.Update(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool wasStaggered = IsStaggered;
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);
        _buffTimeRemaining = MathF.Max(
            0f,
            _buffTimeRemaining - elapsedSeconds);
        _staggerTimeRemaining = MathF.Max(
            0f,
            _staggerTimeRemaining - elapsedSeconds);
        _poiseRecoveryDelayRemaining = MathF.Max(
            0f,
            _poiseRecoveryDelayRemaining - elapsedSeconds);

        if (wasStaggered && !IsStaggered)
            CurrentPoise = MaxPoise;

        if (!IsStaggered && _poiseRecoveryDelayRemaining <= 0f)
        {
            CurrentPoise = MathF.Min(
                MaxPoise,
                CurrentPoise + MaxPoise * 0.35f * elapsedSeconds);
        }

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
        ReceiveDamage(damage, poiseDamage: 0f);
    }

    public virtual void ReceiveDamage(int damage, float poiseDamage)
    {
        if (!CanBeTargeted || damage <= 0)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _hitFeedbackTimeRemaining = poiseDamage >= 45f
            ? HitFeedbackDurationSeconds * 1.55f
            : HitFeedbackDurationSeconds;
        ApplyPoiseDamage(poiseDamage);
        OnDamaged();
    }

    public void ApplyPoiseDamage(float amount)
    {
        if (!CanBeTargeted || IsStaggered || amount <= 0f)
            return;

        CurrentPoise = MathF.Max(0f, CurrentPoise - amount);
        _poiseRecoveryDelayRemaining = 1.2f;
        _hitFeedbackTimeRemaining = MathF.Max(
            _hitFeedbackTimeRemaining,
            HitFeedbackDurationSeconds);

        if (CurrentPoise > 0f)
            return;

        _staggerTimeRemaining = GetStaggerDuration(Type);
        Attack.Cancel();
    }

    public virtual void ApplyKnockback(
        Vector2 sourcePosition,
        float distance,
        DungeonMap dungeon)
    {
        if (!CanBeTargeted || distance <= 0f)
            return;

        float direction = Position.X < sourcePosition.X ? -1f : 1f;
        MoveBy(new Vector2(direction * distance, 0f), dungeon);
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

    protected bool TryMeleeAttack(PlayerCharacter player)
    {
        if (Attack.IsReady)
        {
            _attackFacing = Facing;
            Attack.StartIfReady();
            return false;
        }

        return TryResolveMeleeContact(player);
    }

    private bool TryResolveMeleeContact(PlayerCharacter player)
    {
        if (!Attack.IsActive)
            return false;

        Rectangle attackArea = AttackArea;
        bool canContact = attackArea.Intersects(player.Bounds) ||
            (player.Combat.IsBlocking &&
             attackArea.Intersects(player.DefenseBounds));

        if (!canContact || !Attack.TryConsumeActiveHit())
            return false;

        player.ReceiveMeleeAttack(
            new AttackContact(
                EffectiveAttackDamage,
                Position,
                Attack.IsBlockable,
                Attack.IsUnblockable,
                attackArea));
        return true;
    }

    private Rectangle CreateAttackArea(EnemyFacingDirection facing)
    {
        int reach = (int)MathF.Ceiling(Attack.Range);
        int height = (int)MathF.Ceiling(MathF.Max(Size.Y * 1.15f, 52f));
        int y = (int)Position.Y - height / 2;

        if (facing == EnemyFacingDirection.Left)
        {
            return new Rectangle(
                Bounds.Left - reach,
                y,
                reach,
                height);
        }

        return new Rectangle(Bounds.Right, y, reach, height);
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
        float horizontalOffset = target.X - Position.X;
        float distance = MathF.Abs(horizontalOffset);

        UpdateFacing(horizontalOffset, TargetFacingDeadzone);

        if (distance <= stoppingRange || distance <= 0f)
        {
            return;
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementDistance = MathF.Min(
            EffectiveMovementSpeed * speedMultiplier * elapsedSeconds,
            distance - stoppingRange);
        MoveBy(new Vector2(MathF.Sign(horizontalOffset) * movementDistance, 0f), dungeon);
    }

    protected void MoveAway(
        GameTime gameTime,
        Vector2 threat,
        DungeonMap dungeon,
        float speedMultiplier = 1f)
    {
        float direction = Position.X < threat.X ? -1f : 1f;

        UpdateFacing(direction, 0f);

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MoveBy(
            new Vector2(
                direction * EffectiveMovementSpeed * speedMultiplier * elapsedSeconds,
                0f),
            dungeon);
    }

    protected void MoveWithinRoom(Vector2 movement, DungeonMap dungeon)
    {
        UpdateFacing(movement.X, MovementFacingDeadzone);

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
        UpdateFacing(movement.X, MovementFacingDeadzone);

        Position = DungeonCollision.ResolveMovement(
            Position,
            Position + movement,
            Size,
            dungeon);
        DungeonRoom room = dungeon.FindRoomContaining(Position);

        if (room != null)
            RoomId = room.Id;
    }

    internal void SnapToGround(DungeonRoom room)
    {
        if (IsFlying)
            return;

        if (room == null)
            return;

        Position = SideScrollingCollision.PlaceOnGround(Position.X, Size, room);
        _verticalVelocity = 0f;
        IsGrounded = true;
    }

    private void ApplySideViewPhysics(GameTime gameTime, DungeonMap dungeon)
    {
        if (IsFlying)
            return;

        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 20f);
        _verticalVelocity = MathF.Min(
            PlayerCharacter.TerminalFallSpeed,
            _verticalVelocity + PlayerCharacter.Gravity * elapsedSeconds);
        MovementResult result = SideScrollingCollision.Resolve(
            Position,
            new Vector2(0f, _verticalVelocity),
            Size,
            elapsedSeconds,
            dungeon);
        Position = result.Position;
        _verticalVelocity = result.Velocity.Y;
        IsGrounded = result.IsGrounded;
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

    private void UpdateFacing(float horizontalAmount, float deadzone)
    {
        if (MathF.Abs(horizontalAmount) <= deadzone)
            return;

        Facing = horizontalAmount < 0f
            ? EnemyFacingDirection.Left
            : EnemyFacingDirection.Right;
    }

    private static float GetMaximumPoise(EnemyType type)
    {
        return type switch
        {
            EnemyType.Spiderling => 22f,
            EnemyType.BloodBat => 34f,
            EnemyType.Goblin => 42f,
            EnemyType.GoblinHunter => 46f,
            EnemyType.DireWolf => 60f,
            EnemyType.ThornCrawler => 66f,
            EnemyType.GiantSpider => 72f,
            EnemyType.GoblinChief => 110f,
            EnemyType.CorruptedTreant => 135f,
            EnemyType.MotherSpider => 165f,
            _ => 60f
        };
    }

    private static MeleeAttack CreateAttack(
        EnemyType type,
        float range,
        float cooldownSeconds)
    {
        return type switch
        {
            EnemyType.DireWolf => new MeleeAttack(
                range, cooldownSeconds, 0.10f, 0.07f, 0.14f),
            EnemyType.BloodBat => new MeleeAttack(
                range, cooldownSeconds, 0.11f, 0.07f, 0.15f),
            EnemyType.Goblin => new MeleeAttack(
                range, cooldownSeconds, 0.48f, 0.17f, 0.40f),
            EnemyType.CorruptedTreant => new MeleeAttack(
                range, cooldownSeconds, 0.38f, 0.13f, 0.38f),
            EnemyType.GoblinChief => new MeleeAttack(
                range, cooldownSeconds, 0.24f, 0.10f, 0.26f),
            EnemyType.MotherSpider => new MeleeAttack(
                range, cooldownSeconds, 0.28f, 0.11f, 0.30f),
            EnemyType.GoblinHunter => new MeleeAttack(
                range, cooldownSeconds, 0.26f, 0.06f, 0.18f,
                isBlockable: false),
            EnemyType.GiantSpider => new MeleeAttack(
                range, cooldownSeconds, 0.18f, 0.09f, 0.22f),
            EnemyType.ThornCrawler => new MeleeAttack(
                range, cooldownSeconds, 0.22f, 0.09f, 0.24f),
            _ => new MeleeAttack(
                range, cooldownSeconds, 0.23f, 0.08f, 0.19f)
        };
    }

    private static float GetStaggerDuration(EnemyType type)
    {
        return type switch
        {
            EnemyType.CorruptedTreant => 0.42f,
            EnemyType.MotherSpider => 0.46f,
            EnemyType.GoblinChief => 0.52f,
            EnemyType.DireWolf => 0.58f,
            EnemyType.GiantSpider => 0.65f,
            EnemyType.Goblin => 0.82f,
            _ => 0.68f
        };
    }
}
