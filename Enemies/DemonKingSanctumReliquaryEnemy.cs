using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum ReliquaryCombatState
{
    Ready,
    SepulchralCleaveWindup, SepulchralCleaveActive, SepulchralCleaveRecovery,
    ReliquaryBurstWindup, ReliquaryBurstActive, ReliquaryBurstRecovery,
    BoneDragWindup, BoneDragActive, BoneDragRecovery,
    OssuaryGuardRaise, OssuaryGuardActive, OssuaryGuardRecovery,
    WraithClawWindup, WraithClawActive, WraithClawRecovery,
    VeilPassTelegraph, VeilPassMove, VeilPassRecovery,
    CloisterGraspCast, CloisterGraspRecovery,
    EchoStrikeCast, EchoStrikeRecovery
}

public enum ReliquaryAttackKind
{
    None, SepulchralCleave, ReliquaryBurst, BoneDrag, OssuaryGuard,
    WraithClaw, VeilPass, CloisterGrasp, EchoStrike
}

/// <summary>
/// Detail-passed Ancient Kings/Cloister pair. The wraith's phase and owned
/// hazards are bounded and finite; the reliquary guard is frontal and timed.
/// </summary>
public sealed class DemonKingSanctumReliquaryEnemy : Enemy
{
    public const float ReliquaryBurstRadius = 68f;
    public const float MaximumVeilPassDistance = 128f;
    public const float GraspTelegraphSeconds = .72f;
    public const float GraspActiveSeconds = .18f;
    public const float EchoDelaySeconds = .68f;
    public const float EchoActiveSeconds = .14f;

    private float _actionRemaining, _actionDuration;
    private float _combatCooldown = .42f, _specialCooldown,
        _guardCooldown, _phaseCooldown;
    private float _lockedDirection = 1f;
    private bool _hitConsumed;
    private int _decisionSerial;
    private float _graspTelegraphRemaining, _graspActiveRemaining;
    private bool _graspHitConsumed;
    private float _echoDelayRemaining, _echoActiveRemaining;
    private bool _echoHitConsumed;
    private Rectangle? _authoredEncounterBounds;

    public ReliquaryCombatState CombatState { get; private set; } =
        ReliquaryCombatState.Ready;
    public ReliquaryAttackKind AttackKind { get; private set; } =
        ReliquaryAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f ? 0f :
        Math.Clamp(1f - _actionRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool IsOssuaryGuarding =>
        CombatState == ReliquaryCombatState.OssuaryGuardActive;
    public float GuardCooldownRemaining => _guardCooldown;
    public bool IsReliquaryBurstActive =>
        CombatState == ReliquaryCombatState.ReliquaryBurstActive;
    public Vector2 VeilPassOrigin { get; private set; }
    public Vector2 VeilPassDestination { get; private set; }
    public float VeilPassDistance => Vector2.Distance(
        VeilPassOrigin, VeilPassDestination);
    public float PhaseCooldownRemaining => _phaseCooldown;
    public bool HasCloisterGrasp =>
        _graspTelegraphRemaining > 0f || _graspActiveRemaining > 0f;
    public bool IsCloisterGraspActive => _graspActiveRemaining > 0f;
    public Vector2 CloisterGraspPosition { get; private set; }
    public float GraspTelegraphRatio => Math.Clamp(
        1f - _graspTelegraphRemaining / GraspTelegraphSeconds, 0f, 1f);
    public bool HasEcho => _echoDelayRemaining > 0f || _echoActiveRemaining > 0f;
    public bool IsEchoActive => _echoActiveRemaining > 0f;
    public Vector2 EchoPosition { get; private set; }
    public bool EchoFacesLeft { get; private set; }
    public int EchoCreatedCount { get; private set; }
    public int EchoActivationCount { get; private set; }
    public float EchoDelayRatio => Math.Clamp(
        1f - _echoDelayRemaining / EchoDelaySeconds, 0f, 1f);

    public override bool IsFlying => Type == EnemyType.CloisterWraith;
    public override bool IsCombatAttackTelegraphing => CombatState is
        ReliquaryCombatState.SepulchralCleaveWindup or
        ReliquaryCombatState.ReliquaryBurstWindup or
        ReliquaryCombatState.BoneDragWindup or
        ReliquaryCombatState.OssuaryGuardRaise or
        ReliquaryCombatState.WraithClawWindup or
        ReliquaryCombatState.VeilPassTelegraph or
        ReliquaryCombatState.CloisterGraspCast or
        ReliquaryCombatState.EchoStrikeCast;
    public override bool IsCombatAttackActive => CombatState is
        ReliquaryCombatState.SepulchralCleaveActive or
        ReliquaryCombatState.ReliquaryBurstActive or
        ReliquaryCombatState.BoneDragActive or
        ReliquaryCombatState.WraithClawActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        ReliquaryCombatState.SepulchralCleaveActive =>
            ForwardArea(100, 80, -72),
        ReliquaryCombatState.ReliquaryBurstActive => new Rectangle(
            (int)(Position.X - ReliquaryBurstRadius),
            (int)(Position.Y - ReliquaryBurstRadius),
            (int)(ReliquaryBurstRadius * 2f),
            (int)(ReliquaryBurstRadius * 2f)),
        ReliquaryCombatState.BoneDragActive => ForwardArea(88, 58, -52),
        ReliquaryCombatState.WraithClawActive => ForwardArea(70, 66, -52),
        _ => Rectangle.Empty
    };

