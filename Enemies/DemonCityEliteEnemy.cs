using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum DemonCityEliteState
{
    Ready,
    WardenReposition,
    WardenCutWindup,
    WardenCutActive,
    WardenCutRecovery,
    WardenThrustWindup,
    WardenThrustActive,
    WardenThrustRecovery,
    WardenGuardRaise,
    WardenGuardActive,
    WardenGuardLower,
    WardenReprisalPause,
    WardenReprisalActive,
    WardenReprisalRecovery,
    HarrierTakeoff,
    HarrierAirPosition,
    HarrierDiveSetup,
    HarrierDiveCommit,
    HarrierDiveBrake,
    HarrierLand,
    HarrierGroundRecovery,
    HarrierRendWindup,
    HarrierRendFirst,
    HarrierRendSecondWindup,
    HarrierRendSecond,
    HarrierRendRecovery,
    HarrierShearWindup,
    HarrierShearActive,
    HarrierShearRecovery,
    HarrierFeintSetup,
    HarrierFeintRelocate
}

public enum DemonCityEliteAttackKind
{
    None,
    WardenCut,
    VeilThrust,
    VeilGuard,
    WardenReprisal,
    GloomDive,
    TalonRend,
    WingShear,
    SkyFeint
}

/// <summary>
/// Map 6 elite pair. The Warden owns a directional, finite guard/counter
/// loop; the Harrier owns a bounded takeoff/attack/landing flight loop.
/// </summary>
public sealed class DemonCityEliteEnemy : Enemy
{
    public const float CutWindupSeconds = .42f;
    public const float CutActiveSeconds = .14f;
    public const float CutRecoverySeconds = .58f;
    public const float ThrustWindupSeconds = .58f;
    public const float ThrustActiveSeconds = .16f;
    public const float ThrustRecoverySeconds = .70f;
    public const float GuardRaiseSeconds = .24f;
    public const float GuardActiveSeconds = .56f;
    public const float GuardLowerSeconds = .28f;
    public const float GuardCooldownSeconds = 5.2f;
    public const float ReprisalPauseSeconds = .30f;
    public const float ReprisalActiveSeconds = .13f;
    public const float ReprisalRecoverySeconds = .76f;

    public const float TakeoffSeconds = .58f;
    public const float AirPositionSeconds = .90f;
    public const float DiveSetupSeconds = .54f;
    public const float DiveCommitSeconds = .46f;
    public const float DiveBrakeSeconds = .36f;
    public const float LandSeconds = .48f;
    public const float GroundRecoverySeconds = .62f;
    public const float RendWindupSeconds = .30f;
    public const float RendStrikeSeconds = .11f;
    public const float RendRecoverySeconds = .52f;
    public const float ShearWindupSeconds = .40f;
    public const float ShearActiveSeconds = .14f;
    public const float ShearRecoverySeconds = .62f;
    public const float FeintSetupSeconds = .30f;
    public const float FeintRelocateSeconds = .46f;
    public const float MaximumFlightHeight = 178f;
    public const float MaximumFlightOffset = 340f;
    public const float GroundTargetSearchRadius = 112f;
    private const float GroundTargetSearchStep = 16f;

    private float _actionTimeRemaining;
    private float _actionDuration;
    private float _combatCooldownRemaining = .36f;
    private float _guardCooldownRemaining;
    private float _shearCooldownRemaining;
    private float _feintCooldownRemaining;
    private float _guardBlockReactionRemaining;
    private float _lockedDirection = 1f;
    private int _decisionSerial;
    private int _guardDefenseSerial;
    private bool _hitConsumed;
    private bool _secondRendQueued;
    private Vector2 _homeGroundPosition;
    private Vector2 _lastValidGroundPosition;
    private Vector2 _flightTarget;
    private Vector2 _lockedDiveTarget;
    private Vector2 _diveVelocity;
    private DungeonMap _activeDungeon;
    private Rectangle? _authoredArenaBounds;
    private bool _hasValidGroundPosition;
    private float _deathLandingCenterY = float.NaN;

