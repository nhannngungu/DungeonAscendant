using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum DemonCityIndustrialState
{
    Ready,
    ServitorReposition,
    PressWindup,
    PressImpact,
    PressRecovery,
    ClampWindup,
    ClampSweep,
    ClampRecovery,
    ShoveWindup,
    ShoveActive,
    ShoveRecovery,
    VentWindup,
    VentActive,
    VentRecovery,
    CantorReposition,
    BindingCast,
    BindingCommit,
    BindingRecovery,
    WarCast,
    WarCommit,
    WarRecovery,
    LanceAim,
    LanceRelease,
    LanceRecovery,
    BreakWindup,
    BreakActive,
    BreakRecovery
}

public enum DemonCityIndustrialAttackKind
{
    None,
    PressHammer,
    ClampSweep,
    IndustrialShove,
    PressureVent,
    BindingSigil,
    WarSigil,
    SigilLance,
    BreakSeal
}

/// <summary>
/// Industrial/Ritual District pair: a restrained heavy laborer and a civic
/// ritual technician. All casts and attacks are finite, interruptible states.
/// </summary>
public sealed class DemonCityIndustrialEnemy : Enemy
{
    public const float PressWindupSeconds = .68f;
    public const float PressActiveSeconds = .14f;
    public const float PressRecoverySeconds = .82f;
    public const float ClampWindupSeconds = .46f;
    public const float ClampActiveSeconds = .18f;
    public const float ClampRecoverySeconds = .62f;
    public const float ShoveWindupSeconds = .28f;
    public const float ShoveActiveSeconds = .13f;
    public const float ShoveRecoverySeconds = .54f;
    public const float VentWindupSeconds = .48f;
    public const float VentActiveSeconds = .16f;
    public const float VentRecoverySeconds = .66f;
    public const float VentCooldownSeconds = 5.6f;

    public const float BindingCastSeconds = .72f;
    public const float BindingCommitSeconds = .20f;
    public const float BindingRecoverySeconds = .62f;
    public const float BindingTelegraphSeconds = 1.02f;
    public const float BindingActiveSeconds = 1.75f;
    public const float BindingCooldownSeconds = 5.8f;
    public const float WarCastSeconds = .78f;
    public const float WarCommitSeconds = .16f;
    public const float WarRecoverySeconds = .66f;
    public const float WarBuffSeconds = 3.4f;
    public const float WarCooldownSeconds = 6.2f;
    public const float LanceAimSeconds = .52f;
    public const float LanceReleaseSeconds = .10f;
    public const float LanceRecoverySeconds = .64f;
    public const float BreakCooldownSeconds = 4.2f;

    private float _actionTimeRemaining;
    private float _actionDuration;
    private float _combatCooldownRemaining = .4f;
    private float _ventCooldownRemaining;
    private float _bindingCooldownRemaining;
    private float _warCooldownRemaining;
    private float _breakCooldownRemaining;
    private float _repositionCooldownRemaining;
    private float _primarySigilTimeRemaining;
    private float _lockedDirection = 1f;
    private Vector2 _lockedLanceTarget;
    private int _decisionSerial;
    private bool _hitConsumed;
    private bool _lanceReleased;
    private bool _warApplied;
    private RootHazardManager _activeHazards;

    public DemonCityIndustrialState CombatState { get; private set; } =
        DemonCityIndustrialState.Ready;
    public DemonCityIndustrialAttackKind AttackKind { get; private set; } =
        DemonCityIndustrialAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool IsWarSigilActive => CombatState == DemonCityIndustrialState.WarCommit;
    public float BindingCooldownRemaining => _bindingCooldownRemaining;
    public float WarCooldownRemaining => _warCooldownRemaining;
    public bool HasPrimarySigil => _primarySigilTimeRemaining > 0f;

