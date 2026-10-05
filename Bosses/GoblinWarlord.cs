using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Bosses;

public sealed class GoblinWarlord
{
    private const int BaseGoblinHealth = 100;
    private const int GoblinHealthPerLevel = 20;
    private const int BaseGoblinDamage = 10;
    private const int GoblinDamagePerLevel = 2;
    private const float PhaseOneMovementSpeed = 92f;
    private const float EnragedMovementSpeed = 125f;
    private const float ActivationDistance = 180f;
    private const float HitFeedbackDurationSeconds = 0.18f;

    private float _hitFeedbackTimeRemaining;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; } = new(76f, 88f);
    public int RoomId { get; }
    public int Level { get; }
    public int DungeonDepth { get; }
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public int AttackDamage { get; }
    public int ExperienceReward { get; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsActivated { get; private set; }
    public bool IsEnraged => IsAlive && CurrentHealth * 2 <= MaxHealth;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public MeleeAttack Attack { get; } = new(range: 88f, cooldownSeconds: 1.15f);
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));

    public GoblinWarlord(
        Vector2 position,
        int roomId,
        int playerLevel,
        int dungeonDepth)
    {
        Position = position;
        RoomId = roomId;
        DungeonDepth = Math.Max(1, dungeonDepth);
        Level = DungeonProgression.GetEnemyLevel(playerLevel, DungeonDepth);
        int equivalentGoblinHealth =
            BaseGoblinHealth + (Level - 1) * GoblinHealthPerLevel;
        int equivalentGoblinDamage =
            BaseGoblinDamage + (Level - 1) * GoblinDamagePerLevel;
        MaxHealth = equivalentGoblinHealth * 6;
        CurrentHealth = MaxHealth;
        AttackDamage = (equivalentGoblinDamage * 9 + 2) / 5;
        ExperienceReward = 300 +
            (Math.Max(1, playerLevel) - 1) * 75 +
            (DungeonDepth - 1) * 100;
    }

    public void Update(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        DungeonRoom bossRoom)
    {
        if (!IsAlive)
            return;

        if (!IsActivated)
        {
            IsActivated = IsPlayerNearBossRoom(
                player.Position,
                dungeon,
                bossRoom);

            if (!IsActivated)
                return;
        }

        Attack.Update(gameTime);
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);

        if (!player.IsAlive)
            return;

        Vector2 toPlayer = player.Position - Position;
        float distanceSquared = toPlayer.LengthSquared();

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(AttackDamage);

            return;
        }

        float distance = MathF.Sqrt(distanceSquared);

        if (distance <= 0f)
            return;

        float movementSpeed = IsEnraged
            ? EnragedMovementSpeed
            : PhaseOneMovementSpeed;
        float movementDistance = MathF.Min(
            movementSpeed * elapsedSeconds,
            distance - Attack.Range);
        Vector2 desiredPosition = Position +
            toPlayer / distance * movementDistance;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
    }

    public void ReceiveDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        IsActivated = true;
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

        Vector2 desiredPosition = Position + direction * distance * 0.35f;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
    }

    private static bool IsPlayerNearBossRoom(
        Vector2 playerPosition,
        DungeonMap dungeon,
        DungeonRoom bossRoom)
    {
        DungeonRoom playerRoom = dungeon.FindRoomContaining(playerPosition);

        if (playerRoom != null && playerRoom.Id == bossRoom.Id)
            return true;

        float horizontalDistance = MathF.Max(
            bossRoom.Bounds.Left - playerPosition.X,
            MathF.Max(0f, playerPosition.X - bossRoom.Bounds.Right));
        float verticalDistance = MathF.Max(
            bossRoom.Bounds.Top - playerPosition.Y,
            MathF.Max(0f, playerPosition.Y - bossRoom.Bounds.Bottom));
        return horizontalDistance * horizontalDistance +
            verticalDistance * verticalDistance <=
            ActivationDistance * ActivationDistance;
    }
}