    protected override float DeathPresentationDurationSeconds => .90f;

    public DemonKingSanctumReliquaryEnemy(
        EnemyType type, Vector2 position, int level, int worldTier, int roomId)
        : base(type, position, SizeFor(type), SpeedFor(type),
            type == EnemyType.GraveReliquary ? 650f : 720f,
            EnemyStatScaling.Health(BaseHealth(type),
                type == EnemyType.GraveReliquary ? 29 : 21, level, worldTier),
            EnemyStatScaling.Damage(BaseDamage(type), 2, level, worldTier),
            type == EnemyType.GraveReliquary ? 238 : 218,
            level, worldTier, false, roomId,
            type == EnemyType.GraveReliquary ? 102f : 72f, 0f)
    {
        if (type is not (EnemyType.GraveReliquary or EnemyType.CloisterWraith))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    public override void ReceiveDamageFrom(
        int damage, float poiseDamage, Vector2 sourcePosition)
    {
        bool sourceInFront = _lockedDirection < 0f
            ? sourcePosition.X <= Position.X
            : sourcePosition.X >= Position.X;
        if (IsOssuaryGuarding && sourceInFront)
        {
            damage = Math.Max(1, (int)MathF.Round(damage * .58f));
            poiseDamage *= .42f;
        }
        base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
    }

    protected override void UpdateBehavior(
        GameTime gameTime, PlayerCharacter player, DungeonMap dungeon,
        ProjectileManager projectiles, RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _combatCooldown = MathF.Max(0f, _combatCooldown - dt);
        _specialCooldown = MathF.Max(0f, _specialCooldown - dt);
        _guardCooldown = MathF.Max(0f, _guardCooldown - dt);
        _phaseCooldown = MathF.Max(0f, _phaseCooldown - dt);
        UpdateOwnedEffects(dt, player);

        if (CombatState != ReliquaryCombatState.Ready)
        {
            UpdateAction(dt, player, dungeon);
            return;
        }
        if (Type == EnemyType.GraveReliquary)
            UpdateReliquary(gameTime, player, dungeon);
        else
            UpdateWraith(gameTime, player, dungeon);
    }

    private void UpdateReliquary(
        GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 115f)
        {
            MoveToward(time, player.Position, 94f, map, .65f);
            return;
        }
        if (_combatCooldown > 0f) return;
        Lock(player.Position);
        int choice = _decisionSerial++;
        if (_guardCooldown <= 0f && choice % 6 == 5)
        {
            _guardCooldown = 5.6f;
            Start(ReliquaryCombatState.OssuaryGuardRaise, .30f,
                ReliquaryAttackKind.OssuaryGuard);
        }
        else if (_specialCooldown <= 0f && choice % 5 == 4)
        {
            _specialCooldown = 5.0f;
            Start(ReliquaryCombatState.ReliquaryBurstWindup, .72f,
                ReliquaryAttackKind.ReliquaryBurst);
        }
        else if (distance >= 70f && choice % 3 == 1)
            Start(ReliquaryCombatState.BoneDragWindup, .46f,
                ReliquaryAttackKind.BoneDrag);
        else
            Start(ReliquaryCombatState.SepulchralCleaveWindup, .58f,
                ReliquaryAttackKind.SepulchralCleave);
    }

