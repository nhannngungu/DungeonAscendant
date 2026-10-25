using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum SanctumEnemyState
{
    Ready, ResetStep,
    OathThrustWindup, OathThrustActive, OathThrustRecovery,
    VowSweepWindup, VowSweepActive, VowSweepRecovery,
    BrokenVowWindup, BrokenVowActive, BrokenVowRecovery,
    PommelWindup, PommelActive, PommelRecovery,
    VowshardCharge, VowshardRelease, VowshardRecovery,
    FracturedOathCast, FracturedOathRecovery,
    ShardFanCharge, ShardFanRelease, ShardFanRecovery,
    ShardWardWindup, ShardWardActive, ShardWardRecovery
}

public enum SanctumAttackKind
{
    None, OathThrust, VowSweep, BrokenVow, PommelRebuke,
    Vowshard, FracturedOath, ShardFan, ShardWard
}

/// <summary>
/// Detail-passed Hall of Broken Vows pair. All attacks use explicit tell,
/// active and recovery states; committed attacks retain their locked facing.
/// </summary>
public sealed class DemonKingSanctumEnemy : Enemy
{
    public const float MarkerTelegraphSeconds = .72f;
    public const float MarkerActiveSeconds = .18f;
    public const float MarkerRadius = 38f;

    private float _actionRemaining, _actionDuration;
    private float _combatCooldown = .4f, _defenseCooldown, _specialCooldown,
        _retreatCooldown;
    private float _lockedDirection = 1f;
    private Vector2 _lockedTarget;
    private int _decisionSerial;
    private bool _hitConsumed, _projectileReleased;
    private float _markerTelegraphRemaining, _markerActiveRemaining;
    private bool _markerHitConsumed;
    private ProjectileManager _ownedProjectiles;

    public SanctumEnemyState CombatState { get; private set; } =
        SanctumEnemyState.Ready;
    public SanctumAttackKind AttackKind { get; private set; } =
        SanctumAttackKind.None;
    public float ActionProgress => _actionDuration <= 0f ? 0f :
        Math.Clamp(1f - _actionRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool HasFracturedOathMarker =>
        _markerTelegraphRemaining > 0f || _markerActiveRemaining > 0f;
    public bool IsFracturedOathActive => _markerActiveRemaining > 0f;
    public Vector2 FracturedOathPosition { get; private set; }
    public float MarkerTelegraphRatio => Math.Clamp(
        1f - _markerTelegraphRemaining / MarkerTelegraphSeconds, 0f, 1f);
    public float MarkerActiveRatio => Math.Clamp(
        _markerActiveRemaining / MarkerActiveSeconds, 0f, 1f);
    public float DefensiveCooldownRemaining => _defenseCooldown;
    public float SpecialCooldownRemaining => _specialCooldown;

    public override bool UsesProjectileAttack =>
        Type == EnemyType.VowshardAcolyte;
    public override bool IsCombatAttackTelegraphing => CombatState is
        SanctumEnemyState.OathThrustWindup or
        SanctumEnemyState.VowSweepWindup or
        SanctumEnemyState.BrokenVowWindup or
        SanctumEnemyState.PommelWindup or
        SanctumEnemyState.VowshardCharge or
        SanctumEnemyState.FracturedOathCast or
        SanctumEnemyState.ShardFanCharge or
        SanctumEnemyState.ShardWardWindup;
    public override bool IsCombatAttackActive => CombatState is
        SanctumEnemyState.OathThrustActive or
        SanctumEnemyState.VowSweepActive or
        SanctumEnemyState.BrokenVowActive or
        SanctumEnemyState.PommelActive or
        SanctumEnemyState.VowshardRelease or
        SanctumEnemyState.ShardFanRelease or
        SanctumEnemyState.ShardWardActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        SanctumEnemyState.OathThrustActive => ForwardArea(122, 45, -55),
        SanctumEnemyState.VowSweepActive => ForwardArea(108, 75, -67),
        SanctumEnemyState.BrokenVowActive => ForwardArea(96, 92, -82),
        SanctumEnemyState.PommelActive => ForwardArea(48, 50, -52),
        SanctumEnemyState.ShardWardActive => new Rectangle(
            (int)Position.X - 57, (int)Position.Y - 55, 114, 108),
        _ => Rectangle.Empty
    };