    public DemonCityEliteState CombatState { get; private set; } =
        DemonCityEliteState.Ready;
    public DemonCityEliteAttackKind AttackKind { get; private set; } =
        DemonCityEliteAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool IsVeilGuarding => Type == EnemyType.VeilWarden &&
        CombatState == DemonCityEliteState.WardenGuardActive;
    public bool IsVeilShieldExpanded => Type == EnemyType.VeilWarden &&
        CombatState is DemonCityEliteState.WardenGuardRaise or
            DemonCityEliteState.WardenGuardActive or
            DemonCityEliteState.WardenGuardLower or
            DemonCityEliteState.WardenReprisalPause or
            DemonCityEliteState.WardenReprisalActive;
    public bool IsGuardBlockReacting => _guardBlockReactionRemaining > 0f;
    public bool IsAirborne => Type == EnemyType.GloomwingHarrier &&
        CombatState is DemonCityEliteState.HarrierTakeoff or
            DemonCityEliteState.HarrierAirPosition or
            DemonCityEliteState.HarrierDiveSetup or
            DemonCityEliteState.HarrierDiveCommit or
            DemonCityEliteState.HarrierDiveBrake or
            DemonCityEliteState.HarrierLand or
            DemonCityEliteState.HarrierFeintSetup or
            DemonCityEliteState.HarrierFeintRelocate;
    public float DeathLandingCenterY => _deathLandingCenterY;
    public float DiveAngle => MathF.Atan2(_diveVelocity.Y, _diveVelocity.X);