    private void UpdateWraith(
        GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 330f)
        {
            MoveToward(time, player.Position, 250f, map, .70f);
            return;
        }
        if (_combatCooldown > 0f || HasCloisterGrasp || HasEcho) return;
        Lock(player.Position);
        int choice = _decisionSerial++;
        if (_phaseCooldown <= 0f && distance > 105f && choice % 4 == 3)
        {
            _phaseCooldown = 4.2f;
            PrepareVeilPass(player.Position, map);
            Start(ReliquaryCombatState.VeilPassTelegraph, .40f,
                ReliquaryAttackKind.VeilPass);
        }
        else if (_specialCooldown <= 0f && choice % 5 == 4)
        {
            _specialCooldown = 5.2f;
            CreateEcho(player.Position);
            Start(ReliquaryCombatState.EchoStrikeCast, .38f,
                ReliquaryAttackKind.EchoStrike);
        }
        else if (choice % 3 == 1)
        {
            CloisterGraspPosition = new Vector2(
                ClampEffectX(player.Position.X, 42f), player.Bounds.Bottom);
            Start(ReliquaryCombatState.CloisterGraspCast, .56f,
                ReliquaryAttackKind.CloisterGrasp);
        }
        else if (distance > 78f)
            MoveToward(time, player.Position, 66f, map, .76f);
        else
            Start(ReliquaryCombatState.WraithClawWindup, .36f,
                ReliquaryAttackKind.WraithClaw);
    }

    private void UpdateAction(float dt, PlayerCharacter player, DungeonMap map)
    {
        if (CombatState == ReliquaryCombatState.BoneDragActive)
            MoveBy(new Vector2(_lockedDirection * EffectiveMovementSpeed *
                .92f * dt, 0f), map);
        _actionRemaining -= dt;
        if (_actionRemaining <= 0f) Advance(map);
        if (CombatState is ReliquaryCombatState.SepulchralCleaveActive or
            ReliquaryCombatState.ReliquaryBurstActive or
            ReliquaryCombatState.BoneDragActive or
            ReliquaryCombatState.WraithClawActive)
            ResolveContact(player, map);
    }

    private void Advance(DungeonMap map)
    {
        switch (CombatState)
        {
            case ReliquaryCombatState.SepulchralCleaveWindup:
                Start(ReliquaryCombatState.SepulchralCleaveActive, .18f,
                    ReliquaryAttackKind.SepulchralCleave); break;
            case ReliquaryCombatState.SepulchralCleaveActive:
                Start(ReliquaryCombatState.SepulchralCleaveRecovery, .74f,
                    ReliquaryAttackKind.SepulchralCleave); break;
            case ReliquaryCombatState.SepulchralCleaveRecovery: Finish(.78f); break;
            case ReliquaryCombatState.ReliquaryBurstWindup:
                Start(ReliquaryCombatState.ReliquaryBurstActive, .18f,
                    ReliquaryAttackKind.ReliquaryBurst); break;
            case ReliquaryCombatState.ReliquaryBurstActive:
                Start(ReliquaryCombatState.ReliquaryBurstRecovery, .82f,
                    ReliquaryAttackKind.ReliquaryBurst); break;
            case ReliquaryCombatState.ReliquaryBurstRecovery: Finish(.90f); break;
            case ReliquaryCombatState.BoneDragWindup:
                Start(ReliquaryCombatState.BoneDragActive, .30f,
                    ReliquaryAttackKind.BoneDrag); break;
            case ReliquaryCombatState.BoneDragActive:
                Start(ReliquaryCombatState.BoneDragRecovery, .66f,
                    ReliquaryAttackKind.BoneDrag); break;
            case ReliquaryCombatState.BoneDragRecovery: Finish(.82f); break;
            case ReliquaryCombatState.OssuaryGuardRaise:
                Start(ReliquaryCombatState.OssuaryGuardActive, .78f,
                    ReliquaryAttackKind.OssuaryGuard); break;
            case ReliquaryCombatState.OssuaryGuardActive:
                Start(ReliquaryCombatState.OssuaryGuardRecovery, .44f,
                    ReliquaryAttackKind.OssuaryGuard); break;
            case ReliquaryCombatState.OssuaryGuardRecovery: Finish(.68f); break;
            case ReliquaryCombatState.WraithClawWindup:
                Start(ReliquaryCombatState.WraithClawActive, .13f,
                    ReliquaryAttackKind.WraithClaw); break;
            case ReliquaryCombatState.WraithClawActive:
                Start(ReliquaryCombatState.WraithClawRecovery, .52f,
                    ReliquaryAttackKind.WraithClaw); break;
            case ReliquaryCombatState.WraithClawRecovery: Finish(.64f); break;
            case ReliquaryCombatState.VeilPassTelegraph:
                MoveBy(VeilPassDestination - Position, map);
                VeilPassDestination = Position;
                Start(ReliquaryCombatState.VeilPassMove, .12f,
                    ReliquaryAttackKind.VeilPass); break;
            case ReliquaryCombatState.VeilPassMove:
                Start(ReliquaryCombatState.VeilPassRecovery, .48f,
                    ReliquaryAttackKind.VeilPass); break;
            case ReliquaryCombatState.VeilPassRecovery: Finish(.80f); break;
            case ReliquaryCombatState.CloisterGraspCast:
                _graspTelegraphRemaining = GraspTelegraphSeconds;
                _graspActiveRemaining = 0f;
                _graspHitConsumed = false;
                Start(ReliquaryCombatState.CloisterGraspRecovery, .62f,
                    ReliquaryAttackKind.CloisterGrasp); break;
            case ReliquaryCombatState.CloisterGraspRecovery: Finish(.82f); break;
            case ReliquaryCombatState.EchoStrikeCast:
                Start(ReliquaryCombatState.EchoStrikeRecovery, .58f,
                    ReliquaryAttackKind.EchoStrike); break;
            case ReliquaryCombatState.EchoStrikeRecovery: Finish(.88f); break;
        }
    }

    private void PrepareVeilPass(Vector2 playerPosition, DungeonMap dungeon)
    {
        VeilPassOrigin = Position;
        float direction = playerPosition.X < Position.X ? -1f : 1f;
        float desired = MathF.Min(MaximumVeilPassDistance,
            MathF.Max(72f, MathF.Abs(playerPosition.X - Position.X) - 68f));
        Rectangle bounds = _authoredEncounterBounds ??
            dungeon.FindRoomContaining(Position)?.Bounds ?? dungeon.WorldBounds;
        float margin = Size.X / 2f + 18f;
        float minimumX = bounds.Left + margin;
        float maximumX = bounds.Right - margin;
        float preferredX = MathHelper.Clamp(
            Position.X + direction * desired, minimumX, maximumX);
        float alternateX = MathHelper.Clamp(
            Position.X - direction * desired, minimumX, maximumX);
        if (MathF.Abs(alternateX - Position.X) >
            MathF.Abs(preferredX - Position.X) &&
            MathF.Abs(preferredX - Position.X) < 48f)
        {
            preferredX = alternateX;
        }
        VeilPassDestination = new Vector2(preferredX, Position.Y);
    }

    private float ClampEffectX(float x, float radius)
    {
        if (!_authoredEncounterBounds.HasValue)
            return x;
        Rectangle bounds = _authoredEncounterBounds.Value;
        return MathHelper.Clamp(x, bounds.Left + radius, bounds.Right - radius);
    }

    internal void SetAuthoredEncounterBounds(Rectangle bounds)
    {
        if (bounds != Rectangle.Empty)
            _authoredEncounterBounds = bounds;
    }

    private void CreateEcho(Vector2 playerPosition)
    {
        EchoPosition = Position;
        EchoFacesLeft = playerPosition.X < Position.X;
        _echoDelayRemaining = EchoDelaySeconds;
        _echoActiveRemaining = 0f;
        _echoHitConsumed = false;
        EchoCreatedCount++;
    }

    private void UpdateOwnedEffects(float dt, PlayerCharacter player)
    {
        if (_graspTelegraphRemaining > 0f)
        {
            _graspTelegraphRemaining = MathF.Max(0f,
                _graspTelegraphRemaining - dt);
            if (_graspTelegraphRemaining <= 0f && IsAlive)
                _graspActiveRemaining = GraspActiveSeconds;
        }
        else if (_graspActiveRemaining > 0f)
        {
            _graspActiveRemaining = MathF.Max(0f, _graspActiveRemaining - dt);
            Rectangle area = new((int)CloisterGraspPosition.X - 42,
                (int)CloisterGraspPosition.Y - 72, 84, 76);
            if (ResolveOwnedContact(player, area, .64f,
                ref _graspHitConsumed))
                player.ApplySlow(.76f, 1.0f);
        }

        if (_echoDelayRemaining > 0f)
        {
            _echoDelayRemaining = MathF.Max(0f, _echoDelayRemaining - dt);
            if (_echoDelayRemaining <= 0f && IsAlive)
            {
                _echoActiveRemaining = EchoActiveSeconds;
                EchoActivationCount++;
            }
        }
        else if (_echoActiveRemaining > 0f)
        {
            _echoActiveRemaining = MathF.Max(0f, _echoActiveRemaining - dt);
            Rectangle area = new(
                EchoFacesLeft ? (int)EchoPosition.X - 76 : (int)EchoPosition.X,
                (int)EchoPosition.Y - 48, 76, 70);
            ResolveOwnedContact(player, area, .78f, ref _echoHitConsumed);
        }
    }

    private bool ResolveOwnedContact(
        PlayerCharacter player, Rectangle area, float multiplier,
        ref bool consumed)
    {
        if (consumed || (!area.Intersects(player.BodyHurtbox) &&
            !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))))
            return false;
        consumed = true;
        player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
        return true;
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap map)
    {
        Rectangle area = ActiveAttackArea;
        if (_hitConsumed || area.IsEmpty ||
            (!area.Intersects(player.BodyHurtbox) &&
             !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))))
            return;
        _hitConsumed = true;
        float multiplier = CombatState switch
        {
            ReliquaryCombatState.ReliquaryBurstActive => .72f,
            ReliquaryCombatState.BoneDragActive => .92f,
            ReliquaryCombatState.WraithClawActive => .82f,
            _ => 1.12f
        };
        player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset) => new(
        _lockedDirection < 0f ? Bounds.Left - reach : Bounds.Right,
        (int)Position.Y + yOffset, reach, height);

    private void Lock(Vector2 target)
    {
        FaceToward(target);
        _lockedDirection = target.X < Position.X ? -1f : 1f;
    }

    private void Start(
        ReliquaryCombatState state, float duration, ReliquaryAttackKind kind)
    {
        CombatState = state;
        AttackKind = kind;
        _actionDuration = MathF.Max(.01f, duration);
        _actionRemaining = _actionDuration;
        _hitConsumed = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = ReliquaryCombatState.Ready;
        AttackKind = ReliquaryAttackKind.None;
        _actionRemaining = _actionDuration = 0f;
        _hitConsumed = false;
        _combatCooldown = MathF.Max(_combatCooldown, cooldown);
    }

    private void Cancel(float cooldown)
    {
        Finish(cooldown);
        _graspTelegraphRemaining = _graspActiveRemaining = 0f;
        _echoDelayRemaining = _echoActiveRemaining = 0f;
        _graspHitConsumed = _echoHitConsumed = true;
        Attack.Cancel();
    }

    protected override void OnDamaged()
    {
        if (!IsAlive) Cancel(0f);
    }
    protected override void OnCombatInterrupted() => Cancel(.70f);

    private static Vector2 SizeFor(EnemyType type) =>
        type == EnemyType.GraveReliquary
            ? new Vector2(59f, 96f)
            : new Vector2(47f, 82f);
    private static float SpeedFor(EnemyType type) =>
        type == EnemyType.GraveReliquary ? 72f : 90f;
    private static int BaseHealth(EnemyType type) =>
        type == EnemyType.GraveReliquary ? 278 : 190;
    private static int BaseDamage(EnemyType type) =>
        type == EnemyType.GraveReliquary ? 24 : 21;
}
