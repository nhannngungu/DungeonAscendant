using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum SanctumEliteState { Ready, Windup, Active, Recovery }
public enum SanctumEliteAttack
{
    None, RoyalSever, ThroneThrust, CrownSweep, SovereignGuard, RoyalRiposte,
    ForeseenSpear, FateCircle, ThreeOmens, FracturedFuture, VeilShift
}

/// <summary>A fixed prophecy: its position, order and truth never track the target.</summary>
public sealed class SanctumProphecyMarker
{
    public Vector2 Position { get; }
    public float RevealAt { get; }
    public float ActivateAt { get; }
    public bool IsFalse { get; }
    public int Order { get; }
    public const float Radius = 34f;
    public const float ActiveSeconds = .20f;
    internal bool Consumed;
    public SanctumProphecyMarker(Vector2 position, float revealAt,
        float activateAt, bool isFalse, int order)
    {
        Position = position; RevealAt = revealAt; ActivateAt = activateAt;
        IsFalse = isFalse; Order = order;
    }
    public bool IsVisible(float age) => age >= RevealAt &&
        age < (IsFalse ? ActivateAt - .40f : ActivateAt + ActiveSeconds);
    public bool IsActive(float age) => !IsFalse && age >= ActivateAt &&
        age < ActivateAt + ActiveSeconds;
    public Rectangle Area => new((int)Position.X - 34,
        (int)Position.Y - 64, 68, 68);
}

/// <summary>
/// Two finite elite kits. All decisions use world positions and elapsed time;
/// neither kit observes input. Owned prophecies are cleared on interruption,
/// disengagement and death, including the travelling lance.
/// </summary>
public sealed class DemonKingSanctumEliteEnemy : Enemy
{
    public const float MaximumShiftDistance = 112f;
    public const float OracleEngagementRange = 540f;
    public const float OmenLaneSpacing = 156f;
    private readonly List<SanctumProphecyMarker> _markers = new(3);
    private Rectangle? _authoredBounds;
    private float _remaining, _duration, _cooldown = .65f;
    private float _guardCooldown, _shiftCooldown;
    private float _direction = 1f;
    private int _decision;
    private bool _hitConsumed, _guardEarned;
    private Vector2 _guardSource;
    private float _lanceLife;
    private bool _lanceHit;

    public SanctumEliteState CombatState { get; private set; }
    public SanctumEliteAttack AttackKind { get; private set; }
    public float ActionProgress => _duration <= 0f ? 0f :
        Math.Clamp(1f - _remaining / _duration, 0f, 1f);
    public bool AttackFacesLeft => _direction < 0f;
    public bool IsSovereignGuarding => AttackKind == SanctumEliteAttack.SovereignGuard &&
        CombatState == SanctumEliteState.Active;
    public float GuardCooldownRemaining => _guardCooldown;
    public float ShiftCooldownRemaining => _shiftCooldown;
    public int ValidGuardCount { get; private set; }
    public int RiposteCount { get; private set; }
    public int ProphecyActivationCount { get; private set; }
    public int ProphecyHitCount { get; private set; }
    public IReadOnlyList<SanctumProphecyMarker> Markers => _markers;
    public float ProphecyAge { get; private set; }
    public Vector2 ShiftOrigin { get; private set; }
    public Vector2 ShiftDestination { get; private set; }
    public Vector2 SpearOrigin { get; private set; }
    public Vector2 SpearDestination { get; private set; }
    public Vector2 SpearPosition { get; private set; }
    public bool HasLance => _lanceLife > 0f;
    public bool HasOwnedEffects => _markers.Count > 0 || HasLance;
    public override bool IsCombatAttackTelegraphing => CombatState == SanctumEliteState.Windup;
    public override bool IsCombatAttackActive => CombatState == SanctumEliteState.Active &&
        AttackKind is not (SanctumEliteAttack.SovereignGuard or SanctumEliteAttack.VeilShift);
    public override Rectangle ActiveAttackArea => IsCombatAttackActive ? AttackKind switch
    {
        SanctumEliteAttack.RoyalSever => ForwardArea(112, 96),
        SanctumEliteAttack.ThroneThrust => ForwardArea(178, 54),
        SanctumEliteAttack.CrownSweep => ForwardArea(146, 80),
        SanctumEliteAttack.RoyalRiposte => ForwardArea(128, 80),
        _ => Rectangle.Empty
    } : Rectangle.Empty;
    protected override float DeathPresentationDurationSeconds => .95f;

