using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum SkeletonAttackKind
{
    None,
    RustedSlash,
    ReturnCut,
    OverheadChop
}

public enum RottenCorpseAttackKind
{
    None,
    Grab,
    BodySlam,
    RotVomit
}

public enum WraithAttackKind
{
    None,
    SpectralClaw,
    SpectralDash,
    CurseWave
}

public enum WraithPhase
{
    Ethereal,
    Materializing,
    Attacking,
    Recovering,
    Fading
}

public enum UndeadGuardAttackKind
{
    None,
    GuardedStrike,
    ShieldBash,
    HeavyCleave
}

public enum GraveBatAttackKind
{
    None,
    GraveScreech,
    ClawPass,
    SwarmFeint
}

public enum CursedKnightAttackKind
{
    None,
    CursedSweep,
    ExecutionStrike,
    AdvancingThrust
}

public enum DeathKnightAttackKind
{
    None,
    WarSweep,
    ExecutionCrush,
    DreadCharge,
    TombbreakerSlam
}

public enum FallenKnightAttackKind
{
    None,
    RoyalSlash,
    GuardedCounter,
    AdvancingThrust,
    HeavyOverhead,
    CursedExtensionSlash,
    GraveStep,
    CursedCounter,
    FinalOathCleave,
    OathbreakerRush,
    LastJudgment
}

public enum SoulCollectorAttackKind
{
    None,
    SoulBolt,
    RitualCurseField,
    GravePull,
    SoulBurst
}

public enum CatacombCombatState
{
    Ready,
    MeleeWindup,
    MeleeActive,
    MeleeRecovery,
    ArcherNock,
    ArcherDraw,
    ArcherRelease,
    ArcherRecovery,
    BowShoveWindup,
    BowShoveActive,
    BowShoveRecovery,
    CorpseWindup,
    CorpseActive,
    CorpseRecovery,
    WraithEthereal,
    WraithMaterialize,
    WraithWindup,
    WraithActive,
    WraithRecovery,
    WraithFade,
    GuardApproach,
    GuardStance,
    GuardWindup,
    GuardActive,
    GuardRecovery,
    GuardBroken,
    GraveHover,
    GraveCircle,
    GraveWindup,
    GraveActive,
    GraveRecovery,
    GraveRetreat,
    KnightDormant,
    KnightApproach,
    KnightReady,
    KnightWindup,
    KnightActive,
    KnightRecovery,
    KnightTransition,
    SoulApproach,
    SoulReady,
    SoulWindup,
    SoulActive,
    SoulRecovery
}

/// <summary>
/// Data-driven Catacomb combatant. Skeleton and Skeleton Archer own authored
/// combat clocks so their procedural poses, hit windows, and projectile
/// releases all read from the same state. The remaining Map 2 roster keeps its
/// existing compact behavior until its own polish pass.
/// </summary>
public sealed class CatacombEnemy : Enemy
{
    public const float SlashWindupSeconds = .42f;
    public const float SlashActiveSeconds = .12f;
    public const float SlashRecoverySeconds = .34f;
    public const float ReturnCutWindupSeconds = .24f;
    public const float ReturnCutActiveSeconds = .10f;
    public const float ReturnCutRecoverySeconds = .40f;
    public const float OverheadWindupSeconds = .68f;
    public const float OverheadActiveSeconds = .13f;
    public const float OverheadRecoverySeconds = .58f;
    public const float ArcherNockSeconds = .28f;
    public const float StandardDrawSeconds = .48f;
    public const float HeavyDrawSeconds = .86f;
    public const float ArcherReleaseSeconds = .08f;
    public const float StandardRecoverySeconds = .42f;
    public const float HeavyRecoverySeconds = .62f;
    public const float BowShoveWindupSeconds = .25f;
    public const float BowShoveActiveSeconds = .10f;
    public const float BowShoveRecoverySeconds = .36f;
    public const float GrabWindupSeconds = .62f;
    public const float GrabActiveSeconds = .16f;
    public const float GrabRecoverySeconds = .58f;
    public const float BodySlamWindupSeconds = .82f;
    public const float BodySlamActiveSeconds = .18f;
    public const float BodySlamRecoverySeconds = .72f;
    public const float RotVomitWindupSeconds = .74f;
    public const float RotVomitActiveSeconds = .28f;
    public const float RotVomitRecoverySeconds = .68f;
    public const float WraithEtherealSeconds = 1.25f;
    public const float WraithMaterializeSeconds = .48f;
    public const float WraithFadeSeconds = .34f;
    public const float ClawWindupSeconds = .42f;
    public const float ClawActiveSeconds = .13f;
    public const float ClawRecoverySeconds = .48f;
    public const float DashWindupSeconds = .56f;
    public const float DashActiveSeconds = .22f;
    public const float DashRecoverySeconds = .58f;
    public const float CurseWaveWindupSeconds = .68f;
    public const float CurseWaveActiveSeconds = .20f;
    public const float CurseWaveRecoverySeconds = .66f;
    public const float GuardStanceSeconds = .42f;
    public const float GuardedStrikeWindupSeconds = .38f;
    public const float GuardedStrikeActiveSeconds = .14f;
    public const float GuardedStrikeRecoverySeconds = .42f;
    public const float ShieldBashWindupSeconds = .56f;
    public const float ShieldBashActiveSeconds = .14f;
    public const float ShieldBashRecoverySeconds = .58f;
    public const float HeavyGuardWindupSeconds = .82f;
    public const float HeavyGuardActiveSeconds = .17f;
    public const float HeavyGuardRecoverySeconds = .78f;
    public const float GuardBreakSeconds = 1.25f;
    public const float GraveHoverSeconds = .58f;
    public const float GraveCircleSeconds = .76f;
    public const float GraveScreechWindupSeconds = .62f;
    public const float GraveScreechActiveSeconds = .18f;
    public const float GraveScreechRecoverySeconds = .52f;
    public const float GraveClawWindupSeconds = .30f;
    public const float GraveClawActiveSeconds = .24f;
    public const float GraveClawRecoverySeconds = .36f;
    public const float GraveFeintWindupSeconds = .22f;
    public const float GraveFeintActiveSeconds = .18f;
    public const float GraveFeintRecoverySeconds = .28f;
    public const float GraveRetreatSeconds = .62f;
    public const float KnightDormantSeconds = .72f;
    public const float KnightReadySeconds = .38f;
    public const float SweepWindupSeconds = .66f;
    public const float SweepActiveSeconds = .18f;
    public const float SweepRecoverySeconds = .64f;
    public const float ExecutionWindupSeconds = 1.02f;
    public const float ExecutionActiveSeconds = .18f;
    public const float ExecutionRecoverySeconds = 1.05f;
    public const float ThrustWindupSeconds = .58f;
    public const float ThrustActiveSeconds = .26f;
    public const float ThrustRecoverySeconds = .72f;
    public const float CursedArmorBreakSeconds = 1.18f;
    public const float DeathSweepWindupSeconds = .78f;
    public const float DeathSweepActiveSeconds = .20f;
    public const float DeathSweepRecoverySeconds = .84f;
    public const float DeathCrushWindupSeconds = 1.18f;
    public const float DeathCrushActiveSeconds = .20f;
    public const float DeathCrushRecoverySeconds = 1.12f;
    public const float DeathChargeWindupSeconds = .72f;
    public const float DeathChargeActiveSeconds = .48f;
    public const float DeathChargeRecoverySeconds = .92f;
    public const float TombbreakerWindupSeconds = .96f;
    public const float TombbreakerActiveSeconds = .22f;
    public const float TombbreakerRecoverySeconds = 1.02f;
    public const float FallenPhaseTwoTransitionSeconds = .92f;
    public const float FallenPhaseThreeTransitionSeconds = 1.08f;
    public const float SoulReadySeconds = .44f;
    public const float SoulBoltWindupSeconds = .62f;
    public const float SoulBoltRecoverySeconds = .72f;
    public const float RitualFieldWindupSeconds = 1.0f;
    public const float RitualFieldRecoverySeconds = .82f;
    public const float GravePullWindupSeconds = .88f;
    public const float GravePullRecoverySeconds = .78f;
    public const float SoulBurstWindupSeconds = .48f;
    public const float SoulBurstRecoverySeconds = .70f;
    public const float SoulActionActiveSeconds = .16f;
    public const float RitualFieldDurationSeconds = 3.4f;

    private float _specialCooldown;
    private float _actionTimeRemaining;
    private float _actionDuration;
    private float _forcedRetreatRemaining;
    private bool _actionHitConsumed;
    private bool _returnCutQueued;
    private int _meleePatternIndex;
    private int _shotPatternIndex;
    private int _rottenPatternIndex;
    private int _wraithPatternIndex;
    private float _dashDistanceRemaining;
    private float _pendingCursePressure;
    private float _guardIntegrity = 72f;
    private float _guardBlockReactionRemaining;
    private int _guardPatternIndex;
    private int _graveBatPatternIndex;
    private float _graveBatPressureRemaining;
    private float _graveBatFlightDirection = 1f;
    private float _lastKnownGroundY = float.NaN;
    private int _knightPatternIndex;
    private int _deathKnightPatternIndex;
    private int _fallenKnightPatternIndex;
    private int _activeFallenPhase = 1;
    private float _lockedAttackDirection = 1f;
    private bool _counterTriggered;
    private FallenKnightAttackKind _lastFallenKnightAttack;
    private int _soulPatternIndex;
    private float _cursedArmorBreakRemaining;
    private float _soulFieldTimeRemaining;
    private Rectangle _soulFieldBounds;
    private Rectangle _ritualPreviewBounds;

    public bool IsEthereal { get; private set; }
    public WraithPhase SpectralPhase { get; private set; }
    public RottenCorpseAttackKind RottenCorpseAttack { get; private set; }
    public WraithAttackKind WraithAttack { get; private set; }
    public UndeadGuardAttackKind UndeadGuardAttack { get; private set; }
    public GraveBatAttackKind GraveBatAttack { get; private set; }
    public CursedKnightAttackKind CursedKnightAttack { get; private set; }
    public DeathKnightAttackKind DeathKnightAttack { get; private set; }
    public FallenKnightAttackKind FallenKnightAttack { get; private set; }
    public SoulCollectorAttackKind SoulCollectorAttack { get; private set; }
    public int CombatPhase => Type == EnemyType.FallenKnight
        ? CurrentHealth * 100 > MaxHealth * 65 ? 1
            : CurrentHealth * 100 > MaxHealth * 30 ? 2 : 3
        : 1;
    public override bool IsFlying => Type == EnemyType.GraveBat;
    public override bool CanBeTargeted => base.CanBeTargeted && !IsEthereal;
    public int ActiveFallenPhase => Type == EnemyType.FallenKnight
        ? _activeFallenPhase
        : 1;
    public bool IsPhaseTransitioning => Type == EnemyType.FallenKnight &&
        CombatState == CatacombCombatState.KnightTransition;
    public float LockedAttackDirection => _lockedAttackDirection;

