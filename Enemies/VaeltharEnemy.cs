using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum VaeltharState
{
    Intro, Ready, PhaseTransition, Windup, Active, FollowupTell, Recovery,
    GuardRaise, GuardActive, CounterTell, MoveTell, Pursuit,
    CenterAdvance, PatternTell, PatternSequence, FinalWindup, FinalActive,
    UltimateRaise, UltimateOmens
}
public enum VaeltharAttack
{
    None, KingsCut, RoyalAdvance, ThroneBreaker, SovereignReversal,
    AbyssArc, CrownPillar, KingsPursuit, RoyalDecree,
    FinalSever, ThronelessRush, KingsDomain, EndOfKings
}

public sealed class VaeltharLane
{
    public Vector2 Position { get; }
    public int Width { get; }
    public int Order { get; }
    public float RevealAt { get; }
    public float ActivateAt { get; }
    public bool IsFinisher { get; }
    internal bool Consumed;
    public VaeltharLane(Vector2 position, int width, int order, float revealAt,
        float activateAt, bool finisher = false)
    {
        Position = position; Width = width; Order = order;
        RevealAt = revealAt; ActivateAt = activateAt; IsFinisher = finisher;
    }
    public Rectangle Area => new((int)Position.X - Width / 2, (int)Position.Y - 150, Width, 154);
    public bool IsActive(float age) => !IsFinisher && age >= ActivateAt && age < ActivateAt + .22f;
    public bool IsVisible(float age) => age >= RevealAt && (IsFinisher || age < ActivateAt + .30f);
}

/// <summary>
/// Final sovereign: finite melee commitments and locally authored abyss lanes.
/// This owns all waves/lanes; interruption, leash and death clear them together.
/// No attack observes keyboard input or displaces the Player.
/// </summary>
public sealed class VaeltharEnemy : Enemy
{
    public const string EncounterId = "sanctum-vaelthar";
    public const float MaximumPursuitDistance = 128f;
    public const float MaximumAdvanceDistance = 72f;
    public const float UltimateCooldownSeconds = 32f;
    public const float EngagementRange = 560f;
    private readonly List<VaeltharLane> _lanes = new(3);
    private float _remaining, _duration, _cooldown = .6f;
    private float _guardCooldown, _arcCooldown, _domainCooldown, _ultimateCooldown = 12f;
    private float _direction = -1, _waveLife, _waveDirection;
    private bool _hitConsumed, _waveHit, _guardEarned, _deathRaised;
    private Vector2 _guardSource, _movementStart, _movementEnd;
    private int _decision, _comboIndex, _comboLength;
    private bool _arenaConfigured;