    public DemonKingSanctumEliteEnemy(EnemyType type, Vector2 position,
        int level, int worldTier, int roomId)
        : base(type, position,
            type == EnemyType.RoyalSanctumKnight ? new Vector2(54, 112) : new Vector2(44, 108),
            type == EnemyType.RoyalSanctumKnight ? 112f : 78f, 680f,
            EnemyStatScaling.Health(type == EnemyType.RoyalSanctumKnight ? 390 : 310,
                30, level, worldTier),
            EnemyStatScaling.Damage(type == EnemyType.RoyalSanctumKnight ? 29 : 26,
                2, level, worldTier), 340, level, worldTier, true, roomId,
            128f, 0f, applyEliteModifiers: false)
    {
        if (type is not (EnemyType.RoyalSanctumKnight or EnemyType.AbyssOracle))
            throw new ArgumentOutOfRangeException(nameof(type));
    }

    internal void SetAuthoredEncounterBounds(Rectangle bounds) => _authoredBounds = bounds;

    public override void ReceiveDamageFrom(int damage, float poiseDamage, Vector2 sourcePosition)
    {
        // A real, frontal hit during the active weapon guard earns one counter.
        // Back hits, zero damage, the raise pose and ordinary damage cannot arm it.
        if (IsSovereignGuarding && damage > 0 &&
            (sourcePosition.X - Position.X) * _direction >= 0f &&
            MathF.Abs(sourcePosition.Y - Position.Y) <= 110f)
        {
            if (!_guardEarned) ValidGuardCount++;
            _guardEarned = true;
            _guardSource = sourcePosition;
            damage = Math.Max(1, (int)MathF.Round(damage * .38f));
            poiseDamage *= .35f;
        }
        base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
    }

    protected override void UpdateBehavior(GameTime time, PlayerCharacter player,
        DungeonMap map, ProjectileManager projectiles, RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float dt = MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        _cooldown = MathF.Max(0, _cooldown - dt);
        _guardCooldown = MathF.Max(0, _guardCooldown - dt);
        _shiftCooldown = MathF.Max(0, _shiftCooldown - dt);
        float distance = MathF.Abs(player.Position.X - Position.X);
        // Stop stale prophecies when the target leaves the local combat view.
        if (Type == EnemyType.AbyssOracle && (distance > OracleEngagementRange ||
            MathF.Abs(player.Position.Y - Position.Y) > 260f))
        {
            Cancel(.85f);
            MoveToward(time, player.Position, 370f, map, .65f);
            return;
        }
        UpdateProphecies(dt, player, map);
        if (CombatState != SanctumEliteState.Ready)
        {
            // Thrust follows the body during anticipation, then freezes on release.
            if (AttackKind == SanctumEliteAttack.ThroneThrust &&
                CombatState == SanctumEliteState.Windup) Lock(player.Position);
            _remaining -= dt;
            if (_remaining <= 0f) Advance(player, map);
            if (!ActiveAttackArea.IsEmpty && !_hitConsumed &&
                TryContact(player, ActiveAttackArea, AttackKind ==
                    SanctumEliteAttack.RoyalRiposte ? 1.28f : 1f)) _hitConsumed = true;
            return;
        }
        if (_cooldown > 0f || HasOwnedEffects) return;
        if (Type == EnemyType.RoyalSanctumKnight)
        {
            if (distance > 180f)
            { MoveToward(time, player.Position, 145f, map, .78f); return; }
            Lock(player.Position);
            int choice = _decision++ % 5;
            if (choice == 3 && _guardCooldown <= 0f)
            {
                _guardCooldown = 5.8f;
                _guardEarned = false;
                Begin(SanctumEliteAttack.SovereignGuard, .28f);
            }
            else Begin(choice == 1 ? SanctumEliteAttack.ThroneThrust :
                choice == 2 ? SanctumEliteAttack.CrownSweep : SanctumEliteAttack.RoyalSever,
                choice == 1 ? .64f : choice == 2 ? .60f : .38f);
        }
        else
        {
            Lock(player.Position);
            int choice = _decision++ % 5;
            if (choice == 4 && _shiftCooldown <= 0f)
            {
                PrepareShift(player.Position, map);
                _shiftCooldown = 6.8f;
                Begin(SanctumEliteAttack.VeilShift, .58f);
            }
            else if (distance < 105f)
            {
                // Close pressure interrupts the Oracle's rhythm; it must step away
                // instead of casting point-blank or repeatedly teleporting.
                MoveAway(time, player.Position, map, .55f);
                _cooldown = .30f;
            }
            else
            {
                SanctumEliteAttack kind = choice switch
                {
                    1 => SanctumEliteAttack.FateCircle,
                    2 => SanctumEliteAttack.ThreeOmens,
                    3 => SanctumEliteAttack.FracturedFuture,
                    _ => SanctumEliteAttack.ForeseenSpear
                };
                PrepareProphecy(kind, player, map);
                Begin(kind, kind == SanctumEliteAttack.ThreeOmens ? 2.22f :
                    kind == SanctumEliteAttack.FracturedFuture ? 1.64f : 1.10f);
            }
        }
    }