    public override bool UsesProjectileAttack => Type == EnemyType.SigilCantor;
    public override bool IsCombatAttackTelegraphing => CombatState is
        DemonCityIndustrialState.PressWindup or
        DemonCityIndustrialState.ClampWindup or
        DemonCityIndustrialState.ShoveWindup or
        DemonCityIndustrialState.VentWindup or
        DemonCityIndustrialState.BindingCast or
        DemonCityIndustrialState.WarCast or
        DemonCityIndustrialState.LanceAim or
        DemonCityIndustrialState.BreakWindup;
    public override bool IsCombatAttackActive => CombatState is
        DemonCityIndustrialState.PressImpact or
        DemonCityIndustrialState.ClampSweep or
        DemonCityIndustrialState.ShoveActive or
        DemonCityIndustrialState.VentActive or
        DemonCityIndustrialState.BindingCommit or
        DemonCityIndustrialState.WarCommit or
        DemonCityIndustrialState.LanceRelease or
        DemonCityIndustrialState.BreakActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        DemonCityIndustrialState.PressImpact => ForwardArea(76, 98, -70),
        DemonCityIndustrialState.ClampSweep => ForwardArea(118, 88, -55),
        DemonCityIndustrialState.ShoveActive => ForwardArea(68, 70, -42),
        DemonCityIndustrialState.VentActive =>
            new Rectangle((int)Position.X - 76, (int)Position.Y - 58, 152, 116),
        DemonCityIndustrialState.BreakActive =>
            new Rectangle((int)Position.X - 62, (int)Position.Y - 52, 124, 104),
        _ => Rectangle.Empty
    };

    public DemonCityIndustrialEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId)
        : base(
            type,
            position,
            type == EnemyType.BloodworksServitor
                ? new Vector2(70f, 100f)
                : new Vector2(46f, 96f),
            type == EnemyType.BloodworksServitor ? 70f : 76f,
            type == EnemyType.BloodworksServitor ? 610f : 730f,
            EnemyStatScaling.Health(
                type == EnemyType.BloodworksServitor ? 292 : 158,
                type == EnemyType.BloodworksServitor ? 32 : 20,
                level,
                worldTier),
            EnemyStatScaling.Damage(
                type == EnemyType.BloodworksServitor ? 23 : 17,
                2,
                level,
                worldTier),
            type == EnemyType.BloodworksServitor ? 215 : 185,
            level,
            worldTier,
            isElite: false,
            roomId,
            attackRange: type == EnemyType.BloodworksServitor ? 105f : 72f,
            attackCooldownSeconds: 0f)
    {
        if (type is not (EnemyType.BloodworksServitor or EnemyType.SigilCantor))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
        _activeHazards = rootHazards;
        TickCooldowns(elapsed);

        if (CombatState != DemonCityIndustrialState.Ready)
        {
            UpdateCommittedAction(
                elapsed, player, dungeon, projectiles, rootHazards, enemies);
            return;
        }
        if (!IsPlayerDetected(player.Position))
            return;

        if (Type == EnemyType.BloodworksServitor)
            UpdateServitorReady(gameTime, player, dungeon);
        else
            UpdateCantorReady(gameTime, player, dungeon, rootHazards, enemies);
    }

    private void TickCooldowns(float elapsed)
    {
        _combatCooldownRemaining = MathF.Max(0f, _combatCooldownRemaining - elapsed);
        _ventCooldownRemaining = MathF.Max(0f, _ventCooldownRemaining - elapsed);
        _bindingCooldownRemaining = MathF.Max(0f, _bindingCooldownRemaining - elapsed);
        _warCooldownRemaining = MathF.Max(0f, _warCooldownRemaining - elapsed);
        _breakCooldownRemaining = MathF.Max(0f, _breakCooldownRemaining - elapsed);
        _repositionCooldownRemaining = MathF.Max(0f, _repositionCooldownRemaining - elapsed);
        _primarySigilTimeRemaining = MathF.Max(
            0f, _primarySigilTimeRemaining - elapsed);
    }

    private void UpdateServitorReady(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 126f)
        {
            MoveToward(gameTime, player.Position, 104f, dungeon, .74f);
            return;
        }
        if (_combatCooldownRemaining > 0f)
            return;

        LockDirection(player.Position);
        int decision = _decisionSerial++;
        if (distance <= 58f && _ventCooldownRemaining <= 0f &&
            decision % 5 == 3)
        {
            _ventCooldownRemaining = VentCooldownSeconds;
            Start(DemonCityIndustrialState.VentWindup,
                VentWindupSeconds, DemonCityIndustrialAttackKind.PressureVent);
        }
        else if (distance <= 54f && decision % 3 == 1)
        {
            Start(DemonCityIndustrialState.ShoveWindup,
                ShoveWindupSeconds, DemonCityIndustrialAttackKind.IndustrialShove);
        }
        else if (decision % 3 == 2)
        {
            Start(DemonCityIndustrialState.ClampWindup,
                ClampWindupSeconds, DemonCityIndustrialAttackKind.ClampSweep);
        }
        else
        {
            Start(DemonCityIndustrialState.PressWindup,
                PressWindupSeconds, DemonCityIndustrialAttackKind.PressHammer);
        }
    }

    private void UpdateCantorReady(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance < 88f)
        {
            LockDirection(player.Position);
            if (_breakCooldownRemaining <= 0f && _combatCooldownRemaining <= 0f)
            {
                _breakCooldownRemaining = BreakCooldownSeconds;
                Start(DemonCityIndustrialState.BreakWindup,
                    .30f, DemonCityIndustrialAttackKind.BreakSeal);
            }
            else if (_repositionCooldownRemaining <= 0f)
            {
                _repositionCooldownRemaining = 3.4f;
                Start(DemonCityIndustrialState.CantorReposition,
                    .38f, DemonCityIndustrialAttackKind.None);
            }
            return;
        }
        if (distance > 470f)
        {
            MoveToward(gameTime, player.Position, 350f, dungeon, .62f);
            return;
        }
        if (_combatCooldownRemaining > 0f)
            return;

        LockDirection(player.Position);
        int decision = _decisionSerial++;
        if (_primarySigilTimeRemaining <= 0f &&
            _warCooldownRemaining <= 0f && HasWarTarget(enemies) &&
            decision % 3 == 1)
        {
            _warCooldownRemaining = WarCooldownSeconds;
            _primarySigilTimeRemaining = WarCastSeconds + WarBuffSeconds;
            Start(DemonCityIndustrialState.WarCast,
                WarCastSeconds, DemonCityIndustrialAttackKind.WarSigil);
            return;
        }
        if (_primarySigilTimeRemaining <= 0f &&
            _bindingCooldownRemaining <= 0f && rootHazards != null &&
            decision % 3 == 0)
        {
            Vector2 sigilPosition = new(
                player.Position.X,
                player.Bounds.Bottom - 5f);
            if (rootHazards.TryAddBindingSigil(
                sigilPosition,
                new Vector2(142f, 20f),
                BindingTelegraphSeconds,
                BindingActiveSeconds,
                slowMultiplier: .72f,
                slowDurationSeconds: .28f,
                RoomId,
                this,
                dungeon))
            {
                _bindingCooldownRemaining = BindingCooldownSeconds;
                _primarySigilTimeRemaining =
                    BindingTelegraphSeconds + BindingActiveSeconds;
                Start(DemonCityIndustrialState.BindingCast,
                    BindingCastSeconds, DemonCityIndustrialAttackKind.BindingSigil);
                return;
            }
        }

        Vector2 target = player.Position + new Vector2(0f, -10f);
        Vector2 origin = StaffMouth();
        if (!HasClearLine(origin, target, dungeon))
        {
            if (distance > 220f)
                MoveToward(gameTime, player.Position, 205f, dungeon, .52f);
            return;
        }
        _lockedLanceTarget = target;
        Start(DemonCityIndustrialState.LanceAim,
            LanceAimSeconds, DemonCityIndustrialAttackKind.SigilLance);
    }

    private void UpdateCommittedAction(
        float elapsed,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (CombatState == DemonCityIndustrialState.ServitorReposition)
            MoveLocked(-.38f, elapsed, dungeon);
        else if (CombatState == DemonCityIndustrialState.CantorReposition)
            MoveLocked(-.64f, elapsed, dungeon);
        else if (CombatState == DemonCityIndustrialState.ShoveActive)
            MoveLocked(.48f, elapsed, dungeon);

        _actionTimeRemaining -= elapsed;
        if (_actionTimeRemaining <= 0f)
            AdvanceAction(projectiles, dungeon, rootHazards, enemies);

        if (CombatState is DemonCityIndustrialState.PressImpact or
            DemonCityIndustrialState.ClampSweep or
            DemonCityIndustrialState.ShoveActive or
            DemonCityIndustrialState.VentActive or
            DemonCityIndustrialState.BreakActive)
        {
            ResolveContact(player, dungeon);
        }
    }

    private void AdvanceAction(
        ProjectileManager projectiles,
        DungeonMap dungeon,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        switch (CombatState)
        {
            case DemonCityIndustrialState.ServitorReposition:
                Finish(.48f);
                break;
            case DemonCityIndustrialState.PressWindup:
                Start(DemonCityIndustrialState.PressImpact,
                    PressActiveSeconds, DemonCityIndustrialAttackKind.PressHammer);
                break;
            case DemonCityIndustrialState.PressImpact:
                Start(DemonCityIndustrialState.PressRecovery,
                    PressRecoverySeconds, DemonCityIndustrialAttackKind.PressHammer);
                break;
            case DemonCityIndustrialState.PressRecovery:
                BeginServitorReposition();
                break;
            case DemonCityIndustrialState.ClampWindup:
                Start(DemonCityIndustrialState.ClampSweep,
                    ClampActiveSeconds, DemonCityIndustrialAttackKind.ClampSweep);
                break;
            case DemonCityIndustrialState.ClampSweep:
                Start(DemonCityIndustrialState.ClampRecovery,
                    ClampRecoverySeconds, DemonCityIndustrialAttackKind.ClampSweep);
                break;
            case DemonCityIndustrialState.ClampRecovery:
                BeginServitorReposition();
                break;
            case DemonCityIndustrialState.ShoveWindup:
                Start(DemonCityIndustrialState.ShoveActive,
                    ShoveActiveSeconds, DemonCityIndustrialAttackKind.IndustrialShove);
                break;
            case DemonCityIndustrialState.ShoveActive:
                Start(DemonCityIndustrialState.ShoveRecovery,
                    ShoveRecoverySeconds, DemonCityIndustrialAttackKind.IndustrialShove);
                break;
            case DemonCityIndustrialState.ShoveRecovery:
                BeginServitorReposition();
                break;
            case DemonCityIndustrialState.VentWindup:
                Start(DemonCityIndustrialState.VentActive,
                    VentActiveSeconds, DemonCityIndustrialAttackKind.PressureVent);
                break;
            case DemonCityIndustrialState.VentActive:
                Start(DemonCityIndustrialState.VentRecovery,
                    VentRecoverySeconds, DemonCityIndustrialAttackKind.PressureVent);
                break;
            case DemonCityIndustrialState.VentRecovery:
                BeginServitorReposition();
                break;
            case DemonCityIndustrialState.CantorReposition:
                Finish(.42f);
                break;
            case DemonCityIndustrialState.BindingCast:
                Start(DemonCityIndustrialState.BindingCommit,
                    BindingCommitSeconds, DemonCityIndustrialAttackKind.BindingSigil);
                break;
            case DemonCityIndustrialState.BindingCommit:
                Start(DemonCityIndustrialState.BindingRecovery,
                    BindingRecoverySeconds, DemonCityIndustrialAttackKind.BindingSigil);
                break;
            case DemonCityIndustrialState.BindingRecovery:
                Finish(.88f);
                break;
            case DemonCityIndustrialState.WarCast:
                Start(DemonCityIndustrialState.WarCommit,
                    WarCommitSeconds, DemonCityIndustrialAttackKind.WarSigil);
                ApplyWarSigil(enemies);
                break;
            case DemonCityIndustrialState.WarCommit:
                Start(DemonCityIndustrialState.WarRecovery,
                    WarRecoverySeconds, DemonCityIndustrialAttackKind.WarSigil);
                break;
            case DemonCityIndustrialState.WarRecovery:
                Finish(.92f);
                break;
            case DemonCityIndustrialState.LanceAim:
                Start(DemonCityIndustrialState.LanceRelease,
                    LanceReleaseSeconds, DemonCityIndustrialAttackKind.SigilLance);
                ReleaseSigilLance(projectiles, dungeon);
                break;
            case DemonCityIndustrialState.LanceRelease:
                Start(DemonCityIndustrialState.LanceRecovery,
                    LanceRecoverySeconds, DemonCityIndustrialAttackKind.SigilLance);
                break;
            case DemonCityIndustrialState.LanceRecovery:
                Finish(.92f);
                break;
            case DemonCityIndustrialState.BreakWindup:
                Start(DemonCityIndustrialState.BreakActive,
                    .13f, DemonCityIndustrialAttackKind.BreakSeal);
                break;
            case DemonCityIndustrialState.BreakActive:
                Start(DemonCityIndustrialState.BreakRecovery,
                    .56f, DemonCityIndustrialAttackKind.BreakSeal);
                break;
            case DemonCityIndustrialState.BreakRecovery:
                Finish(.72f);
                break;
        }
    }

    private void BeginServitorReposition() => Start(
        DemonCityIndustrialState.ServitorReposition,
        .28f,
        DemonCityIndustrialAttackKind.None);

    private bool HasWarTarget(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        const float radiusSquared = 300f * 300f;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy.IsAlive && !ReferenceEquals(enemy, this) &&
                enemy.RoomId == RoomId && !enemy.IsBuffed &&
                enemy is DemonCityEnemy or DemonCityIndustrialEnemy &&
                Vector2.DistanceSquared(enemy.Position, Position) <= radiusSquared)
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyWarSigil(EnemyManager enemies)
    {
        if (_warApplied || enemies == null)
            return;
        _warApplied = true;
        const float radiusSquared = 300f * 300f;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!enemy.IsAlive || ReferenceEquals(enemy, this) ||
                enemy.RoomId != RoomId ||
                enemy is not (DemonCityEnemy or DemonCityIndustrialEnemy) ||
                Vector2.DistanceSquared(enemy.Position, Position) > radiusSquared)
            {
                continue;
            }
            enemy.ApplyTemporaryBuff(6, 4, WarBuffSeconds);
        }
    }

    private void ReleaseSigilLance(
        ProjectileManager projectiles,
        DungeonMap dungeon)
    {
        if (_lanceReleased || projectiles == null || !IsAlive)
            return;
        Vector2 origin = StaffMouth();
        if (!HasClearLine(origin, _lockedLanceTarget, dungeon))
            return;
        _lanceReleased = true;
        projectiles.SpawnSigilLance(
            origin,
            _lockedLanceTarget - origin,
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * .82f)),
            RoomId);
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap dungeon)
    {
        Rectangle area = ActiveAttackArea;
        if (_hitConsumed || area.IsEmpty ||
            !HasClearLine(
                Position + new Vector2(0f, -Size.Y * .18f),
                player.Position + new Vector2(0f, -8f),
                dungeon) ||
            (!area.Intersects(player.BodyHurtbox) &&
             !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))))
        {
            return;
        }

        _hitConsumed = true;
        float multiplier = CombatState switch
        {
            DemonCityIndustrialState.PressImpact => 1.28f,
            DemonCityIndustrialState.ClampSweep => .92f,
            DemonCityIndustrialState.ShoveActive => .62f,
            DemonCityIndustrialState.VentActive => .48f,
            DemonCityIndustrialState.BreakActive => .46f,
            _ => 1f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position,
            blockable: true,
            unblockable: false,
            hitArea: area));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        {
            float push = CombatState switch
            {
                DemonCityIndustrialState.ShoveActive => 27f,
                DemonCityIndustrialState.VentActive => 20f,
                DemonCityIndustrialState.BreakActive => 16f,
                DemonCityIndustrialState.ClampSweep => 14f,
                _ => 8f
            };
            player.PushAwayFrom(Position, push, dungeon);
        }
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset)
    {
        int x = _lockedDirection < 0f ? Bounds.Left - reach : Bounds.Right;
        return new Rectangle(x, (int)Position.Y + yOffset, reach, height);
    }

    private void MoveLocked(float scale, float elapsed, DungeonMap dungeon) =>
        MoveBy(new Vector2(
            _lockedDirection * EffectiveMovementSpeed * scale * elapsed,
            0f), dungeon);

    private Vector2 StaffMouth() => Position + new Vector2(
        _lockedDirection * 25f,
        -Size.Y * .40f);

    public bool HasClearLancePath(Vector2 target, DungeonMap dungeon) =>
        HasClearLine(StaffMouth(), target, dungeon);

    private static bool HasClearLine(
        Vector2 origin,
        Vector2 target,
        DungeonMap dungeon)
    {
        if (dungeon == null)
            return false;
        Vector2 delta = target - origin;
        float length = delta.Length();
        if (length <= 20f)
            return true;
        int steps = Math.Max(2, (int)MathF.Ceiling(length / 8f));
        for (int step = 2; step < steps - 2; step++)
        {
            Vector2 point = origin + delta * (step / (float)steps);
            foreach (Platform platform in dungeon.Platforms)
            {
                if (platform.Bounds.Contains(point.ToPoint()))
                    return false;
            }
        }
        return true;
    }

    private void LockDirection(Vector2 target)
    {
        FaceToward(target);
        _lockedDirection = target.X < Position.X ? -1f : 1f;
    }

    private void Start(
        DemonCityIndustrialState state,
        float duration,
        DemonCityIndustrialAttackKind kind)
    {
        CombatState = state;
        AttackKind = kind;
        _actionDuration = MathF.Max(.01f, duration);
        _actionTimeRemaining = _actionDuration;
        _hitConsumed = false;
        if (state != DemonCityIndustrialState.LanceRelease)
            _lanceReleased = false;
        if (state != DemonCityIndustrialState.WarCommit)
            _warApplied = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = DemonCityIndustrialState.Ready;
        AttackKind = DemonCityIndustrialAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _hitConsumed = false;
        _lanceReleased = false;
        _warApplied = false;
        _combatCooldownRemaining = MathF.Max(_combatCooldownRemaining, cooldown);
    }

    private void Cancel(float cooldown)
    {
        _activeHazards?.RemoveOwnedBy(this);
        _primarySigilTimeRemaining = 0f;
        Finish(cooldown);
        Attack.Cancel();
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
            Cancel(0f);
    }

    protected override void OnCombatInterrupted() => Cancel(.72f);
}
