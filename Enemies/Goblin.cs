using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns one Goblin's scaled stats, combat state, and chase behavior.
/// </summary>
public sealed class Goblin
{
    private const int BaseMaxHealth = 100;
    private const int HealthIncreasePerLevel = 20;
    private const int BaseAttackDamage = 10;
    private const int DamageIncreasePerLevel = 2;
    private const float HitFeedbackDurationSeconds = 0.14f;

    private float _hitFeedbackTimeRemaining;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; }
    public float MovementSpeed { get; }
    public float DetectionRange { get; }
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public int Level { get; }
    public int AttackDamage { get; }
    public int ExperienceReward { get; }
    public GoblinVariant Variant { get; }
    public bool IsElite { get; }
    public int RoomId { get; private set; }
    public MeleeAttack Attack { get; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));

    public Goblin(
        Vector2 position,
        int level = 1,
        GoblinVariant variant = GoblinVariant.Normal,
        bool isElite = false,
        float detectionRange = 200f,
        int roomId = -1)
    {
        Position = position;
        DetectionRange = detectionRange;
        Level = Math.Max(1, level);
        Variant = variant;
        IsElite = isElite;
        RoomId = roomId;

        int scaledHealth = BaseMaxHealth + (Level - 1) * HealthIncreasePerLevel;
        int scaledDamage = BaseAttackDamage + (Level - 1) * DamageIncreasePerLevel;

        switch (Variant)
        {
            case GoblinVariant.Fast:
                MaxHealth = scaledHealth * 3 / 4;
                AttackDamage = Math.Max(1, scaledDamage - 2);
                MovementSpeed = 155f;
                ExperienceReward = 45;
                Size = new Vector2(32f, 40f);
                break;
            case GoblinVariant.Brute:
                MaxHealth = scaledHealth * 3 / 2;
                AttackDamage = scaledDamage + 5;
                MovementSpeed = 75f;
                ExperienceReward = 75;
                Size = new Vector2(44f, 52f);
                break;
            default:
                MaxHealth = scaledHealth;
                AttackDamage = scaledDamage;
                MovementSpeed = 110f;
                ExperienceReward = 50;
                Size = new Vector2(36f, 44f);
                break;
        }

        if (IsElite)
        {
            MaxHealth = (MaxHealth * 7 + 3) / 4;
            AttackDamage = (AttackDamage * 3 + 1) / 2;
            ExperienceReward *= 2;
            Size *= 1.15f;
        }

        CurrentHealth = MaxHealth;
        Attack = new MeleeAttack(range: 50f, cooldownSeconds: 1f);
    }

    public void UpdateTimers(GameTime gameTime)
    {
        Attack.Update(gameTime);

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);
    }

    public void UpdateMovement(
        GameTime gameTime,
        Vector2 playerPosition,
        float stoppingRange,
        DungeonMap dungeon)
    {
        if (!IsAlive)
            return;

        Vector2 toPlayer = playerPosition - Position;
        float distanceSquared = toPlayer.LengthSquared();

        if (distanceSquared > DetectionRange * DetectionRange ||
            distanceSquared <= stoppingRange * stoppingRange)
        {
            return;
        }

        float distance = MathF.Sqrt(distanceSquared);
        Vector2 direction = toPlayer / distance;
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementDistance = MathF.Min(
            MovementSpeed * elapsedSeconds,
            distance - stoppingRange);

        Vector2 desiredPosition = Position + direction * movementDistance;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
        UpdateRoom(dungeon);
    }

    public void ReceiveDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;
    }

    public void ApplyKnockback(
        Vector2 sourcePosition,
        float distance,
        DungeonMap dungeon)
    {
        if (!IsAlive || distance <= 0f)
            return;

        Vector2 direction = Position - sourcePosition;

        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;
        else
            direction.Normalize();

        Vector2 desiredPosition = Position + direction * distance;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
        UpdateRoom(dungeon);
    }

    private void UpdateRoom(DungeonMap dungeon)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);

        if (room != null)
            RoomId = room.Id;
    }
}
