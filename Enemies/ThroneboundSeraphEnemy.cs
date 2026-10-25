using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum SeraphState
{
    Intro, Ready, PhaseShift,
    ThrustWindup, ThrustActive, ThrustRecovery,
    WingWindup, WingActive, WingRecovery,
    ChainWindup, ChainExtend, ChainRetract, ChainRecovery,
    AscentWindup, AscentRise, AscentHover, AscentDescent, AscentStrike, AscentRecovery,
    HaloCharge, HaloActive, HaloRecovery
}

/// <summary>
/// Enemy 70: a grounded pre-final guardian with a short, scripted rise rather
/// than free flight. Owns every chain/halo/landing marker, including cleanup
/// when the existing enemy-only leash interrupts an airborne action.
/// </summary>
public sealed class ThroneboundSeraphEnemy : Enemy
{
    public const float MaximumAscentHeight = 84f;
    public const float MaximumDescentOffset = 96f;
    public const float MaximumChainReach = 320f;
    public const float MinimumChainCooldown = 5.4f;
    public const float MaximumHaloReach = 152f;
    public const float EngagementRange = 500f;
    private float _remaining, _duration, _cooldown = .65f;
    private float _chainCooldown, _ascentCooldown, _haloCooldown;
    private float _direction = -1f;
    private int _decision;
    private bool _hitConsumed, _phasePlayed, _deathRaised;
    private Rectangle? _authoredBounds;
    private DungeonMap _map;
    private Vector2 _ascentOrigin, _descentOrigin;

    public SeraphState CombatState { get; private set; } = SeraphState.Intro;
    public float ActionProgress => _duration <= 0 ? 0 : Math.Clamp(1f - _remaining / _duration, 0f, 1f);
    public bool AttackFacesLeft => _direction < 0;
    public int CombatPhase { get; private set; } = 1;
    public int PhaseTransitionCount { get; private set; }
    public int IntroCompletionCount { get; private set; }
    public int CompletionCount { get; private set; }
    public int ChainPullCount { get; private set; }
    public int ChainHitCount { get; private set; }
    public int HaloHitCount { get; private set; }
    public float ChainCooldownRemaining => _chainCooldown;
    public float AscentCooldownRemaining => _ascentCooldown;
    public Vector2 ChainOrigin { get; private set; }
    public Vector2 ChainDestination { get; private set; }
    public Vector2 LandingPosition { get; private set; }
    public Vector2 AscentOrigin => _ascentOrigin;
    public Vector2 HaloOrigin { get; private set; }
    public float HaloReach => CombatPhase == 2 ? 150f : 134f;
    public bool HasChain => CombatState is SeraphState.ChainWindup or SeraphState.ChainExtend or SeraphState.ChainRetract;
    public bool HasLandingMarker => CombatState is SeraphState.AscentWindup or SeraphState.AscentRise or
        SeraphState.AscentHover or SeraphState.AscentDescent or SeraphState.AscentStrike;
    public bool HaloLinesVisible => CombatState == SeraphState.HaloActive ||
        (CombatState == SeraphState.HaloCharge && ActionProgress >= .42f);
    public bool HasOwnedEffects => HasChain || HasLandingMarker ||
        CombatState is SeraphState.HaloCharge or SeraphState.HaloActive;
    public override bool IsFlying => CombatState is SeraphState.AscentRise or
        SeraphState.AscentHover or SeraphState.AscentDescent;
    public override bool IsCombatAttackTelegraphing => CombatState is
        SeraphState.ThrustWindup or SeraphState.WingWindup or SeraphState.ChainWindup or
        SeraphState.AscentWindup or SeraphState.AscentHover or SeraphState.HaloCharge;
    public override bool IsCombatAttackActive => CombatState is SeraphState.ThrustActive or
        SeraphState.WingActive or SeraphState.ChainExtend or SeraphState.AscentStrike or SeraphState.HaloActive;
    public override Rectangle ActiveAttackArea => CombatState switch
    {
        SeraphState.ThrustActive => ForwardArea(202, 68),
        SeraphState.WingActive => ForwardArea(142, 100),
        SeraphState.AscentStrike => new Rectangle((int)Position.X - 48, Bounds.Bottom - 96, 96, 100),
        // Chain and halo use the displayed line segments, never a filled AoE.
        _ => Rectangle.Empty
    };
    protected override float DeathPresentationDurationSeconds => 1.55f;
    public event Action<ThroneboundSeraphEnemy> SubBossDefeated;