    public VaeltharState CombatState { get; private set; } = VaeltharState.Intro;
    public VaeltharAttack AttackKind { get; private set; }
    public int CombatPhase { get; private set; } = 1;
    public int PhaseTwoTransitionCount { get; private set; }
    public int PhaseThreeTransitionCount { get; private set; }
    public int IntroCompletionCount { get; private set; }
    public int CompletionCount { get; private set; }
    public int ValidReversalCount { get; private set; }
    public int CounterCount { get; private set; }
    public int UltimateCount { get; private set; }
    public int LaneActivationCount { get; private set; }
    public int LaneHitCount { get; private set; }
    public int AttackHitCount { get; private set; }
    public float ReversalCooldownRemaining => _guardCooldown;
    public float ArcCooldownRemaining => _arcCooldown;
    public float UltimateCooldownRemaining => _ultimateCooldown;
    public int ComboIndex => _comboIndex;
    public int ComboLength => _comboLength;
    public float ActionProgress => _duration <= 0 ? 0 : Math.Clamp(1 - _remaining / _duration, 0, 1);
    public bool AttackFacesLeft => _direction < 0;
    public Rectangle AuthoredArenaBounds { get; private set; }
    public Vector2 MovementOrigin => _movementStart;
    public Vector2 MovementDestination => _movementEnd;
    public Vector2 WavePosition { get; private set; }
    public float WaveDirection => _waveDirection;
    public bool HasWave => _waveLife > 0;
    public IReadOnlyList<VaeltharLane> Lanes => _lanes;
    public float PatternAge { get; private set; }
    public bool HasOwnedEffects => HasWave || _lanes.Count > 0;
    public bool IsReversing => CombatState == VaeltharState.GuardActive;
    public bool IsEndOfKings => AttackKind == VaeltharAttack.EndOfKings;
    public float UltimateDimRatio => IsEndOfKings ? CombatState == VaeltharState.Recovery
        ? 1 - ActionProgress : CombatState == VaeltharState.UltimateRaise ? ActionProgress : 1 : 0;
    public override bool CanDropLoot => false;
    protected override float DeathPresentationDurationSeconds => 3.25f;
    public event Action<VaeltharEnemy> FinalBossDefeated;
    public override bool IsCombatAttackTelegraphing => CombatState is VaeltharState.Windup or
        VaeltharState.FollowupTell or VaeltharState.GuardRaise or VaeltharState.CounterTell or
        VaeltharState.MoveTell or VaeltharState.PatternTell or VaeltharState.FinalWindup or VaeltharState.UltimateRaise;
    public override bool IsCombatAttackActive => CombatState is VaeltharState.Active or VaeltharState.FinalActive;
    public override Rectangle ActiveAttackArea => !IsCombatAttackActive ? Rectangle.Empty :
        CombatState == VaeltharState.FinalActive && _lanes.Count > 0 && _lanes[^1].IsFinisher ?
            _lanes[^1].Area : AttackKind switch
        {
            VaeltharAttack.ThroneBreaker => ForwardArea(138, 144),
            VaeltharAttack.FinalSever => ForwardArea(205, 150),
            VaeltharAttack.KingsDomain => ForwardArea(195, 150),
            VaeltharAttack.EndOfKings => ForwardArea(200, 150),
            VaeltharAttack.AbyssArc => ForwardArea(115, 94),
            _ => ForwardArea(150, 104)
        };

    public VaeltharEnemy(Vector2 position, int level, int worldTier, int roomId)
        : base(EnemyType.Vaelthar, position, new Vector2(78, 152), 100f, 820f,
            EnemyStatScaling.Health(1880, 74, level, worldTier),
            EnemyStatScaling.Damage(37, 3, level, worldTier), 2200, level, worldTier,
            true, roomId, 176f, 0f, applyEliteModifiers: false)
    { Start(VaeltharState.Intro, 1.10f); }

    internal void ConfigureArena(DungeonMap map)
    {
        if (_arenaConfigured) return;
        Rectangle room = map.FindRoomContaining(Position)?.Bounds ?? map.WorldBounds;
        AuthoredArenaBounds = new Rectangle(room.Left + 120, room.Top + 35, room.Width - 240, room.Height - 65);
        ConfigureAuthoredLeash(AuthoredArenaBounds);
        _arenaConfigured = true;
    }

    public override void ReceiveDamageFrom(int damage, float poiseDamage, Vector2 sourcePosition)
    {
        if (IsReversing && damage > 0 && (sourcePosition.X - Position.X) * _direction >= 0 &&
            MathF.Abs(sourcePosition.Y - Position.Y) < 130 && Vector2.Distance(sourcePosition, Position) < 300)
        {
            if (!_guardEarned) ValidReversalCount++;
            _guardEarned = true; _guardSource = sourcePosition;
            damage = Math.Max(1, (int)MathF.Round(damage * .42f));
            poiseDamage *= .40f;
        }
        base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
    }

