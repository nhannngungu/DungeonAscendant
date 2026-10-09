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
    public const float PhaseTransitionSeconds = 0.78f;
    public const float RootStrikeVisualSeconds = 0.20f;
    public const float RootStrikeRecoverySeconds = 0.48f;
    public const float DeathPresentationSeconds = 1.20f;

    private float _hitFeedbackTimeRemaining;
    private float _rootStrikeCooldownRemaining = 1.6f;
    private float _terrainRootCooldownRemaining = 4f;
    private float _staggerTimeRemaining;
    private float _staggerResistanceTimeRemaining;
    private float _poiseRecoveryDelayRemaining;
    private float _attackDirection = 1f;
    private float _phaseTransitionTimeRemaining;
    private float _rootVisualTimeRemaining;
    private float _rootVisualDuration;
    private float _deathPresentationElapsed;
    private float _ultimateHuntMarkTimeRemaining;
    private int _ultimateHuntShotMask;
    private int _arcaneImprintCount;
    private float _arcaneImprintTimeRemaining;
    private float _arcaneSlowTimeRemaining;
    private float _arcaneSlowMultiplier = 1f;

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
    public float MaxPoise { get; }
    public float CurrentPoise { get; private set; }
    public bool IsStaggered => _staggerTimeRemaining > 0f;
    public int ArcaneImprintCount => _arcaneImprintCount;
    public float ArcaneImprintTimeRemaining => _arcaneImprintTimeRemaining;
    public bool FacesLeft => _attackDirection < 0f;
    public float VisualVelocityX { get; private set; }
    public bool IsPhaseTransitioning => _phaseTransitionTimeRemaining > 0f;
    public float PhaseTransitionProgress => MathHelper.Clamp(
        1f - _phaseTransitionTimeRemaining / PhaseTransitionSeconds,
        0f,
        1f);
    public AncientTreantRootVisualState RootVisualState { get; private set; }
    public float RootVisualProgress => _rootVisualDuration <= 0f
        ? 0f
        : MathHelper.Clamp(
            1f - _rootVisualTimeRemaining / _rootVisualDuration,
            0f,
            1f);
    public float DeathPresentationProgress => MathHelper.Clamp(
        _deathPresentationElapsed / DeathPresentationSeconds,
        0f,
        1f);
    public AncientTreantPhase Phase => CurrentHealth * 100 > MaxHealth * 65
        ? AncientTreantPhase.PhaseOne
        : CurrentHealth * 100 > MaxHealth * 30
            ? AncientTreantPhase.PhaseTwo
            : AncientTreantPhase.PhaseThree;
    public MeleeAttack Attack { get; } = new(
        range: 92f,
        cooldownSeconds: 1.35f,
        windupSeconds: 0.44f,
        activeSeconds: 0.14f,
        recoverySeconds: 0.42f);
    public Rectangle AttackArea => CreateAttackArea();
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));
    public Rectangle WeakPointBounds => new(
        Bounds.Center.X - 18,
        Bounds.Y + 28,
        36,
        30);
    public bool IsUltimateHuntMarked =>
        _ultimateHuntMarkTimeRemaining > 0f && _ultimateHuntShotMask != 0;
    public bool HasBothOpeningUltimateShots =>
        IsUltimateHuntMarked && (_ultimateHuntShotMask & 3) == 3;

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
        MaxPoise = 360f + Level * 20f;
        CurrentPoise = MaxPoise;
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
        RootHazardManager rootHazards,
        float? proximityActivationDistance = null)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _arcaneImprintTimeRemaining = MathF.Max(
            0f,
            _arcaneImprintTimeRemaining - elapsedSeconds);
        _arcaneSlowTimeRemaining = MathF.Max(
            0f,
            _arcaneSlowTimeRemaining - elapsedSeconds);
        if (_arcaneImprintTimeRemaining <= 0f)
            _arcaneImprintCount = 0;
        if (_arcaneSlowTimeRemaining <= 0f)
            _arcaneSlowMultiplier = 1f;
        _ultimateHuntMarkTimeRemaining = MathF.Max(
            0f,
            _ultimateHuntMarkTimeRemaining - elapsedSeconds);
        if (_ultimateHuntMarkTimeRemaining <= 0f)
            _ultimateHuntShotMask = 0;

        if (!IsAlive)
        {
            _deathPresentationElapsed = MathF.Min(
                DeathPresentationSeconds,
                _deathPresentationElapsed + elapsedSeconds);
            return;
        }

        VisualVelocityX = 0f;
        float startingX = Position.X;

        if (proximityActivationDistance.HasValue &&
            Vector2.DistanceSquared(player.Position, Position) >
                proximityActivationDistance.Value *
                proximityActivationDistance.Value)
        {
            return;
        }

        if (!IsActivated)
        {
            IsActivated = proximityActivationDistance.HasValue ||
                IsPlayerNearBossRoom(player.Position, dungeon, bossRoom);

            if (!IsActivated)
                return;
        }

        bool wasStaggered = IsStaggered;
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
        _staggerTimeRemaining = MathF.Max(
            0f,
            _staggerTimeRemaining - elapsedSeconds);
        _staggerResistanceTimeRemaining = MathF.Max(
            0f,
            _staggerResistanceTimeRemaining - elapsedSeconds);
        _poiseRecoveryDelayRemaining = MathF.Max(
            0f,
            _poiseRecoveryDelayRemaining - elapsedSeconds);
        _phaseTransitionTimeRemaining = MathF.Max(
            0f,
            _phaseTransitionTimeRemaining - elapsedSeconds);
        UpdateRootVisualState(elapsedSeconds);

        if (wasStaggered && !IsStaggered)
            CurrentPoise = MaxPoise;

        if (!IsStaggered && _poiseRecoveryDelayRemaining <= 0f)
        {
            CurrentPoise = MathF.Min(
                MaxPoise,
                CurrentPoise + MaxPoise * 0.28f * elapsedSeconds);
        }

        if (!player.IsAlive || IsStaggered || IsPhaseTransitioning)
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

        if (MathF.Abs(toPlayer.X) > 1f)
            _attackDirection = toPlayer.X < 0f ? -1f : 1f;

        if (Attack.IsActive)
            TryResolveMeleeContact(player);

        if (horizontalDistance <= Attack.Range &&
            MathF.Abs(toPlayer.Y) <= Size.Y)
        {
            if (Attack.IsReady)
            {
                _attackDirection = toPlayer.X < 0f ? -1f : 1f;
                Attack.StartIfReady();
            }
            else if (Attack.IsActive)
                TryResolveMeleeContact(player);

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
            speed * _arcaneSlowMultiplier * elapsedSeconds,
            horizontalDistance - Attack.Range);
        Vector2 desiredPosition = Position +
            new Vector2(MathF.Sign(toPlayer.X) * movementDistance, 0f);
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
        VisualVelocityX = elapsedSeconds > 0f
            ? (Position.X - startingX) / elapsedSeconds
            : 0f;
    }

    public void ReceiveDamage(int damage)
    {
        ReceiveDamage(damage, poiseDamage: 0f);
    }

    public void ReceiveDamage(int damage, float poiseDamage)
    {
        if (!IsAlive || damage <= 0)
            return;

        AncientTreantPhase previousPhase = Phase;
        IsActivated = true;
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;
        ApplyPoiseDamage(poiseDamage);

        if (!IsAlive)
        {
            Attack.Cancel();
            _arcaneImprintCount = 0;
            _arcaneImprintTimeRemaining = 0f;
            _arcaneSlowTimeRemaining = 0f;
            _arcaneSlowMultiplier = 1f;
            VisualVelocityX = 0f;
            _phaseTransitionTimeRemaining = 0f;
            RootVisualState = AncientTreantRootVisualState.Ready;
            _rootVisualTimeRemaining = 0f;
            return;
        }

        if (Phase != previousPhase)
        {
            _phaseTransitionTimeRemaining = PhaseTransitionSeconds;
            Attack.Cancel();
        }
    }

    public void ApplyPoiseDamage(float amount)
    {
        if (!IsAlive || IsStaggered ||
            _staggerResistanceTimeRemaining > 0f || amount <= 0f)
        {
            return;
        }

        CurrentPoise = MathF.Max(0f, CurrentPoise - amount);
        _poiseRecoveryDelayRemaining = 1.4f;
        _hitFeedbackTimeRemaining = MathF.Max(
            _hitFeedbackTimeRemaining,
            HitFeedbackDurationSeconds);

        if (CurrentPoise > 0f)
            return;

        _staggerTimeRemaining = 0.55f;
        _staggerResistanceTimeRemaining = 2.5f;
        Attack.Cancel();
    }

    public int AddArcaneImprint()
    {
        if (!IsAlive)
            return _arcaneImprintCount;
        _arcaneImprintCount = Math.Min(
            SpellbladeTuning.MaximumImprints,
            _arcaneImprintCount + 1);
        _arcaneImprintTimeRemaining = SpellbladeTuning.ImprintLifetimeSeconds;
        return _arcaneImprintCount;
    }

    public bool RefreshArcaneImprints()
    {
        if (!IsAlive || _arcaneImprintCount <= 0)
            return false;
        _arcaneImprintTimeRemaining = MathF.Min(
            SpellbladeTuning.MaximumRefreshedLifetimeSeconds,
            _arcaneImprintTimeRemaining +
                SpellbladeTuning.ImprintRefreshSeconds);
        return true;
    }

    public int ConsumeArcaneImprints()
    {
        int count = _arcaneImprintCount;
        _arcaneImprintCount = 0;
        _arcaneImprintTimeRemaining = 0f;
        return count;
    }

    public void ApplyArcaneSlow(float multiplier, float durationSeconds)
    {
        if (!IsAlive || durationSeconds <= 0f)
            return;
        _arcaneSlowMultiplier = MathF.Min(
            _arcaneSlowMultiplier,
            Math.Clamp(multiplier, .55f, 1f));
        _arcaneSlowTimeRemaining = MathF.Max(
            _arcaneSlowTimeRemaining,
            durationSeconds);
    }

    public void RegisterUltimateHuntShot(int shotIndex)
    {
        if (!IsAlive || shotIndex < 0 || shotIndex > 1)
            return;

        _ultimateHuntShotMask |= 1 << shotIndex;
        _ultimateHuntMarkTimeRemaining = 8f;
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
        bool added = false;

        for (int index = 0; index < zoneCount; index++)
        {
            float angle = MathHelper.TwoPi * index / zoneCount;
            Vector2 offset = new(MathF.Cos(angle), MathF.Sin(angle));
            added |= rootHazards.TryAdd(
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

        if (added)
        {
            EnterRootVisualState(
                AncientTreantRootVisualState.Windup,
                telegraph);
        }
    }

    private void UpdateRootVisualState(float elapsedSeconds)
    {
        if (RootVisualState == AncientTreantRootVisualState.Ready)
            return;

        _rootVisualTimeRemaining -= elapsedSeconds;

        if (_rootVisualTimeRemaining > 0f)
            return;

        switch (RootVisualState)
        {
            case AncientTreantRootVisualState.Windup:
                EnterRootVisualState(
                    AncientTreantRootVisualState.Strike,
                    RootStrikeVisualSeconds);
                break;
            case AncientTreantRootVisualState.Strike:
                EnterRootVisualState(
                    AncientTreantRootVisualState.Recovery,
                    RootStrikeRecoverySeconds);
                break;
            default:
                EnterRootVisualState(AncientTreantRootVisualState.Ready, 0f);
                break;
        }
    }

    private void EnterRootVisualState(
        AncientTreantRootVisualState state,
        float durationSeconds)
    {
        RootVisualState = state;
        _rootVisualTimeRemaining = durationSeconds;
        _rootVisualDuration = durationSeconds;
    }

    private bool TryResolveMeleeContact(PlayerCharacter player)
    {
        Rectangle attackArea = AttackArea;
        bool canContact = attackArea.Intersects(player.Bounds) ||
            (player.Combat.IsBlocking &&
             attackArea.Intersects(player.DefenseBounds));

        if (!canContact || !Attack.TryConsumeActiveHit())
            return false;

        AttackResolution resolution = player.ReceiveMeleeAttack(
            new AttackContact(
                AttackDamage,
                Position,
                Attack.IsBlockable,
                Attack.IsUnblockable,
                attackArea));

        if (resolution == AttackResolution.PerfectGuard)
            ApplyPoiseDamage(24f);
        return true;
    }

    private Rectangle CreateAttackArea()
    {
        int reach = (int)MathF.Ceiling(Attack.Range);
        int height = (int)MathF.Ceiling(Size.Y * 0.9f);
        int y = (int)Position.Y - height / 2;

        if (_attackDirection < 0f)
            return new Rectangle(Bounds.Left - reach, y, reach, height);

        return new Rectangle(Bounds.Right, y, reach, height);
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

public enum AncientTreantRootVisualState
{
    Ready,
    Windup,
    Strike,
    Recovery
}