    public DemonKingSanctumEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId)
        : base(type, position, SizeFor(type), SpeedFor(type),
            type == EnemyType.OathboundSentinel ? 650f : 760f,
            EnemyStatScaling.Health(BaseHealth(type),
                type == EnemyType.OathboundSentinel ? 27 : 21,
                level, worldTier),
            EnemyStatScaling.Damage(BaseDamage(type), 2, level, worldTier),
            type == EnemyType.OathboundSentinel ? 205 : 190,
            level, worldTier, false, roomId,
            type == EnemyType.OathboundSentinel ? 122f : 58f, 0f)
    {
        if (type is not (EnemyType.OathboundSentinel or
            EnemyType.VowshardAcolyte))
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    protected override float DeathPresentationDurationSeconds => .82f;

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        _ownedProjectiles = projectiles;
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        _combatCooldown = MathF.Max(0f, _combatCooldown - dt);
        _defenseCooldown = MathF.Max(0f, _defenseCooldown - dt);
        _specialCooldown = MathF.Max(0f, _specialCooldown - dt);
        _retreatCooldown = MathF.Max(0f, _retreatCooldown - dt);
        UpdateMarker(dt, player, dungeon);

        if (CombatState != SanctumEnemyState.Ready)
        {
            UpdateAction(dt, player, dungeon, projectiles);
            return;
        }

        if (Type == EnemyType.OathboundSentinel)
            UpdateSentinel(gameTime, player, dungeon);
        else
            UpdateAcolyte(gameTime, player, dungeon);
    }

    private void UpdateSentinel(
        GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance > 128f)
        {
            MoveToward(time, player.Position, 106f, map, .72f);
            return;
        }
        if (distance < 43f && _defenseCooldown <= 0f)
        {
            Lock(player.Position);
            _defenseCooldown = 3.2f;
            Start(SanctumEnemyState.PommelWindup, .24f,
                SanctumAttackKind.PommelRebuke);
            return;
        }
        if (_combatCooldown > 0f)
            return;

        Lock(player.Position);
        int decision = _decisionSerial++;
        if (_specialCooldown <= 0f && decision % 4 == 3)
        {
            _specialCooldown = 4.5f;
            Start(SanctumEnemyState.BrokenVowWindup, .78f,
                SanctumAttackKind.BrokenVow);
        }
        else if (decision % 3 == 1)
            Start(SanctumEnemyState.VowSweepWindup, .46f,
                SanctumAttackKind.VowSweep);
        else
            Start(SanctumEnemyState.OathThrustWindup, .40f,
                SanctumAttackKind.OathThrust);
    }

    private void UpdateAcolyte(
        GameTime time, PlayerCharacter player, DungeonMap map)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance < 82f)
        {
            if (_defenseCooldown <= 0f)
            {
                Lock(player.Position);
                _defenseCooldown = 3.4f;
                Start(SanctumEnemyState.ShardWardWindup, .30f,
                    SanctumAttackKind.ShardWard);
            }
            else if (_retreatCooldown <= 0f)
            {
                Lock(player.Position);
                _retreatCooldown = 2.6f;
                Start(SanctumEnemyState.ResetStep, .28f,
                    SanctumAttackKind.None);
            }
            return;
        }
        if (distance > 470f)
        {
            MoveToward(time, player.Position, 360f, map, .62f);
            return;
        }
        if (_combatCooldown > 0f)
            return;

        Lock(player.Position);
        _lockedTarget = player.Position + new Vector2(0f, -12f);
        int decision = _decisionSerial++;
        if (_specialCooldown <= 0f && decision % 5 == 4)
        {
            _specialCooldown = 5.2f;
            Start(SanctumEnemyState.ShardFanCharge, .72f,
                SanctumAttackKind.ShardFan);
        }
        else if (!HasFracturedOathMarker && decision % 3 == 1)
            Start(SanctumEnemyState.FracturedOathCast, .58f,
                SanctumAttackKind.FracturedOath);
        else
            Start(SanctumEnemyState.VowshardCharge, .56f,
                SanctumAttackKind.Vowshard);
    }

    private void UpdateAction(
        float dt, PlayerCharacter player, DungeonMap map,
        ProjectileManager projectiles)
    {
        if (CombatState == SanctumEnemyState.ResetStep)
            MoveBy(new Vector2(-_lockedDirection * EffectiveMovementSpeed *
                .68f * dt, 0f), map);
        _actionRemaining -= dt;
        if (_actionRemaining <= 0f)
            Advance(projectiles, map);

        if (CombatState is SanctumEnemyState.OathThrustActive or
            SanctumEnemyState.VowSweepActive or
            SanctumEnemyState.BrokenVowActive or
            SanctumEnemyState.PommelActive or
            SanctumEnemyState.ShardWardActive)
            ResolveContact(player, map);
    }

    private void Advance(ProjectileManager projectiles, DungeonMap map)
    {
        switch (CombatState)
        {
            case SanctumEnemyState.ResetStep: Finish(.55f); break;
            case SanctumEnemyState.OathThrustWindup:
                Start(SanctumEnemyState.OathThrustActive, .14f,
                    SanctumAttackKind.OathThrust); break;
            case SanctumEnemyState.OathThrustActive:
                Start(SanctumEnemyState.OathThrustRecovery, .55f,
                    SanctumAttackKind.OathThrust); break;
            case SanctumEnemyState.OathThrustRecovery: Finish(.62f); break;
            case SanctumEnemyState.VowSweepWindup:
                Start(SanctumEnemyState.VowSweepActive, .18f,
                    SanctumAttackKind.VowSweep); break;
            case SanctumEnemyState.VowSweepActive:
                Start(SanctumEnemyState.VowSweepRecovery, .64f,
                    SanctumAttackKind.VowSweep); break;
            case SanctumEnemyState.VowSweepRecovery: Finish(.70f); break;
            case SanctumEnemyState.BrokenVowWindup:
                Start(SanctumEnemyState.BrokenVowActive, .16f,
                    SanctumAttackKind.BrokenVow); break;
            case SanctumEnemyState.BrokenVowActive:
                Start(SanctumEnemyState.BrokenVowRecovery, .86f,
                    SanctumAttackKind.BrokenVow); break;
            case SanctumEnemyState.BrokenVowRecovery: Finish(.88f); break;
            case SanctumEnemyState.PommelWindup:
                Start(SanctumEnemyState.PommelActive, .10f,
                    SanctumAttackKind.PommelRebuke); break;
            case SanctumEnemyState.PommelActive:
                Start(SanctumEnemyState.PommelRecovery, .46f,
                    SanctumAttackKind.PommelRebuke); break;
            case SanctumEnemyState.PommelRecovery: Finish(.72f); break;
            case SanctumEnemyState.VowshardCharge:
                Start(SanctumEnemyState.VowshardRelease, .10f,
                    SanctumAttackKind.Vowshard);
                ReleaseShard(projectiles, map, fan: false); break;
            case SanctumEnemyState.VowshardRelease:
                Start(SanctumEnemyState.VowshardRecovery, .62f,
                    SanctumAttackKind.Vowshard); break;
            case SanctumEnemyState.VowshardRecovery: Finish(.90f); break;
            case SanctumEnemyState.FracturedOathCast:
                CreateMarker();
                Start(SanctumEnemyState.FracturedOathRecovery, .68f,
                    SanctumAttackKind.FracturedOath); break;
            case SanctumEnemyState.FracturedOathRecovery: Finish(.86f); break;
            case SanctumEnemyState.ShardFanCharge:
                Start(SanctumEnemyState.ShardFanRelease, .12f,
                    SanctumAttackKind.ShardFan);
                ReleaseShard(projectiles, map, fan: true); break;
            case SanctumEnemyState.ShardFanRelease:
                Start(SanctumEnemyState.ShardFanRecovery, .78f,
                    SanctumAttackKind.ShardFan); break;
            case SanctumEnemyState.ShardFanRecovery: Finish(1.05f); break;
            case SanctumEnemyState.ShardWardWindup:
                Start(SanctumEnemyState.ShardWardActive, .15f,
                    SanctumAttackKind.ShardWard); break;
            case SanctumEnemyState.ShardWardActive:
                Start(SanctumEnemyState.ShardWardRecovery, .56f,
                    SanctumAttackKind.ShardWard); break;
            case SanctumEnemyState.ShardWardRecovery: Finish(.82f); break;
        }
    }

    private void ReleaseShard(
        ProjectileManager projectiles, DungeonMap map, bool fan)
    {
        if (_projectileReleased || projectiles == null || !IsAlive)
            return;
        Vector2 origin = Position + new Vector2(_lockedDirection * 20f,
            -Size.Y * .34f);
        Vector2 direction = _lockedTarget - origin;
        if (!ClearLine(origin, _lockedTarget, map))
            return;
        _projectileReleased = true;
        if (!fan)
        {
            projectiles.SpawnVowshard(origin, direction,
                EffectiveAttackDamage, RoomId, owner: this);
            return;
        }
        float baseAngle = MathF.Atan2(direction.Y, direction.X);
        for (int index = -1; index <= 1; index++)
        {
            float angle = baseAngle + index * .12f;
            projectiles.SpawnVowshard(origin,
                new Vector2(MathF.Cos(angle), MathF.Sin(angle)),
                Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * .58f)),
                RoomId, small: true, owner: this);
        }
    }

    private void CreateMarker()
    {
        FracturedOathPosition = new Vector2(_lockedTarget.X,
            _lockedTarget.Y + Size.Y * .42f);
        _markerTelegraphRemaining = MarkerTelegraphSeconds;
        _markerActiveRemaining = 0f;
        _markerHitConsumed = false;
    }

    private void UpdateMarker(float dt, PlayerCharacter player, DungeonMap map)
    {
        if (_markerTelegraphRemaining > 0f)
        {
            _markerTelegraphRemaining = MathF.Max(0f,
                _markerTelegraphRemaining - dt);
            if (_markerTelegraphRemaining <= 0f && IsAlive)
                _markerActiveRemaining = MarkerActiveSeconds;
        }
        else if (_markerActiveRemaining > 0f)
        {
            _markerActiveRemaining = MathF.Max(0f,
                _markerActiveRemaining - dt);
            var area = new Rectangle(
                (int)(FracturedOathPosition.X - MarkerRadius),
                (int)(FracturedOathPosition.Y - 82f),
                (int)(MarkerRadius * 2f), 86);
            if (!_markerHitConsumed && (area.Intersects(player.BodyHurtbox) ||
                player.Combat.IsBlocking && area.Intersects(player.DefenseBounds)))
            {
                _markerHitConsumed = true;
                player.ReceiveMeleeAttack(new AttackContact(
                    Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * .72f)),
                    Position, true, false, area));
            }
        }
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
            SanctumEnemyState.BrokenVowActive => 1.32f,
            SanctumEnemyState.PommelActive => .48f,
            SanctumEnemyState.ShardWardActive => .55f,
            SanctumEnemyState.VowSweepActive => .88f,
            _ => 1f
        };
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
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
        SanctumEnemyState state, float duration, SanctumAttackKind kind)
    {
        CombatState = state;
        AttackKind = kind;
        _actionDuration = MathF.Max(.01f, duration);
        _actionRemaining = _actionDuration;
        _hitConsumed = false;
        if (state is not (SanctumEnemyState.VowshardRelease or
            SanctumEnemyState.ShardFanRelease))
            _projectileReleased = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = SanctumEnemyState.Ready;
        AttackKind = SanctumAttackKind.None;
        _actionRemaining = _actionDuration = 0f;
        _hitConsumed = _projectileReleased = false;
        _combatCooldown = MathF.Max(_combatCooldown, cooldown);
    }

    private void Cancel(float cooldown)
    {
        Finish(cooldown);
        _markerTelegraphRemaining = _markerActiveRemaining = 0f;
        _markerHitConsumed = true;
        _ownedProjectiles?.RemoveOwnedBy(this);
        Attack.Cancel();
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
            Cancel(0f);
    }

    protected override void OnCombatInterrupted() => Cancel(.68f);

    private static bool ClearLine(Vector2 origin, Vector2 target, DungeonMap map)
    {
        if (map == null)
            return false;
        Vector2 delta = target - origin;
        int steps = Math.Max(2, (int)MathF.Ceiling(delta.Length() / 8f));
        for (int step = 2; step < steps - 2; step++)
        {
            Vector2 point = origin + delta * (step / (float)steps);
            foreach (World.Platform platform in map.Platforms)
                if (platform.Bounds.Contains(point.ToPoint()))
                    return false;
        }
        return true;
    }

    private static Vector2 SizeFor(EnemyType type) =>
        type == EnemyType.OathboundSentinel
            ? new Vector2(50f, 92f)
            : new Vector2(44f, 84f);
    private static float SpeedFor(EnemyType type) =>
        type == EnemyType.OathboundSentinel ? 82f : 75f;
    private static int BaseHealth(EnemyType type) =>
        type == EnemyType.OathboundSentinel ? 245 : 172;
    private static int BaseDamage(EnemyType type) =>
        type == EnemyType.OathboundSentinel ? 22 : 20;
}
