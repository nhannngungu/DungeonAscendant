using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum MalacharState
{
    Intro,
    Ready,
    ReturnHome,
    PhaseTransition,
    CutOneWindup,
    CutOneActive,
    CutTwoTell,
    CutTwoActive,
    CutThreeDelay,
    CutThreeActive,
    ComboRecovery,
    AdvanceWindup,
    AdvanceStep,
    AdvanceActive,
    AdvanceRecovery,
    ReversalStance,
    ReversalPause,
    ReversalActive,
    ReversalRecovery,
    RisingWindup,
    RisingActive,
    RisingEdge,
    RisingRecovery,
    StepCompress,
    StepMove,
    StepRecovery,
    ArcWindup,
    ArcRelease,
    ArcRecovery,
    PillarGesture,
    PillarCommit,
    PillarRecovery,
    PursuitStep,
    PursuitPause,
    PursuitStrike,
    PursuitRecovery,
    BreakRaise,
    BreakPause,
    BreakActive,
    BreakRecovery,
    RushOne,
    RushTwo,
    RushStep,
    RushPause,
    RushThree,
    RushFour,
    RushRecovery,
    DominionPlant,
    DominionSequence,
    DominionSlash,
    DominionRecovery,
    EdictRaise,
    EdictPressure,
    EdictStep,
    EdictStrike,
    EdictMark,
    EdictRise,
    EdictDescend,
    EdictRecovery
}

public enum MalacharAttackKind
{
    None,
    SovereignCut,
    KingsAdvance,
    Reversal,
    RisingDivide,
    AbyssStep,
    DominionArc,
    AbyssalPillar,
    SovereignPursuit,
    SovereignBreak,
    CrownlessRush,
    FinalDominion,
    MalacharsEdict
}

/// <summary>
/// Map 6's sovereign boss. Movement is bounded on the enemy, never on the
/// player; leaving the arena makes Malachar withdraw to his authored home.
/// </summary>
public sealed class MalacharEnemy : Enemy
{
    public const float MaximumAbyssStepDistance = 270f;
    public const float ReversalWindowSeconds = .52f;
    public const int MaximumCutHits = 3;
    public const int MaximumRushHits = 4;

    private float _actionRemaining;
    private float _actionDuration;
    private float _combatCooldown = .55f;
    private float _stepCooldown;
    private float _reversalCooldown;
    private float _signatureCooldown = 5.5f;
    private float _lockedDirection = 1f;
    private int _decisionSerial;
    private int _comboHits;
    private bool _hitConsumed;
    private bool _shotReleased;
    private bool _phaseTwoPlayed;
    private bool _phaseThreePlayed;
    private bool _deathRaised;
    private Vector2 _lockedTarget;
    private Vector2 _homePosition;
    private Rectangle _arenaBounds;
    private ProjectileManager _projectiles;
    private RootHazardManager _hazards;

