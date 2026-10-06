using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Bosses;

public sealed class AncientTreant
{
    private const float ActivationDistance = 190f;
    private const float HitFeedbackDurationSeconds = 0.18f;

    private float _hitFeedbackTimeRemaining;
    private float _rootStrikeCooldownRemaining = 1.6f;
    private float _terrainRootCooldownRemaining = 4f;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; } = new(110f, 126f);
    public int RoomId { get; }
    public int Level { get; }
    public int DungeonDepth { get; }
    public int WorldTier { get; }
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public int AttackDamage { get; }
    public int ExperienceReward { get; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsActivated { get; private set; }
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public AncientTreantPhase Phase => CurrentHealth * 100 > MaxHealth * 65
        ? AncientTreantPhase.PhaseOne
        : CurrentHealth * 100 > MaxHealth * 30
            ? AncientTreantPhase.PhaseTwo
            : AncientTreantPhase.PhaseThree;
    public MeleeAttack Attack { get; } = new(
        range: 92f,
        cooldownSeconds: 1.35f);
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));

    public AncientTreant(
        Vector2 position,
        int roomId,
        int playerLevel,
        int dungeonDepth,
        int worldTier)
    {
        Position = position;
        RoomId = roomId;
        DungeonDepth = Math.Max(1, dungeonDepth);
        WorldTier = WorldProgression.ClampWorldTier(worldTier);
        Level = WorldProgression.GetEnemyLevel(
            playerLevel,
            DungeonDepth,
            WorldTier);
        MaxHealth = WorldProgression.ApplyPercent(
            900 + (Level - 1) * 180,
            WorldProgression.GetHealthMultiplierPercent(WorldTier));
        CurrentHealth = MaxHealth;
        AttackDamage = WorldProgression.ApplyPercent(
            28 + (Level - 1) * 4,
            WorldProgression.GetDamageMultiplierPercent(WorldTier));
        ExperienceReward = 650 +
            (Math.Max(1, playerLevel) - 1) * 100 +
            (DungeonDepth - 1) * 150 +
            (WorldTier - 1) * 150;
    }

    public void Update(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        DungeonRoom bossRoom,
        RootHazardManager rootHazards)
    {
        if (!IsAlive)
            return;

        if (!IsActivated)
        {
            IsActivated = IsPlayerNearBossRoom(player.Position, dungeon, bossRoom);

            if (!IsActivated)
                return;
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Attack.Update(gameTime);
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);
        _rootStrikeCooldownRemaining = MathF.Max(
            0f,
            _rootStrikeCooldownRemaining - elapsedSeconds);
        _terrainRootCooldownRemaining = MathF.Max(
            0f,
            _terrainRootCooldownRemaining - elapsedSeconds);

        if (!player.IsAlive)
            return;

        if (_rootStrikeCooldownRemaining <= 0f)
        {
            CreateRootStrike(player.Position, dungeon, rootHazards);
            _rootStrikeCooldownRemaining = Phase switch
            {
                AncientTreantPhase.PhaseThree => 2.4f,
                AncientTreantPhase.PhaseTwo => 3.5f,
                _ => 4.8f
            };
        }

        if (_terrainRootCooldownRemaining <= 0f)
        {
            CreateTerrainRoots(player.Position, dungeon, rootHazards);
            _terrainRootCooldownRemaining = Phase switch
            {
                AncientTreantPhase.PhaseThree => 4.5f,
                AncientTreantPhase.PhaseTwo => 6.5f,
                _ => 8.5f
            };
        }

        Vector2 toPlayer = player.Position - Position;
        float horizontalDistance = MathF.Abs(toPlayer.X);

        if (horizontalDistance <= Attack.Range &&
            MathF.Abs(toPlayer.Y) <= Size.Y)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(AttackDamage);

            return;
        }

        if (horizontalDistance <= 0f)
            return;

        float speed = Phase switch
        {
            AncientTreantPhase.PhaseThree => 72f,
            AncientTreantPhase.PhaseTwo => 58f,
            _ => 45f
        };
        float movementDistance = MathF.Min(
            speed * elapsedSeconds,
            horizontalDistance - Attack.Range);
        Vector2 desiredPosition = Position +
            new Vector2(MathF.Sign(toPlayer.X) * movementDistance, 0f);
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

        Vector2 direction = new(
            Position.X < sourcePosition.X ? -1f : 1f,
            0f);

        Vector2 desiredPosition = Position + direction * distance * 0.15f;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
    }

    private void CreateRootStrike(
        Vector2 playerPosition,
        DungeonMap dungeon,
        RootHazardManager rootHazards)
    {
        int zoneCount = (int)Phase;
        float radius = Phase == AncientTreantPhase.PhaseOne ? 0f : 48f;
        float telegraph = Phase == AncientTreantPhase.PhaseThree ? 0.7f : 0.9f;

        for (int index = 0; index < zoneCount; index++)
        {
            float angle = MathHelper.TwoPi * index / zoneCount;
            Vector2 offset = new(MathF.Cos(angle), MathF.Sin(angle));
            rootHazards.TryAdd(
                playerPosition + offset * radius,
                new Vector2(64f, 64f),
                telegraph,
                activeSeconds: 1.1f,
                damage: Math.Max(1, AttackDamage * 4 / 5),
                slowMultiplier: 0.62f,
                slowDurationSeconds: 0.65f,
                RoomId,
                this,
                isTerrainRoot: false,
                dungeon);
        }
    }

    private void CreateTerrainRoots(
        Vector2 playerPosition,
        DungeonMap dungeon,
        RootHazardManager rootHazards)
    {
        int segmentCount = Phase switch
        {
            AncientTreantPhase.PhaseThree => 4,
            AncientTreantPhase.PhaseTwo => 2,
            _ => 1
        };

        for (int index = 0; index < segmentCount; index++)
        {
            bool horizontal = index % 2 == 0;
            float offset = (index / 2 + 1) * 68f;
            Vector2 position = playerPosition + (horizontal
                ? new Vector2(0f, index == 0 ? offset : -offset)
                : new Vector2(index == 1 ? offset : -offset, 0f));
            rootHazards.TryAdd(
                position,
                horizontal
                    ? new Vector2(94f, 26f)
                    : new Vector2(26f, 94f),
                telegraphSeconds: 1f,
                activeSeconds: 3.5f,
                damage: Math.Max(1, AttackDamage / 2),
                slowMultiplier: 0.55f,
                slowDurationSeconds: 0.45f,
                RoomId,
                this,
                isTerrainRoot: true,
                dungeon);
        }
    }

    private static bool IsPlayerNearBossRoom(
        Vector2 playerPosition,
        DungeonMap dungeon,
        DungeonRoom bossRoom)
    {
        DungeonRoom playerRoom = dungeon.FindRoomContaining(playerPosition);

        if (playerRoom != null && playerRoom.Id == bossRoom.Id)
            return true;

        float horizontal = MathF.Max(
            bossRoom.Bounds.Left - playerPosition.X,
            MathF.Max(0f, playerPosition.X - bossRoom.Bounds.Right));
        float vertical = MathF.Max(
            bossRoom.Bounds.Top - playerPosition.Y,
            MathF.Max(0f, playerPosition.Y - bossRoom.Bounds.Bottom));
        return horizontal * horizontal + vertical * vertical <=
            ActivationDistance * ActivationDistance;
    }
}