    private void Begin(SanctumEliteAttack kind, float duration)
    {
        AttackKind = kind;
        CombatState = SanctumEliteState.Windup;
        SetTime(duration);
    }
    private void SetTime(float duration)
    { _remaining = _duration = duration; _hitConsumed = false; }
    private void Advance(PlayerCharacter player, DungeonMap map)
    {
        if (CombatState == SanctumEliteState.Windup)
        {
            CombatState = SanctumEliteState.Active;
            if (AttackKind == SanctumEliteAttack.VeilShift)
            {
                MoveBy(ShiftDestination - Position, map);
                ShiftDestination = Position;
            }
            if (AttackKind == SanctumEliteAttack.ForeseenSpear)
            {
                SpearPosition = SpearOrigin;
                _lanceLife = .82f;
                _lanceHit = false;
            }
            SetTime(AttackKind == SanctumEliteAttack.SovereignGuard ? .62f :
                AttackKind == SanctumEliteAttack.VeilShift ? .10f : .18f);
        }
        else if (CombatState == SanctumEliteState.Active)
        {
            if (AttackKind == SanctumEliteAttack.SovereignGuard && _guardEarned)
            {
                _guardEarned = false;
                Lock(_guardSource);
                RiposteCount++;
                Begin(SanctumEliteAttack.RoyalRiposte, .46f);
                return;
            }
            CombatState = SanctumEliteState.Recovery;
            SetTime(AttackKind == SanctumEliteAttack.ThroneThrust ? .95f :
                AttackKind == SanctumEliteAttack.RoyalSever ? .34f :
                Type == EnemyType.AbyssOracle ? .82f : .62f);
        }
        else
        {
            bool reposition = AttackKind == SanctumEliteAttack.CrownSweep;
            Finish(Type == EnemyType.AbyssOracle ? .85f : .48f);
            if (reposition) MoveBy(new Vector2(-_direction * 26f, 0), map);
        }
    }

    private void PrepareProphecy(SanctumEliteAttack kind, PlayerCharacter player, DungeonMap map)
    {
        _markers.Clear(); ProphecyAge = 0f;
        if (kind == SanctumEliteAttack.ForeseenSpear)
        {
            SpearOrigin = Position + new Vector2(_direction * 20f, -18f);
            Vector2 delta = player.BodyHurtbox.Center.ToVector2() - SpearOrigin;
            if (delta.LengthSquared() < 1f) delta = new Vector2(_direction, 0f);
            float length = MathF.Min(OracleEngagementRange, delta.Length() + 120f);
            Vector2 target = SpearOrigin + Vector2.Normalize(delta) * length;
            Rectangle bounds = BoundsFor(map);
            // Clamp by distance along the original ray; never bend the path.
            float t = 1f;
            if (target.X < bounds.Left + 20f)
                t = (bounds.Left + 20f - SpearOrigin.X) / (target.X - SpearOrigin.X);
            else if (target.X > bounds.Right - 20f)
                t = (bounds.Right - 20f - SpearOrigin.X) / (target.X - SpearOrigin.X);
            SpearDestination = Vector2.Lerp(SpearOrigin, target, Math.Clamp(t, 0f, 1f));
            return;
        }
        float center = ClampX(player.Position.X,
            kind == SanctumEliteAttack.ThreeOmens ? OmenLaneSpacing + 44f :
            kind == SanctumEliteAttack.FracturedFuture ? 130f : 44f, map);
        if (kind == SanctumEliteAttack.ThreeOmens)
        {
            // Disjoint 68px lanes with 88px gaps. At least one full player width
            // remains safe even if the player stays inside this small pattern.
            for (int i = 0; i < 3; i++)
                AddMarker(center + (i - 1) * OmenLaneSpacing,
                    i * .28f, 1.18f + i * .38f, false, i + 1, player, map);
        }
        else if (kind == SanctumEliteAttack.FracturedFuture)
        {
            // The solid double-ring / crown rune is ALWAYS real. The broken
            // single-ring future is ALWAYS harmless and dissolves early.
            AddMarker(center - 88f, 0f, 1.42f, false, 1, player, map);
            AddMarker(center + 88f, 0f, 1.42f, true, 2, player, map);
        }
        else AddMarker(center, 0f, .90f, false, 1, player, map);
    }