    public override bool IsFlying => Type == EnemyType.GloomwingHarrier;
    public override bool IsCombatAttackTelegraphing => CombatState is
        DemonCityEliteState.WardenCutWindup or
        DemonCityEliteState.WardenThrustWindup or
        DemonCityEliteState.WardenReprisalPause or
        DemonCityEliteState.HarrierDiveSetup or
        DemonCityEliteState.HarrierRendWindup or
        DemonCityEliteState.HarrierRendSecondWindup or
        DemonCityEliteState.HarrierShearWindup;
    public override bool IsCombatAttackActive => CombatState is
        DemonCityEliteState.WardenCutActive or
        DemonCityEliteState.WardenThrustActive or
        DemonCityEliteState.WardenReprisalActive or
        DemonCityEliteState.HarrierDiveCommit or
        DemonCityEliteState.HarrierRendFirst or
        DemonCityEliteState.HarrierRendSecond or
        DemonCityEliteState.HarrierShearActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        DemonCityEliteState.WardenCutActive => ForwardArea(90, 76, -54),
        DemonCityEliteState.WardenThrustActive => ForwardArea(116, 62, -50),
        DemonCityEliteState.WardenReprisalActive => ForwardArea(105, 74, -54),
        DemonCityEliteState.HarrierDiveCommit => new Rectangle(
            Bounds.Left - 10, Bounds.Bottom - 42, Bounds.Width + 20, 58),
        DemonCityEliteState.HarrierRendFirst or
            DemonCityEliteState.HarrierRendSecond => ForwardArea(78, 60, -48),
        DemonCityEliteState.HarrierShearActive => new Rectangle(
            (int)Position.X - 92, (int)Position.Y - 52, 184, 104),
        _ => Rectangle.Empty
    };

    public DemonCityEliteEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId)
        : base(
            type,
            position,
            type == EnemyType.VeilWarden
                ? new Vector2(50f, 98f)
                : new Vector2(48f, 82f),
            type == EnemyType.VeilWarden ? 90f : 116f,
            type == EnemyType.VeilWarden ? 700f : 760f,
            EnemyStatScaling.Health(
                type == EnemyType.VeilWarden ? 205 : 166,
                type == EnemyType.VeilWarden ? 25 : 21,
                level,
                worldTier),
            EnemyStatScaling.Damage(
                type == EnemyType.VeilWarden ? 18 : 16,
                2,
                level,
                worldTier),
            type == EnemyType.VeilWarden ? 300 : 275,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: type == EnemyType.VeilWarden ? 112f : 94f,
            attackCooldownSeconds: 0f)
    {
        if (type is not (EnemyType.VeilWarden or EnemyType.GloomwingHarrier))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        _homeGroundPosition = position;
        _lastValidGroundPosition = position;
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _guardBlockReactionRemaining = MathF.Max(
            0f, _guardBlockReactionRemaining - elapsed);
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        if (!IsVeilGuarding || damage <= 0 ||
            !IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        int guardedDamage = Math.Max(1, (int)MathF.Ceiling(damage * .24f));
        float guardedPoise = poiseDamage * .22f;
        base.ReceiveDamage(guardedDamage, guardedPoise);
        _guardBlockReactionRemaining = .20f;
        bool counterAvailable = (_guardDefenseSerial++ & 1) == 0;
        if (IsAlive && !IsStaggered && counterAvailable)
        {
            LockDirection(sourcePosition);
            Start(
                DemonCityEliteState.WardenReprisalPause,
                ReprisalPauseSeconds,
                DemonCityEliteAttackKind.WardenReprisal);
        }
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _activeDungeon = dungeon;
        TickCooldowns(elapsed);

        if (Type == EnemyType.VeilWarden)
            UpdateWarden(gameTime, player, dungeon, elapsed);
        else
            UpdateHarrier(gameTime, player, dungeon, elapsed);
    }

    private void TickCooldowns(float elapsed)
    {
        _combatCooldownRemaining = MathF.Max(
            0f, _combatCooldownRemaining - elapsed);
        _guardCooldownRemaining = MathF.Max(
            0f, _guardCooldownRemaining - elapsed);
        _shearCooldownRemaining = MathF.Max(
            0f, _shearCooldownRemaining - elapsed);
        _feintCooldownRemaining = MathF.Max(
            0f, _feintCooldownRemaining - elapsed);
    }

    private void UpdateWarden(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState != DemonCityEliteState.Ready)
        {
            if (CombatState == DemonCityEliteState.WardenReposition)
                MoveLocked(-.52f, elapsed, dungeon);
            else if (CombatState == DemonCityEliteState.WardenThrustActive)
                MoveLocked(.50f, elapsed, dungeon);
            else if (CombatState == DemonCityEliteState.WardenReprisalActive)
                MoveLocked(.34f, elapsed, dungeon);

            _actionTimeRemaining -= elapsed;
            if (IsCombatAttackActive)
                ResolveContact(player, dungeon);
            if (_actionTimeRemaining <= 0f)
                AdvanceWarden();
            return;
        }

        if (!IsPlayerDetected(player.Position))
            return;
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 116f)
        {
            MoveToward(gameTime, player.Position, 94f, dungeon, .82f);
            return;
        }
        if (_combatCooldownRemaining > 0f)
            return;

        LockDirection(player.Position);
        int decision = _decisionSerial++;
        if (_guardCooldownRemaining <= 0f && decision % 4 == 2)
        {
            _guardCooldownRemaining = GuardCooldownSeconds;
            Start(DemonCityEliteState.WardenGuardRaise,
                GuardRaiseSeconds, DemonCityEliteAttackKind.VeilGuard);
        }
        else if (decision % 2 == 1)
        {
            Start(DemonCityEliteState.WardenThrustWindup,
                ThrustWindupSeconds, DemonCityEliteAttackKind.VeilThrust);
        }
        else
        {
            Start(DemonCityEliteState.WardenCutWindup,
                CutWindupSeconds, DemonCityEliteAttackKind.WardenCut);
        }
    }

    private void AdvanceWarden()
    {
        switch (CombatState)
        {
            case DemonCityEliteState.WardenReposition:
                Finish(.38f);
                break;
            case DemonCityEliteState.WardenCutWindup:
                Start(DemonCityEliteState.WardenCutActive,
                    CutActiveSeconds, DemonCityEliteAttackKind.WardenCut);
                break;
            case DemonCityEliteState.WardenCutActive:
                Start(DemonCityEliteState.WardenCutRecovery,
                    CutRecoverySeconds, DemonCityEliteAttackKind.WardenCut);
                break;
            case DemonCityEliteState.WardenCutRecovery:
                BeginWardenReposition();
                break;
            case DemonCityEliteState.WardenThrustWindup:
                Start(DemonCityEliteState.WardenThrustActive,
                    ThrustActiveSeconds, DemonCityEliteAttackKind.VeilThrust);
                break;
            case DemonCityEliteState.WardenThrustActive:
                Start(DemonCityEliteState.WardenThrustRecovery,
                    ThrustRecoverySeconds, DemonCityEliteAttackKind.VeilThrust);
                break;
            case DemonCityEliteState.WardenThrustRecovery:
                BeginWardenReposition();
                break;
            case DemonCityEliteState.WardenGuardRaise:
                Start(DemonCityEliteState.WardenGuardActive,
                    GuardActiveSeconds, DemonCityEliteAttackKind.VeilGuard);
                break;
            case DemonCityEliteState.WardenGuardActive:
                Start(DemonCityEliteState.WardenGuardLower,
                    GuardLowerSeconds, DemonCityEliteAttackKind.VeilGuard);
                break;
            case DemonCityEliteState.WardenGuardLower:
                Finish(.46f);
                break;
            case DemonCityEliteState.WardenReprisalPause:
                Start(DemonCityEliteState.WardenReprisalActive,
                    ReprisalActiveSeconds,
                    DemonCityEliteAttackKind.WardenReprisal);
                break;
            case DemonCityEliteState.WardenReprisalActive:
                Start(DemonCityEliteState.WardenReprisalRecovery,
                    ReprisalRecoverySeconds,
                    DemonCityEliteAttackKind.WardenReprisal);
                break;
            case DemonCityEliteState.WardenReprisalRecovery:
                BeginWardenReposition();
                break;
        }
    }

    private void BeginWardenReposition() => Start(
        DemonCityEliteState.WardenReposition,
        .26f,
        DemonCityEliteAttackKind.None);

    private void UpdateHarrier(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        if (CombatState == DemonCityEliteState.Ready)
        {
            _homeGroundPosition = ResolveGroundPosition(Position.X, dungeon);
            if (!IsPlayerDetected(player.Position))
                return;
            float distance = MathF.Abs(player.Position.X - Position.X);
            if (_combatCooldownRemaining > 0f)
                return;

            LockDirection(player.Position);
            int decision = _decisionSerial++;
            if (distance <= 82f && _shearCooldownRemaining <= 0f &&
                decision % 3 == 1)
            {
                _shearCooldownRemaining = 4.4f;
                Start(DemonCityEliteState.HarrierShearWindup,
                    ShearWindupSeconds, DemonCityEliteAttackKind.WingShear);
            }
            else if (distance <= 94f && decision % 3 == 0)
            {
                _secondRendQueued = (decision & 1) == 0;
                Start(DemonCityEliteState.HarrierRendWindup,
                    RendWindupSeconds, DemonCityEliteAttackKind.TalonRend);
            }
            else
            {
                _flightTarget = CreateAirTarget(player, dungeon, _lockedDirection);
                Start(DemonCityEliteState.HarrierTakeoff,
                    TakeoffSeconds, DemonCityEliteAttackKind.None);
            }
            return;
        }

        switch (CombatState)
        {
            case DemonCityEliteState.HarrierTakeoff:
                MoveFlightToward(_flightTarget, dungeon, elapsed, 1.55f);
                break;
            case DemonCityEliteState.HarrierAirPosition:
                MoveFlightToward(_flightTarget, dungeon, elapsed, 1.25f);
                break;
            case DemonCityEliteState.HarrierDiveCommit:
                MoveBy(_diveVelocity * elapsed, dungeon);
                ResolveContact(player, dungeon);
                break;
            case DemonCityEliteState.HarrierDiveBrake:
                MoveBy(_diveVelocity * elapsed * .36f, dungeon);
                break;
            case DemonCityEliteState.HarrierLand:
                MoveFlightToward(_flightTarget, dungeon, elapsed, 1.35f);
                break;
            case DemonCityEliteState.HarrierFeintRelocate:
                MoveFlightToward(_flightTarget, dungeon, elapsed, 1.22f);
                break;
            case DemonCityEliteState.HarrierRendFirst:
            case DemonCityEliteState.HarrierRendSecond:
            case DemonCityEliteState.HarrierShearActive:
                ResolveContact(player, dungeon);
                break;
        }

        if (IsAirborne)
            ConstrainHarrierFlight(dungeon);

        _actionTimeRemaining -= elapsed;
        if (_actionTimeRemaining <= 0f)
            AdvanceHarrier(player, dungeon);
    }

    private void AdvanceHarrier(PlayerCharacter player, DungeonMap dungeon)
    {
        switch (CombatState)
        {
            case DemonCityEliteState.HarrierTakeoff:
                _flightTarget = CreateAirTarget(player, dungeon, -_lockedDirection);
                Start(DemonCityEliteState.HarrierAirPosition,
                    AirPositionSeconds, DemonCityEliteAttackKind.None);
                break;
            case DemonCityEliteState.HarrierAirPosition:
                if (MathF.Abs(player.Position.X - Position.X) > 410f)
                {
                    BeginLanding();
                }
                else if (_feintCooldownRemaining <= 0f &&
                    _decisionSerial++ % 4 == 2)
                {
                    _feintCooldownRemaining = 6.4f;
                    LockDirection(player.Position);
                    Start(DemonCityEliteState.HarrierFeintSetup,
                        FeintSetupSeconds, DemonCityEliteAttackKind.SkyFeint);
                }
                else
                {
                    LockDive(player.Position);
                    Start(DemonCityEliteState.HarrierDiveSetup,
                        DiveSetupSeconds, DemonCityEliteAttackKind.GloomDive);
                }
                break;
            case DemonCityEliteState.HarrierFeintSetup:
                _flightTarget = CreateAirTarget(player, dungeon, -_lockedDirection);
                Start(DemonCityEliteState.HarrierFeintRelocate,
                    FeintRelocateSeconds, DemonCityEliteAttackKind.SkyFeint);
                break;
            case DemonCityEliteState.HarrierFeintRelocate:
                LockDive(player.Position);
                Start(DemonCityEliteState.HarrierDiveSetup,
                    DiveSetupSeconds, DemonCityEliteAttackKind.GloomDive);
                break;
            case DemonCityEliteState.HarrierDiveSetup:
                BeginDiveCommit();
                break;
            case DemonCityEliteState.HarrierDiveCommit:
                Start(DemonCityEliteState.HarrierDiveBrake,
                    DiveBrakeSeconds, DemonCityEliteAttackKind.GloomDive);
                break;
            case DemonCityEliteState.HarrierDiveBrake:
                BeginLanding();
                break;
            case DemonCityEliteState.HarrierLand:
                Position = _flightTarget;
                RememberGroundPosition(Position);
                Start(DemonCityEliteState.HarrierGroundRecovery,
                    GroundRecoverySeconds, DemonCityEliteAttackKind.None);
                break;
            case DemonCityEliteState.HarrierGroundRecovery:
                Finish(.46f);
                break;
            case DemonCityEliteState.HarrierRendWindup:
                Start(DemonCityEliteState.HarrierRendFirst,
                    RendStrikeSeconds, DemonCityEliteAttackKind.TalonRend);
                break;
            case DemonCityEliteState.HarrierRendFirst:
                if (_secondRendQueued)
                {
                    _secondRendQueued = false;
                    Start(DemonCityEliteState.HarrierRendSecondWindup,
                        .22f, DemonCityEliteAttackKind.TalonRend);
                }
                else
                {
                    Start(DemonCityEliteState.HarrierRendRecovery,
                        RendRecoverySeconds, DemonCityEliteAttackKind.TalonRend);
                }
                break;
            case DemonCityEliteState.HarrierRendSecondWindup:
                Start(DemonCityEliteState.HarrierRendSecond,
                    RendStrikeSeconds, DemonCityEliteAttackKind.TalonRend);
                break;
            case DemonCityEliteState.HarrierRendSecond:
                Start(DemonCityEliteState.HarrierRendRecovery,
                    RendRecoverySeconds, DemonCityEliteAttackKind.TalonRend);
                break;
            case DemonCityEliteState.HarrierRendRecovery:
                Finish(.42f);
                break;
            case DemonCityEliteState.HarrierShearWindup:
                Start(DemonCityEliteState.HarrierShearActive,
                    ShearActiveSeconds, DemonCityEliteAttackKind.WingShear);
                break;
            case DemonCityEliteState.HarrierShearActive:
                Start(DemonCityEliteState.HarrierShearRecovery,
                    ShearRecoverySeconds, DemonCityEliteAttackKind.WingShear);
                break;
            case DemonCityEliteState.HarrierShearRecovery:
                Finish(.48f);
                break;
        }
    }

    private void BeginDiveCommit()
    {
        Vector2 vector = _lockedDiveTarget - Position;
        if (vector.LengthSquared() < 1f)
            vector = new Vector2(_lockedDirection, .65f);
        vector.Normalize();
        if (vector.Y < .28f)
        {
            vector.Y = .28f;
            vector.Normalize();
        }
        _diveVelocity = vector * 390f;
        _lockedDirection = MathF.Abs(_diveVelocity.X) > 1f
            ? MathF.Sign(_diveVelocity.X)
            : _lockedDirection;
        Start(DemonCityEliteState.HarrierDiveCommit,
            DiveCommitSeconds, DemonCityEliteAttackKind.GloomDive);
    }

    private void BeginLanding()
    {
        _flightTarget = ResolveGroundPosition(Position.X, _activeDungeon);
        Start(DemonCityEliteState.HarrierLand,
            LandSeconds, DemonCityEliteAttackKind.None);
    }

    private void LockDive(Vector2 target)
    {
        _lockedDiveTarget = target + new Vector2(0f, 16f);
        LockDirection(_lockedDiveTarget);
    }

    private Vector2 CreateAirTarget(
        PlayerCharacter player,
        DungeonMap dungeon,
        float side)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);
        Rectangle bounds = room?.Bounds ?? dungeon.WorldBounds;
        float halfWidth = Size.X / 2f;
        float minimumX = MathF.Max(
            bounds.Left + halfWidth + 42f,
            _homeGroundPosition.X - MaximumFlightOffset);
        float maximumX = MathF.Min(
            bounds.Right - halfWidth - 42f,
            _homeGroundPosition.X + MaximumFlightOffset);
        float x = MathHelper.Clamp(
            player.Position.X + side * 154f, minimumX, maximumX);
        Vector2 ground = ResolveGroundPosition(x, dungeon);
        float ceiling = bounds.Top + Size.Y / 2f + 34f;
        float y = MathF.Max(ceiling, ground.Y - MaximumFlightHeight);
        return new Vector2(ground.X, y);
    }

    private Vector2 ResolveGroundPosition(float x, DungeonMap dungeon)
    {
        if (dungeon == null)
            return _homeGroundPosition;

        GetHarrierHorizontalRange(dungeon, out float minimumX,
            out float maximumX);
        float desiredX = MathHelper.Clamp(x, minimumX, maximumX);
        int steps = (int)MathF.Ceiling(
            GroundTargetSearchRadius / GroundTargetSearchStep);
        for (int step = 0; step <= steps; step++)
        {
            if (step == 0)
            {
                if (TryResolveGroundAtX(desiredX, dungeon,
                    out Vector2 exactGround))
                {
                    RememberGroundPosition(exactGround);
                    return exactGround;
                }
                continue;
            }

            float offset = MathF.Min(
                GroundTargetSearchRadius,
                step * GroundTargetSearchStep);
            float rightX = MathHelper.Clamp(
                desiredX + offset, minimumX, maximumX);
            if (TryResolveGroundAtX(rightX, dungeon,
                out Vector2 rightGround))
            {
                RememberGroundPosition(rightGround);
                return rightGround;
            }

            float leftX = MathHelper.Clamp(
                desiredX - offset, minimumX, maximumX);
            if (MathF.Abs(leftX - rightX) > .01f &&
                TryResolveGroundAtX(leftX, dungeon,
                    out Vector2 leftGround))
            {
                RememberGroundPosition(leftGround);
                return leftGround;
            }
        }

        if (_hasValidGroundPosition &&
            _homeGroundPosition.X >= minimumX &&
            _homeGroundPosition.X <= maximumX &&
            TryResolveGroundAtX(_homeGroundPosition.X, dungeon,
                out Vector2 homeGround))
        {
            RememberGroundPosition(homeGround);
            return homeGround;
        }

        if (_hasValidGroundPosition &&
            _lastValidGroundPosition.X >= minimumX &&
            _lastValidGroundPosition.X <= maximumX)
            return _lastValidGroundPosition;

        // Authored Harrier sockets are validated safe. This deterministic
        // first-use fallback stays local and avoids treating exceptions as AI.
        float fallbackX = MathHelper.Clamp(Position.X, minimumX, maximumX);
        for (int step = 0; step <= steps; step++)
        {
            float offset = MathF.Min(
                GroundTargetSearchRadius,
                step * GroundTargetSearchStep);
            float leftX = MathHelper.Clamp(
                fallbackX - offset, minimumX, maximumX);
            if (TryResolveGroundAtX(leftX, dungeon,
                out Vector2 fallbackGround))
            {
                RememberGroundPosition(fallbackGround);
                return fallbackGround;
            }
            float rightX = MathHelper.Clamp(
                fallbackX + offset, minimumX, maximumX);
            if (MathF.Abs(rightX - leftX) > .01f &&
                TryResolveGroundAtX(rightX, dungeon,
                    out fallbackGround))
            {
                RememberGroundPosition(fallbackGround);
                return fallbackGround;
            }
        }

        return new Vector2(
            MathHelper.Clamp(_homeGroundPosition.X, minimumX, maximumX),
            _homeGroundPosition.Y);
    }

    private bool TryResolveGroundAtX(
        float x,
        DungeonMap dungeon,
        out Vector2 ground)
    {
        ground = Vector2.Zero;
        float halfWidth = Size.X / 2f;
        int bestFloorTop = int.MaxValue;
        foreach (Platform platform in dungeon.Platforms)
        {
            if (x - halfWidth < platform.Bounds.Left ||
                x + halfWidth > platform.Bounds.Right ||
                platform.Bounds.Top >= bestFloorTop)
            {
                continue;
            }

            Vector2 candidate = new(
                x,
                platform.Bounds.Top - Size.Y / 2f);
            if (!SideScrollingCollision.IsPositionFree(
                    candidate, Size, dungeon) ||
                !SideScrollingCollision.IsSupported(
                    candidate, Size, dungeon))
            {
                continue;
            }

            bestFloorTop = platform.Bounds.Top;
            ground = candidate;
        }
        return bestFloorTop != int.MaxValue;
    }

    private void GetHarrierHorizontalRange(
        DungeonMap dungeon,
        out float minimumX,
        out float maximumX)
    {
        DungeonRoom room = dungeon.FindRoomContaining(_homeGroundPosition) ??
            dungeon.FindRoomContaining(Position);
        Rectangle bounds = room?.Bounds ?? dungeon.WorldBounds;
        float halfWidth = Size.X / 2f;
        minimumX = MathF.Max(
            bounds.Left + halfWidth + 18f,
            _homeGroundPosition.X - MaximumFlightOffset);
        maximumX = MathF.Min(
            bounds.Right - halfWidth - 18f,
            _homeGroundPosition.X + MaximumFlightOffset);

        if (_authoredArenaBounds.HasValue)
        {
            Rectangle arena = _authoredArenaBounds.Value;
            minimumX = MathF.Max(
                minimumX, arena.Left + halfWidth + 12f);
            maximumX = MathF.Min(
                maximumX, arena.Right - halfWidth - 12f);
        }
        if (minimumX > maximumX)
            minimumX = maximumX = MathHelper.Clamp(
                _homeGroundPosition.X,
                bounds.Left + halfWidth + 18f,
                bounds.Right - halfWidth - 18f);
    }

    private void RememberGroundPosition(Vector2 ground)
    {
        _lastValidGroundPosition = ground;
        _hasValidGroundPosition = true;
    }

    internal void SetAuthoredArenaBounds(Rectangle bounds)
    {
        if (Type == EnemyType.GloomwingHarrier)
            _authoredArenaBounds = bounds;
    }

    internal Vector2 ResolveHarrierGroundTargetForValidation(
        float x,
        DungeonMap dungeon) => ResolveGroundPosition(x, dungeon);

    private void MoveFlightToward(
        Vector2 target,
        DungeonMap dungeon,
        float elapsed,
        float speedMultiplier)
    {
        Vector2 offset = target - Position;
        float distance = offset.Length();
        if (distance <= .5f)
            return;
        Vector2 movement = offset / distance * MathF.Min(
            distance,
            EffectiveMovementSpeed * speedMultiplier * elapsed);
        MoveBy(movement, dungeon);
    }

    private void ConstrainHarrierFlight(DungeonMap dungeon)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);
        Rectangle bounds = room?.Bounds ?? dungeon.WorldBounds;
        float halfWidth = Size.X / 2f;
        float minimumX = MathF.Max(
            bounds.Left + halfWidth + 18f,
            _homeGroundPosition.X - MaximumFlightOffset);
        float maximumX = MathF.Min(
            bounds.Right - halfWidth - 18f,
            _homeGroundPosition.X + MaximumFlightOffset);
        float groundY = _hasValidGroundPosition
            ? _lastValidGroundPosition.Y
            : Position.Y + Size.Y / 2f;
        float minimumY = MathF.Max(
            bounds.Top + Size.Y / 2f + 24f,
            groundY - MaximumFlightHeight);
        Position = new Vector2(
            MathHelper.Clamp(Position.X, minimumX, maximumX),
            MathHelper.Clamp(Position.Y, minimumY, groundY));
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap dungeon)
    {
        if (_hitConsumed || !ActiveAttackArea.Intersects(player.Bounds))
            return;

        int damage = EffectiveAttackDamage;
        bool blockable = true;
        float push = 0f;
        if (CombatState == DemonCityEliteState.HarrierDiveCommit)
            damage = (damage * 6 + 4) / 5;
        else if (CombatState == DemonCityEliteState.HarrierShearActive)
        {
            damage = Math.Max(1, damage * 3 / 5);
            push = 26f;
        }
        else if (CombatState == DemonCityEliteState.WardenReprisalActive)
            damage = (damage * 5 + 3) / 4;

        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            damage,
            Position,
            blockable,
            unblockable: false,
            ActiveAttackArea));
        if (push > 0f && result is AttackResolution.Damaged or
            AttackResolution.GuardBroken)
        {
            player.PushAwayFrom(Position, push, dungeon);
        }
        if (result == AttackResolution.PerfectGuard)
            ApplyPoiseDamage(MaxPoise * .40f);
        _hitConsumed = true;
    }

    private void MoveLocked(float multiplier, float elapsed, DungeonMap dungeon)
    {
        Vector2 desired = Position + new Vector2(
            _lockedDirection * EffectiveMovementSpeed * multiplier * elapsed,
            0f);
        Vector2 resolved = SideScrollingCollision.ResolveDisplacement(
            Position, desired - Position, Size, dungeon);
        if (SideScrollingCollision.IsSupported(resolved, Size, dungeon))
            MoveBy(resolved - Position, dungeon);
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset)
    {
        int y = (int)Position.Y + yOffset;
        return _lockedDirection < 0f
            ? new Rectangle(Bounds.Left - reach + 14, y, reach, height)
            : new Rectangle(Bounds.Right - 14, y, reach, height);
    }

    private bool IsSourceInFront(Vector2 sourcePosition)
    {
        float offset = sourcePosition.X - Position.X;
        return MathF.Abs(offset) <= 5f || MathF.Sign(offset) == _lockedDirection;
    }

    private void LockDirection(Vector2 target)
    {
        float horizontal = target.X - Position.X;
        if (MathF.Abs(horizontal) > 2f)
            _lockedDirection = MathF.Sign(horizontal);
        FaceToward(target);
    }

    private void Start(
        DemonCityEliteState state,
        float duration,
        DemonCityEliteAttackKind attack)
    {
        CombatState = state;
        AttackKind = attack;
        _actionDuration = duration;
        _actionTimeRemaining = duration;
        _hitConsumed = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = DemonCityEliteState.Ready;
        AttackKind = DemonCityEliteAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _hitConsumed = false;
        _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, cooldown);
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
        {
            if (Type == EnemyType.GloomwingHarrier && IsAirborne)
            {
                _deathLandingCenterY = ResolveGroundPosition(
                    Position.X, _activeDungeon).Y;
            }
            CancelAction();
            return;
        }
        if (IsStaggered)
            CancelAction();
    }

    protected override void OnCombatInterrupted() => CancelAction();

    private void CancelAction()
    {
        CombatState = DemonCityEliteState.Ready;
        AttackKind = DemonCityEliteAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _hitConsumed = false;
        _secondRendQueued = false;
        _diveVelocity = Vector2.Zero;
        _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, .55f);
    }
}