    public CatacombCombatState CombatState { get; private set; }
    public SkeletonAttackKind SkeletonAttack { get; private set; }
    public bool IsHeavyArrow { get; private set; }
    public bool IsRetreating { get; private set; }
    public bool IsGuardBroken => Type == EnemyType.UndeadGuard &&
        CombatState == CatacombCombatState.GuardBroken;
    public bool IsGuardBlockReacting => _guardBlockReactionRemaining > 0f;
    public bool IsGuarding => Type == EnemyType.UndeadGuard &&
        !IsGuardBroken &&
        (CombatState is CatacombCombatState.Ready or
                CatacombCombatState.GuardApproach or
                CatacombCombatState.GuardStance or
                CatacombCombatState.GuardWindup ||
            CombatState == CatacombCombatState.GuardActive &&
                UndeadGuardAttack == UndeadGuardAttackKind.GuardedStrike);
    public float GuardIntegrityRatio => Math.Clamp(_guardIntegrity / 72f, 0f, 1f);
    public float GraveBatStaminaPressureMultiplier =>
        _graveBatPressureRemaining > 0f ? .64f : 1f;
    public float GraveBatDeathLandingY => _lastKnownGroundY;
    public bool IsCursedArmorBroken => _cursedArmorBreakRemaining > 0f;
    public float CursedArmorBreakRatio => Math.Clamp(
        _cursedArmorBreakRemaining / CursedArmorBreakSeconds, 0f, 1f);
    public bool HasActiveSoulField => _soulFieldTimeRemaining > 0f;
    public Rectangle SoulFieldBounds => _soulFieldBounds;
    public Rectangle RitualPreviewBounds => _ritualPreviewBounds;
    public float SoulFieldRatio => Math.Clamp(
        _soulFieldTimeRemaining / RitualFieldDurationSeconds, 0f, 1f);
    public int EquipmentVariant { get; }
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool IsPolishedSkeleton => Type is
        EnemyType.Skeleton or EnemyType.SkeletonArcher;
    public bool IsPolishedCatacombEnemy => Type is
        EnemyType.Skeleton or EnemyType.SkeletonArcher or
        EnemyType.RottenCorpse or EnemyType.Wraith or
        EnemyType.UndeadGuard or EnemyType.GraveBat or
        EnemyType.CursedKnight or EnemyType.SoulCollector or
        EnemyType.DeathKnight or EnemyType.FallenKnight;
    protected override float DeathPresentationDurationSeconds => Type switch
    {
        EnemyType.RottenCorpse => 1.22f,
        EnemyType.Wraith => .92f,
        EnemyType.UndeadGuard => .88f,
        EnemyType.GraveBat => 1.02f,
        EnemyType.CursedKnight => 1.10f,
        EnemyType.SoulCollector => 1.05f,
        EnemyType.DeathKnight => 1.62f,
        EnemyType.FallenKnight => 2.05f,
        _ => base.DeathPresentationDurationSeconds
    };
    public override bool IsCombatAttackActive => IsPolishedCatacombEnemy
        ? CombatState is CatacombCombatState.MeleeActive or
            CatacombCombatState.BowShoveActive or
            CatacombCombatState.CorpseActive or
            CatacombCombatState.WraithActive or
            CatacombCombatState.GuardActive or
            CatacombCombatState.GraveActive or
            CatacombCombatState.KnightActive or
            CatacombCombatState.SoulActive
        : base.IsCombatAttackActive;
    public override bool IsCombatAttackTelegraphing => IsPolishedCatacombEnemy
        ? CombatState is CatacombCombatState.MeleeWindup or
            CatacombCombatState.ArcherNock or
            CatacombCombatState.ArcherDraw or
            CatacombCombatState.BowShoveWindup or
            CatacombCombatState.CorpseWindup or
            CatacombCombatState.WraithMaterialize or
            CatacombCombatState.WraithWindup or
            CatacombCombatState.GuardWindup or
            CatacombCombatState.GraveWindup or
            CatacombCombatState.KnightWindup or
            CatacombCombatState.SoulWindup
        : base.IsCombatAttackTelegraphing;
    public override Rectangle ActiveAttackArea => IsPolishedCatacombEnemy
        ? CreatePolishedAttackArea()
        : base.ActiveAttackArea;
    public float MeleeWeaponAngle
    {
        get
        {
            float progress = ActionProgress;
            return CombatState switch
            {
                CatacombCombatState.MeleeWindup when
                    SkeletonAttack == SkeletonAttackKind.OverheadChop =>
                    MathHelper.Lerp(.7f, -1.55f, progress),
                CatacombCombatState.MeleeWindup when
                    SkeletonAttack == SkeletonAttackKind.ReturnCut =>
                    MathHelper.Lerp(.25f, .55f, progress),
                CatacombCombatState.MeleeWindup =>
                    MathHelper.Lerp(.8f, -2.25f, progress),
                CatacombCombatState.MeleeActive when
                    SkeletonAttack == SkeletonAttackKind.OverheadChop =>
                    MathHelper.Lerp(-1.55f, 1.15f, progress),
                CatacombCombatState.MeleeActive when
                    SkeletonAttack == SkeletonAttackKind.ReturnCut =>
                    MathHelper.Lerp(.45f, -1.75f, progress),
                CatacombCombatState.MeleeActive =>
                    MathHelper.Lerp(-2.25f, .2f, progress),
                CatacombCombatState.MeleeRecovery =>
                    MathHelper.Lerp(.35f, .88f, progress),
                _ => .88f
            };
        }
    }