    protected override void UpdateBehavior(GameTime time, PlayerCharacter player, DungeonMap map,
        ProjectileManager projectiles, RootHazardManager rootHazards, EnemyManager enemies)
    {
        ConfigureArena(map);
        float dt = MathF.Min((float)time.ElapsedGameTime.TotalSeconds, .05f);
        _cooldown = MathF.Max(0, _cooldown - dt);
        _guardCooldown = MathF.Max(0, _guardCooldown - dt);
        _arcCooldown = MathF.Max(0, _arcCooldown - dt);
        _domainCooldown = MathF.Max(0, _domainCooldown - dt);
        if (CombatPhase == 3) _ultimateCooldown = MathF.Max(0, _ultimateCooldown - dt);
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState != VaeltharState.Intro && (distance > EngagementRange ||
            MathF.Abs(player.Position.Y - Position.Y) > 250))
        {
            Cancel(.9f);
            MoveToward(time, player.Position, 250, map, .68f);
            ConstrainToAuthoredArena(AuthoredArenaBounds);
            return;
        }
        UpdateOwnedEffects(dt, player, map);
        if (CombatState != VaeltharState.Ready)
        {
            if (AttackKind == VaeltharAttack.ThronelessRush && CombatState == VaeltharState.FollowupTell)
            {
                MoveBy(new Vector2(_direction * 70f * dt, 0), map);
                ConstrainToAuthoredArena(AuthoredArenaBounds);
            }
            UpdateMovement(dt, map);
            _remaining -= dt;
            if (_remaining <= 0) Advance(player, map);
            if (IsCombatAttackActive && !_hitConsumed && Contact(player, ActiveAttackArea,
                AttackKind is VaeltharAttack.FinalSever or VaeltharAttack.EndOfKings ? 1.42f :
                AttackKind == VaeltharAttack.ThroneBreaker ? 1.25f : 1f, map))
            { _hitConsumed = true; AttackHitCount++; }
            return;
        }
        if (CombatPhase == 1 && (long)CurrentHealth * 100 <= (long)MaxHealth * 70)
        { EnterPhase(2); return; }
        if (CombatPhase == 2 && (long)CurrentHealth * 100 < (long)MaxHealth * 35)
        { EnterPhase(3); return; }
        if (_cooldown > 0 || HasOwnedEffects) return;
        if (distance > 225)
        { MoveToward(time, player.Position, 180, map, .72f); ConstrainToAuthoredArena(AuthoredArenaBounds); return; }
        Lock(player.Position);
        int choice = _decision++;
        _comboIndex = 0; _comboLength = 1;
        if (CombatPhase == 3 && _ultimateCooldown <= 0)
        { BeginUltimate(player, map); return; }
        if (CombatPhase == 3 && _domainCooldown <= 0 && choice % 7 == 5)
        {
            AttackKind = VaeltharAttack.KingsDomain; _domainCooldown = 14f;
            PrepareMovement(new Vector2(AuthoredArenaBounds.Center.X, Position.Y), 156, map);
            Start(VaeltharState.CenterAdvance, 1.30f); return;
        }
        if (CombatPhase == 3 && choice % 6 == 4)
        { AttackKind = VaeltharAttack.ThronelessRush; _comboLength = 4; Start(VaeltharState.Windup, .68f); return; }
        if (CombatPhase == 3 && choice % 10 == 7)
        { AttackKind = VaeltharAttack.FinalSever; Start(VaeltharState.Windup, 1.38f); return; }
        if (CombatPhase >= 2 && choice % 8 == 6)
        { BeginLanes(VaeltharAttack.RoyalDecree, player, map); return; }
        if (CombatPhase >= 2 && choice % 7 == 4)
        { BeginLanes(VaeltharAttack.CrownPillar, player, map); return; }
        if (CombatPhase >= 2 && _arcCooldown <= 0 && choice % 6 == 2)
        {
            AttackKind = VaeltharAttack.AbyssArc; _arcCooldown = 6.4f;
            Start(VaeltharState.Windup, .72f); return;
        }
        if (CombatPhase >= 2 && choice % 10 == 8)
        {
            AttackKind = VaeltharAttack.KingsPursuit;
            PrepareMovement(player.Position - new Vector2(_direction * 85, 0), MaximumPursuitDistance, map);
            Start(VaeltharState.MoveTell, .60f); return;
        }
        if (_guardCooldown <= 0 && choice % 5 == 3)
        {
            AttackKind = VaeltharAttack.SovereignReversal; _guardCooldown = 7.4f; _guardEarned = false;
            Start(VaeltharState.GuardRaise, .28f); return;
        }
        if (choice % 4 == 2)
        { AttackKind = VaeltharAttack.ThroneBreaker; Start(VaeltharState.Windup, .98f); return; }
        if (choice % 4 == 1)
        {
            AttackKind = VaeltharAttack.RoyalAdvance;
            PrepareMovement(Position + new Vector2(_direction * MaximumAdvanceDistance, 0), MaximumAdvanceDistance, map);
            Start(VaeltharState.MoveTell, .52f); return;
        }
        AttackKind = VaeltharAttack.KingsCut;
        _comboLength = choice % 3 == 0 ? 2 : 3;
        Start(VaeltharState.Windup, .54f);
    }

    private void EnterPhase(int phase)
    {
        CombatPhase = phase;
        if (phase == 2) PhaseTwoTransitionCount++; else { PhaseThreeTransitionCount++; _ultimateCooldown = 12f; }
        ClearEffects(); _guardEarned = false; AttackKind = VaeltharAttack.None;
        Start(VaeltharState.PhaseTransition, .98f);
    }
    private void BeginLanes(VaeltharAttack kind, PlayerCharacter player, DungeonMap map)
    {
        AttackKind = kind; _lanes.Clear(); PatternAge = 0;
        float center = ClampX(player.Position.X, 290);
        if (kind == VaeltharAttack.CrownPillar)
        {
            AddLane(center - 108, 76, 1, 0, 1.02f, map);
            AddLane(center + 108, 76, 2, 0, 1.45f, map);
        }
        else
            for (int i = 0; i < 3; i++) AddLane(center + (i - 1) * 185,
                68, i + 1, i * .22f, 1.02f + i * .48f, map);
        Start(VaeltharState.PatternTell, .74f);
    }
    private void BeginDomain(PlayerCharacter player, DungeonMap map)
    {
        _lanes.Clear(); PatternAge = 0;
        float center = ClampX(Position.X, 320);
        for (int i = 0; i < 3; i++) AddLane(center + (i - 1) * 210,
            80, i + 1, i * .18f, 1.12f + i * .50f, map);
        Lock(player.Position);
        Start(VaeltharState.PatternTell, .90f);
    }
    private void BeginUltimate(PlayerCharacter player, DungeonMap map)
    {
        AttackKind = VaeltharAttack.EndOfKings;
        UltimateCount++; _ultimateCooldown = UltimateCooldownSeconds;
        _lanes.Clear(); PatternAge = 0;
        PrepareMovement(player.Position - new Vector2(_direction * 75, 0), MaximumPursuitDistance, map);
        float finalX = _movementEnd.X + _direction * 90;
        float center = ClampX(finalX, 320);
        // Near an arena edge, keep the displayed finisher in front of the
        // blade rather than clamping its marker behind Vaelthar's stance.
        Lock(new Vector2(center, Position.Y));
        PrepareMovement(new Vector2(center - _direction * 90, Position.Y), MaximumPursuitDistance, map);
        AddLane(center - 225, 100, 1, 0, 1.45f, map);
        AddLane(center + 225, 100, 2, 0, 2.15f, map);
        AddLane(center, 136, 3, 0, 4.35f, map, true);
        // Final target and pursuit remain fixed from the first displayed warning.
        Start(VaeltharState.UltimateRaise, 1.05f);
    }
    private void AddLane(float x, int width, int order, float reveal, float activate,
        DungeonMap map, bool final = false)
    {
        float ground = map.FindRoomContaining(Position)?.GroundY ?? Bounds.Bottom;
        _lanes.Add(new VaeltharLane(new Vector2(ClampX(x, width / 2f + 12), ground),
            width, order, reveal, activate, final));
    }
    private void PrepareMovement(Vector2 target, float maximum, DungeonMap map)
    {
        _movementStart = Position;
        float x = ClampX(Position.X + Math.Clamp(target.X - Position.X, -maximum, maximum), Size.X / 2 + 16);
        _movementEnd = SideScrollingCollision.ResolveDisplacement(Position,
            new Vector2(x - Position.X, 0), Size, map);
    }
    private void UpdateMovement(float dt, DungeonMap map)
    {
        if (CombatState is not (VaeltharState.Pursuit or VaeltharState.CenterAdvance)) return;
        float progress = Math.Clamp(ActionProgress + dt / _duration, 0, 1);
        MoveBy(Vector2.Lerp(_movementStart, _movementEnd, progress) - Position, map);
        ConstrainToAuthoredArena(AuthoredArenaBounds);
    }

    private void Advance(PlayerCharacter player, DungeonMap map)
    {
        switch (CombatState)
        {
            case VaeltharState.Intro: IntroCompletionCount++; Finish(.55f); return;
            case VaeltharState.PhaseTransition: Finish(.62f); return;
            case VaeltharState.GuardRaise: Start(VaeltharState.GuardActive, .65f); return;
            case VaeltharState.GuardActive:
                if (_guardEarned)
                { _guardEarned = false; Lock(_guardSource); CounterCount++; Start(VaeltharState.CounterTell, .52f); }
                else Start(VaeltharState.Recovery, .72f);
                return;
            case VaeltharState.MoveTell: Start(VaeltharState.Pursuit, .32f); return;
            case VaeltharState.Pursuit:
                Start(IsEndOfKings ? VaeltharState.FinalWindup : VaeltharState.Windup,
                    IsEndOfKings ? 1.04f : .32f); return;
            case VaeltharState.CenterAdvance: BeginDomain(player, map); return;
            case VaeltharState.PatternTell: Start(VaeltharState.PatternSequence, 1.62f); return;
            case VaeltharState.PatternSequence:
                if (AttackKind == VaeltharAttack.KingsDomain) Start(VaeltharState.FinalWindup, 1.05f);
                else Start(VaeltharState.Recovery, 1.05f);
                return;
            case VaeltharState.UltimateRaise: Start(VaeltharState.UltimateOmens, 1.42f); return;
            case VaeltharState.UltimateOmens: Start(VaeltharState.MoveTell, .58f); return;
            case VaeltharState.FinalWindup: Start(VaeltharState.FinalActive, .22f); return;
            case VaeltharState.Windup:
            case VaeltharState.FollowupTell:
            case VaeltharState.CounterTell:
                if (AttackKind == VaeltharAttack.AbyssArc)
                {
                    WavePosition = Position + new Vector2(_direction * 44, 12);
                    _waveDirection = _direction; _waveLife = 1.1f; _waveHit = false;
                }
                Start(VaeltharState.Active, .18f); return;
            case VaeltharState.Active:
                _comboIndex++;
                if (AttackKind is VaeltharAttack.KingsCut or VaeltharAttack.ThronelessRush && _comboIndex < _comboLength)
                {
                    // Keep the original direction throughout the finite combo.
                    Start(VaeltharState.FollowupTell, AttackKind == VaeltharAttack.ThronelessRush ? .40f : .34f);
                }
                else Start(VaeltharState.Recovery, RecoveryTime());
                return;
            case VaeltharState.FinalActive:
                _lanes.Clear(); Start(VaeltharState.Recovery, IsEndOfKings ? 2.35f : 1.65f); return;
            default: Finish(CombatPhase == 1 ? .72f : .62f); return;
        }
    }
    private float RecoveryTime() => AttackKind switch
    {
        VaeltharAttack.ThroneBreaker => 1.32f,
        VaeltharAttack.FinalSever => 1.72f,
        VaeltharAttack.ThronelessRush => 1.55f,
        VaeltharAttack.SovereignReversal => 1.04f,
        VaeltharAttack.AbyssArc => 1.00f,
        _ => CombatPhase == 1 ? .88f : .80f
    };

    private void UpdateOwnedEffects(float dt, PlayerCharacter player, DungeonMap map)
    {
        float previousAge = PatternAge; PatternAge += dt;
        foreach (VaeltharLane lane in _lanes)
        {
            if (!lane.IsFinisher && previousAge < lane.ActivateAt && PatternAge >= lane.ActivateAt)
                LaneActivationCount++;
            if (lane.IsActive(PatternAge) && !lane.Consumed && Contact(player, lane.Area, .82f, map))
            { lane.Consumed = true; LaneHitCount++; }
        }
        if (_lanes.Count > 0 && !_lanes[^1].IsFinisher && PatternAge > _lanes[^1].ActivateAt + .32f)
            _lanes.Clear();
        if (!HasWave) return;
        Vector2 previous = WavePosition;
        WavePosition += new Vector2(_waveDirection * 420f * dt, 0);
        _waveLife = MathF.Max(0, _waveLife - dt);
        int samples = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(previous, WavePosition) / 6));
        for (int i = 0; i <= samples; i++)
        {
            Vector2 p = Vector2.Lerp(previous, WavePosition, (float)i / samples);
            if (!AuthoredArenaBounds.Contains(p.ToPoint()) ||
                !SideScrollingCollision.IsPositionFree(p, new Vector2(16, 42), map))
            { _waveLife = 0; break; }
            var area = new Rectangle((int)p.X - 20, (int)p.Y - 33, 40, 66);
            if (!_waveHit && area.Intersects(player.BodyHurtbox))
            {
                player.ReceiveProjectileAttack(new AttackContact(EffectiveAttackDamage, Position, true, false, area));
                _waveHit = true; _waveLife = 0; break;
            }
        }
    }
    private bool Contact(PlayerCharacter player, Rectangle area, float multiplier, DungeonMap map)
    {
        if (area.IsEmpty || (!area.Intersects(player.BodyHurtbox) &&
            !(player.Combat.IsBlocking && area.Intersects(player.DefenseBounds)))) return false;
        if (!ThroneboundSeraphEnemy.HasClearLine(Position, player.BodyHurtbox.Center.ToVector2(), map)) return false;
        // Standard combat resolution preserves JKL control immunity, including
        // normal damage. No direct Player movement, interrupts or status writes.
        player.ReceiveMeleeAttack(new AttackContact(Math.Max(1, (int)MathF.Round(EffectiveAttackDamage * multiplier)),
            Position, true, false, area));
        return true;
    }
    private Rectangle ForwardArea(int reach, int height) => new(
        _direction < 0 ? Bounds.Left - reach : Bounds.Right,
        (int)Position.Y - height / 2, reach, height);
    private float ClampX(float x, float margin) => MathHelper.Clamp(x,
        AuthoredArenaBounds.Left + margin, AuthoredArenaBounds.Right - margin);
    private void Lock(Vector2 target)
    { FaceToward(target); _direction = target.X < Position.X ? -1 : 1; }
    private void Start(VaeltharState state, float duration)
    { CombatState = state; _remaining = _duration = duration; _hitConsumed = false; }
    private void ClearEffects() { _lanes.Clear(); _waveLife = 0; }
    private void Finish(float cooldown)
    {
        CombatState = VaeltharState.Ready; AttackKind = VaeltharAttack.None;
        _remaining = _duration = 0; _guardEarned = false;
        _cooldown = MathF.Max(_cooldown, cooldown);
    }
    private void Cancel(float cooldown)
    {
        if (CombatState == VaeltharState.Intro && IntroCompletionCount == 0) IntroCompletionCount = 1;
        ClearEffects(); Finish(cooldown); Attack.Cancel();
    }
    protected override void OnCombatInterrupted() => Cancel(.95f);
    protected override void OnDamaged()
    {
        if (IsAlive || _deathRaised) return;
        _deathRaised = true; Cancel(0); CompletionCount++;
        FinalBossDefeated?.Invoke(this);
    }
}