    public MalacharState CombatState { get; private set; } = MalacharState.Intro;
    public MalacharAttackKind AttackKind { get; private set; }
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionRemaining / _actionDuration, 0f, 1f);
    public bool AttackFacesLeft => _lockedDirection < 0f;
    public bool BossIntroCompleted { get; private set; }
    public int PhaseTransitionCount { get; private set; }
    public int BossCompletionCount { get; private set; }
    public int ReversalTriggerCount { get; private set; }
    public int ComboHitCount => _comboHits;
    public Vector2 StepDestination { get; private set; }
    public float LastAbyssStepDistance { get; private set; }
    public bool AbyssStepDealsArrivalDamage => false;
    public bool DominionArcIsHoming => false;
    public bool FinalDominionHasSafeLanes => true;
    public bool IsReturningHome => CombatState == MalacharState.ReturnHome;
    public int CombatPhase => CurrentHealth * 100 < MaxHealth * 35
        ? 3
        : CurrentHealth * 10 <= MaxHealth * 7 ? 2 : 1;

    public event Action<MalacharEnemy> SovereignDefeated;

    public override bool CanDropLoot => false;
    public override bool UsesProjectileAttack => true;
    public override bool CanBeTargeted => base.CanBeTargeted &&
        CombatState != MalacharState.Intro;
    protected override float DeathPresentationDurationSeconds => 2.05f;

    public override bool IsCombatAttackTelegraphing => CombatState is
        MalacharState.CutOneWindup or MalacharState.CutTwoTell or
        MalacharState.CutThreeDelay or MalacharState.AdvanceWindup or
        MalacharState.ReversalStance or MalacharState.ReversalPause or
        MalacharState.RisingWindup or MalacharState.StepCompress or
        MalacharState.ArcWindup or MalacharState.PillarGesture or
        MalacharState.PursuitPause or MalacharState.BreakRaise or
        MalacharState.BreakPause or MalacharState.RushPause or
        MalacharState.DominionPlant or MalacharState.DominionSequence or
        MalacharState.EdictRaise or MalacharState.EdictPressure or
        MalacharState.EdictMark or MalacharState.EdictRise;

    public override bool IsCombatAttackActive => CombatState is
        MalacharState.CutOneActive or MalacharState.CutTwoActive or
        MalacharState.CutThreeActive or MalacharState.AdvanceActive or
        MalacharState.ReversalActive or MalacharState.RisingActive or
        MalacharState.PursuitStrike or MalacharState.BreakActive or
        MalacharState.RushOne or MalacharState.RushTwo or
        MalacharState.RushThree or MalacharState.RushFour or
        MalacharState.DominionSlash or MalacharState.EdictStrike or
        MalacharState.EdictDescend;

    public override Rectangle ActiveAttackArea => CombatState switch
    {
        MalacharState.CutOneActive => ForwardArea(126, 94, -70),
        MalacharState.CutTwoActive => ForwardArea(136, 96, -72),
        MalacharState.CutThreeActive => ForwardArea(156, 104, -78),
        MalacharState.AdvanceActive => ForwardArea(146, 92, -68),
        MalacharState.ReversalActive => ForwardArea(142, 98, -74),
        MalacharState.RisingActive => ForwardArea(126, 132, -108),
        MalacharState.PursuitStrike => ForwardArea(152, 98, -74),
        MalacharState.BreakActive => ForwardArea(174, 126, -96),
        MalacharState.RushOne or MalacharState.RushTwo or
            MalacharState.RushThree => ForwardArea(136, 94, -70),
        MalacharState.RushFour => ForwardArea(168, 96, -72),
        MalacharState.DominionSlash => ForwardArea(184, 112, -84),
        MalacharState.EdictStrike => ForwardArea(158, 104, -78),
        MalacharState.EdictDescend => new Rectangle(
            (int)Position.X - 104, Bounds.Bottom - 72, 208, 88),
        _ => Rectangle.Empty
    };

    public MalacharEnemy(
        Vector2 position,
        int level,
        int worldTier,
        int roomId)
        : base(
            EnemyType.Malachar,
            position,
            new Vector2(68f, 126f),
            96f,
            920f,
            EnemyStatScaling.Health(760, 62, level, worldTier),
            EnemyStatScaling.Damage(28, 3, level, worldTier),
            1100,
            level,
            worldTier,
            isElite: true,
            roomId,
            attackRange: 144f,
            attackCooldownSeconds: 0f,
            applyEliteModifiers: false)
    {
        _homePosition = position;
        Start(MalacharState.Intro, 1.18f, MalacharAttackKind.None);
    }

    public void SetAuthoredArenaBounds(Rectangle bounds)
    {
        if (bounds == Rectangle.Empty)
            return;
        _arenaBounds = bounds;
        _homePosition = new Vector2(
            MathHelper.Clamp(_homePosition.X,
                bounds.Left + 100f, bounds.Right - 100f),
            _homePosition.Y);
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
        _projectiles = projectiles;
        _hazards = rootHazards;
        _combatCooldown = MathF.Max(0f, _combatCooldown - elapsed);
        _stepCooldown = MathF.Max(0f, _stepCooldown - elapsed);
        _reversalCooldown = MathF.Max(0f, _reversalCooldown - elapsed);
        _signatureCooldown = MathF.Max(0f, _signatureCooldown - elapsed);

        if (ShouldReturnHome(player.Position))
        {
            UpdateReturnHome(gameTime, dungeon);
            return;
        }

        if (CombatState == MalacharState.ReturnHome)
            Finish(.72f);

        if (CombatState != MalacharState.Ready)
        {
            UpdateCommittedMovement(dungeon, elapsed);
            _actionRemaining -= elapsed;
            if (IsCombatAttackActive)
                ResolveContact(player, dungeon);
            if (_actionRemaining <= 0f)
                Advance(player, dungeon);
            return;
        }

        if (!IsPlayerDetected(player.Position))
            return;
        if (BeginRequiredPhaseTransition())
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (_combatCooldown > 0f)
        {
            if (distance > Attack.Range + 34f)
                MoveToward(gameTime, player.Position, Attack.Range + 18f,
                    dungeon, CombatPhase == 3 ? .84f : .66f);
            return;
        }

        LockDirection(player.Position);
        ChooseAttack(player, dungeon, distance);
    }

    private bool ShouldReturnHome(Vector2 playerPosition) =>
        _arenaBounds != Rectangle.Empty &&
        !_arenaBounds.Contains(playerPosition.ToPoint());

    private void UpdateReturnHome(GameTime gameTime, DungeonMap dungeon)
    {
        if (CombatState != MalacharState.ReturnHome)
        {
            ClearOwnedEffects();
            Start(MalacharState.ReturnHome, 0f, MalacharAttackKind.None);
        }
        if (MathF.Abs(Position.X - _homePosition.X) <= 10f)
        {
            Finish(.8f);
            return;
        }
        MoveToward(gameTime, _homePosition, 8f, dungeon, .72f);
    }

    private bool BeginRequiredPhaseTransition()
    {
        if (CombatPhase >= 2 && !_phaseTwoPlayed)
        {
            _phaseTwoPlayed = true;
            PhaseTransitionCount++;
            ClearOwnedEffects();
            Start(MalacharState.PhaseTransition, .92f,
                MalacharAttackKind.None);
            return true;
        }
        if (CombatPhase >= 3 && !_phaseThreePlayed)
        {
            _phaseThreePlayed = true;
            PhaseTransitionCount++;
            ClearOwnedEffects();
            Start(MalacharState.PhaseTransition, 1.02f,
                MalacharAttackKind.None);
            return true;
        }
        return false;
    }

    private void ChooseAttack(
        PlayerCharacter player,
        DungeonMap dungeon,
        float distance)
    {
        int choice = _decisionSerial++;
        if (CombatPhase >= 2 && _signatureCooldown <= 0f && choice % 11 == 8)
        {
            BeginEdict(player, dungeon);
            return;
        }
        if (CombatPhase == 3 && choice % 8 == 6)
        {
            BeginFinalDominion(player);
            return;
        }
        if (CombatPhase == 3 && choice % 7 == 4)
        {
            Start(MalacharState.BreakRaise, .58f,
                MalacharAttackKind.SovereignBreak);
            return;
        }
        if (CombatPhase == 3 && choice % 6 == 3)
        {
            Start(MalacharState.RushOne, .16f,
                MalacharAttackKind.CrownlessRush);
            return;
        }
        if (CombatPhase >= 2 && choice % 7 == 2)
        {
            _lockedTarget = player.Position;
            Start(MalacharState.PillarGesture, .62f,
                MalacharAttackKind.AbyssalPillar);
            return;
        }
        if (CombatPhase >= 2 && choice % 6 == 5)
        {
            Start(MalacharState.ArcWindup, .48f,
                MalacharAttackKind.DominionArc);
            return;
        }
        if (CombatPhase >= 2 && _stepCooldown <= 0f &&
            distance > 210f && choice % 4 == 1)
        {
            BeginStep(player.Position, dungeon,
                MalacharAttackKind.SovereignPursuit, pursuit: true);
            return;
        }
        if (_reversalCooldown <= 0f && distance < 170f && choice % 6 == 2)
        {
            _reversalCooldown = 6.4f;
            Start(MalacharState.ReversalStance, ReversalWindowSeconds,
                MalacharAttackKind.Reversal);
            return;
        }
        if (_stepCooldown <= 0f && distance > 250f && choice % 5 == 3)
        {
            BeginStep(player.Position, dungeon,
                MalacharAttackKind.AbyssStep, pursuit: false);
            return;
        }
        if (distance > 185f)
        {
            Start(MalacharState.AdvanceWindup, .34f,
                MalacharAttackKind.KingsAdvance);
            return;
        }
        if (choice % 5 == 4)
        {
            Start(MalacharState.RisingWindup, .46f,
                MalacharAttackKind.RisingDivide);
            return;
        }
        _comboHits = 0;
        Start(MalacharState.CutOneWindup, .30f,
            MalacharAttackKind.SovereignCut);
    }

    private void BeginStep(
        Vector2 target,
        DungeonMap dungeon,
        MalacharAttackKind kind,
        bool pursuit)
    {
        _stepCooldown = CombatPhase == 3 ? 3.4f : 4.2f;
        float direction = MathF.Sign(target.X - Position.X);
        if (direction == 0f)
            direction = _lockedDirection;
        float requested = pursuit ? 250f : 220f;
        float targetX = Position.X + direction * requested;
        Rectangle bounds = _arenaBounds != Rectangle.Empty
            ? _arenaBounds
            : dungeon.FindRoomContaining(Position)?.Bounds ?? dungeon.WorldBounds;
        targetX = MathHelper.Clamp(targetX,
            bounds.Left + Size.X / 2f + 30f,
            bounds.Right - Size.X / 2f - 30f);
        StepDestination = SideScrollingCollision.ResolveSafeEntrySpawn(
            targetX, Size, dungeon, 0f, 0f);
        LastAbyssStepDistance = MathF.Min(
            MathF.Abs(StepDestination.X - Position.X),
            MaximumAbyssStepDistance);
        Start(pursuit ? MalacharState.PursuitStep : MalacharState.StepCompress,
            pursuit ? .30f : .22f, kind);
    }

    private void BeginFinalDominion(PlayerCharacter player)
    {
        _lockedTarget = player.Position;
        ClearOwnedEffects();
        Start(MalacharState.DominionPlant, .78f,
            MalacharAttackKind.FinalDominion);
    }

    private void BeginEdict(PlayerCharacter player, DungeonMap dungeon)
    {
        _signatureCooldown = CombatPhase == 3 ? 9.5f : 11.5f;
        _lockedTarget = player.Position;
        PrepareStepNear(player.Position, dungeon, 230f);
        ClearOwnedEffects();
        Start(MalacharState.EdictRaise, .68f,
            MalacharAttackKind.MalacharsEdict);
    }

    private void PrepareStepNear(Vector2 target, DungeonMap dungeon, float distance)
    {
        float direction = MathF.Sign(target.X - Position.X);
        if (direction == 0f)
            direction = _lockedDirection;
        Rectangle bounds = _arenaBounds != Rectangle.Empty
            ? _arenaBounds
            : dungeon.FindRoomContaining(Position)?.Bounds ?? dungeon.WorldBounds;
        float targetX = MathHelper.Clamp(Position.X + direction * distance,
            bounds.Left + 70f, bounds.Right - 70f);
        StepDestination = SideScrollingCollision.ResolveSafeEntrySpawn(
            targetX, Size, dungeon, 0f, 0f);
        LastAbyssStepDistance = MathF.Min(
            MathF.Abs(StepDestination.X - Position.X),
            MaximumAbyssStepDistance);
    }

    private void UpdateCommittedMovement(DungeonMap dungeon, float elapsed)
    {
        if (CombatState is MalacharState.AdvanceStep or
            MalacharState.AdvanceActive)
        {
            MoveLocked(2.05f, elapsed, dungeon);
        }
        else if (CombatState is MalacharState.StepMove or
            MalacharState.PursuitStep or MalacharState.RushStep or
            MalacharState.EdictStep)
        {
            MoveTowardStep(dungeon, elapsed);
        }
        else if (CombatState is MalacharState.RushOne or
            MalacharState.RushTwo or MalacharState.RushThree)
        {
            MoveLocked(1.25f, elapsed, dungeon);
        }
    }

    private void Advance(PlayerCharacter player, DungeonMap dungeon)
    {
        switch (CombatState)
        {
            case MalacharState.Intro:
                BossIntroCompleted = true;
                Finish(.55f);
                break;
            case MalacharState.PhaseTransition:
                Finish(.58f);
                break;
            case MalacharState.CutOneWindup:
                _comboHits = 1;
                Start(MalacharState.CutOneActive, .14f, AttackKind);
                break;
            case MalacharState.CutOneActive:
                Start(MalacharState.CutTwoTell, .24f, AttackKind);
                break;
            case MalacharState.CutTwoTell:
                _comboHits = 2;
                Start(MalacharState.CutTwoActive, .14f, AttackKind);
                break;
            case MalacharState.CutTwoActive:
                if ((_decisionSerial + CombatPhase) % 3 == 0)
                    Start(MalacharState.CutThreeDelay, .38f, AttackKind);
                else
                    Start(MalacharState.ComboRecovery, .58f, AttackKind);
                break;
            case MalacharState.CutThreeDelay:
                _comboHits = 3;
                Start(MalacharState.CutThreeActive, .16f, AttackKind);
                break;
            case MalacharState.CutThreeActive:
                Start(MalacharState.ComboRecovery, .72f, AttackKind);
                break;
            case MalacharState.ComboRecovery:
                Finish(.40f);
                break;
            case MalacharState.AdvanceWindup:
                Start(MalacharState.AdvanceStep, .20f, AttackKind);
                break;
            case MalacharState.AdvanceStep:
                Start(MalacharState.AdvanceActive, .15f, AttackKind);
                break;
            case MalacharState.AdvanceActive:
                Start(MalacharState.AdvanceRecovery, .56f, AttackKind);
                break;
            case MalacharState.AdvanceRecovery:
                Finish(.42f);
                break;
            case MalacharState.ReversalStance:
                Start(MalacharState.ReversalRecovery, .42f, AttackKind);
                break;
            case MalacharState.ReversalPause:
                Start(MalacharState.ReversalActive, .16f, AttackKind);
                break;
            case MalacharState.ReversalActive:
                Start(MalacharState.ReversalRecovery, .62f, AttackKind);
                break;
            case MalacharState.ReversalRecovery:
                Finish(.48f);
                break;
            case MalacharState.RisingWindup:
                Start(MalacharState.RisingActive, .17f, AttackKind);
                break;
            case MalacharState.RisingActive:
                ReleaseRisingEdge();
                Start(MalacharState.RisingEdge, .12f, AttackKind);
                break;
            case MalacharState.RisingEdge:
                Start(MalacharState.RisingRecovery, .62f, AttackKind);
                break;
            case MalacharState.RisingRecovery:
                Finish(.44f);
                break;
            case MalacharState.StepCompress:
                Start(MalacharState.StepMove, .28f, AttackKind);
                break;
            case MalacharState.StepMove:
                Start(MalacharState.StepRecovery, .42f, AttackKind);
                break;
            case MalacharState.StepRecovery:
                Finish(.46f);
                break;
            case MalacharState.ArcWindup:
                ReleaseDominionArc();
                Start(MalacharState.ArcRelease, .12f, AttackKind);
                break;
            case MalacharState.ArcRelease:
                Start(MalacharState.ArcRecovery, .70f, AttackKind);
                break;
            case MalacharState.ArcRecovery:
                Finish(.48f);
                break;
            case MalacharState.PillarGesture:
                CreatePillar(dungeon);
                Start(MalacharState.PillarCommit, .20f, AttackKind);
                break;
            case MalacharState.PillarCommit:
                Start(MalacharState.PillarRecovery, .66f, AttackKind);
                break;
            case MalacharState.PillarRecovery:
                Finish(.46f);
                break;
            case MalacharState.PursuitStep:
                Start(MalacharState.PursuitPause, .30f, AttackKind);
                break;
            case MalacharState.PursuitPause:
                Start(MalacharState.PursuitStrike, .16f, AttackKind);
                break;
            case MalacharState.PursuitStrike:
                Start(MalacharState.PursuitRecovery, .64f, AttackKind);
                break;
            case MalacharState.PursuitRecovery:
                Finish(.48f);
                break;
            case MalacharState.BreakRaise:
                Start(MalacharState.BreakPause, .34f, AttackKind);
                break;
            case MalacharState.BreakPause:
                Start(MalacharState.BreakActive, .18f, AttackKind);
                break;
            case MalacharState.BreakActive:
                Start(MalacharState.BreakRecovery, .96f, AttackKind);
                break;
            case MalacharState.BreakRecovery:
                Finish(.58f);
                break;
            case MalacharState.RushOne:
                Start(MalacharState.RushTwo, .16f, AttackKind);
                break;
            case MalacharState.RushTwo:
                PrepareStepNear(player.Position, dungeon, 185f);
                Start(MalacharState.RushStep, .24f, AttackKind);
                break;
            case MalacharState.RushStep:
                Start(MalacharState.RushPause, .28f, AttackKind);
                break;
            case MalacharState.RushPause:
                Start(MalacharState.RushThree, .15f, AttackKind);
                break;
            case MalacharState.RushThree:
                Start(MalacharState.RushFour, .17f, AttackKind);
                break;
            case MalacharState.RushFour:
                Start(MalacharState.RushRecovery, .88f, AttackKind);
                break;
            case MalacharState.RushRecovery:
                Finish(.58f);
                break;
            case MalacharState.DominionPlant:
                CreateDominionLanes(dungeon);
                Start(MalacharState.DominionSequence, 1.62f, AttackKind);
                break;
            case MalacharState.DominionSequence:
                Start(MalacharState.DominionSlash, .20f, AttackKind);
                break;
            case MalacharState.DominionSlash:
                Start(MalacharState.DominionRecovery, 1.02f, AttackKind);
                break;
            case MalacharState.DominionRecovery:
                Finish(.62f);
                break;
            case MalacharState.EdictRaise:
                CreateEdictSeals(dungeon);
                Start(MalacharState.EdictPressure, .74f, AttackKind);
                break;
            case MalacharState.EdictPressure:
                Start(MalacharState.EdictStep, .28f, AttackKind);
                break;
            case MalacharState.EdictStep:
                Start(MalacharState.EdictStrike, .17f, AttackKind);
                break;
            case MalacharState.EdictStrike:
                Start(MalacharState.EdictMark, .42f, AttackKind);
                break;
            case MalacharState.EdictMark:
                Start(MalacharState.EdictRise, .34f, AttackKind);
                break;
            case MalacharState.EdictRise:
                Start(MalacharState.EdictDescend, .18f, AttackKind);
                break;
            case MalacharState.EdictDescend:
                Start(MalacharState.EdictRecovery, 1.06f, AttackKind);
                break;
            case MalacharState.EdictRecovery:
                Finish(.70f);
                break;
        }
    }

    private void ResolveContact(PlayerCharacter player, DungeonMap dungeon)
    {
        if (_hitConsumed || !ActiveAttackArea.Intersects(player.Bounds))
            return;
        int damage = EffectiveAttackDamage;
        float push = 0f;
        if (CombatState is MalacharState.CutThreeActive or
            MalacharState.RushFour or MalacharState.DominionSlash)
            damage = damage * 6 / 5;
        if (CombatState is MalacharState.BreakActive or
            MalacharState.EdictDescend)
        {
            damage = damage * 3 / 2;
            push = 38f;
        }
        AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
            Math.Max(1, damage), Position, blockable: true,
            unblockable: false, ActiveAttackArea));
        if (push > 0f && result is (AttackResolution.Damaged or
            AttackResolution.GuardBroken))
        {
            player.PushAwayFrom(Position, push, dungeon);
        }
        if (result == AttackResolution.PerfectGuard)
            ApplyPoiseDamage(MaxPoise * .18f);
        _hitConsumed = true;
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        if (CombatState == MalacharState.ReversalStance && IsAlive)
        {
            LockDirection(sourcePosition);
            ReversalTriggerCount++;
            Start(MalacharState.ReversalPause, .24f,
                MalacharAttackKind.Reversal);
            return;
        }
        base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
    }

    private void ReleaseRisingEdge()
    {
        if (_shotReleased || _projectiles == null)
            return;
        _projectiles.SpawnDominionArc(
            Position + new Vector2(_lockedDirection * 32f, -54f),
            new Vector2(_lockedDirection, -.18f),
            Math.Max(1, EffectiveAttackDamage * 3 / 5), RoomId,
            empoweredVisual: false);
        _shotReleased = true;
    }

    private void ReleaseDominionArc()
    {
        if (_shotReleased || _projectiles == null)
            return;
        _projectiles.SpawnDominionArc(
            Position + new Vector2(_lockedDirection * 38f, -58f),
            new Vector2(_lockedDirection, -.04f),
            Math.Max(1, EffectiveAttackDamage * 4 / 5), RoomId,
            empoweredVisual: CombatPhase == 3);
        _shotReleased = true;
    }

    private void CreatePillar(DungeonMap dungeon)
    {
        Vector2 ground = GroundAt(_lockedTarget.X, dungeon);
        _hazards?.TryAddMalacharHazard(
            ground + new Vector2(0f, -76f), new Vector2(104f, 166f),
            .82f, .48f, Math.Max(1, EffectiveAttackDamage * 4 / 5),
            RoomId, this, RootHazardVisualKind.AbyssalPillar, dungeon);
    }

    private void CreateDominionLanes(DungeonMap dungeon)
    {
        Rectangle bounds = _arenaBounds != Rectangle.Empty
            ? _arenaBounds
            : dungeon.FindRoomContaining(Position)?.Bounds ?? dungeon.WorldBounds;
        float center = bounds.Center.X;
        float[] offsets = { -570f, -190f, 190f, 570f };
        for (int index = 0; index < offsets.Length; index++)
        {
            Vector2 ground = GroundAt(center + offsets[index], dungeon);
            _hazards?.TryAddMalacharHazard(
                ground + new Vector2(0f, 5f), new Vector2(190f, 48f),
                .78f + index * .18f, .40f,
                Math.Max(1, EffectiveAttackDamage), RoomId, this,
                RootHazardVisualKind.DominionLane, dungeon);
        }
    }

    private void CreateEdictSeals(DungeonMap dungeon)
    {
        float[] offsets = { -250f, 0f, 250f };
        for (int index = 0; index < offsets.Length; index++)
        {
            Vector2 ground = GroundAt(_lockedTarget.X + offsets[index], dungeon);
            _hazards?.TryAddMalacharHazard(
                ground + new Vector2(0f, 5f), new Vector2(118f, 42f),
                .70f + index * .42f, .38f,
                Math.Max(1, EffectiveAttackDamage * 3 / 4), RoomId, this,
                RootHazardVisualKind.EdictSeal, dungeon);
        }
    }

    private Vector2 GroundAt(float x, DungeonMap dungeon)
    {
        Rectangle bounds = _arenaBounds != Rectangle.Empty
            ? _arenaBounds
            : dungeon.FindRoomContaining(Position)?.Bounds ?? dungeon.WorldBounds;
        float safeX = MathHelper.Clamp(x, bounds.Left + 70f, bounds.Right - 70f);
        return SideScrollingCollision.ResolveSafeEntrySpawn(
            safeX, new Vector2(40f, 20f), dungeon, 0f, 0f);
    }

    private void MoveTowardStep(DungeonMap dungeon, float elapsed)
    {
        Vector2 offset = StepDestination - Position;
        float length = MathF.Abs(offset.X);
        if (length <= .5f)
            return;
        float amount = MathF.Min(length, 920f * elapsed);
        Vector2 desired = new(MathF.Sign(offset.X) * amount, 0f);
        Vector2 resolved = SideScrollingCollision.ResolveDisplacement(
            Position, desired, Size, dungeon);
        if (SideScrollingCollision.IsSupported(resolved, Size, dungeon))
            MoveBy(resolved - Position, dungeon);
    }

    private void MoveLocked(float scale, float elapsed, DungeonMap dungeon)
    {
        Vector2 desired = new(
            _lockedDirection * EffectiveMovementSpeed * scale * elapsed, 0f);
        Vector2 resolved = SideScrollingCollision.ResolveDisplacement(
            Position, desired, Size, dungeon);
        if (SideScrollingCollision.IsSupported(resolved, Size, dungeon))
            MoveBy(resolved - Position, dungeon);
    }

    private Rectangle ForwardArea(int reach, int height, int yOffset)
    {
        int y = (int)Position.Y + yOffset;
        return _lockedDirection < 0f
            ? new Rectangle(Bounds.Left - reach + 16, y, reach, height)
            : new Rectangle(Bounds.Right - 16, y, reach, height);
    }

    private void LockDirection(Vector2 target)
    {
        float offset = target.X - Position.X;
        if (MathF.Abs(offset) > 2f)
            _lockedDirection = MathF.Sign(offset);
        FaceToward(target);
    }

    private void Start(
        MalacharState state,
        float duration,
        MalacharAttackKind attack)
    {
        CombatState = state;
        AttackKind = attack;
        _actionDuration = duration;
        _actionRemaining = duration;
        _hitConsumed = false;
        _shotReleased = false;
    }

    private void Finish(float cooldown)
    {
        CombatState = MalacharState.Ready;
        AttackKind = MalacharAttackKind.None;
        _actionDuration = 0f;
        _actionRemaining = 0f;
        _hitConsumed = false;
        _shotReleased = false;
        float cadence = CombatPhase switch
        {
            3 => .76f,
            2 => .88f,
            _ => 1f
        };
        _combatCooldown = MathF.Max(_combatCooldown, cooldown * cadence);
    }

    private void ClearOwnedEffects()
    {
        _hazards?.RemoveOwnedBy(this);
        _projectiles?.ClearEnemyProjectilesInRoom(RoomId);
    }

    protected override void OnDamaged()
    {
        if (!IsAlive)
        {
            if (!_deathRaised)
            {
                _deathRaised = true;
                ClearOwnedEffects();
                BossCompletionCount++;
                SovereignDefeated?.Invoke(this);
            }
            Finish(0f);
            return;
        }
        if (IsStaggered && CombatState != MalacharState.PhaseTransition)
        {
            ClearOwnedEffects();
            Finish(.72f);
        }
    }

    protected override void OnCombatInterrupted()
    {
        if (CombatState is MalacharState.PhaseTransition or
            MalacharState.ReversalPause or MalacharState.ReversalActive)
        {
            return;
        }
        ClearOwnedEffects();
        Finish(.72f);
    }
}