    public CatacombEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId,
        bool elite)
        : base(
            type,
            position,
            SizeFor(type),
            SpeedFor(type),
            620f,
            HealthFor(type, level, worldTier),
            DamageFor(type, level, worldTier),
            RewardFor(type),
            level,
            worldTier,
            elite,
            roomId,
            RangeFor(type),
            CooldownFor(type))
    {
        int variantSeed = unchecked(roomId * 397 ^ (int)position.X * 17);
        EquipmentVariant = (variantSeed & int.MaxValue) % 4;
        if (type == EnemyType.Wraith)
        {
            IsEthereal = true;
            SpectralPhase = WraithPhase.Ethereal;
            CombatState = CatacombCombatState.WraithEthereal;
            _actionDuration = WraithEtherealSeconds;
            _actionTimeRemaining = WraithEtherealSeconds;
        }
        else if (type == EnemyType.UndeadGuard)
        {
            CombatState = CatacombCombatState.GuardApproach;
        }
        else if (type == EnemyType.GraveBat)
        {
            CombatState = CatacombCombatState.GraveHover;
            _actionDuration = GraveHoverSeconds;
            _actionTimeRemaining = GraveHoverSeconds;
            _graveBatFlightDirection = (variantSeed & 1) == 0 ? 1f : -1f;
        }
        else if (type == EnemyType.CursedKnight)
        {
            CombatState = CatacombCombatState.KnightDormant;
            _actionDuration = KnightDormantSeconds;
            _actionTimeRemaining = KnightDormantSeconds;
        }
        else if (type is EnemyType.DeathKnight or EnemyType.FallenKnight)
        {
            CombatState = CatacombCombatState.KnightDormant;
            _actionDuration = type == EnemyType.DeathKnight ? 1.05f : 1.18f;
            _actionTimeRemaining = _actionDuration;
        }
        else if (type == EnemyType.SoulCollector)
        {
            CombatState = CatacombCombatState.SoulApproach;
        }
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float dt = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        _guardBlockReactionRemaining = MathF.Max(
            0f,
            _guardBlockReactionRemaining - dt);
        _graveBatPressureRemaining = MathF.Max(
            0f,
            _graveBatPressureRemaining - dt);
        _cursedArmorBreakRemaining = MathF.Max(
            0f,
            _cursedArmorBreakRemaining - dt);
        _soulFieldTimeRemaining = MathF.Max(
            0f,
            _soulFieldTimeRemaining - dt);
        if (CombatState == CatacombCombatState.GuardBroken)
        {
            _actionTimeRemaining = MathF.Max(0f, _actionTimeRemaining - dt);
            if (_actionTimeRemaining <= 0f && IsAlive)
            {
                _guardIntegrity = 72f;
                UndeadGuardAttack = UndeadGuardAttackKind.None;
                CombatState = CatacombCombatState.GuardApproach;
                _actionDuration = 0f;
            }
        }
    }

    public override void ReceiveDamage(int damage, float poiseDamage)
    {
        float adjustedPoise = Type switch
        {
            EnemyType.DeathKnight when poiseDamage < 45f => poiseDamage * .48f,
            EnemyType.FallenKnight when poiseDamage < 45f => poiseDamage * .56f,
            _ => poiseDamage
        };
        base.ReceiveDamage(damage, adjustedPoise);
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        if (damage <= 0)
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        if (Type == EnemyType.FallenKnight &&
            FallenKnightAttack == FallenKnightAttackKind.CursedCounter &&
            CombatState == CatacombCombatState.KnightWindup &&
            IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamage(
                Math.Max(1, (int)MathF.Ceiling(damage * .32f)),
                poiseDamage * .25f);
            _counterTriggered = true;
            StartAction(CatacombCombatState.KnightActive, .20f);
            return;
        }

        if (Type == EnemyType.CursedKnight)
        {
            float adjustedPoise = !IsCursedArmorBroken && poiseDamage < 45f
                ? poiseDamage * .78f
                : poiseDamage;
            int adjustedDamage = IsCursedArmorBroken
                ? (int)MathF.Ceiling(damage * 1.16f)
                : damage;
            base.ReceiveDamageFrom(
                adjustedDamage,
                adjustedPoise,
                sourcePosition);
            return;
        }

        if (Type != EnemyType.UndeadGuard || !IsGuarding ||
            !IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        bool strongHit = poiseDamage >= 45f || damage >= 40;
        float guardPoise = strongHit ? poiseDamage : poiseDamage * .72f;
        int guardedDamage = Math.Max(
            1,
            (int)MathF.Ceiling(damage * (strongHit ? .82f : .44f)));
        base.ReceiveDamage(guardedDamage, guardPoise);
        _guardBlockReactionRemaining = .20f;
        _guardIntegrity -= poiseDamage + damage * .32f;

        if (strongHit || _guardIntegrity <= 0f || IsStaggered)
            EnterGuardBreak();
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        float dt = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        _specialCooldown = MathF.Max(0f, _specialCooldown - dt);
        _forcedRetreatRemaining = MathF.Max(0f, _forcedRetreatRemaining - dt);

        if (Type == EnemyType.Skeleton)
        {
            UpdateSkeleton(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.SkeletonArcher)
        {
            UpdateSkeletonArcher(gameTime, player, dungeon, projectiles, dt);
            return;
        }

        if (Type == EnemyType.RottenCorpse)
        {
            UpdateRottenCorpse(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.Wraith)
        {
            UpdateWraith(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.UndeadGuard)
        {
            UpdateUndeadGuard(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.GraveBat)
        {
            UpdateGraveBat(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.CursedKnight)
        {
            UpdateCursedKnight(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.DeathKnight)
        {
            UpdateDeathKnight(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.FallenKnight)
        {
            UpdateFallenKnight(gameTime, player, dungeon, dt);
            return;
        }

        if (Type == EnemyType.SoulCollector)
        {
            UpdateSoulCollector(
                gameTime, player, dungeon, projectiles, dt);
            return;
        }

        float stop = Attack.Range * .8f;
        if (Vector2.DistanceSquared(Position, player.Position) <=
            Attack.Range * Attack.Range)
        {
            TryMeleeAttack(player);
        }
        else
        {
            MoveToward(
                gameTime,
                player.Position,
                stop,
                dungeon,
                Type == EnemyType.FallenKnight && CombatPhase == 3
                    ? 1.25f
                    : 1f);
        }
    }

    protected override void OnDamaged()
    {
        if (!IsAlive && Type == EnemyType.SoulCollector)
        {
            _soulFieldTimeRemaining = 0f;
            _ritualPreviewBounds = Rectangle.Empty;
        }
        if (!IsAlive && Type == EnemyType.GraveBat &&
            float.IsNaN(_lastKnownGroundY))
        {
            _lastKnownGroundY = Position.Y + 92f;
        }

        if (Type == EnemyType.UndeadGuard && IsAlive && IsStaggered)
        {
            EnterGuardBreak();
            return;
        }

        if (!IsPolishedCatacombEnemy || (IsAlive && !IsStaggered))
            return;

        CancelPolishedAttack();
    }

    protected override void OnCombatInterrupted()
    {
        if (Type == EnemyType.UndeadGuard && IsAlive)
        {
            EnterGuardBreak();
            return;
        }

        if (Type == EnemyType.CursedKnight && IsAlive)
            _cursedArmorBreakRemaining = CursedArmorBreakSeconds;

        if (IsPolishedCatacombEnemy)
            CancelPolishedAttack();
    }

    public float ConsumeCursePressure()
    {
        float pressure = _pendingCursePressure;
        _pendingCursePressure = 0f;
        return pressure;
    }

    public Vector2 GetBowGripPosition()
    {
        float facing = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        return Position + new Vector2(facing * 20f, -14f);
    }

    public Vector2 GetArrowReleasePosition()
    {
        float facing = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        return GetBowGripPosition() + new Vector2(facing * 21f, 0f);
    }

    private void UpdateSkeleton(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (CombatState != CatacombCombatState.Ready)
        {
            UpdateSkeletonAction(player, dt);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (distance <= 72f)
        {
            _meleePatternIndex++;
            SkeletonAttackKind kind = _meleePatternIndex % 5 == 0
                ? SkeletonAttackKind.OverheadChop
                : SkeletonAttackKind.RustedSlash;
            _returnCutQueued = kind == SkeletonAttackKind.RustedSlash &&
                _meleePatternIndex % 3 == 0;
            StartMelee(kind);
            return;
        }

        MoveToward(gameTime, player.Position, 58f, dungeon);
    }

    private void UpdateSkeletonAction(PlayerCharacter player, float dt)
    {
        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.MeleeActive &&
            !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.Bounds))
        {
            int damage = SkeletonAttack == SkeletonAttackKind.OverheadChop
                ? (int)MathF.Ceiling(EffectiveAttackDamage * 1.35f)
                : SkeletonAttack == SkeletonAttackKind.ReturnCut
                    ? Math.Max(1, EffectiveAttackDamage * 4 / 5)
                    : EffectiveAttackDamage;
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .55f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.MeleeWindup:
                StartAction(
                    CatacombCombatState.MeleeActive,
                    ActiveSecondsFor(SkeletonAttack));
                break;
            case CatacombCombatState.MeleeActive:
                StartAction(
                    CatacombCombatState.MeleeRecovery,
                    RecoverySecondsFor(SkeletonAttack));
                break;
            case CatacombCombatState.MeleeRecovery:
                if (_returnCutQueued)
                {
                    _returnCutQueued = false;
                    StartMelee(SkeletonAttackKind.ReturnCut);
                }
                else
                {
                    CombatState = CatacombCombatState.Ready;
                    SkeletonAttack = SkeletonAttackKind.None;
                    _actionDuration = 0f;
                    _actionTimeRemaining = 0f;
                }
                break;
        }
    }

    private void UpdateSkeletonArcher(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float dt)
    {
        IsRetreating = false;
        if (CombatState != CatacombCombatState.Ready)
        {
            UpdateArcherAction(player, dungeon, projectiles, dt);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (_forcedRetreatRemaining > 0f || distance < 128f)
        {
            IsRetreating = TrySafeRetreat(
                gameTime,
                player.Position,
                dungeon,
                1.05f);
        }

        if (distance < 78f)
        {
            StartAction(
                CatacombCombatState.BowShoveWindup,
                BowShoveWindupSeconds);
            return;
        }

        if (IsRetreating)
            return;

        if (distance < 140f)
            return;

        if (distance > 470f)
        {
            MoveToward(gameTime, player.Position, 410f, dungeon, .48f);
            return;
        }

        if (_specialCooldown <= 0f && distance <= 520f)
        {
            _shotPatternIndex++;
            IsHeavyArrow = _shotPatternIndex % 4 == 0;
            StartAction(CatacombCombatState.ArcherNock, ArcherNockSeconds);
        }
    }

    private void UpdateArcherAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float dt)
    {
        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.BowShoveActive &&
            !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.Bounds))
        {
            AttackResolution result = player.ReceiveMeleeAttack(new AttackContact(
                Math.Max(1, EffectiveAttackDamage / 2),
                Position,
                blockable: true,
                unblockable: false,
                ActiveAttackArea));
            if (result is AttackResolution.Damaged or
                AttackResolution.GuardBroken)
            {
                player.PushAwayFrom(Position, 24f, dungeon);
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.ArcherNock:
                StartAction(
                    CatacombCombatState.ArcherDraw,
                    IsHeavyArrow ? HeavyDrawSeconds : StandardDrawSeconds);
                break;
            case CatacombCombatState.ArcherDraw:
            {
                Vector2 release = GetArrowReleasePosition();
                float direction = Facing == EnemyFacingDirection.Left
                    ? -1f
                    : 1f;
                projectiles.SpawnEnemyArrow(
                    release,
                    new Vector2(direction, 0f),
                    IsHeavyArrow
                        ? (int)MathF.Ceiling(EffectiveAttackDamage * 1.55f)
                        : EffectiveAttackDamage,
                    RoomId,
                    IsHeavyArrow);
                StartAction(
                    CatacombCombatState.ArcherRelease,
                    ArcherReleaseSeconds);
                break;
            }
            case CatacombCombatState.ArcherRelease:
                StartAction(
                    CatacombCombatState.ArcherRecovery,
                    IsHeavyArrow
                        ? HeavyRecoverySeconds
                        : StandardRecoverySeconds);
                break;
            case CatacombCombatState.ArcherRecovery:
                _specialCooldown = IsHeavyArrow ? 1.55f : 1.05f;
                CombatState = CatacombCombatState.Ready;
                IsHeavyArrow = false;
                _actionDuration = 0f;
                break;
            case CatacombCombatState.BowShoveWindup:
                StartAction(
                    CatacombCombatState.BowShoveActive,
                    BowShoveActiveSeconds);
                break;
            case CatacombCombatState.BowShoveActive:
                StartAction(
                    CatacombCombatState.BowShoveRecovery,
                    BowShoveRecoverySeconds);
                break;
            case CatacombCombatState.BowShoveRecovery:
                CombatState = CatacombCombatState.Ready;
                _forcedRetreatRemaining = .8f;
                _specialCooldown = MathF.Max(_specialCooldown, .45f);
                _actionDuration = 0f;
                break;
        }
    }

    private void UpdateRottenCorpse(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (CombatState != CatacombCombatState.Ready)
        {
            UpdateRottenCorpseAction(player, dungeon, dt);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (_specialCooldown > 0f)
        {
            if (distance > 62f)
                MoveToward(gameTime, player.Position, 58f, dungeon, .72f);
            return;
        }

        if (distance <= 72f)
        {
            _rottenPatternIndex++;
            RottenCorpseAttackKind kind = (_rottenPatternIndex % 3) switch
            {
                0 => RottenCorpseAttackKind.RotVomit,
                2 => RottenCorpseAttackKind.BodySlam,
                _ => RottenCorpseAttackKind.Grab
            };
            StartRottenAttack(kind);
            return;
        }

        if (distance <= 122f && _rottenPatternIndex % 2 == 1)
        {
            _rottenPatternIndex++;
            StartRottenAttack(RottenCorpseAttackKind.RotVomit);
            return;
        }

        MoveToward(gameTime, player.Position, 61f, dungeon, .78f);
    }

    private void UpdateRottenCorpseAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.CorpseActive &&
            !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.Bounds))
        {
            int damage = RottenCorpseAttack switch
            {
                RottenCorpseAttackKind.BodySlam =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.35f),
                RottenCorpseAttackKind.RotVomit =>
                    Math.Max(1, EffectiveAttackDamage * 2 / 3),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: RottenCorpseAttack !=
                        RottenCorpseAttackKind.RotVomit,
                    unblockable: false,
                    ActiveAttackArea));
            if (result is AttackResolution.Damaged or
                AttackResolution.GuardBroken)
            {
                if (RottenCorpseAttack == RottenCorpseAttackKind.BodySlam)
                    player.PushAwayFrom(Position, 38f, dungeon);
                if (RottenCorpseAttack == RottenCorpseAttackKind.RotVomit)
                    _pendingCursePressure += 9f;
            }
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .36f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.CorpseWindup:
                StartAction(
                    CatacombCombatState.CorpseActive,
                    RottenActiveSecondsFor(RottenCorpseAttack));
                break;
            case CatacombCombatState.CorpseActive:
                StartAction(
                    CatacombCombatState.CorpseRecovery,
                    RottenRecoverySecondsFor(RottenCorpseAttack));
                break;
            case CatacombCombatState.CorpseRecovery:
                CombatState = CatacombCombatState.Ready;
                RottenCorpseAttack = RottenCorpseAttackKind.None;
                _specialCooldown = .48f;
                _actionDuration = 0f;
                break;
        }
    }

    private void StartRottenAttack(RottenCorpseAttackKind kind)
    {
        RottenCorpseAttack = kind;
        StartAction(
            CatacombCombatState.CorpseWindup,
            kind switch
            {
                RottenCorpseAttackKind.BodySlam => BodySlamWindupSeconds,
                RottenCorpseAttackKind.RotVomit => RotVomitWindupSeconds,
                _ => GrabWindupSeconds
            });
    }

    private void UpdateWraith(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (CombatState == CatacombCombatState.Ready)
        {
            BeginWraithFade();
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.WraithEthereal)
        {
            float distance = MathF.Abs(player.Position.X - Position.X);
            if (distance > 142f)
                MoveToward(gameTime, player.Position, 112f, dungeon, .62f);
            else if (distance < 68f)
                MoveAway(gameTime, player.Position, dungeon, .52f);

            if (_actionTimeRemaining <= 0f)
                BeginWraithMaterialize();
            return;
        }

        if (CombatState == CatacombCombatState.WraithActive)
        {
            if (WraithAttack == WraithAttackKind.SpectralDash &&
                _dashDistanceRemaining > 0f)
            {
                float step = MathF.Min(
                    _dashDistanceRemaining,
                    390f * dt);
                float facing = Facing == EnemyFacingDirection.Left ? -1f : 1f;
                MoveBy(new Vector2(facing * step, 0f), dungeon);
                _dashDistanceRemaining -= step;
            }

            if (!_actionHitConsumed &&
                ActiveAttackArea.Intersects(player.Bounds))
            {
                int damage = WraithAttack switch
                {
                    WraithAttackKind.SpectralDash =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.2f),
                    WraithAttackKind.CurseWave =>
                        Math.Max(1, EffectiveAttackDamage * 3 / 4),
                    _ => EffectiveAttackDamage
                };
                AttackResolution result = player.ReceiveMeleeAttack(
                    new AttackContact(
                        damage,
                        Position,
                        blockable: WraithAttack != WraithAttackKind.CurseWave,
                        unblockable: false,
                        ActiveAttackArea));
                if (result is AttackResolution.Damaged or
                    AttackResolution.GuardBroken)
                {
                    _pendingCursePressure += WraithAttack ==
                        WraithAttackKind.CurseWave ? 11f : 4f;
                }
                if (result == AttackResolution.PerfectGuard)
                    ApplyPoiseDamage(MaxPoise * .48f);
                _actionHitConsumed = true;
            }
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.WraithMaterialize:
                SelectWraithAttack(player);
                break;
            case CatacombCombatState.WraithWindup:
                _dashDistanceRemaining = WraithAttack ==
                    WraithAttackKind.SpectralDash ? 86f : 0f;
                StartAction(
                    CatacombCombatState.WraithActive,
                    WraithActiveSecondsFor(WraithAttack));
                break;
            case CatacombCombatState.WraithActive:
                SpectralPhase = WraithPhase.Recovering;
                StartAction(
                    CatacombCombatState.WraithRecovery,
                    WraithRecoverySecondsFor(WraithAttack));
                break;
            case CatacombCombatState.WraithRecovery:
                BeginWraithFade();
                break;
            case CatacombCombatState.WraithFade:
                IsEthereal = true;
                SpectralPhase = WraithPhase.Ethereal;
                WraithAttack = WraithAttackKind.None;
                StartAction(
                    CatacombCombatState.WraithEthereal,
                    WraithEtherealSeconds);
                break;
        }
    }

    private void BeginWraithMaterialize()
    {
        IsEthereal = false;
        SpectralPhase = WraithPhase.Materializing;
        StartAction(
            CatacombCombatState.WraithMaterialize,
            WraithMaterializeSeconds);
    }

    private void SelectWraithAttack(PlayerCharacter player)
    {
        _wraithPatternIndex++;
        float distance = MathF.Abs(player.Position.X - Position.X);
        WraithAttack = (_wraithPatternIndex % 3) switch
        {
            0 => WraithAttackKind.CurseWave,
            2 => WraithAttackKind.SpectralDash,
            _ when distance > 88f => WraithAttackKind.SpectralDash,
            _ => WraithAttackKind.SpectralClaw
        };
        SpectralPhase = WraithPhase.Attacking;
        StartAction(
            CatacombCombatState.WraithWindup,
            WraithAttack switch
            {
                WraithAttackKind.SpectralDash => DashWindupSeconds,
                WraithAttackKind.CurseWave => CurseWaveWindupSeconds,
                _ => ClawWindupSeconds
            });
    }

    private void BeginWraithFade()
    {
        IsEthereal = false;
        SpectralPhase = WraithPhase.Fading;
        StartAction(CatacombCombatState.WraithFade, WraithFadeSeconds);
    }

    private void UpdateUndeadGuard(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState == CatacombCombatState.GuardBroken)
            return;

        if (CombatState == CatacombCombatState.GuardApproach)
        {
            if (distance > 82f)
            {
                MoveToward(gameTime, player.Position, 74f, dungeon, .56f);
                return;
            }

            StartAction(
                CatacombCombatState.GuardStance,
                GuardStanceSeconds);
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.GuardActive &&
            !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.Bounds))
        {
            int damage = UndeadGuardAttack switch
            {
                UndeadGuardAttackKind.ShieldBash =>
                    Math.Max(1, EffectiveAttackDamage * 3 / 4),
                UndeadGuardAttackKind.HeavyCleave =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.5f),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result is AttackResolution.Damaged or
                AttackResolution.GuardBroken)
            {
                float push = UndeadGuardAttack switch
                {
                    UndeadGuardAttackKind.ShieldBash => 46f,
                    UndeadGuardAttackKind.HeavyCleave => 28f,
                    _ => 12f
                };
                player.PushAwayFrom(Position, push, dungeon);
            }
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .45f);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.GuardStance:
                _guardPatternIndex++;
                UndeadGuardAttack = (_guardPatternIndex % 3) switch
                {
                    2 => UndeadGuardAttackKind.ShieldBash,
                    0 => UndeadGuardAttackKind.HeavyCleave,
                    _ => UndeadGuardAttackKind.GuardedStrike
                };
                StartAction(
                    CatacombCombatState.GuardWindup,
                    GuardWindupSecondsFor(UndeadGuardAttack));
                break;
            case CatacombCombatState.GuardWindup:
                StartAction(
                    CatacombCombatState.GuardActive,
                    GuardActiveSecondsFor(UndeadGuardAttack));
                break;
            case CatacombCombatState.GuardActive:
                StartAction(
                    CatacombCombatState.GuardRecovery,
                    GuardRecoverySecondsFor(UndeadGuardAttack));
                break;
            case CatacombCombatState.GuardRecovery:
                UndeadGuardAttack = UndeadGuardAttackKind.None;
                CombatState = CatacombCombatState.GuardApproach;
                _actionDuration = 0f;
                break;
        }
    }

    private void EnterGuardBreak()
    {
        if (Type != EnemyType.UndeadGuard || !IsAlive)
            return;
        UndeadGuardAttack = UndeadGuardAttackKind.None;
        _guardIntegrity = 0f;
        _guardBlockReactionRemaining = 0f;
        StartAction(CatacombCombatState.GuardBroken, GuardBreakSeconds);
    }

    private bool IsSourceInFront(Vector2 sourcePosition)
    {
        float horizontal = sourcePosition.X - Position.X;
        if (MathF.Abs(horizontal) < 2f)
            return true;
        return Facing == EnemyFacingDirection.Right
            ? horizontal > 0f
            : horizontal < 0f;
    }

    private void UpdateGraveBat(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        DungeonRoom room = dungeon.FindRoomContaining(Position);
        if (room != null)
            _lastKnownGroundY = room.GroundY;

        _actionTimeRemaining -= dt;
        float preferredY = GetGraveBatPreferredY(player, room, dungeon);
        if (CombatState == CatacombCombatState.GraveHover)
        {
            float horizontal = MathF.Abs(player.Position.X - Position.X);
            Vector2 hoverTarget = new(
                horizontal < 64f
                    ? Position.X - MathF.Sign(player.Position.X - Position.X) * 58f
                    : Position.X,
                preferredY);
            MoveGraveBatToward(hoverTarget, dungeon, dt, .52f);
            if (_actionTimeRemaining <= 0f)
            {
                StartAction(
                    CatacombCombatState.GraveCircle,
                    GraveCircleSeconds);
            }
            return;
        }

        if (CombatState == CatacombCombatState.GraveCircle)
        {
            Vector2 circleTarget = new(
                player.Position.X + _graveBatFlightDirection * 112f,
                preferredY - 14f);
            MoveGraveBatToward(circleTarget, dungeon, dt, .74f);
            if (_actionTimeRemaining <= 0f)
                StartGraveBatAttack(player);
            return;
        }

        if (CombatState == CatacombCombatState.GraveWindup)
        {
            Vector2 windupTarget = new(
                Position.X,
                GraveBatAttack == GraveBatAttackKind.GraveScreech
                    ? preferredY - 8f
                    : preferredY);
            MoveGraveBatToward(windupTarget, dungeon, dt, .32f);
        }
        else if (CombatState == CatacombCombatState.GraveActive &&
            GraveBatAttack != GraveBatAttackKind.GraveScreech)
        {
            float speed = GraveBatAttack == GraveBatAttackKind.ClawPass
                ? 1.42f
                : 1.08f;
            Vector2 motion = new(
                _graveBatFlightDirection * EffectiveMovementSpeed * speed * dt,
                GraveBatAttack == GraveBatAttackKind.ClawPass
                    ? 28f * dt
                    : -18f * dt);
            MoveBy(motion, dungeon);
        }
        else if (CombatState == CatacombCombatState.GraveRetreat)
        {
            Vector2 retreatTarget = new(
                player.Position.X - _graveBatFlightDirection * 132f,
                preferredY - 42f);
            MoveGraveBatToward(retreatTarget, dungeon, dt, 1.05f);
        }

        if (CombatState == CatacombCombatState.GraveActive &&
            !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.Bounds))
        {
            int damage = GraveBatAttack switch
            {
                GraveBatAttackKind.GraveScreech =>
                    Math.Max(1, EffectiveAttackDamage / 3),
                GraveBatAttackKind.SwarmFeint =>
                    Math.Max(1, EffectiveAttackDamage / 2),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: GraveBatAttack !=
                        GraveBatAttackKind.GraveScreech,
                    unblockable: false,
                    ActiveAttackArea));
            if (GraveBatAttack == GraveBatAttackKind.GraveScreech &&
                result != AttackResolution.Ignored)
            {
                _graveBatPressureRemaining = 1.15f;
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.GraveWindup:
                StartAction(
                    CatacombCombatState.GraveActive,
                    GraveActiveSecondsFor(GraveBatAttack));
                break;
            case CatacombCombatState.GraveActive:
                StartAction(
                    CatacombCombatState.GraveRecovery,
                    GraveRecoverySecondsFor(GraveBatAttack));
                break;
            case CatacombCombatState.GraveRecovery:
                StartAction(
                    CatacombCombatState.GraveRetreat,
                    GraveRetreatSeconds);
                break;
            case CatacombCombatState.GraveRetreat:
                GraveBatAttack = GraveBatAttackKind.None;
                _graveBatFlightDirection *= -1f;
                StartAction(
                    CatacombCombatState.GraveHover,
                    GraveHoverSeconds);
                break;
        }
    }

    private void StartGraveBatAttack(PlayerCharacter player)
    {
        _graveBatPatternIndex++;
        GraveBatAttack = (_graveBatPatternIndex % 3) switch
        {
            1 => GraveBatAttackKind.GraveScreech,
            2 => GraveBatAttackKind.ClawPass,
            _ => GraveBatAttackKind.SwarmFeint
        };
        float horizontal = player.Position.X - Position.X;
        if (MathF.Abs(horizontal) > 2f)
            _graveBatFlightDirection = MathF.Sign(horizontal);
        StartAction(
            CatacombCombatState.GraveWindup,
            GraveBatAttack switch
            {
                GraveBatAttackKind.GraveScreech =>
                    GraveScreechWindupSeconds,
                GraveBatAttackKind.ClawPass => GraveClawWindupSeconds,
                _ => GraveFeintWindupSeconds
            });
    }

    private void MoveGraveBatToward(
        Vector2 target,
        DungeonMap dungeon,
        float dt,
        float speedMultiplier)
    {
        Vector2 offset = target - Position;
        float distance = offset.Length();
        if (distance <= .5f)
            return;
        Vector2 movement = offset / distance * MathF.Min(
            distance,
            EffectiveMovementSpeed * speedMultiplier * dt);
        MoveBy(movement, dungeon);
    }

    private float GetGraveBatPreferredY(
        PlayerCharacter player,
        DungeonRoom room,
        DungeonMap dungeon)
    {
        float minimum = (room?.Bounds.Top ?? dungeon.WorldBounds.Top) +
            Size.Y / 2f + 22f;
        float maximum = (room?.GroundY ?? dungeon.WorldBounds.Bottom) -
            Size.Y / 2f - 62f;
        return MathHelper.Clamp(player.Position.Y - 88f, minimum, maximum);
    }

    private void UpdateCursedKnight(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (IsCursedArmorBroken)
            return;

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState == CatacombCombatState.KnightDormant)
        {
            _actionTimeRemaining -= dt;
            if (_actionTimeRemaining <= 0f)
            {
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
            }
            return;
        }

        if (CombatState == CatacombCombatState.KnightApproach)
        {
            if (distance > 235f)
            {
                MoveToward(gameTime, player.Position, 190f, dungeon, .62f);
                return;
            }
            StartAction(CatacombCombatState.KnightReady, KnightReadySeconds);
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.KnightActive)
        {
            if (CursedKnightAttack == CursedKnightAttackKind.AdvancingThrust)
            {
                float facing = Facing == EnemyFacingDirection.Left ? -1f : 1f;
                MoveBy(new Vector2(facing * 205f * dt, 0f), dungeon);
            }

            if (!_actionHitConsumed &&
                ActiveAttackArea.Intersects(player.Bounds))
            {
                int damage = CursedKnightAttack switch
                {
                    CursedKnightAttackKind.ExecutionStrike =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.55f),
                    CursedKnightAttackKind.AdvancingThrust =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.18f),
                    _ => EffectiveAttackDamage
                };
                AttackResolution result = player.ReceiveMeleeAttack(
                    new AttackContact(
                        damage,
                        Position,
                        blockable: true,
                        unblockable: false,
                        ActiveAttackArea));
                if (result is AttackResolution.Damaged or
                    AttackResolution.GuardBroken)
                {
                    float push = CursedKnightAttack ==
                        CursedKnightAttackKind.ExecutionStrike ? 34f : 22f;
                    player.PushAwayFrom(Position, push, dungeon);
                }
                if (result == AttackResolution.PerfectGuard)
                    ApplyPoiseDamage(MaxPoise * .42f);
                _actionHitConsumed = true;
            }
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.KnightReady:
                _knightPatternIndex++;
                CursedKnightAttack = distance > 108f
                    ? CursedKnightAttackKind.AdvancingThrust
                    : _knightPatternIndex % 3 == 0
                        ? CursedKnightAttackKind.ExecutionStrike
                        : CursedKnightAttackKind.CursedSweep;
                StartAction(
                    CatacombCombatState.KnightWindup,
                    KnightWindupSecondsFor(CursedKnightAttack));
                break;
            case CatacombCombatState.KnightWindup:
                StartAction(
                    CatacombCombatState.KnightActive,
                    KnightActiveSecondsFor(CursedKnightAttack));
                break;
            case CatacombCombatState.KnightActive:
                StartAction(
                    CatacombCombatState.KnightRecovery,
                    KnightRecoverySecondsFor(CursedKnightAttack));
                break;
            case CatacombCombatState.KnightRecovery:
                CursedKnightAttack = CursedKnightAttackKind.None;
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
                break;
        }
    }

    private void UpdateDeathKnight(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState == CatacombCombatState.KnightDormant)
        {
            _actionTimeRemaining -= dt;
            if (_actionTimeRemaining <= 0f)
            {
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
            }
            return;
        }

        if (CombatState == CatacombCombatState.KnightApproach)
        {
            if (distance > 245f)
            {
                MoveToward(gameTime, player.Position, 215f, dungeon, .58f);
                return;
            }
            StartAction(CatacombCombatState.KnightReady, .48f);
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.KnightActive)
        {
            if (DeathKnightAttack == DeathKnightAttackKind.DreadCharge)
            {
                MoveBy(new Vector2(
                    _lockedAttackDirection * 255f * dt, 0f), dungeon);
            }

            if (!_actionHitConsumed &&
                ActiveAttackArea.Intersects(player.Bounds))
            {
                int damage = DeathKnightAttack switch
                {
                    DeathKnightAttackKind.ExecutionCrush =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.72f),
                    DeathKnightAttackKind.DreadCharge =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.28f),
                    DeathKnightAttackKind.TombbreakerSlam =>
                        (int)MathF.Ceiling(EffectiveAttackDamage * 1.42f),
                    _ => EffectiveAttackDamage
                };
                AttackResolution result = player.ReceiveMeleeAttack(
                    new AttackContact(
                        damage,
                        Position,
                        blockable: true,
                        unblockable: false,
                        ActiveAttackArea));
                if (result is AttackResolution.Damaged or
                    AttackResolution.GuardBroken)
                {
                    float push = DeathKnightAttack switch
                    {
                        DeathKnightAttackKind.DreadCharge => 38f,
                        DeathKnightAttackKind.ExecutionCrush => 34f,
                        DeathKnightAttackKind.TombbreakerSlam => 30f,
                        _ => 24f
                    };
                    player.PushAwayFrom(Position, push, dungeon);
                    if (DeathKnightAttack ==
                        DeathKnightAttackKind.TombbreakerSlam)
                        _pendingCursePressure += 4f;
                }
                if (result == AttackResolution.PerfectGuard)
                    ApplyPoiseDamage(MaxPoise * .34f);
                _actionHitConsumed = true;
            }
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.KnightReady:
                SelectDeathKnightAttack(distance);
                break;
            case CatacombCombatState.KnightWindup:
                StartAction(
                    CatacombCombatState.KnightActive,
                    DeathKnightActiveSecondsFor(DeathKnightAttack));
                break;
            case CatacombCombatState.KnightActive:
                float recovery = DeathKnightRecoverySecondsFor(
                    DeathKnightAttack);
                if (CurrentHealth * 100 <= MaxHealth * 35)
                    recovery *= .86f;
                StartAction(CatacombCombatState.KnightRecovery, recovery);
                break;
            case CatacombCombatState.KnightRecovery:
                DeathKnightAttack = DeathKnightAttackKind.None;
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
                break;
        }
    }

    private void SelectDeathKnightAttack(float distance)
    {
        _deathKnightPatternIndex++;
        DeathKnightAttack = distance > 150f
            ? DeathKnightAttackKind.DreadCharge
            : (_deathKnightPatternIndex % 4) switch
            {
                0 => DeathKnightAttackKind.ExecutionCrush,
                2 => DeathKnightAttackKind.TombbreakerSlam,
                _ => DeathKnightAttackKind.WarSweep
            };
        _lockedAttackDirection = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        StartAction(
            CatacombCombatState.KnightWindup,
            DeathKnightWindupSecondsFor(DeathKnightAttack));
    }

    private void UpdateFallenKnight(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (CombatPhase > _activeFallenPhase &&
            CombatState != CatacombCombatState.KnightTransition)
        {
            FallenKnightAttack = FallenKnightAttackKind.None;
            _counterTriggered = false;
            StartAction(
                CatacombCombatState.KnightTransition,
                CombatPhase == 3
                    ? FallenPhaseThreeTransitionSeconds
                    : FallenPhaseTwoTransitionSeconds);
            return;
        }

        if (CombatState == CatacombCombatState.KnightTransition)
        {
            _actionTimeRemaining -= dt;
            if (_actionTimeRemaining <= 0f)
            {
                _activeFallenPhase = CombatPhase;
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
                _specialCooldown = .36f;
            }
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState == CatacombCombatState.KnightDormant)
        {
            _actionTimeRemaining -= dt;
            if (_actionTimeRemaining <= 0f)
            {
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
            }
            return;
        }

        if (CombatState == CatacombCombatState.KnightApproach)
        {
            float preferred = _activeFallenPhase == 1 ? 102f : 126f;
            if (distance > preferred + 34f)
            {
                MoveToward(
                    gameTime,
                    player.Position,
                    preferred,
                    dungeon,
                    _activeFallenPhase == 3 ? 1.16f : .88f);
                return;
            }
            if (distance < preferred - 42f && _activeFallenPhase >= 2)
            {
                MoveAway(gameTime, player.Position, dungeon, .42f);
                return;
            }
            StartAction(
                CatacombCombatState.KnightReady,
                _activeFallenPhase == 3 ? .22f : .34f);
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.KnightActive)
        {
            UpdateFallenKnightActive(player, dungeon, dt);
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.KnightReady:
                SelectFallenKnightAttack(distance);
                break;
            case CatacombCombatState.KnightWindup:
                if (FallenKnightAttack == FallenKnightAttackKind.CursedCounter &&
                    !_counterTriggered)
                {
                    StartAction(CatacombCombatState.KnightRecovery, .44f);
                }
                else
                {
                    StartAction(
                        CatacombCombatState.KnightActive,
                        FallenKnightActiveSecondsFor(FallenKnightAttack));
                }
                break;
            case CatacombCombatState.KnightActive:
                StartAction(
                    CatacombCombatState.KnightRecovery,
                    FallenKnightRecoverySecondsFor(FallenKnightAttack));
                break;
            case CatacombCombatState.KnightRecovery:
                _lastFallenKnightAttack = FallenKnightAttack;
                FallenKnightAttack = FallenKnightAttackKind.None;
                _counterTriggered = false;
                CombatState = CatacombCombatState.KnightApproach;
                _actionDuration = 0f;
                break;
        }
    }

    private void UpdateFallenKnightActive(
        PlayerCharacter player,
        DungeonMap dungeon,
        float dt)
    {
        if (FallenKnightAttack is FallenKnightAttackKind.AdvancingThrust or
            FallenKnightAttackKind.OathbreakerRush)
        {
            float speed = FallenKnightAttack ==
                FallenKnightAttackKind.OathbreakerRush ? 285f : 205f;
            MoveBy(new Vector2(_lockedAttackDirection * speed * dt, 0f), dungeon);
        }
        else if (FallenKnightAttack == FallenKnightAttackKind.GraveStep)
        {
            MoveBy(new Vector2(_lockedAttackDirection * 330f * dt, 0f), dungeon);
            _actionHitConsumed = true;
            return;
        }

        if (_actionHitConsumed)
            return;

        bool hits = FallenKnightAttack == FallenKnightAttackKind.LastJudgment
            ? IsInsideLastJudgmentDanger(player.Bounds)
            : ActiveAttackArea.Intersects(player.Bounds);
        if (!hits)
            return;

        int damage = FallenKnightAttack switch
        {
            FallenKnightAttackKind.HeavyOverhead =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.48f),
            FallenKnightAttackKind.CursedExtensionSlash =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.22f),
            FallenKnightAttackKind.FinalOathCleave =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.70f),
            FallenKnightAttackKind.OathbreakerRush =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.35f),
            FallenKnightAttackKind.LastJudgment =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.52f),
            FallenKnightAttackKind.CursedCounter =>
                (int)MathF.Ceiling(EffectiveAttackDamage * 1.38f),
            _ => EffectiveAttackDamage
        };
        AttackResolution result = player.ReceiveMeleeAttack(
            new AttackContact(
                damage,
                Position,
                blockable: FallenKnightAttack !=
                    FallenKnightAttackKind.LastJudgment,
                unblockable: false,
                ActiveAttackArea));
        if (result is AttackResolution.Damaged or AttackResolution.GuardBroken)
        {
            float push = FallenKnightAttack is
                FallenKnightAttackKind.FinalOathCleave or
                FallenKnightAttackKind.LastJudgment ? 38f : 24f;
            player.PushAwayFrom(Position, push, dungeon);
            if (_activeFallenPhase >= 2)
                _pendingCursePressure += 3f;
        }
        if (result == AttackResolution.PerfectGuard)
            ApplyPoiseDamage(MaxPoise * .25f);
        _actionHitConsumed = true;
    }

    private bool IsInsideLastJudgmentDanger(Rectangle playerBounds)
    {
        float distance = MathF.Abs(playerBounds.Center.X - Position.X);
        return distance >= 92f && distance <= 335f &&
            MathF.Abs(playerBounds.Center.Y - Position.Y) <= 105f;
    }

    private void SelectFallenKnightAttack(float distance)
    {
        _fallenKnightPatternIndex++;
        FallenKnightAttackKind selected;
        if (_activeFallenPhase == 1)
        {
            selected = distance > 132f
                ? FallenKnightAttackKind.AdvancingThrust
                : (_fallenKnightPatternIndex % 4) switch
                {
                    0 => FallenKnightAttackKind.HeavyOverhead,
                    2 => FallenKnightAttackKind.GuardedCounter,
                    _ => FallenKnightAttackKind.RoyalSlash
                };
        }
        else if (_activeFallenPhase == 2)
        {
            selected = (_fallenKnightPatternIndex % 5) switch
            {
                0 => FallenKnightAttackKind.CursedCounter,
                2 when distance < 88f => FallenKnightAttackKind.GraveStep,
                2 => FallenKnightAttackKind.AdvancingThrust,
                3 => FallenKnightAttackKind.CursedExtensionSlash,
                4 => FallenKnightAttackKind.HeavyOverhead,
                _ => FallenKnightAttackKind.RoyalSlash
            };
        }
        else
        {
            selected = (_fallenKnightPatternIndex % 6) switch
            {
                0 => FallenKnightAttackKind.LastJudgment,
                2 => FallenKnightAttackKind.FinalOathCleave,
                3 when distance > 92f => FallenKnightAttackKind.OathbreakerRush,
                3 => FallenKnightAttackKind.CursedCounter,
                4 => FallenKnightAttackKind.CursedExtensionSlash,
                5 => FallenKnightAttackKind.HeavyOverhead,
                _ => FallenKnightAttackKind.RoyalSlash
            };
        }

        if (selected == _lastFallenKnightAttack)
            selected = _activeFallenPhase >= 2
                ? FallenKnightAttackKind.CursedExtensionSlash
                : FallenKnightAttackKind.RoyalSlash;

        FallenKnightAttack = selected;
        _counterTriggered = selected == FallenKnightAttackKind.GuardedCounter;
        float facing = Facing == EnemyFacingDirection.Left ? -1f : 1f;
        _lockedAttackDirection = selected == FallenKnightAttackKind.GraveStep
            ? -facing
            : facing;
        StartAction(
            CatacombCombatState.KnightWindup,
            FallenKnightWindupSecondsFor(selected));
    }

    private void UpdateSoulCollector(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float dt)
    {
        float distance = MathF.Abs(player.Position.X - Position.X);
        if (CombatState == CatacombCombatState.SoulApproach)
        {
            if (distance < 96f)
            {
                SoulCollectorAttack = SoulCollectorAttackKind.SoulBurst;
                StartAction(
                    CatacombCombatState.SoulWindup,
                    SoulBurstWindupSeconds);
                return;
            }
            if (distance > 390f)
            {
                MoveToward(gameTime, player.Position, 330f, dungeon, .46f);
                return;
            }
            StartAction(CatacombCombatState.SoulReady, SoulReadySeconds);
            return;
        }

        _actionTimeRemaining -= dt;
        if (CombatState == CatacombCombatState.SoulActive &&
            !_actionHitConsumed)
        {
            ResolveSoulCollectorAction(player, dungeon, projectiles);
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (CombatState)
        {
            case CatacombCombatState.SoulReady:
                SelectSoulCollectorAction(player);
                break;
            case CatacombCombatState.SoulWindup:
                StartAction(
                    CatacombCombatState.SoulActive,
                    SoulActionActiveSeconds);
                break;
            case CatacombCombatState.SoulActive:
                StartAction(
                    CatacombCombatState.SoulRecovery,
                    SoulRecoverySecondsFor(SoulCollectorAttack));
                break;
            case CatacombCombatState.SoulRecovery:
                SoulCollectorAttack = SoulCollectorAttackKind.None;
                _ritualPreviewBounds = Rectangle.Empty;
                CombatState = CatacombCombatState.SoulApproach;
                _actionDuration = 0f;
                break;
        }
    }

    private void SelectSoulCollectorAction(PlayerCharacter player)
    {
        _soulPatternIndex++;
        SoulCollectorAttack = (_soulPatternIndex % 4) switch
        {
            1 => SoulCollectorAttackKind.RitualCurseField,
            2 => SoulCollectorAttackKind.SoulBolt,
            3 => SoulCollectorAttackKind.GravePull,
            _ => SoulCollectorAttackKind.SoulBolt
        };
        if (SoulCollectorAttack == SoulCollectorAttackKind.RitualCurseField)
        {
            int floorY = player.Bounds.Bottom;
            _ritualPreviewBounds = new Rectangle(
                (int)player.Position.X - 92,
                floorY - 28,
                184,
                32);
        }
        StartAction(
            CatacombCombatState.SoulWindup,
            SoulWindupSecondsFor(SoulCollectorAttack));
    }

    private void ResolveSoulCollectorAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles)
    {
        switch (SoulCollectorAttack)
        {
            case SoulCollectorAttackKind.SoulBolt:
                projectiles.SpawnSoulBolt(
                    Position + new Vector2(
                        Facing == EnemyFacingDirection.Left ? -20f : 20f,
                        -17f),
                    player.Position - Position,
                    EffectiveAttackDamage,
                    RoomId);
                break;
            case SoulCollectorAttackKind.RitualCurseField:
                _soulFieldBounds = _ritualPreviewBounds;
                _soulFieldTimeRemaining = RitualFieldDurationSeconds;
                break;
            case SoulCollectorAttackKind.GravePull:
                if (Vector2.DistanceSquared(Position, player.Position) <=
                    360f * 360f)
                {
                    player.ReceiveMeleeAttack(new AttackContact(
                        Math.Max(1, EffectiveAttackDamage / 3),
                        Position,
                        blockable: false,
                        unblockable: false,
                        ActiveAttackArea));
                    player.PullToward(Position, 30f, dungeon);
                    _pendingCursePressure += 5f;
                }
                break;
            case SoulCollectorAttackKind.SoulBurst:
                if (ActiveAttackArea.Intersects(player.Bounds))
                {
                    player.ReceiveMeleeAttack(new AttackContact(
                        Math.Max(1, EffectiveAttackDamage * 2 / 3),
                        Position,
                        blockable: true,
                        unblockable: false,
                        ActiveAttackArea));
                    player.PushAwayFrom(Position, 30f, dungeon);
                    _pendingCursePressure += 3f;
                }
                break;
        }
    }

    private bool TrySafeRetreat(
        GameTime gameTime,
        Vector2 threat,
        DungeonMap dungeon,
        float speedMultiplier)
    {
        float direction = Position.X < threat.X ? -1f : 1f;
        float seconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        float distance = EffectiveMovementSpeed * speedMultiplier * seconds;
        Vector2 desired = Position + new Vector2(direction * distance, 0f);
        Vector2 resolved = SideScrollingCollision.ResolveDisplacement(
            Position,
            desired - Position,
            Size,
            dungeon);
        if (MathF.Abs(resolved.X - Position.X) < .25f ||
            !SideScrollingCollision.IsSupported(resolved, Size, dungeon))
        {
            return false;
        }

        MoveBy(resolved - Position, dungeon);
        return true;
    }

    private void StartMelee(SkeletonAttackKind kind)
    {
        SkeletonAttack = kind;
        StartAction(
            CatacombCombatState.MeleeWindup,
            WindupSecondsFor(kind));
    }

    private void StartAction(CatacombCombatState state, float duration)
    {
        CombatState = state;
        _actionDuration = duration;
        _actionTimeRemaining = duration;
        _actionHitConsumed = false;
    }

    private void CancelPolishedAttack()
    {
        CombatState = CatacombCombatState.Ready;
        SkeletonAttack = SkeletonAttackKind.None;
        RottenCorpseAttack = RottenCorpseAttackKind.None;
        WraithAttack = WraithAttackKind.None;
        UndeadGuardAttack = UndeadGuardAttackKind.None;
        GraveBatAttack = GraveBatAttackKind.None;
        CursedKnightAttack = CursedKnightAttackKind.None;
        DeathKnightAttack = DeathKnightAttackKind.None;
        FallenKnightAttack = FallenKnightAttackKind.None;
        SoulCollectorAttack = SoulCollectorAttackKind.None;
        IsHeavyArrow = false;
        IsRetreating = false;
        _returnCutQueued = false;
        _actionHitConsumed = false;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _dashDistanceRemaining = 0f;
        _counterTriggered = false;
        _ritualPreviewBounds = Rectangle.Empty;
        _specialCooldown = MathF.Max(_specialCooldown, .35f);
        if (IsAlive && Type == EnemyType.GraveBat)
            StartAction(CatacombCombatState.GraveRetreat, GraveRetreatSeconds);
        else if (IsAlive && Type == EnemyType.CursedKnight)
            CombatState = CatacombCombatState.KnightApproach;
        else if (IsAlive && Type is EnemyType.DeathKnight or
            EnemyType.FallenKnight)
            CombatState = CatacombCombatState.KnightApproach;
        else if (IsAlive && Type == EnemyType.SoulCollector)
            CombatState = CatacombCombatState.SoulApproach;
    }

    private Rectangle CreatePolishedAttackArea()
    {
        bool lockedKnightAttack =
            (Type is EnemyType.DeathKnight or EnemyType.FallenKnight) &&
            (CombatState is CatacombCombatState.KnightWindup or
                CatacombCombatState.KnightActive);
        bool left = lockedKnightAttack
            ? _lockedAttackDirection < 0f
            : Facing == EnemyFacingDirection.Left;

        if (Type == EnemyType.RottenCorpse)
        {
            int reach = RottenCorpseAttack switch
            {
                RottenCorpseAttackKind.BodySlam => 60,
                RottenCorpseAttackKind.RotVomit => 108,
                _ => 48
            };
            int height = RottenCorpseAttack == RottenCorpseAttackKind.BodySlam
                ? 44
                : 38;
            int y = Bounds.Bottom - height -
                (RottenCorpseAttack == RottenCorpseAttackKind.RotVomit
                    ? 15
                    : 3);
            return left
                ? new Rectangle(Bounds.Left - reach + 12, y, reach, height)
                : new Rectangle(Bounds.Right - 12, y, reach, height);
        }

        if (Type == EnemyType.Wraith)
        {
            int reach = WraithAttack switch
            {
                WraithAttackKind.SpectralDash => 82,
                WraithAttackKind.CurseWave => 126,
                _ => 56
            };
            int height = WraithAttack == WraithAttackKind.CurseWave ? 34 : 48;
            int y = Bounds.Bottom - height -
                (WraithAttack == WraithAttackKind.CurseWave ? 0 : 8);
            return left
                ? new Rectangle(Bounds.Left - reach + 8, y, reach, height)
                : new Rectangle(Bounds.Right - 8, y, reach, height);
        }

        if (Type == EnemyType.UndeadGuard)
        {
            int reach = UndeadGuardAttack switch
            {
                UndeadGuardAttackKind.ShieldBash => 43,
                UndeadGuardAttackKind.HeavyCleave => 76,
                _ => 62
            };
            int height = UndeadGuardAttack ==
                UndeadGuardAttackKind.ShieldBash ? 45 : 52;
            int y = Bounds.Bottom - height - 5;
            return left
                ? new Rectangle(Bounds.Left - reach + 10, y, reach, height)
                : new Rectangle(Bounds.Right - 10, y, reach, height);
        }

        if (Type == EnemyType.GraveBat)
        {
            if (GraveBatAttack == GraveBatAttackKind.GraveScreech)
            {
                const int radius = 112;
                return new Rectangle(
                    (int)Position.X - radius,
                    (int)Position.Y - radius,
                    radius * 2,
                    radius * 2);
            }

            int reach = GraveBatAttack == GraveBatAttackKind.SwarmFeint
                ? 34
                : 48;
            return left
                ? new Rectangle(Bounds.Left - reach / 2, Bounds.Top - 4,
                    Bounds.Width + reach, Bounds.Height + 8)
                : new Rectangle(Bounds.Left - reach / 2, Bounds.Top - 4,
                    Bounds.Width + reach, Bounds.Height + 8);
        }

        if (Type == EnemyType.CursedKnight)
        {
            int reach = CursedKnightAttack switch
            {
                CursedKnightAttackKind.CursedSweep => 112,
                CursedKnightAttackKind.ExecutionStrike => 82,
                CursedKnightAttackKind.AdvancingThrust => 108,
                _ => 72
            };
            int height = CursedKnightAttack ==
                CursedKnightAttackKind.ExecutionStrike ? 78 : 58;
            int y = Bounds.Bottom - height - 2;
            return left
                ? new Rectangle(Bounds.Left - reach + 12, y, reach, height)
                : new Rectangle(Bounds.Right - 12, y, reach, height);
        }

        if (Type == EnemyType.DeathKnight)
        {
            int reach = DeathKnightAttack switch
            {
                DeathKnightAttackKind.WarSweep => 142,
                DeathKnightAttackKind.ExecutionCrush => 96,
                DeathKnightAttackKind.DreadCharge => 112,
                DeathKnightAttackKind.TombbreakerSlam => 168,
                _ => 84
            };
            int height = DeathKnightAttack switch
            {
                DeathKnightAttackKind.ExecutionCrush => 92,
                DeathKnightAttackKind.TombbreakerSlam => 42,
                _ => 68
            };
            int y = DeathKnightAttack == DeathKnightAttackKind.TombbreakerSlam
                ? Bounds.Bottom - 34
                : Bounds.Bottom - height - 2;
            if (DeathKnightAttack == DeathKnightAttackKind.TombbreakerSlam)
            {
                return new Rectangle(
                    (int)Position.X - reach,
                    y,
                    reach * 2,
                    height);
            }
            return left
                ? new Rectangle(Bounds.Left - reach + 14, y, reach, height)
                : new Rectangle(Bounds.Right - 14, y, reach, height);
        }

        if (Type == EnemyType.FallenKnight)
        {
            if (FallenKnightAttack == FallenKnightAttackKind.GraveStep)
                return Rectangle.Empty;
            if (FallenKnightAttack == FallenKnightAttackKind.LastJudgment)
            {
                return new Rectangle(
                    (int)Position.X - 335,
                    Bounds.Bottom - 92,
                    670,
                    112);
            }
            int reach = FallenKnightAttack switch
            {
                FallenKnightAttackKind.AdvancingThrust => 126,
                FallenKnightAttackKind.HeavyOverhead => 98,
                FallenKnightAttackKind.CursedExtensionSlash => 174,
                FallenKnightAttackKind.CursedCounter => 118,
                FallenKnightAttackKind.GuardedCounter => 108,
                FallenKnightAttackKind.FinalOathCleave => 218,
                FallenKnightAttackKind.OathbreakerRush => 132,
                _ => 116
            };
            int height = FallenKnightAttack is
                FallenKnightAttackKind.HeavyOverhead or
                FallenKnightAttackKind.FinalOathCleave ? 104 : 72;
            int y = Bounds.Bottom - height - 2;
            return left
                ? new Rectangle(Bounds.Left - reach + 14, y, reach, height)
                : new Rectangle(Bounds.Right - 14, y, reach, height);
        }

        if (Type == EnemyType.SoulCollector)
        {
            if (SoulCollectorAttack == SoulCollectorAttackKind.SoulBurst)
            {
                const int radius = 105;
                return new Rectangle(
                    (int)Position.X - radius,
                    (int)Position.Y - radius,
                    radius * 2,
                    radius * 2);
            }
            if (SoulCollectorAttack == SoulCollectorAttackKind.GravePull)
            {
                return new Rectangle(
                    (int)Position.X - 360,
                    (int)Position.Y - 70,
                    720,
                    140);
            }
            return Bounds;
        }

        if (Type == EnemyType.SkeletonArcher)
        {
            const int shoveReach = 42;
            const int shoveHeight = 38;
            int shoveY = Bounds.Bottom - shoveHeight - 7;
            return left
                ? new Rectangle(
                    Bounds.Left - shoveReach + 7,
                    shoveY,
                    shoveReach,
                    shoveHeight)
                : new Rectangle(
                    Bounds.Right - 7,
                    shoveY,
                    shoveReach,
                    shoveHeight);
        }

        float facing = left ? -1f : 1f;
        Vector2 direction = new(
            MathF.Cos(MeleeWeaponAngle) * facing,
            MathF.Sin(MeleeWeaponAngle));
        Vector2 shoulder = new(
            Position.X + facing * 10f,
            Bounds.Bottom - 47f);
        Vector2 hand = shoulder + direction * 17f;
        Vector2 tip = hand + direction * 34f;
        const int padding = 9;
        int minX = (int)MathF.Floor(MathF.Min(hand.X, tip.X)) - padding;
        int maxX = (int)MathF.Ceiling(MathF.Max(hand.X, tip.X)) + padding;
        int minY = (int)MathF.Floor(MathF.Min(hand.Y, tip.Y)) - padding;
        int maxY = (int)MathF.Ceiling(MathF.Max(hand.Y, tip.Y)) + padding;
        return new Rectangle(
            minX,
            minY,
            Math.Max(1, maxX - minX),
            Math.Max(1, maxY - minY));
    }

    private static float WindupSecondsFor(SkeletonAttackKind kind) =>
        kind switch
        {
            SkeletonAttackKind.ReturnCut => ReturnCutWindupSeconds,
            SkeletonAttackKind.OverheadChop => OverheadWindupSeconds,
            _ => SlashWindupSeconds
        };

    private static float ActiveSecondsFor(SkeletonAttackKind kind) =>
        kind switch
        {
            SkeletonAttackKind.ReturnCut => ReturnCutActiveSeconds,
            SkeletonAttackKind.OverheadChop => OverheadActiveSeconds,
            _ => SlashActiveSeconds
        };

    private static float RecoverySecondsFor(SkeletonAttackKind kind) =>
        kind switch
        {
            SkeletonAttackKind.ReturnCut => ReturnCutRecoverySeconds,
            SkeletonAttackKind.OverheadChop => OverheadRecoverySeconds,
            _ => SlashRecoverySeconds
        };

    private static float RottenActiveSecondsFor(
        RottenCorpseAttackKind kind) => kind switch
        {
            RottenCorpseAttackKind.BodySlam => BodySlamActiveSeconds,
            RottenCorpseAttackKind.RotVomit => RotVomitActiveSeconds,
            _ => GrabActiveSeconds
        };

    private static float RottenRecoverySecondsFor(
        RottenCorpseAttackKind kind) => kind switch
        {
            RottenCorpseAttackKind.BodySlam => BodySlamRecoverySeconds,
            RottenCorpseAttackKind.RotVomit => RotVomitRecoverySeconds,
            _ => GrabRecoverySeconds
        };

    private static float WraithActiveSecondsFor(WraithAttackKind kind) =>
        kind switch
        {
            WraithAttackKind.SpectralDash => DashActiveSeconds,
            WraithAttackKind.CurseWave => CurseWaveActiveSeconds,
            _ => ClawActiveSeconds
        };

    private static float WraithRecoverySecondsFor(WraithAttackKind kind) =>
        kind switch
        {
            WraithAttackKind.SpectralDash => DashRecoverySeconds,
            WraithAttackKind.CurseWave => CurseWaveRecoverySeconds,
            _ => ClawRecoverySeconds
        };

    private static float GuardWindupSecondsFor(
        UndeadGuardAttackKind kind) => kind switch
        {
            UndeadGuardAttackKind.ShieldBash => ShieldBashWindupSeconds,
            UndeadGuardAttackKind.HeavyCleave => HeavyGuardWindupSeconds,
            _ => GuardedStrikeWindupSeconds
        };

    private static float GuardActiveSecondsFor(
        UndeadGuardAttackKind kind) => kind switch
        {
            UndeadGuardAttackKind.ShieldBash => ShieldBashActiveSeconds,
            UndeadGuardAttackKind.HeavyCleave => HeavyGuardActiveSeconds,
            _ => GuardedStrikeActiveSeconds
        };

    private static float GuardRecoverySecondsFor(
        UndeadGuardAttackKind kind) => kind switch
        {
            UndeadGuardAttackKind.ShieldBash => ShieldBashRecoverySeconds,
            UndeadGuardAttackKind.HeavyCleave => HeavyGuardRecoverySeconds,
            _ => GuardedStrikeRecoverySeconds
        };

    private static float GraveActiveSecondsFor(GraveBatAttackKind kind) =>
        kind switch
        {
            GraveBatAttackKind.GraveScreech => GraveScreechActiveSeconds,
            GraveBatAttackKind.ClawPass => GraveClawActiveSeconds,
            _ => GraveFeintActiveSeconds
        };

    private static float GraveRecoverySecondsFor(GraveBatAttackKind kind) =>
        kind switch
        {
            GraveBatAttackKind.GraveScreech => GraveScreechRecoverySeconds,
            GraveBatAttackKind.ClawPass => GraveClawRecoverySeconds,
            _ => GraveFeintRecoverySeconds
        };

    private static float KnightWindupSecondsFor(
        CursedKnightAttackKind kind) => kind switch
        {
            CursedKnightAttackKind.ExecutionStrike => ExecutionWindupSeconds,
            CursedKnightAttackKind.AdvancingThrust => ThrustWindupSeconds,
            _ => SweepWindupSeconds
        };

    private static float KnightActiveSecondsFor(
        CursedKnightAttackKind kind) => kind switch
        {
            CursedKnightAttackKind.ExecutionStrike => ExecutionActiveSeconds,
            CursedKnightAttackKind.AdvancingThrust => ThrustActiveSeconds,
            _ => SweepActiveSeconds
        };

    private static float KnightRecoverySecondsFor(
        CursedKnightAttackKind kind) => kind switch
        {
            CursedKnightAttackKind.ExecutionStrike => ExecutionRecoverySeconds,
            CursedKnightAttackKind.AdvancingThrust => ThrustRecoverySeconds,
            _ => SweepRecoverySeconds
        };

    private static float DeathKnightWindupSecondsFor(
        DeathKnightAttackKind kind) => kind switch
        {
            DeathKnightAttackKind.ExecutionCrush => DeathCrushWindupSeconds,
            DeathKnightAttackKind.DreadCharge => DeathChargeWindupSeconds,
            DeathKnightAttackKind.TombbreakerSlam => TombbreakerWindupSeconds,
            _ => DeathSweepWindupSeconds
        };

    private static float DeathKnightActiveSecondsFor(
        DeathKnightAttackKind kind) => kind switch
        {
            DeathKnightAttackKind.ExecutionCrush => DeathCrushActiveSeconds,
            DeathKnightAttackKind.DreadCharge => DeathChargeActiveSeconds,
            DeathKnightAttackKind.TombbreakerSlam => TombbreakerActiveSeconds,
            _ => DeathSweepActiveSeconds
        };

    private static float DeathKnightRecoverySecondsFor(
        DeathKnightAttackKind kind) => kind switch
        {
            DeathKnightAttackKind.ExecutionCrush => DeathCrushRecoverySeconds,
            DeathKnightAttackKind.DreadCharge => DeathChargeRecoverySeconds,
            DeathKnightAttackKind.TombbreakerSlam => TombbreakerRecoverySeconds,
            _ => DeathSweepRecoverySeconds
        };

    private static float FallenKnightWindupSecondsFor(
        FallenKnightAttackKind kind) => kind switch
        {
            FallenKnightAttackKind.HeavyOverhead => .88f,
            FallenKnightAttackKind.AdvancingThrust => .56f,
            FallenKnightAttackKind.GuardedCounter => .62f,
            FallenKnightAttackKind.CursedExtensionSlash => .58f,
            FallenKnightAttackKind.GraveStep => .34f,
            FallenKnightAttackKind.CursedCounter => .58f,
            FallenKnightAttackKind.FinalOathCleave => .98f,
            FallenKnightAttackKind.OathbreakerRush => .64f,
            FallenKnightAttackKind.LastJudgment => .96f,
            _ => .46f
        };

    private static float FallenKnightActiveSecondsFor(
        FallenKnightAttackKind kind) => kind switch
        {
            FallenKnightAttackKind.AdvancingThrust => .25f,
            FallenKnightAttackKind.GraveStep => .28f,
            FallenKnightAttackKind.OathbreakerRush => .62f,
            FallenKnightAttackKind.LastJudgment => .24f,
            FallenKnightAttackKind.FinalOathCleave => .22f,
            _ => .17f
        };

    private static float FallenKnightRecoverySecondsFor(
        FallenKnightAttackKind kind) => kind switch
        {
            FallenKnightAttackKind.HeavyOverhead => .78f,
            FallenKnightAttackKind.GuardedCounter => .58f,
            FallenKnightAttackKind.CursedExtensionSlash => .62f,
            FallenKnightAttackKind.GraveStep => .48f,
            FallenKnightAttackKind.CursedCounter => .64f,
            FallenKnightAttackKind.FinalOathCleave => 1.08f,
            FallenKnightAttackKind.OathbreakerRush => .82f,
            FallenKnightAttackKind.LastJudgment => 1.12f,
            _ => .48f
        };

    private static float SoulWindupSecondsFor(
        SoulCollectorAttackKind kind) => kind switch
        {
            SoulCollectorAttackKind.RitualCurseField =>
                RitualFieldWindupSeconds,
            SoulCollectorAttackKind.GravePull => GravePullWindupSeconds,
            SoulCollectorAttackKind.SoulBurst => SoulBurstWindupSeconds,
            _ => SoulBoltWindupSeconds
        };

    private static float SoulRecoverySecondsFor(
        SoulCollectorAttackKind kind) => kind switch
        {
            SoulCollectorAttackKind.RitualCurseField =>
                RitualFieldRecoverySeconds,
            SoulCollectorAttackKind.GravePull => GravePullRecoverySeconds,
            SoulCollectorAttackKind.SoulBurst => SoulBurstRecoverySeconds,
            _ => SoulBoltRecoverySeconds
        };

    private static Vector2 SizeFor(EnemyType type) => type switch
    {
        EnemyType.GraveBat => new Vector2(44f, 30f),
        EnemyType.RottenCorpse => new Vector2(58f, 68f),
        EnemyType.UndeadGuard => new Vector2(48f, 62f),
        EnemyType.CursedKnight => new Vector2(54f, 70f),
        EnemyType.DeathKnight => new Vector2(62f, 78f),
        EnemyType.SoulCollector => new Vector2(50f, 68f),
        EnemyType.FallenKnight => new Vector2(68f, 86f),
        EnemyType.SkeletonArcher => new Vector2(36f, 58f),
        EnemyType.Skeleton => new Vector2(40f, 56f),
        _ => new Vector2(40f, 54f)
    };

    private static float SpeedFor(EnemyType type) => type switch
    {
        EnemyType.GraveBat => 145f,
        EnemyType.Wraith => 125f,
        EnemyType.RottenCorpse => 55f,
        EnemyType.UndeadGuard => 68f,
        EnemyType.CursedKnight => 82f,
        EnemyType.DeathKnight => 76f,
        EnemyType.FallenKnight => 96f,
        EnemyType.SkeletonArcher => 52f,
        EnemyType.Skeleton => 88f,
        _ => 88f
    };

    private static int HealthFor(EnemyType type, int level, int worldTier)
    {
        int baseHealth = type switch
        {
            EnemyType.Skeleton => 95,
            EnemyType.SkeletonArcher => 80,
            EnemyType.GraveBat => 65,
            EnemyType.Wraith => 110,
            EnemyType.RottenCorpse => 175,
            EnemyType.UndeadGuard => 190,
            EnemyType.CursedKnight => 420,
            EnemyType.SoulCollector => 460,
            EnemyType.DeathKnight => 650,
            EnemyType.FallenKnight => 1500,
            _ => 100
        };
        return WorldProgression.ApplyPercent(
            baseHealth + (Math.Max(1, level) - 1) * 20,
            WorldProgression.GetHealthMultiplierPercent(worldTier));
    }

    private static int DamageFor(EnemyType type, int level, int worldTier)
    {
        int baseDamage = type switch
        {
            EnemyType.RottenCorpse => 18,
            EnemyType.CursedKnight => 24,
            EnemyType.SoulCollector => 20,
            EnemyType.DeathKnight => 28,
            EnemyType.FallenKnight => 32,
            _ => 12
        };
        return WorldProgression.ApplyPercent(
            baseDamage + (Math.Max(1, level) - 1) * 2,
            WorldProgression.GetDamageMultiplierPercent(worldTier));
    }

    private static int RewardFor(EnemyType type) => type switch
    {
        EnemyType.CursedKnight => 240,
        EnemyType.SoulCollector => 280,
        EnemyType.DeathKnight => 400,
        EnemyType.FallenKnight => 1000,
        _ => 75
    };

    private static float RangeFor(EnemyType type) => type switch
    {
        EnemyType.FallenKnight => 105f,
        EnemyType.DeathKnight => 96f,
        EnemyType.CursedKnight => 88f,
        EnemyType.UndeadGuard => 76f,
        EnemyType.RottenCorpse => 70f,
        EnemyType.Skeleton => 67f,
        _ => 55f
    };

    private static float CooldownFor(EnemyType type) => type switch
    {
        EnemyType.FallenKnight => 1.05f,
        EnemyType.DeathKnight => 1.2f,
        EnemyType.CursedKnight => 1.3f,
        _ => 1.5f
    };
}