    private void AddMarker(float x, float reveal, float activate, bool fake, int order,
        PlayerCharacter player, DungeonMap map)
    {
        // Resolve authored floor locally without an unsafe exact-X entry search.
        float y = map.FindRoomContaining(new Vector2(x, Position.Y))?.GroundY ?? player.Bounds.Bottom;
        float best = float.MaxValue;
        foreach (Platform platform in map.Platforms)
        {
            if (x - 34f < platform.Bounds.Left || x + 34f > platform.Bounds.Right) continue;
            float difference = MathF.Abs(platform.Bounds.Top - player.Bounds.Bottom);
            if (difference < best) { best = difference; y = platform.Bounds.Top; }
        }
        _markers.Add(new SanctumProphecyMarker(new Vector2(x, y), reveal, activate, fake, order));
    }

    private void UpdateProphecies(float dt, PlayerCharacter player, DungeonMap map)
    {
        float priorAge = ProphecyAge;
        ProphecyAge += dt;
        foreach (SanctumProphecyMarker marker in _markers)
        {
            if (!marker.IsFalse && priorAge < marker.ActivateAt && ProphecyAge >= marker.ActivateAt)
                ProphecyActivationCount++;
            if (marker.IsActive(ProphecyAge) && !marker.Consumed &&
                TryContact(player, marker.Area, .78f))
            { marker.Consumed = true; ProphecyHitCount++; }
        }
        if (_markers.Count > 0 && ProphecyAge >=
            _markers[^1].ActivateAt + SanctumProphecyMarker.ActiveSeconds + .05f)
            _markers.Clear();
        if (!HasLance) return;
        Vector2 previous = SpearPosition;
        Vector2 delta = SpearDestination - SpearPosition;
        float distance = delta.Length();
        Vector2 movement = distance < .01f ? Vector2.Zero :
            delta / distance * MathF.Min(distance, 680f * dt);
        SpearPosition += movement;
        _lanceLife = MathF.Max(0f, _lanceLife - dt);
        int steps = Math.Max(1, (int)MathF.Ceiling(movement.Length() / 8f));
        for (int i = 0; i <= steps && !_lanceHit; i++)
        {
            Vector2 sample = Vector2.Lerp(previous, SpearPosition, (float)i / steps);
            var area = new Rectangle((int)sample.X - 9, (int)sample.Y - 9, 18, 18);
            if (!SideScrollingCollision.IsPositionFree(sample, new Vector2(12), map))
            { _lanceLife = 0f; break; }
            if (TryContact(player, area, .96f))
            { _lanceHit = true; _lanceLife = 0f; }
        }
        if (distance <= movement.Length() + .1f) _lanceLife = 0f;
    }

    private void PrepareShift(Vector2 playerPosition, DungeonMap map)
    {
        ShiftOrigin = Position;
        float direction = playerPosition.X < Position.X ? 1f : -1f;
        float x = ClampX(Position.X + direction * MaximumShiftDistance, Size.X / 2f + 20f, map);
        ShiftDestination = Position;
        // Only accept locally supported, collision-free destinations. Staying
        // put is preferable to snapping to another terrace or crossing geometry.
        for (float fraction = 1f; fraction >= .25f; fraction -= .25f)
        {
            Vector2 candidate = new(MathHelper.Lerp(Position.X, x, fraction), Position.Y);
            if (SideScrollingCollision.IsPositionFree(candidate, Size, map) &&
                SideScrollingCollision.IsSupported(candidate, Size, map))
            { ShiftDestination = candidate; break; }
        }
    }
    private Rectangle BoundsFor(DungeonMap map) => _authoredBounds ??
        map.FindRoomContaining(Position)?.Bounds ?? map.WorldBounds;
    private float ClampX(float x, float margin, DungeonMap map)
    {
        Rectangle bounds = BoundsFor(map);
        return MathHelper.Clamp(x, bounds.Left + margin, bounds.Right - margin);
    }
    private Rectangle ForwardArea(int reach, int height) => new(
        _direction < 0 ? Bounds.Left - reach : Bounds.Right,
        (int)Position.Y - height / 2, reach, height);
    private bool TryContact(PlayerCharacter player, Rectangle area, float multiplier)
    {
        if (!area.Intersects(player.BodyHurtbox) &&
            !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds))) return false;
        player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
        return true;
    }
    private void Lock(Vector2 target)
    { FaceToward(target); _direction = target.X < Position.X ? -1f : 1f; }
    private void Finish(float cooldown)
    {
        CombatState = SanctumEliteState.Ready; AttackKind = SanctumEliteAttack.None;
        _remaining = _duration = 0; _guardEarned = false;
        _cooldown = MathF.Max(_cooldown, cooldown);
    }
    private void Cancel(float cooldown)
    {
        Finish(cooldown); _markers.Clear(); _lanceLife = 0f;
        Attack.Cancel();
    }
    protected override void OnDamaged() { if (!IsAlive) Cancel(0f); }
    protected override void OnCombatInterrupted() => Cancel(.85f);
}