    public ThroneboundSeraphEnemy(Vector2 position, int level, int worldTier, int roomId)
        : base(EnemyType.ThroneboundSeraph, position, new Vector2(64, 136),
            78f, 750f, EnemyStatScaling.Health(790, 50, level, worldTier),
            EnemyStatScaling.Damage(31, 3, level, worldTier), 880, level, worldTier,
            true, roomId, 190f, 0f, applyEliteModifiers: false)
    {
        Start(SeraphState.Intro, .62f);
    }

    internal void SetAuthoredEncounterBounds(Rectangle bounds) => _authoredBounds = bounds;

    protected override void UpdateBehavior(GameTime time, PlayerCharacter player, DungeonMap map,
        ProjectileManager projectiles, RootHazardManager rootHazards, EnemyManager enemies)
    {
        _map = map;
        float dt = MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        _cooldown = MathF.Max(0, _cooldown - dt);
        _chainCooldown = MathF.Max(0, _chainCooldown - dt);
        _ascentCooldown = MathF.Max(0, _ascentCooldown - dt);
        _haloCooldown = MathF.Max(0, _haloCooldown - dt);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState != SeraphState.Intro && (distance > EngagementRange ||
            MathF.Abs(player.Position.Y - Position.Y) > 260f))
        {
            Cancel(1f);
            MoveToward(time, player.Position, 230f, map, .65f);
            ConstrainToAuthoredArena(BoundsFor(map));
            return;
        }
        if (CombatState != SeraphState.Ready)
        {
            UpdateCommittedMotion(dt, map);
            _remaining -= dt;
            if (_remaining <= 0) Advance(map);
            ResolveContact(player, map);
            return;
        }
        if (!_phasePlayed && (long)CurrentHealth * 100 < (long)MaxHealth * 55)
        {
            _phasePlayed = true;
            CombatPhase = 2;
            PhaseTransitionCount++;
            Start(SeraphState.PhaseShift, .92f);
            return;
        }
        if (_cooldown > 0) return;
        Lock(player.Position);
        int choice = _decision++;
        if (distance > 225f)
        {
            if (_chainCooldown <= 0) BeginChain(player, map);
            else MoveToward(time, player.Position, 180f, map, .72f);
            ConstrainToAuthoredArena(BoundsFor(map));
            return;
        }
        // Deterministic positioning/timer decisions, never input prediction.
        if (_chainCooldown <= 0 && (CombatPhase == 2 ? choice % 3 == 1 : choice % 5 == 2))
            BeginChain(player, map);
        else if (_ascentCooldown <= 0 && (CombatPhase == 2 ? choice % 4 == 3 : choice % 6 == 3))
            BeginAscent(player, map);
        else if (_haloCooldown <= 0 && choice % 5 == 4)
        {
            // Emit from the physical crown behind the head. This leaves a
            // grounded, full-body gap between the downward marked rays.
            HaloOrigin = new Vector2(Position.X, Bounds.Top + 10);
            _haloCooldown = CombatPhase == 2 ? 8f : 10f;
            Start(SeraphState.HaloCharge, 1.14f);
        }
        else Start(choice % 3 == 1 ? SeraphState.WingWindup : SeraphState.ThrustWindup,
            choice % 3 == 1 ? .68f : .72f);
    }

    private void BeginChain(PlayerCharacter player, DungeonMap map)
    {
        _chainCooldown = CombatPhase == 2 ? MinimumChainCooldown : 7.2f;
        ChainOrigin = Position + new Vector2(_direction * 23, -12);
        Vector2 delta = player.BodyHurtbox.Center.ToVector2() - ChainOrigin;
        if (delta.LengthSquared() < 1) delta = new Vector2(_direction, 0);
        delta = Vector2.Normalize(delta) * MathF.Min(MaximumChainReach, delta.Length() + 30f);
        Rectangle bounds = BoundsFor(map);
        // Trace to the first obstruction. The warning and active chain share
        // exactly this endpoint, including when a wall blocks the target.
        Vector2 last = ChainOrigin;
        int steps = Math.Max(1, (int)MathF.Ceiling(delta.Length() / 4f));
        for (int i = 1; i <= steps; i++)
        {
            Vector2 sample = ChainOrigin + delta * ((float)i / steps);
            if (!bounds.Contains(sample.ToPoint()) ||
                !SideScrollingCollision.IsPositionFree(sample, new Vector2(8), map)) break;
            last = sample;
        }
        ChainDestination = last;
        Start(SeraphState.ChainWindup, CombatPhase == 2 ? .72f : .82f);
    }

    private void BeginAscent(PlayerCharacter player, DungeonMap map)
    {
        _ascentOrigin = Position;
        _ascentCooldown = CombatPhase == 2 ? 6.8f : 10.2f;
        float x = Position.X + Math.Clamp(player.Position.X - Position.X,
            -MaximumDescentOffset, MaximumDescentOffset);
        if (!TryFindLanding(x, map, out Vector2 landing)) landing = Position;
        LandingPosition = landing;
        Start(SeraphState.AscentWindup, .66f);
    }

    private void UpdateCommittedMotion(float dt, DungeonMap map)
    {
        if (CombatState == SeraphState.AscentRise)
        {
            float targetY = _ascentOrigin.Y - MaximumAscentHeight;
            MoveBy(new Vector2(0, -MathF.Min(175f * dt, MathF.Max(0, Position.Y - targetY))), map);
        }
        else if (CombatState == SeraphState.AscentDescent)
        {
            float nextProgress = Math.Clamp(ActionProgress + dt / _duration, 0, 1);
            Vector2 target = Vector2.Lerp(_descentOrigin, LandingPosition, nextProgress);
            MoveBy(target - Position, map);
        }
    }

    private void Advance(DungeonMap map)
    {
        switch (CombatState)
        {
            case SeraphState.Intro:
                IntroCompletionCount++; Finish(.65f); break;
            case SeraphState.PhaseShift: Finish(.70f); break;
            case SeraphState.ThrustWindup: Start(SeraphState.ThrustActive, .20f); break;
            case SeraphState.ThrustActive: Start(SeraphState.ThrustRecovery, Recovery(1.06f)); break;
            case SeraphState.WingWindup: Start(SeraphState.WingActive, .22f); break;
            case SeraphState.WingActive: Start(SeraphState.WingRecovery, Recovery(.96f)); break;
            case SeraphState.ChainWindup: Start(SeraphState.ChainExtend, .28f); break;
            case SeraphState.ChainExtend: Start(SeraphState.ChainRetract, .34f); break;
            case SeraphState.ChainRetract: Start(SeraphState.ChainRecovery, Recovery(.88f)); break;
            case SeraphState.AscentWindup: Start(SeraphState.AscentRise, .48f); break;
            case SeraphState.AscentRise: Start(SeraphState.AscentHover, .40f); break;
            case SeraphState.AscentHover:
                _descentOrigin = Position;
                Start(SeraphState.AscentDescent, .32f); break;
            case SeraphState.AscentDescent:
                LandSafely(map);
                Start(SeraphState.AscentStrike, .18f); break;
            case SeraphState.AscentStrike: Start(SeraphState.AscentRecovery, Recovery(1.12f)); break;
            case SeraphState.HaloCharge: Start(SeraphState.HaloActive, CombatPhase == 2 ? .22f : .18f); break;
            case SeraphState.HaloActive: Start(SeraphState.HaloRecovery, Recovery(1.08f)); break;
            default: Finish(CombatPhase == 2 ? .60f : .78f); break;
        }
    }

    private float Recovery(float seconds) => seconds * (CombatPhase == 2 ? .85f : 1f);

    private void ResolveContact(PlayerCharacter player, DungeonMap map)
    {
        if (_hitConsumed || !IsCombatAttackActive) return;
        bool touches;
        if (CombatState == SeraphState.ChainExtend)
            touches = SegmentTouches(ChainOrigin,
                Vector2.Lerp(ChainOrigin, ChainDestination, ActionProgress), player.BodyHurtbox, 8f);
        else if (CombatState == SeraphState.HaloActive)
        {
            touches = false;
            for (int i = 0; i < 3; i++)
                touches |= SegmentTouches(HaloOrigin, HaloRayEnd(i), player.BodyHurtbox,
                    CombatPhase == 2 ? 10f : 8f);
        }
        else touches = ActiveAttackArea.Intersects(player.BodyHurtbox) ||
            (player.Combat.IsBlocking && ActiveAttackArea.Intersects(player.DefenseBounds));
        if (!touches) return;
        // Every attack checks terrain. Chain pressure uses a brief slow only;
        // Map 7 encounters never force Player displacement.
        if (!HasClearLine(CombatState == SeraphState.HaloActive ? HaloOrigin : Position,
            player.BodyHurtbox.Center.ToVector2(), map)) return;
        _hitConsumed = true;
        float multiplier = CombatState == SeraphState.ChainExtend ? .60f :
            CombatState == SeraphState.HaloActive ? .78f :
            CombatState == SeraphState.AscentStrike ? 1.15f : 1f;
        var contact = new AttackContact(Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, player.BodyHurtbox);
        AttackResolution result = player.ReceiveMeleeAttack(contact);
        if (CombatState == SeraphState.HaloActive) HaloHitCount++;
        if (CombatState == SeraphState.ChainExtend)
        {
            ChainHitCount++;
            if (result != AttackResolution.Damaged) return;
            player.ApplySlow(.80f, .65f);
        }
    }

    public Vector2 HaloRayEnd(int index)
    {
        Vector2 direction = index switch
        {
            0 => new Vector2(-.80f, .60f),
            1 => Vector2.UnitY,
            2 => new Vector2(.80f, .60f),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        return HaloOrigin + direction * HaloReach;
    }
    internal static bool SegmentTouches(Vector2 start, Vector2 end, Rectangle body, float width)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(start, end) / 4f));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(start, end, (float)i / steps);
            if (new Rectangle((int)(p.X - width / 2), (int)(p.Y - width / 2),
                (int)width, (int)width).Intersects(body)) return true;
        }
        return false;
    }
    internal static bool HasClearLine(Vector2 start, Vector2 end, DungeonMap map)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(start, end) / 4f));
        for (int i = 0; i <= steps; i++)
            if (!SideScrollingCollision.IsPositionFree(Vector2.Lerp(start, end, (float)i / steps),
                new Vector2(6), map)) return false;
        return true;
    }

    private bool TryFindLanding(float intendedX, DungeonMap map, out Vector2 landing)
    {
        Rectangle bounds = BoundsFor(map);
        for (int step = 0; step <= 8; step++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = MathHelper.Clamp(intendedX + side * step * 16,
                    bounds.Left + Size.X / 2 + 16, bounds.Right - Size.X / 2 - 16);
                if (MathF.Abs(x - _ascentOrigin.X) > MaximumDescentOffset) continue;
                foreach (Platform platform in map.Platforms)
                {
                    if (x - Size.X / 2 < platform.Bounds.Left || x + Size.X / 2 > platform.Bounds.Right) continue;
                    Vector2 candidate = new(x, platform.Bounds.Top - Size.Y / 2);
                    if (MathF.Abs(candidate.Y - _ascentOrigin.Y) > 32f ||
                        !SideScrollingCollision.IsPositionFree(candidate, Size, map) ||
                        !SideScrollingCollision.IsSupported(candidate, Size, map)) continue;
                    // Never pick a landing beyond a wall at the starting level.
                    Vector2 swept = SideScrollingCollision.ResolveDisplacement(_ascentOrigin,
                        candidate - _ascentOrigin, Size, map);
                    if (Vector2.DistanceSquared(swept, candidate) > 4f) continue;
                    landing = candidate; return true;
                }
            }
        }
        landing = _ascentOrigin; return false;
    }
    private void LandSafely(DungeonMap map)
    {
        if (map == null) return;
        if (TryFindLanding(Position.X, map, out Vector2 landing)) Position = landing;
        else if (SideScrollingCollision.IsPositionFree(_ascentOrigin, Size, map)) Position = _ascentOrigin;
    }
    private Rectangle BoundsFor(DungeonMap map) => _authoredBounds ??
        map.FindRoomContaining(Position)?.Bounds ?? map.WorldBounds;
    private Rectangle ForwardArea(int reach, int height) => new(
        _direction < 0 ? Bounds.Left - reach : Bounds.Right,
        (int)Position.Y - height / 2, reach, height);
    private void Lock(Vector2 target)
    { FaceToward(target); _direction = target.X < Position.X ? -1 : 1; }
    private void Start(SeraphState state, float seconds)
    {
        CombatState = state; _remaining = _duration = seconds; _hitConsumed = false;
    }
    private void Finish(float cooldown)
    {
        CombatState = SeraphState.Ready; _remaining = _duration = 0;
        _cooldown = MathF.Max(_cooldown, cooldown);
    }
    private void Cancel(float cooldown)
    {
        if (CombatState == SeraphState.Intro && IntroCompletionCount == 0) IntroCompletionCount = 1;
        if (IsFlying) LandSafely(_map);
        // This class owns effects through its state: leaving the state clears all.
        Finish(cooldown); Attack.Cancel();
    }
    protected override void OnCombatInterrupted() => Cancel(.90f);
    protected override void OnDamaged()
    {
        if (IsAlive || _deathRaised) return;
        _deathRaised = true;
        Cancel(0);
        CompletionCount++;
        SubBossDefeated?.Invoke(this);
    }
}
