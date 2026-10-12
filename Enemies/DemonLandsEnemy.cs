using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public enum HornedGeneralAttackKind
{
    None,
    LegionThrust,
    HornedSweep,
    CommandGuard,
    WarStomp,
    TacticalStep,
    RiftCleave,
    CorruptionPulse,
    SummonAnchor,
    InfernalCharge,
    LegionsEnd
}

public enum DemonLegionnaireAttackKind
{
    None,
    LegionThrust,
    InfernalShieldBash,
    FormationSweep,
    CommandGuard
}

public enum DemonFrontierCombatState
{
    Ready,
    ImpSlashWindup,
    ImpSlashActive,
    ImpSlashRecovery,
    ImpPounceCrouch,
    ImpPounceActive,
    ImpPounceRecovery,
    ImpFrenzyWindup,
    ImpFrenzyActive,
    ImpFrenzyRecovery,
    ArcherDraw,
    ArcherRelease,
    ArcherRecovery,
    ArcherReposition,
    ArcherRetreat,
    HoundThreaten,
    HoundLeapCrouch,
    HoundLeapActive,
    HoundLeapRecovery,
    HoundRushWindup,
    HoundRushActive,
    HoundRushRecovery,
    HoundSpineFlare,
    HoundSpineBurstActive,
    HoundSpineBurstRecovery,
    HoundReposition,
    StalkerSlashWindup,
    StalkerSlashActive,
    StalkerSlashRecovery,
    StalkerRiftEntry,
    StalkerRiftHidden,
    StalkerRiftExit,
    StalkerAmbushWindup,
    StalkerAmbushActive,
    StalkerAmbushRecovery,
    StalkerVoidEntry,
    StalkerVoidHidden,
    StalkerVoidExit,
    StalkerVoidRecovery,
    StalkerTwinWindup,
    StalkerTwinFirst,
    StalkerTwinGap,
    StalkerTwinSecond,
    StalkerTwinRecovery,
    StalkerBackstep,
    HarvesterSweepWindup,
    HarvesterSweepActive,
    HarvesterSweepRecovery,
    HarvesterHookWindup,
    HarvesterHookActive,
    HarvesterHookRecovery,
    HarvesterReapWindup,
    HarvesterReapActive,
    HarvesterReapRecovery,
    BruteSlamWindup,
    BruteSlamActive,
    BruteSlamRecovery,
    BruteChargeWindup,
    BruteChargeActive,
    BruteChargeRecovery,
    BruteBackhandWindup,
    BruteBackhandActive,
    BruteBackhandRecovery,
    BruteSeismicWindup,
    BruteSeismicActive,
    BruteSeismicRecovery,
    BruteEnrage,
    PriestChantWindup,
    PriestChantActive,
    PriestChantRecovery,
    PriestAnchorWindup,
    PriestAnchorActive,
    PriestAnchorRecovery,
    PriestGroundWindup,
    PriestGroundActive,
    PriestGroundRecovery,
    PriestStaffWindup,
    PriestStaffActive,
    PriestStaffRecovery,
    PriestFinalRiteWindup,
    PriestFinalRiteActive,
    PriestFinalRiteRecovery,
    PriestRetreat,
    WardenThrustWindup,
    WardenThrustActive,
    WardenThrustRecovery,
    WardenGateWindup,
    WardenGateOpen,
    WardenGateAttackWindup,
    WardenGateAttackActive,
    WardenGateRecovery,
    WardenSweepWindup,
    WardenSweepActive,
    WardenSweepRecovery,
    WardenStepEntry,
    WardenStepHidden,
    WardenStepExit,
    WardenStepRecovery,
    LegionGuardStance,
    LegionThrustWindup,
    LegionThrustActive,
    LegionThrustRecovery,
    LegionBashWindup,
    LegionBashActive,
    LegionBashRecovery,
    LegionSweepWindup,
    LegionSweepActive,
    LegionSweepRecovery,
    LegionCommandGuard,
    LegionCommandRecovery,
    LegionGuardBroken,
    GeneralPhaseTransition,
    GeneralThrustWindup,
    GeneralThrustActive,
    GeneralThrustRecovery,
    GeneralSweepWindup,
    GeneralSweepActive,
    GeneralSweepRecovery,
    GeneralGuard,
    GeneralGuardRecovery,
    GeneralGuardBroken,
    GeneralStompWindup,
    GeneralStompActive,
    GeneralStompRecovery,
    GeneralTacticalStep,
    GeneralRiftCleaveWindup,
    GeneralRiftCleaveActive,
    GeneralRiftCleaveRecovery,
    GeneralPulseWindup,
    GeneralPulseActive,
    GeneralPulseRecovery,
    GeneralAnchorWindup,
    GeneralAnchorActive,
    GeneralAnchorRecovery,
    GeneralChargeWindup,
    GeneralChargeActive,
    GeneralChargeRecovery,
    GeneralLegionsEndRaise,
    GeneralLegionsEndWarning,
    GeneralLegionsEndPause,
    GeneralLegionsEndCharge,
    GeneralLegionsEndCleave,
    GeneralLegionsEndRecovery
}

public enum ImpRaiderAttackKind
{
    None,
    RaiderSlash,
    PouncingCut,
    FrenziedStab
}

public enum HellArcherAttackKind
{
    None,
    EmberArrow,
    HighArcShot,
    RetreatShot
}

public enum SpineHoundAttackKind
{
    None,
    SpineLeap,
    BodyRush,
    SpineBurst
}

public enum RiftStalkerAttackKind
{
    None,
    RiftSlash,
    RiftAmbush,
    VoidStep,
    TwinClawSequence
}

public enum BoneHarvesterAttackKind
{
    None,
    ReapingSweep,
    HarvesterHook,
    GraveReap
}

public enum HellbruteAttackKind
{
    None,
    BruteSlam,
    InfernalCharge,
    CrushingBackhand,
    SeismicPound
}

public enum CorruptionPriestAbilityKind
{
    None,
    CorruptionChant,
    AnchorBlessing,
    TaintedGround,
    StaffStrike,
    FinalRite
}

public enum RiftWardenAttackKind
{
    None,
    RiftThrust,
    RiftGate,
    WarpSweep,
    WardenStep
}

/// <summary>
/// Basic shared Map 4 combat foundation. It intentionally uses readable,
/// compact state and placeholder projectiles until each demon receives its
/// dedicated polish pass.
/// </summary>
public sealed class DemonLandsEnemy : Enemy
{
    public const float ImpSlashWindupSeconds = .18f;
    public const float ImpSlashActiveSeconds = .10f;
    public const float ImpSlashRecoverySeconds = .22f;
    public const float ImpPounceCrouchSeconds = .34f;
    public const float ImpPounceActiveSeconds = .24f;
    public const float ImpPounceRecoverySeconds = .31f;
    public const float ImpFrenzyWindupSeconds = .17f;
    public const float ImpFrenzyActiveSeconds = .08f;
    public const float ImpFrenzyRecoverySeconds = .30f;
    public const int ImpMaximumFrenzyStabs = 3;
    public const float ArcherStandardDrawSeconds = .44f;
    public const float ArcherHighArcDrawSeconds = .72f;
    public const float ArcherRetreatDrawSeconds = .27f;
    public const float ArcherReleaseSeconds = .09f;
    public const float ArcherRecoverySeconds = .34f;
    public const float ArcherRepositionSeconds = .32f;
    public const float ArcherRetreatSeconds = .24f;
    public const float HoundThreatenSeconds = .24f;
    public const float HoundLeapCrouchSeconds = .34f;
    public const float HoundLeapActiveSeconds = .27f;
    public const float HoundRushWindupSeconds = .40f;
    public const float HoundRushActiveSeconds = .36f;
    public const float HoundSpineFlareSeconds = .44f;
    public const float HoundSpineBurstActiveSeconds = .12f;
    public const float StalkerRiftEntrySeconds = .30f;
    public const float StalkerRiftHiddenSeconds = .10f;
    public const float StalkerRiftExitSeconds = .32f;
    public const float StalkerAmbushReactionSeconds = .21f;
    public const int StalkerMaximumTwinClawHits = 2;
    public const float HarvesterSweepWindupSeconds = .54f;
    public const float HarvesterSweepActiveSeconds = .16f;
    public const float HarvesterHookWindupSeconds = .68f;
    public const float HarvesterHookActiveSeconds = .14f;
    public const float HarvesterReapWindupSeconds = .82f;
    public const float HarvesterReapActiveSeconds = .17f;
    public const float BruteSlamWindupSeconds = .82f;
    public const float BruteSlamActiveSeconds = .18f;
    public const float BruteChargeWindupSeconds = .72f;
    public const float BruteChargeActiveSeconds = .58f;
    public const float BruteBackhandWindupSeconds = .48f;
    public const float BruteBackhandActiveSeconds = .15f;
    public const float BruteSeismicWindupSeconds = .96f;
    public const float BruteSeismicActiveSeconds = .34f;
    public const float HarvesterMaximumPullDistance = 28f;
    public const float PriestChantWindupSeconds = .86f;
    public const float PriestAnchorWindupSeconds = .76f;
    public const float PriestGroundWindupSeconds = .68f;
    public const float PriestFinalRiteWindupSeconds = .92f;
    public const float PriestTaintedGroundDurationSeconds = 5.5f;
    public const float WardenThrustWindupSeconds = .42f;
    public const float WardenGateWindupSeconds = .72f;
    public const float WardenGateReactionSeconds = .34f;
    public const float WardenSweepWindupSeconds = .58f;
    public const float LegionGuardBreakSeconds = 1.05f;
    public const float GeneralGuardBreakSeconds = 1.18f;
    public const float GeneralPulseRadius = 142f;

    protected override float DeathPresentationDurationSeconds => Type switch
    {
        EnemyType.HornedGeneral => 2.55f,
        EnemyType.DemonLegionnaire => 1.35f,
        _ => base.DeathPresentationDurationSeconds
    };

    private float _rangedCooldown;
    private float _supportCooldown = 2f;
    private float _pendingCorruptionPressure;
    private bool _phaseTwoEntered;
    private bool _phaseThreeEntered;
    private float _actionDuration;
    private float _actionTimeRemaining;
    private float _polishCooldown;
    private float _pounceCooldown;
    private float _retreatShotCooldown;
    private float _spineLeapCooldown;
    private float _bodyRushCooldown;
    private float _spineBurstCooldown;
    private float _riftAmbushCooldown;
    private float _voidStepCooldown;
    private float _twinClawCooldown;
    private float _harvesterHookCooldown;
    private float _graveReapCooldown;
    private float _bruteChargeCooldown;
    private float _bruteSeismicCooldown;
    private float _priestChantCooldown;
    private float _priestAnchorCooldown;
    private float _priestGroundCooldown;
    private float _wardenGateCooldown;
    private float _wardenStepCooldown;
    private float _wardenSweepCooldown;
    private float _legionCommandCooldown;
    private float _generalGuardCooldown;
    private float _generalAnchorCooldown;
    private float _generalChargeCooldown;
    private float _generalPulseCooldown;
    private float _taintedGroundTimeRemaining;
    private float _anchorTetherTimeRemaining;
    private float _riftGateTimeRemaining;
    private float _riftScarTimeRemaining;
    private float _riftScarDamageCooldown;
    private float _corruptionPulseTimeRemaining;
    private float _generalRiftPathTimeRemaining;
    private float _legionGuardIntegrity = 105f;
    private float _generalGuardIntegrity = 145f;
    private float _guardReactionTimeRemaining;
    private float _lockedAttackDirection = 1f;
    private bool _actionHitConsumed;
    private int _frenzyStabsRemaining;
    private int _archerShotSequence;
    private int _houndAttackSequence;
    private int _stalkerAttackSequence;
    private int _harvesterAttackSequence;
    private int _twinClawsRemaining;
    private float _repositionDirection;
    private Vector2 _riftEntryPosition;
    private Vector2 _riftExitPosition;
    private bool _hasRiftExitPosition;
    private bool _bruteEnrageEntered;
    private bool _finalRiteUsed;
    private bool _anchorBlessingPending;
    private bool _legionsEndUsed;
    private bool _commandBuffApplied;
    private int _legionPatternIndex;
    private int _generalPatternIndex;
    private Rectangle _taintedGroundBounds;
    private string _blessedAnchorId = string.Empty;
    private Vector2 _blessedAnchorPosition;
    private Vector2 _riftGatePosition;
    private float _riftGateDirection = 1f;
    private Rectangle _riftScarBounds;
    private Rectangle _generalRiftPathBounds;
    private Vector2 _pulseCenter;
    private Vector2 _requestedBossAnchorPosition;
    private readonly float _formationSide;

    public int CombatPhase { get; private set; } = 1;
    public HornedGeneralAttackKind HornedGeneralAttack { get; private set; }
    public DemonLegionnaireAttackKind DemonLegionnaireAttack { get; private set; }
    public bool IsLegionsEndTelegraphing => Type == EnemyType.HornedGeneral &&
        (CombatPhase == 3 && FrontierCombatState ==
            DemonFrontierCombatState.GeneralPhaseTransition ||
        FrontierCombatState is DemonFrontierCombatState.GeneralLegionsEndRaise or
            DemonFrontierCombatState.GeneralLegionsEndWarning or
            DemonFrontierCombatState.GeneralLegionsEndPause);
    public bool AnchorPulsePending { get; private set; }
    public DemonFrontierCombatState FrontierCombatState { get; private set; }
    public ImpRaiderAttackKind ImpRaiderAttack { get; private set; }
    public HellArcherAttackKind HellArcherAttack { get; private set; }
    public SpineHoundAttackKind SpineHoundAttack { get; private set; }
    public RiftStalkerAttackKind RiftStalkerAttack { get; private set; }
    public BoneHarvesterAttackKind BoneHarvesterAttack { get; private set; }
    public HellbruteAttackKind HellbruteAttack { get; private set; }
    public CorruptionPriestAbilityKind CorruptionPriestAbility { get; private set; }
    public RiftWardenAttackKind RiftWardenAttack { get; private set; }
    public bool IsEnragedBrute => Type == EnemyType.Hellbrute &&
        CurrentHealth <= MaxHealth * .30f;
    public Vector2 RiftEntryPosition => _riftEntryPosition;
    public Vector2 RiftExitPosition => _riftExitPosition;
    public bool HasRiftExitPosition => _hasRiftExitPosition;
    public bool IsRiftHidden => Type == EnemyType.RiftStalker &&
        FrontierCombatState is DemonFrontierCombatState.StalkerRiftHidden or
            DemonFrontierCombatState.StalkerVoidHidden;
    public float RiftAmbushCooldownRemaining => _riftAmbushCooldown;
    public float VoidStepCooldownRemaining => _voidStepCooldown;
    public bool HasTaintedGround => IsAlive && _taintedGroundTimeRemaining > 0f;
    public Rectangle TaintedGroundBounds => _taintedGroundBounds;
    public float TaintedGroundTimeRemaining => _taintedGroundTimeRemaining;
    public bool HasAnchorTether => IsAlive &&
        _anchorTetherTimeRemaining > 0f && _blessedAnchorId.Length > 0;
    public string BlessedAnchorId => _blessedAnchorId;
    public Vector2 BlessedAnchorPosition => _blessedAnchorPosition;
    public bool HasRiftGate => IsAlive && _riftGateTimeRemaining > 0f;
    public Vector2 RiftGatePosition => _riftGatePosition;
    public float RiftGateTimeRemaining => _riftGateTimeRemaining;
    public bool HasRiftScar => IsAlive && _riftScarTimeRemaining > 0f;
    public Rectangle RiftScarBounds => _riftScarBounds;
    public bool IsWardenHidden => Type == EnemyType.RiftWarden &&
        FrontierCombatState == DemonFrontierCombatState.WardenStepHidden;
    public bool IsLegionGuarding => Type == EnemyType.DemonLegionnaire &&
        FrontierCombatState is DemonFrontierCombatState.LegionGuardStance or
            DemonFrontierCombatState.LegionCommandGuard;
    public bool IsGeneralGuarding => Type == EnemyType.HornedGeneral &&
        FrontierCombatState == DemonFrontierCombatState.GeneralGuard;
    public bool IsGuardBroken => Type switch
    {
        EnemyType.DemonLegionnaire =>
            FrontierCombatState == DemonFrontierCombatState.LegionGuardBroken,
        EnemyType.HornedGeneral =>
            FrontierCombatState == DemonFrontierCombatState.GeneralGuardBroken,
        _ => false
    };
    public bool IsGuardReacting => _guardReactionTimeRemaining > 0f;
    public float GuardIntegrityRatio => Type == EnemyType.HornedGeneral
        ? Math.Clamp(_generalGuardIntegrity / 145f, 0f, 1f)
        : Math.Clamp(_legionGuardIntegrity / 105f, 0f, 1f);
    public bool HasCorruptionPulse => IsAlive &&
        _corruptionPulseTimeRemaining > 0f;
    public Vector2 CorruptionPulseCenter => _pulseCenter;
    public float CorruptionPulseRadius => GeneralPulseRadius;
    public bool HasGeneralRiftPath => IsAlive &&
        _generalRiftPathTimeRemaining > 0f;
    public Rectangle GeneralRiftPathBounds => _generalRiftPathBounds;
    public Vector2 RequestedBossAnchorPosition => _requestedBossAnchorPosition;
    public bool AttackFacesLeft => _lockedAttackDirection < 0f;
    public float ActionProgress => _actionDuration <= 0f
        ? 0f
        : Math.Clamp(1f - _actionTimeRemaining / _actionDuration, 0f, 1f);
    public bool IsFrontierPolished => Type is
        EnemyType.ImpRaider or EnemyType.HellArcher or
        EnemyType.SpineHound or EnemyType.RiftStalker or
        EnemyType.BoneHarvester or EnemyType.Hellbrute or
        EnemyType.CorruptionPriest or EnemyType.RiftWarden or
        EnemyType.DemonLegionnaire or EnemyType.HornedGeneral;
    public override bool CanBeTargeted => base.CanBeTargeted &&
        !IsRiftHidden && !IsWardenHidden;
    public override bool UsesProjectileAttack => Type is
        EnemyType.HellArcher;
    public override bool IsCombatAttackActive => Type switch
    {
        EnemyType.ImpRaider => FrontierCombatState is
            DemonFrontierCombatState.ImpSlashActive or
            DemonFrontierCombatState.ImpPounceActive or
            DemonFrontierCombatState.ImpFrenzyActive,
        EnemyType.HellArcher =>
            FrontierCombatState == DemonFrontierCombatState.ArcherRelease,
        EnemyType.SpineHound => FrontierCombatState is
            DemonFrontierCombatState.HoundLeapActive or
            DemonFrontierCombatState.HoundRushActive or
            DemonFrontierCombatState.HoundSpineBurstActive,
        EnemyType.RiftStalker => FrontierCombatState is
            DemonFrontierCombatState.StalkerSlashActive or
            DemonFrontierCombatState.StalkerAmbushActive or
            DemonFrontierCombatState.StalkerTwinFirst or
            DemonFrontierCombatState.StalkerTwinSecond,
        EnemyType.BoneHarvester => FrontierCombatState is
            DemonFrontierCombatState.HarvesterSweepActive or
            DemonFrontierCombatState.HarvesterHookActive or
            DemonFrontierCombatState.HarvesterReapActive,
        EnemyType.Hellbrute => FrontierCombatState is
            DemonFrontierCombatState.BruteSlamActive or
            DemonFrontierCombatState.BruteChargeActive or
            DemonFrontierCombatState.BruteBackhandActive or
            DemonFrontierCombatState.BruteSeismicActive,
        EnemyType.CorruptionPriest => FrontierCombatState is
            DemonFrontierCombatState.PriestChantActive or
            DemonFrontierCombatState.PriestAnchorActive or
            DemonFrontierCombatState.PriestGroundActive or
            DemonFrontierCombatState.PriestStaffActive or
            DemonFrontierCombatState.PriestFinalRiteActive,
        EnemyType.RiftWarden => FrontierCombatState is
            DemonFrontierCombatState.WardenThrustActive or
            DemonFrontierCombatState.WardenGateAttackActive or
            DemonFrontierCombatState.WardenSweepActive,
        EnemyType.DemonLegionnaire => FrontierCombatState is
            DemonFrontierCombatState.LegionThrustActive or
            DemonFrontierCombatState.LegionBashActive or
            DemonFrontierCombatState.LegionSweepActive,
        EnemyType.HornedGeneral => FrontierCombatState is
            DemonFrontierCombatState.GeneralThrustActive or
            DemonFrontierCombatState.GeneralSweepActive or
            DemonFrontierCombatState.GeneralStompActive or
            DemonFrontierCombatState.GeneralRiftCleaveActive or
            DemonFrontierCombatState.GeneralChargeActive or
            DemonFrontierCombatState.GeneralLegionsEndCharge or
            DemonFrontierCombatState.GeneralLegionsEndCleave,
        _ => base.IsCombatAttackActive
    };
    public override bool IsCombatAttackTelegraphing => Type switch
    {
        EnemyType.ImpRaider => FrontierCombatState is
            DemonFrontierCombatState.ImpSlashWindup or
            DemonFrontierCombatState.ImpPounceCrouch or
            DemonFrontierCombatState.ImpFrenzyWindup,
        EnemyType.HellArcher =>
            FrontierCombatState == DemonFrontierCombatState.ArcherDraw,
        EnemyType.SpineHound => FrontierCombatState is
            DemonFrontierCombatState.HoundThreaten or
            DemonFrontierCombatState.HoundLeapCrouch or
            DemonFrontierCombatState.HoundRushWindup or
            DemonFrontierCombatState.HoundSpineFlare,
        EnemyType.RiftStalker => FrontierCombatState is
            DemonFrontierCombatState.StalkerSlashWindup or
            DemonFrontierCombatState.StalkerRiftEntry or
            DemonFrontierCombatState.StalkerRiftExit or
            DemonFrontierCombatState.StalkerAmbushWindup or
            DemonFrontierCombatState.StalkerVoidEntry or
            DemonFrontierCombatState.StalkerVoidExit or
            DemonFrontierCombatState.StalkerTwinWindup or
            DemonFrontierCombatState.StalkerTwinGap,
        EnemyType.BoneHarvester => FrontierCombatState is
            DemonFrontierCombatState.HarvesterSweepWindup or
            DemonFrontierCombatState.HarvesterHookWindup or
            DemonFrontierCombatState.HarvesterReapWindup,
        EnemyType.Hellbrute => FrontierCombatState is
            DemonFrontierCombatState.BruteSlamWindup or
            DemonFrontierCombatState.BruteChargeWindup or
            DemonFrontierCombatState.BruteBackhandWindup or
            DemonFrontierCombatState.BruteSeismicWindup or
            DemonFrontierCombatState.BruteEnrage,
        EnemyType.CorruptionPriest => FrontierCombatState is
            DemonFrontierCombatState.PriestChantWindup or
            DemonFrontierCombatState.PriestAnchorWindup or
            DemonFrontierCombatState.PriestGroundWindup or
            DemonFrontierCombatState.PriestStaffWindup or
            DemonFrontierCombatState.PriestFinalRiteWindup,
        EnemyType.RiftWarden => FrontierCombatState is
            DemonFrontierCombatState.WardenThrustWindup or
            DemonFrontierCombatState.WardenGateWindup or
            DemonFrontierCombatState.WardenGateOpen or
            DemonFrontierCombatState.WardenGateAttackWindup or
            DemonFrontierCombatState.WardenSweepWindup or
            DemonFrontierCombatState.WardenStepEntry or
            DemonFrontierCombatState.WardenStepExit,
        EnemyType.DemonLegionnaire => FrontierCombatState is
            DemonFrontierCombatState.LegionGuardStance or
            DemonFrontierCombatState.LegionThrustWindup or
            DemonFrontierCombatState.LegionBashWindup or
            DemonFrontierCombatState.LegionSweepWindup or
            DemonFrontierCombatState.LegionCommandGuard,
        EnemyType.HornedGeneral => FrontierCombatState is
            DemonFrontierCombatState.GeneralPhaseTransition or
            DemonFrontierCombatState.GeneralThrustWindup or
            DemonFrontierCombatState.GeneralSweepWindup or
            DemonFrontierCombatState.GeneralGuard or
            DemonFrontierCombatState.GeneralStompWindup or
            DemonFrontierCombatState.GeneralRiftCleaveWindup or
            DemonFrontierCombatState.GeneralPulseWindup or
            DemonFrontierCombatState.GeneralAnchorWindup or
            DemonFrontierCombatState.GeneralChargeWindup or
            DemonFrontierCombatState.GeneralLegionsEndRaise or
            DemonFrontierCombatState.GeneralLegionsEndWarning or
            DemonFrontierCombatState.GeneralLegionsEndPause,
        _ => base.IsCombatAttackTelegraphing
    };
    public override Rectangle ActiveAttackArea => Type switch
    {
        EnemyType.ImpRaider => CreateImpAttackArea(),
        EnemyType.SpineHound => CreateSpineHoundAttackArea(),
        EnemyType.RiftStalker => CreateRiftStalkerAttackArea(),
        EnemyType.BoneHarvester => CreateBoneHarvesterAttackArea(),
        EnemyType.Hellbrute => CreateHellbruteAttackArea(),
        EnemyType.CorruptionPriest => CreateCorruptionPriestAttackArea(),
        EnemyType.RiftWarden => CreateRiftWardenAttackArea(),
        EnemyType.DemonLegionnaire => CreateLegionnaireAttackArea(),
        EnemyType.HornedGeneral => CreateHornedGeneralAttackArea(),
        _ => base.ActiveAttackArea
    };

    public DemonLandsEnemy(
        EnemyType type,
        Vector2 position,
        int level,
        int worldTier,
        int roomId,
        bool isElite)
        : base(
            type,
            position,
            GetSize(type),
            GetSpeed(type),
            GetDetection(type),
            Scale(GetBaseHealth(type), level, worldTier, true),
            Scale(GetBaseDamage(type), level, worldTier, false),
            GetExperience(type),
            level,
            worldTier,
            isElite,
            roomId,
            GetRange(type),
            GetCooldown(type))
    {
        _formationSide = ((roomId * 17 + (int)position.X / 37) & 1) == 0
            ? -1f
            : 1f;
    }

    public override void UpdateTimers(GameTime gameTime)
    {
        base.UpdateTimers(gameTime);
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        _rangedCooldown = MathF.Max(0f, _rangedCooldown - elapsed);
        _supportCooldown = MathF.Max(0f, _supportCooldown - elapsed);
        _polishCooldown = MathF.Max(0f, _polishCooldown - elapsed);
        _pounceCooldown = MathF.Max(0f, _pounceCooldown - elapsed);
        _retreatShotCooldown = MathF.Max(0f, _retreatShotCooldown - elapsed);
        _spineLeapCooldown = MathF.Max(0f, _spineLeapCooldown - elapsed);
        _bodyRushCooldown = MathF.Max(0f, _bodyRushCooldown - elapsed);
        _spineBurstCooldown = MathF.Max(0f, _spineBurstCooldown - elapsed);
        _riftAmbushCooldown = MathF.Max(0f, _riftAmbushCooldown - elapsed);
        _voidStepCooldown = MathF.Max(0f, _voidStepCooldown - elapsed);
        _twinClawCooldown = MathF.Max(0f, _twinClawCooldown - elapsed);
        _harvesterHookCooldown = MathF.Max(0f, _harvesterHookCooldown - elapsed);
        _graveReapCooldown = MathF.Max(0f, _graveReapCooldown - elapsed);
        _bruteChargeCooldown = MathF.Max(0f, _bruteChargeCooldown - elapsed);
        _bruteSeismicCooldown = MathF.Max(0f, _bruteSeismicCooldown - elapsed);
        _priestChantCooldown = MathF.Max(0f, _priestChantCooldown - elapsed);
        _priestAnchorCooldown = MathF.Max(0f, _priestAnchorCooldown - elapsed);
        _priestGroundCooldown = MathF.Max(0f, _priestGroundCooldown - elapsed);
        _wardenGateCooldown = MathF.Max(0f, _wardenGateCooldown - elapsed);
        _wardenStepCooldown = MathF.Max(0f, _wardenStepCooldown - elapsed);
        _wardenSweepCooldown = MathF.Max(0f, _wardenSweepCooldown - elapsed);
        _legionCommandCooldown = MathF.Max(0f, _legionCommandCooldown - elapsed);
        _generalGuardCooldown = MathF.Max(0f, _generalGuardCooldown - elapsed);
        _generalAnchorCooldown = MathF.Max(0f, _generalAnchorCooldown - elapsed);
        _generalChargeCooldown = MathF.Max(0f, _generalChargeCooldown - elapsed);
        _generalPulseCooldown = MathF.Max(0f, _generalPulseCooldown - elapsed);
        _taintedGroundTimeRemaining = MathF.Max(
            0f, _taintedGroundTimeRemaining - elapsed);
        _anchorTetherTimeRemaining = MathF.Max(
            0f, _anchorTetherTimeRemaining - elapsed);
        _riftGateTimeRemaining = MathF.Max(0f, _riftGateTimeRemaining - elapsed);
        _riftScarTimeRemaining = MathF.Max(0f, _riftScarTimeRemaining - elapsed);
        _riftScarDamageCooldown = MathF.Max(0f, _riftScarDamageCooldown - elapsed);
        _corruptionPulseTimeRemaining = MathF.Max(
            0f, _corruptionPulseTimeRemaining - elapsed);
        _generalRiftPathTimeRemaining = MathF.Max(
            0f, _generalRiftPathTimeRemaining - elapsed);
        _guardReactionTimeRemaining = MathF.Max(
            0f, _guardReactionTimeRemaining - elapsed);
        if (_anchorTetherTimeRemaining <= 0f)
            _blessedAnchorId = string.Empty;
    }

    public float ConsumeCorruptionPressure()
    {
        float result = _pendingCorruptionPressure;
        _pendingCorruptionPressure = 0f;
        return result;
    }

    public bool ConsumeAnchorPulseRequest()
    {
        bool pending = AnchorPulsePending;
        AnchorPulsePending = false;
        return pending;
    }

    public bool ConsumeAnchorBlessingRequest()
    {
        bool pending = _anchorBlessingPending;
        _anchorBlessingPending = false;
        AnchorPulsePending = false;
        return pending;
    }

    public void ConfirmAnchorBlessing(string anchorId, Vector2 position)
    {
        if (Type != EnemyType.CorruptionPriest || !IsAlive ||
            string.IsNullOrEmpty(anchorId))
        {
            return;
        }
        _blessedAnchorId = anchorId;
        _blessedAnchorPosition = position;
        _anchorTetherTimeRemaining =
            CorruptionAnchorSystem.PriestBlessingDurationSeconds;
    }

    public void SynchronizeAnchorBlessing(bool remainsActive)
    {
        if (remainsActive)
            return;
        _blessedAnchorId = string.Empty;
        _anchorTetherTimeRemaining = 0f;
    }

    public void ClearEncounterTransientEffects()
    {
        if (Type is not (EnemyType.CorruptionPriest or EnemyType.RiftWarden or
            EnemyType.DemonLegionnaire or EnemyType.HornedGeneral))
            return;
        _anchorBlessingPending = false;
        _anchorTetherTimeRemaining = 0f;
        _taintedGroundTimeRemaining = 0f;
        _riftGateTimeRemaining = 0f;
        _riftScarTimeRemaining = 0f;
        _riftScarDamageCooldown = 0f;
        _corruptionPulseTimeRemaining = 0f;
        _generalRiftPathTimeRemaining = 0f;
        _requestedBossAnchorPosition = Vector2.Zero;
        _blessedAnchorId = string.Empty;
        _hasRiftExitPosition = false;
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
            CancelFrontierAction(.25f);
    }

    public override void ReceiveDamageFrom(
        int damage,
        float poiseDamage,
        Vector2 sourcePosition)
    {
        bool guarding = IsLegionGuarding || IsGeneralGuarding;
        if (!guarding || damage <= 0 || !IsSourceInFront(sourcePosition))
        {
            base.ReceiveDamageFrom(damage, poiseDamage, sourcePosition);
            return;
        }

        bool strongHit = poiseDamage >= 45f || damage >= 42;
        int guardedDamage = Math.Max(
            1,
            (int)MathF.Ceiling(damage * (strongHit ? .84f : .52f)));
        float guardedPoise = strongHit ? poiseDamage : poiseDamage * .66f;
        base.ReceiveDamage(guardedDamage, guardedPoise);
        _guardReactionTimeRemaining = .20f;
        if (Type == EnemyType.HornedGeneral)
        {
            _generalGuardIntegrity -= poiseDamage + damage * .36f;
            if (strongHit || _generalGuardIntegrity <= 0f || IsStaggered)
                EnterGeneralGuardBreak();
        }
        else
        {
            _legionGuardIntegrity -= poiseDamage + damage * .40f;
            if (strongHit || _legionGuardIntegrity <= 0f || IsStaggered)
                EnterLegionGuardBreak();
        }
    }

    private bool IsSourceInFront(Vector2 sourcePosition)
    {
        float offset = sourcePosition.X - Position.X;
        return MathF.Abs(offset) <= 2f ||
            (offset < 0f && Facing == EnemyFacingDirection.Left) ||
            (offset > 0f && Facing == EnemyFacingDirection.Right);
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (!IsPlayerDetected(player.Position))
            return;

        if (Type == EnemyType.ImpRaider)
        {
            UpdateImpRaider(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.HellArcher)
        {
            UpdateHellArcher(
                gameTime,
                player,
                dungeon,
                projectiles);
            return;
        }

        if (Type == EnemyType.SpineHound)
        {
            UpdateSpineHound(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.RiftStalker)
        {
            UpdateRiftStalker(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.BoneHarvester)
        {
            UpdateBoneHarvester(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.Hellbrute)
        {
            UpdateHellbrute(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.CorruptionPriest)
        {
            UpdateCorruptionPriest(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.RiftWarden)
        {
            UpdateRiftWarden(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.DemonLegionnaire)
        {
            UpdateDemonLegionnaire(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.HornedGeneral)
        {
            UpdateHornedGeneral(gameTime, player, dungeon, enemies);
            return;
        }

        if (Type == EnemyType.CorruptionPriest && _supportCooldown <= 0f)
        {
            _supportCooldown = 5f;
            foreach (Enemy enemy in enemies?.Enemies ?? Array.Empty<Enemy>())
            {
                if (enemy.IsAlive && !ReferenceEquals(enemy, this) &&
                    Vector2.DistanceSquared(enemy.Position, Position) <= 250f * 250f)
                {
                    enemy.ApplyTemporaryBuff(10, 6, 3.5f);
                }
            }
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        if (UsesProjectileAttack)
        {
            UpdateRanged(gameTime, player, dungeon, projectiles, distance);
            return;
        }

        float stoppingRange = Attack.Range;
        if (distance <= stoppingRange)
        {
            if (TryMeleeAttack(player))
            {
                _pendingCorruptionPressure += Type switch
                {
                    EnemyType.RiftStalker => 5f,
                    EnemyType.BoneHarvester => 4f,
                    EnemyType.Hellbrute => 6f,
                    EnemyType.DemonLegionnaire => 4f,
                    EnemyType.HornedGeneral => CombatPhase + 3f,
                    _ => 2f
                };
            }
            return;
        }

        float aggression = Type == EnemyType.HornedGeneral
            ? CombatPhase == 3 ? 1.35f : CombatPhase == 2 ? 1.15f : 1f
            : 1f;
        MoveToward(gameTime, player.Position, stoppingRange, dungeon, aggression);
    }

    protected override void OnDamaged()
    {
        if (IsFrontierPolished && IsAlive &&
            FrontierCombatState == DemonFrontierCombatState.Ready)
        {
            _polishCooldown = MathF.Max(_polishCooldown, .12f);
        }
    }

    protected override void OnCombatInterrupted()
    {
        if (IsLegionGuarding)
            EnterLegionGuardBreak();
        else if (IsGeneralGuarding)
            EnterGeneralGuardBreak();
        else if (Type == EnemyType.HornedGeneral &&
            FrontierCombatState is
                DemonFrontierCombatState.GeneralPhaseTransition or
                DemonFrontierCombatState.GeneralLegionsEndRaise or
                DemonFrontierCombatState.GeneralLegionsEndWarning or
                DemonFrontierCombatState.GeneralLegionsEndPause or
                DemonFrontierCombatState.GeneralLegionsEndCharge or
                DemonFrontierCombatState.GeneralLegionsEndCleave or
                DemonFrontierCombatState.GeneralLegionsEndRecovery)
        {
            // Phase presentation and the bounded signature resume after the
            // base stagger instead of being silently consumed.
        }
        else if (IsFrontierPolished)
            CancelFrontierAction(.38f);
    }

    private void UpdateImpRaider(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateImpAction(player, dungeon, elapsed);
            return;
        }

        float horizontal = player.Position.X - Position.X;
        float distance = MathF.Abs(horizontal);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        if (CurrentHealth <= MaxHealth * .34f && distance <= 72f)
        {
            _frenzyStabsRemaining = ImpMaximumFrenzyStabs;
            StartImpAction(
                ImpRaiderAttackKind.FrenziedStab,
                DemonFrontierCombatState.ImpFrenzyWindup,
                ImpFrenzyWindupSeconds,
                player.Position.X);
            return;
        }

        if (_pounceCooldown <= 0f && distance >= 92f && distance <= 215f)
        {
            _pounceCooldown = 2.6f;
            StartImpAction(
                ImpRaiderAttackKind.PouncingCut,
                DemonFrontierCombatState.ImpPounceCrouch,
                ImpPounceCrouchSeconds,
                player.Position.X);
            return;
        }

        if (distance <= 58f)
        {
            StartImpAction(
                ImpRaiderAttackKind.RaiderSlash,
                DemonFrontierCombatState.ImpSlashWindup,
                ImpSlashWindupSeconds,
                player.Position.X);
            return;
        }

        float flankOffset = HasNearbyImpPartner(enemies)
            ? _formationSide * 58f
            : 0f;
        Vector2 pressurePoint = new(
            player.Position.X + flankOffset,
            player.Position.Y);
        MoveToward(
            gameTime,
            pressurePoint,
            flankOffset == 0f ? 54f : 28f,
            dungeon,
            1.18f);
    }

    private void UpdateImpAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState == DemonFrontierCombatState.ImpPounceActive)
        {
            MoveBy(
                new Vector2(_lockedAttackDirection * 248f * elapsed, 0f),
                dungeon);
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = ImpRaiderAttack switch
            {
                ImpRaiderAttackKind.PouncingCut =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.15f),
                ImpRaiderAttackKind.FrenziedStab =>
                    Math.Max(1, (int)MathF.Ceiling(EffectiveAttackDamage * .62f)),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
                _pendingCorruptionPressure += 2f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.ImpSlashWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.ImpSlashActive,
                    ImpSlashActiveSeconds);
                break;
            case DemonFrontierCombatState.ImpSlashActive:
                StartFrontierAction(
                    DemonFrontierCombatState.ImpSlashRecovery,
                    ImpSlashRecoverySeconds);
                break;
            case DemonFrontierCombatState.ImpSlashRecovery:
                FinishFrontierAction(.18f);
                break;
            case DemonFrontierCombatState.ImpPounceCrouch:
                ApplyRaiderLaunch(-245f);
                StartFrontierAction(
                    DemonFrontierCombatState.ImpPounceActive,
                    ImpPounceActiveSeconds);
                break;
            case DemonFrontierCombatState.ImpPounceActive:
                StartFrontierAction(
                    DemonFrontierCombatState.ImpPounceRecovery,
                    ImpPounceRecoverySeconds);
                break;
            case DemonFrontierCombatState.ImpPounceRecovery:
                FinishFrontierAction(.26f);
                break;
            case DemonFrontierCombatState.ImpFrenzyWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.ImpFrenzyActive,
                    ImpFrenzyActiveSeconds);
                break;
            case DemonFrontierCombatState.ImpFrenzyActive:
                _frenzyStabsRemaining--;
                if (_frenzyStabsRemaining > 0)
                {
                    StartFrontierAction(
                        DemonFrontierCombatState.ImpFrenzyWindup,
                        ImpFrenzyWindupSeconds * .76f);
                }
                else
                {
                    StartFrontierAction(
                        DemonFrontierCombatState.ImpFrenzyRecovery,
                        ImpFrenzyRecoverySeconds);
                }
                break;
            case DemonFrontierCombatState.ImpFrenzyRecovery:
                FinishFrontierAction(.72f);
                break;
        }
    }

    private void UpdateHellArcher(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateHellArcherAction(
                player,
                dungeon,
                projectiles,
                elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f || _rangedCooldown > 0f)
            return;

        if (distance < 142f && _retreatShotCooldown <= 0f)
        {
            _retreatShotCooldown = 4.4f;
            HellArcherAttack = HellArcherAttackKind.RetreatShot;
            LockAttackDirection(player.Position.X);
            StartFrontierAction(
                DemonFrontierCombatState.ArcherRetreat,
                ArcherRetreatSeconds);
            return;
        }
        if (distance < 165f)
        {
            MoveAway(gameTime, player.Position, dungeon, 1.24f);
            return;
        }
        if (distance > 440f)
        {
            MoveToward(gameTime, player.Position, 360f, dungeon, 1.02f);
            return;
        }

        _archerShotSequence++;
        HellArcherAttack = distance >= 275f && _archerShotSequence % 3 == 0
            ? HellArcherAttackKind.HighArcShot
            : HellArcherAttackKind.EmberArrow;
        LockAttackDirection(player.Position.X);
        StartFrontierAction(
            DemonFrontierCombatState.ArcherDraw,
            HellArcherAttack == HellArcherAttackKind.HighArcShot
                ? ArcherHighArcDrawSeconds
                : ArcherStandardDrawSeconds);
    }

    private void UpdateHellArcherAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState == DemonFrontierCombatState.ArcherRetreat)
        {
            float away = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(away * EffectiveMovementSpeed * 1.28f * elapsed, 0f),
                dungeon);
        }
        else if (FrontierCombatState == DemonFrontierCombatState.ArcherReposition)
        {
            float desired = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(
                new Vector2(desired * EffectiveMovementSpeed * .58f * elapsed, 0f),
                dungeon);
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.ArcherRetreat:
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.ArcherDraw,
                    ArcherRetreatDrawSeconds);
                break;
            case DemonFrontierCombatState.ArcherDraw:
            {
                Vector2 release = GetHellArrowReleasePosition();
                Vector2 target = player.Position + new Vector2(0f, -8f);
                bool highArc = HellArcherAttack == HellArcherAttackKind.HighArcShot;
                int damage = HellArcherAttack == HellArcherAttackKind.RetreatShot
                    ? Math.Max(1, (int)MathF.Ceiling(EffectiveAttackDamage * .78f))
                    : highArc
                        ? (int)MathF.Ceiling(EffectiveAttackDamage * 1.08f)
                        : EffectiveAttackDamage;
                projectiles.SpawnHellArrow(
                    release,
                    target,
                    damage,
                    RoomId,
                    highArc);
                StartFrontierAction(
                    DemonFrontierCombatState.ArcherRelease,
                    ArcherReleaseSeconds);
                break;
            }
            case DemonFrontierCombatState.ArcherRelease:
                StartFrontierAction(
                    DemonFrontierCombatState.ArcherRecovery,
                    HellArcherAttack == HellArcherAttackKind.HighArcShot
                        ? ArcherRecoverySeconds + .12f
                        : ArcherRecoverySeconds);
                break;
            case DemonFrontierCombatState.ArcherRecovery:
                StartFrontierAction(
                    DemonFrontierCombatState.ArcherReposition,
                    ArcherRepositionSeconds);
                break;
            case DemonFrontierCombatState.ArcherReposition:
                _rangedCooldown = HellArcherAttack == HellArcherAttackKind.RetreatShot
                    ? .82f
                    : .55f;
                FinishFrontierAction(0f);
                break;
        }
    }

    private void UpdateSpineHound(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateSpineHoundAction(player, dungeon, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        // A hound does not begin a committed horizontal burst while a nearby
        // Stalker is completing a rift ambush. This preserves a reaction gap
        // when the pair appears in the same encounter.
        if (HasCommittingRiftStalker(enemies))
        {
            MoveToward(gameTime, player.Position, 94f, dungeon, .72f);
            return;
        }

        if (distance <= 78f && _spineBurstCooldown <= 0f)
        {
            _spineBurstCooldown = 4.2f;
            StartSpineHoundThreat(SpineHoundAttackKind.SpineBurst, player.Position.X);
            return;
        }

        bool canRush = distance >= 145f && distance <= 285f &&
            _bodyRushCooldown <= 0f;
        bool canLeap = distance >= 84f && distance <= 205f &&
            _spineLeapCooldown <= 0f;
        _houndAttackSequence++;
        if (canRush && (_houndAttackSequence & 1) == 0)
        {
            _bodyRushCooldown = 3.8f;
            StartSpineHoundThreat(SpineHoundAttackKind.BodyRush, player.Position.X);
            return;
        }
        if (canLeap)
        {
            _spineLeapCooldown = 2.7f;
            StartSpineHoundThreat(SpineHoundAttackKind.SpineLeap, player.Position.X);
            return;
        }
        if (canRush)
        {
            _bodyRushCooldown = 3.8f;
            StartSpineHoundThreat(SpineHoundAttackKind.BodyRush, player.Position.X);
            return;
        }

        if (distance < 58f)
        {
            MoveAway(gameTime, player.Position, dungeon, .70f);
            return;
        }

        float aggression = CurrentHealth <= MaxHealth * .30f ? 1.24f : 1.02f;
        MoveToward(gameTime, player.Position, 76f, dungeon, aggression);
    }

    private void UpdateSpineHoundAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState is DemonFrontierCombatState.HoundLeapActive or
            DemonFrontierCombatState.HoundRushActive)
        {
            float speed = FrontierCombatState ==
                DemonFrontierCombatState.HoundRushActive ? 372f : 304f;
            Vector2 before = Position;
            MoveBy(new Vector2(_lockedAttackDirection * speed * elapsed, 0f), dungeon);
            if (MathF.Abs(Position.X - before.X) < .15f)
                _actionTimeRemaining = 0f;
        }
        else if (FrontierCombatState == DemonFrontierCombatState.HoundReposition)
        {
            MoveBy(new Vector2(
                _repositionDirection * EffectiveMovementSpeed * .72f * elapsed,
                0f), dungeon);
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = SpineHoundAttack switch
            {
                SpineHoundAttackKind.BodyRush =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.50f),
                SpineHoundAttackKind.SpineLeap =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.25f),
                SpineHoundAttackKind.SpineBurst =>
                    Math.Max(1, (int)MathF.Ceiling(EffectiveAttackDamage * .82f)),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
                _pendingCorruptionPressure += 3f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.HoundThreaten:
                StartFrontierAction(
                    SpineHoundAttack switch
                    {
                        SpineHoundAttackKind.SpineLeap =>
                            DemonFrontierCombatState.HoundLeapCrouch,
                        SpineHoundAttackKind.BodyRush =>
                            DemonFrontierCombatState.HoundRushWindup,
                        _ => DemonFrontierCombatState.HoundSpineFlare
                    },
                    SpineHoundAttack switch
                    {
                        SpineHoundAttackKind.SpineLeap => HoundLeapCrouchSeconds,
                        SpineHoundAttackKind.BodyRush => HoundRushWindupSeconds,
                        _ => HoundSpineFlareSeconds
                    });
                break;
            case DemonFrontierCombatState.HoundLeapCrouch:
                ApplyRaiderLaunch(-275f);
                StartFrontierAction(
                    DemonFrontierCombatState.HoundLeapActive,
                    HoundLeapActiveSeconds);
                break;
            case DemonFrontierCombatState.HoundLeapActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HoundLeapRecovery,
                    .46f);
                break;
            case DemonFrontierCombatState.HoundRushWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.HoundRushActive,
                    HoundRushActiveSeconds);
                break;
            case DemonFrontierCombatState.HoundRushActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HoundRushRecovery,
                    .58f);
                break;
            case DemonFrontierCombatState.HoundSpineFlare:
                StartFrontierAction(
                    DemonFrontierCombatState.HoundSpineBurstActive,
                    HoundSpineBurstActiveSeconds);
                break;
            case DemonFrontierCombatState.HoundSpineBurstActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HoundSpineBurstRecovery,
                    .40f);
                break;
            case DemonFrontierCombatState.HoundLeapRecovery:
            case DemonFrontierCombatState.HoundRushRecovery:
            case DemonFrontierCombatState.HoundSpineBurstRecovery:
                _repositionDirection = -_lockedAttackDirection;
                StartFrontierAction(
                    DemonFrontierCombatState.HoundReposition,
                    .28f);
                break;
            case DemonFrontierCombatState.HoundReposition:
                FinishFrontierAction(.28f);
                break;
        }
    }

    private void StartSpineHoundThreat(
        SpineHoundAttackKind attack,
        float targetX)
    {
        SpineHoundAttack = attack;
        LockAttackDirection(targetX);
        StartFrontierAction(
            DemonFrontierCombatState.HoundThreaten,
            HoundThreatenSeconds);
    }

    private void UpdateRiftStalker(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateRiftStalkerAction(player, dungeon, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        if (distance < 50f && _voidStepCooldown <= 0f)
        {
            BeginRiftMovement(RiftStalkerAttackKind.VoidStep, player.Position.X);
            _voidStepCooldown = 3.6f;
            return;
        }

        if (distance <= 68f)
        {
            _stalkerAttackSequence++;
            if (_twinClawCooldown <= 0f && _stalkerAttackSequence % 3 == 0)
            {
                _twinClawCooldown = 3.1f;
                _twinClawsRemaining = StalkerMaximumTwinClawHits;
                RiftStalkerAttack = RiftStalkerAttackKind.TwinClawSequence;
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerTwinWindup,
                    .24f);
            }
            else
            {
                RiftStalkerAttack = RiftStalkerAttackKind.RiftSlash;
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerSlashWindup,
                    .22f);
            }
            return;
        }

        if (distance >= 104f && distance <= 285f &&
            _riftAmbushCooldown <= 0f &&
            !HasCommittingSpineHound(enemies))
        {
            _riftAmbushCooldown = 4.8f;
            BeginRiftMovement(RiftStalkerAttackKind.RiftAmbush, player.Position.X);
            return;
        }

        MoveToward(gameTime, player.Position, 62f, dungeon, .68f);
    }

    private void BeginRiftMovement(
        RiftStalkerAttackKind attack,
        float targetX)
    {
        RiftStalkerAttack = attack;
        _riftEntryPosition = Position;
        _riftExitPosition = Vector2.Zero;
        _hasRiftExitPosition = false;
        LockAttackDirection(targetX);
        StartFrontierAction(
            attack == RiftStalkerAttackKind.VoidStep
                ? DemonFrontierCombatState.StalkerVoidEntry
                : DemonFrontierCombatState.StalkerRiftEntry,
            attack == RiftStalkerAttackKind.VoidStep
                ? .20f
                : StalkerRiftEntrySeconds);
    }

    private void UpdateRiftStalkerAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState == DemonFrontierCombatState.StalkerBackstep)
        {
            MoveBy(new Vector2(
                _repositionDirection * EffectiveMovementSpeed * 1.05f * elapsed,
                0f), dungeon);
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = RiftStalkerAttack switch
            {
                RiftStalkerAttackKind.RiftAmbush =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.36f),
                RiftStalkerAttackKind.TwinClawSequence =>
                    Math.Max(1, (int)MathF.Ceiling(EffectiveAttackDamage * .72f)),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
                _pendingCorruptionPressure += 4f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.StalkerSlashWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerSlashActive,
                    .10f);
                break;
            case DemonFrontierCombatState.StalkerSlashActive:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerSlashRecovery,
                    .28f);
                break;
            case DemonFrontierCombatState.StalkerSlashRecovery:
                BeginStalkerBackstep(.22f);
                break;
            case DemonFrontierCombatState.StalkerRiftEntry:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerRiftHidden,
                    StalkerRiftHiddenSeconds);
                break;
            case DemonFrontierCombatState.StalkerRiftHidden:
                if (!TryFindRiftDestination(
                    player,
                    dungeon,
                    defensive: false,
                    out Vector2 ambushDestination))
                {
                    FinishFrontierAction(.42f);
                    break;
                }
                Position = ambushDestination;
                _riftExitPosition = ambushDestination;
                _hasRiftExitPosition = true;
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerRiftExit,
                    StalkerRiftExitSeconds);
                break;
            case DemonFrontierCombatState.StalkerRiftExit:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerAmbushWindup,
                    StalkerAmbushReactionSeconds);
                break;
            case DemonFrontierCombatState.StalkerAmbushWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerAmbushActive,
                    .11f);
                break;
            case DemonFrontierCombatState.StalkerAmbushActive:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerAmbushRecovery,
                    .43f);
                break;
            case DemonFrontierCombatState.StalkerAmbushRecovery:
                BeginStalkerBackstep(.25f);
                break;
            case DemonFrontierCombatState.StalkerVoidEntry:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerVoidHidden,
                    .08f);
                break;
            case DemonFrontierCombatState.StalkerVoidHidden:
                if (!TryFindRiftDestination(
                    player,
                    dungeon,
                    defensive: true,
                    out Vector2 voidDestination))
                {
                    FinishFrontierAction(.36f);
                    break;
                }
                Position = voidDestination;
                _riftExitPosition = voidDestination;
                _hasRiftExitPosition = true;
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerVoidExit,
                    .22f);
                break;
            case DemonFrontierCombatState.StalkerVoidExit:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerVoidRecovery,
                    .30f);
                break;
            case DemonFrontierCombatState.StalkerVoidRecovery:
                FinishFrontierAction(.30f);
                break;
            case DemonFrontierCombatState.StalkerTwinWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerTwinFirst,
                    .08f);
                break;
            case DemonFrontierCombatState.StalkerTwinFirst:
                _twinClawsRemaining--;
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerTwinGap,
                    .14f);
                break;
            case DemonFrontierCombatState.StalkerTwinGap:
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerTwinSecond,
                    .08f);
                break;
            case DemonFrontierCombatState.StalkerTwinSecond:
                _twinClawsRemaining--;
                StartFrontierAction(
                    DemonFrontierCombatState.StalkerTwinRecovery,
                    .35f);
                break;
            case DemonFrontierCombatState.StalkerTwinRecovery:
                BeginStalkerBackstep(.22f);
                break;
            case DemonFrontierCombatState.StalkerBackstep:
                FinishFrontierAction(.30f);
                break;
        }
    }

    private void BeginStalkerBackstep(float duration)
    {
        _repositionDirection = -_lockedAttackDirection;
        StartFrontierAction(
            DemonFrontierCombatState.StalkerBackstep,
            duration);
    }

    private bool TryFindRiftDestination(
        PlayerCharacter player,
        DungeonMap dungeon,
        bool defensive,
        out Vector2 destination)
    {
        destination = Vector2.Zero;
        float currentSide = Position.X < player.Position.X ? -1f : 1f;
        float preferredSide = defensive ? currentSide : -currentSide;
        float maximumMove = defensive ? 165f : 215f;

        for (int index = 0; index < 8; index++)
        {
            float distance = index switch
            {
                0 => defensive ? 126f : 82f,
                1 => defensive ? 102f : 104f,
                2 => defensive ? 148f : 66f,
                3 => defensive ? 84f : 126f,
                4 => defensive ? 116f : 92f,
                5 => defensive ? 94f : 142f,
                6 => defensive ? 154f : 74f,
                _ => defensive ? 76f : 116f
            };
            float side = index < 4 ? preferredSide : -preferredSide;
            float x = player.Position.X + side * distance;
            if (!TryFindGroundedRiftPosition(
                x,
                player,
                dungeon,
                out Vector2 candidate))
            {
                continue;
            }
            if (Vector2.DistanceSquared(Position, candidate) >
                maximumMove * maximumMove)
            {
                continue;
            }

            destination = candidate;
            return true;
        }

        return false;
    }

    private bool TryFindGroundedRiftPosition(
        float x,
        PlayerCharacter player,
        DungeonMap dungeon,
        out Vector2 destination)
    {
        destination = Vector2.Zero;
        float bestScore = float.MaxValue;
        float halfWidth = Size.X * .5f;
        Rectangle playerExclusion = player.Bounds;
        playerExclusion.Inflate(18, 10);

        foreach (Platform platform in dungeon.Platforms)
        {
            if (x - halfWidth < platform.Bounds.Left + 2f ||
                x + halfWidth > platform.Bounds.Right - 2f)
            {
                continue;
            }

            Vector2 candidate = new(
                x,
                platform.Bounds.Top - Size.Y * .5f);
            if (MathF.Abs(candidate.Y - player.Position.Y) > 92f ||
                !SideScrollingCollision.IsPositionFree(candidate, Size, dungeon) ||
                !SideScrollingCollision.IsSupported(candidate, Size, dungeon) ||
                DungeonCollision.CreateBounds(candidate, Size).Intersects(
                    playerExclusion))
            {
                continue;
            }

            float score = MathF.Abs(candidate.Y - player.Position.Y);
            if (score >= bestScore)
                continue;
            bestScore = score;
            destination = candidate;
        }

        return bestScore < float.MaxValue;
    }

    private bool HasCommittingRiftStalker(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy stalker ||
                ReferenceEquals(stalker, this) || !stalker.IsAlive ||
                stalker.Type != EnemyType.RiftStalker ||
                Vector2.DistanceSquared(stalker.Position, Position) > 360f * 360f)
            {
                continue;
            }
            if (stalker.FrontierCombatState is
                DemonFrontierCombatState.StalkerRiftEntry or
                DemonFrontierCombatState.StalkerRiftHidden or
                DemonFrontierCombatState.StalkerRiftExit or
                DemonFrontierCombatState.StalkerAmbushWindup or
                DemonFrontierCombatState.StalkerAmbushActive)
            {
                return true;
            }
        }
        return false;
    }

    private bool HasCommittingSpineHound(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy hound ||
                ReferenceEquals(hound, this) || !hound.IsAlive ||
                hound.Type != EnemyType.SpineHound ||
                Vector2.DistanceSquared(hound.Position, Position) > 360f * 360f)
            {
                continue;
            }
            if (hound.FrontierCombatState is
                DemonFrontierCombatState.HoundThreaten or
                DemonFrontierCombatState.HoundLeapCrouch or
                DemonFrontierCombatState.HoundLeapActive or
                DemonFrontierCombatState.HoundRushWindup or
                DemonFrontierCombatState.HoundRushActive)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateBoneHarvester(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateBoneHarvesterAction(player, dungeon, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        bool bruteCommitted = HasCommittingHellbrute(enemies);
        if (!bruteCommitted && distance >= 102f && distance <= 194f &&
            _harvesterHookCooldown <= 0f)
        {
            _harvesterHookCooldown = 5.4f;
            StartBoneHarvesterAttack(
                BoneHarvesterAttackKind.HarvesterHook,
                DemonFrontierCombatState.HarvesterHookWindup,
                HarvesterHookWindupSeconds,
                player.Position.X);
            return;
        }

        _harvesterAttackSequence++;
        if (distance <= 82f && _graveReapCooldown <= 0f &&
            _harvesterAttackSequence % 3 == 0)
        {
            _graveReapCooldown = 4.6f;
            StartBoneHarvesterAttack(
                BoneHarvesterAttackKind.GraveReap,
                DemonFrontierCombatState.HarvesterReapWindup,
                HarvesterReapWindupSeconds,
                player.Position.X);
            return;
        }

        if (distance <= 112f)
        {
            StartBoneHarvesterAttack(
                BoneHarvesterAttackKind.ReapingSweep,
                DemonFrontierCombatState.HarvesterSweepWindup,
                HarvesterSweepWindupSeconds,
                player.Position.X);
            return;
        }

        MoveToward(gameTime, player.Position, 94f, dungeon,
            CurrentHealth <= MaxHealth * .30f ? .92f : .78f);
    }

    private void StartBoneHarvesterAttack(
        BoneHarvesterAttackKind attack,
        DemonFrontierCombatState state,
        float duration,
        float targetX)
    {
        BoneHarvesterAttack = attack;
        LockAttackDirection(targetX);
        StartFrontierAction(state, duration);
    }

    private void UpdateBoneHarvesterAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = BoneHarvesterAttack switch
            {
                BoneHarvesterAttackKind.GraveReap =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.55f),
                BoneHarvesterAttackKind.HarvesterHook =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.08f),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
            {
                _pendingCorruptionPressure += BoneHarvesterAttack ==
                    BoneHarvesterAttackKind.GraveReap ? 5f : 3f;
                if (BoneHarvesterAttack == BoneHarvesterAttackKind.HarvesterHook)
                {
                    player.PullToward(
                        Position,
                        HarvesterMaximumPullDistance,
                        dungeon);
                }
                else if (BoneHarvesterAttack == BoneHarvesterAttackKind.GraveReap)
                {
                    player.PushAwayFrom(Position, 18f, dungeon);
                }
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.HarvesterSweepWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterSweepActive,
                    HarvesterSweepActiveSeconds);
                break;
            case DemonFrontierCombatState.HarvesterSweepActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterSweepRecovery,
                    .62f);
                break;
            case DemonFrontierCombatState.HarvesterSweepRecovery:
                FinishFrontierAction(
                    CurrentHealth <= MaxHealth * .30f ? .18f : .30f);
                break;
            case DemonFrontierCombatState.HarvesterHookWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterHookActive,
                    HarvesterHookActiveSeconds);
                break;
            case DemonFrontierCombatState.HarvesterHookActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterHookRecovery,
                    .70f);
                break;
            case DemonFrontierCombatState.HarvesterHookRecovery:
                FinishFrontierAction(.36f);
                break;
            case DemonFrontierCombatState.HarvesterReapWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterReapActive,
                    HarvesterReapActiveSeconds);
                break;
            case DemonFrontierCombatState.HarvesterReapActive:
                StartFrontierAction(
                    DemonFrontierCombatState.HarvesterReapRecovery,
                    .92f);
                break;
            case DemonFrontierCombatState.HarvesterReapRecovery:
                FinishFrontierAction(.42f);
                break;
        }
    }

    private void UpdateHellbrute(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateHellbruteAction(player, dungeon, elapsed);
            return;
        }

        if (IsEnragedBrute && !_bruteEnrageEntered)
        {
            _bruteEnrageEntered = true;
            HellbruteAttack = HellbruteAttackKind.None;
            StartFrontierAction(DemonFrontierCombatState.BruteEnrage, .62f);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        bool hookCommitted = HasCommittingHarvesterHook(enemies);
        if (distance >= 168f && distance <= 330f &&
            _bruteChargeCooldown <= 0f &&
            HasSafeBruteChargeRoute(player, dungeon))
        {
            _bruteChargeCooldown = 5.8f;
            StartHellbruteAttack(
                HellbruteAttackKind.InfernalCharge,
                DemonFrontierCombatState.BruteChargeWindup,
                BruteChargeWindupSeconds,
                player.Position.X);
            return;
        }

        if (!hookCommitted && distance <= 74f)
        {
            StartHellbruteAttack(
                HellbruteAttackKind.BruteSlam,
                DemonFrontierCombatState.BruteSlamWindup,
                BruteSlamWindupSeconds,
                player.Position.X);
            return;
        }

        if (distance <= 124f)
        {
            StartHellbruteAttack(
                HellbruteAttackKind.CrushingBackhand,
                DemonFrontierCombatState.BruteBackhandWindup,
                BruteBackhandWindupSeconds,
                player.Position.X);
            return;
        }

        if (!hookCommitted && distance <= 178f &&
            _bruteSeismicCooldown <= 0f)
        {
            _bruteSeismicCooldown = 4.9f;
            StartHellbruteAttack(
                HellbruteAttackKind.SeismicPound,
                DemonFrontierCombatState.BruteSeismicWindup,
                BruteSeismicWindupSeconds,
                player.Position.X);
            return;
        }

        MoveToward(gameTime, player.Position, 106f, dungeon, .72f);
    }

    private void StartHellbruteAttack(
        HellbruteAttackKind attack,
        DemonFrontierCombatState state,
        float duration,
        float targetX)
    {
        HellbruteAttack = attack;
        LockAttackDirection(targetX);
        StartFrontierAction(state, duration);
    }

    private void UpdateHellbruteAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState == DemonFrontierCombatState.BruteChargeActive)
        {
            float speed = IsEnragedBrute ? 246f : 228f;
            Vector2 movement = new(_lockedAttackDirection * speed * elapsed, 0f);
            Vector2 candidate = Position + movement;
            if (!SideScrollingCollision.IsPositionFree(candidate, Size, dungeon) ||
                !SideScrollingCollision.IsSupported(candidate, Size, dungeon))
            {
                _actionTimeRemaining = 0f;
            }
            else
            {
                Vector2 before = Position;
                MoveBy(movement, dungeon);
                if (MathF.Abs(Position.X - before.X) < .15f)
                    _actionTimeRemaining = 0f;
            }
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = HellbruteAttack switch
            {
                HellbruteAttackKind.InfernalCharge =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.50f),
                HellbruteAttackKind.BruteSlam =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.35f),
                HellbruteAttackKind.SeismicPound =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.42f),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
            {
                _pendingCorruptionPressure += 6f;
                if (HellbruteAttack is HellbruteAttackKind.BruteSlam or
                    HellbruteAttackKind.InfernalCharge or
                    HellbruteAttackKind.CrushingBackhand)
                {
                    player.PushAwayFrom(
                        Position,
                        HellbruteAttack == HellbruteAttackKind.InfernalCharge
                            ? 28f
                            : 22f,
                        dungeon);
                }
            }
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        float recoveryScale = IsEnragedBrute ? .84f : 1f;
        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.BruteEnrage:
                FinishFrontierAction(.55f);
                break;
            case DemonFrontierCombatState.BruteSlamWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteSlamActive,
                    BruteSlamActiveSeconds);
                break;
            case DemonFrontierCombatState.BruteSlamActive:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteSlamRecovery,
                    .92f * recoveryScale);
                break;
            case DemonFrontierCombatState.BruteSlamRecovery:
                FinishFrontierAction(.42f);
                break;
            case DemonFrontierCombatState.BruteChargeWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteChargeActive,
                    BruteChargeActiveSeconds);
                break;
            case DemonFrontierCombatState.BruteChargeActive:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteChargeRecovery,
                    1.06f * recoveryScale);
                break;
            case DemonFrontierCombatState.BruteChargeRecovery:
                FinishFrontierAction(.52f);
                break;
            case DemonFrontierCombatState.BruteBackhandWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteBackhandActive,
                    BruteBackhandActiveSeconds);
                break;
            case DemonFrontierCombatState.BruteBackhandActive:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteBackhandRecovery,
                    .68f * recoveryScale);
                break;
            case DemonFrontierCombatState.BruteBackhandRecovery:
                FinishFrontierAction(.34f);
                break;
            case DemonFrontierCombatState.BruteSeismicWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteSeismicActive,
                    BruteSeismicActiveSeconds);
                break;
            case DemonFrontierCombatState.BruteSeismicActive:
                StartFrontierAction(
                    DemonFrontierCombatState.BruteSeismicRecovery,
                    1.12f * recoveryScale);
                break;
            case DemonFrontierCombatState.BruteSeismicRecovery:
                FinishFrontierAction(.48f);
                break;
        }
    }

    private bool HasSafeBruteChargeRoute(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        if (MathF.Abs(Bounds.Bottom - player.Bounds.Bottom) > 38f)
            return false;
        float direction = MathF.Sign(player.Position.X - Position.X);
        if (direction == 0f)
            return false;
        float travel = MathF.Min(
            MathF.Max(0f, MathF.Abs(player.Position.X - Position.X) - 68f),
            220f);
        for (float distance = 12f; distance <= travel; distance += 12f)
        {
            Vector2 candidate = Position + new Vector2(
                direction * distance,
                0f);
            if (!SideScrollingCollision.IsPositionFree(candidate, Size, dungeon) ||
                !SideScrollingCollision.IsSupported(candidate, Size, dungeon))
            {
                return false;
            }
        }
        return travel >= 72f;
    }

    private bool HasCommittingHellbrute(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy brute ||
                ReferenceEquals(brute, this) || !brute.IsAlive ||
                brute.Type != EnemyType.Hellbrute ||
                Vector2.DistanceSquared(brute.Position, Position) > 390f * 390f)
            {
                continue;
            }
            if (brute.FrontierCombatState is
                DemonFrontierCombatState.BruteSlamWindup or
                DemonFrontierCombatState.BruteSlamActive or
                DemonFrontierCombatState.BruteChargeWindup or
                DemonFrontierCombatState.BruteChargeActive or
                DemonFrontierCombatState.BruteSeismicWindup or
                DemonFrontierCombatState.BruteSeismicActive)
            {
                return true;
            }
        }
        return false;
    }

    private bool HasCommittingHarvesterHook(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy harvester ||
                ReferenceEquals(harvester, this) || !harvester.IsAlive ||
                harvester.Type != EnemyType.BoneHarvester ||
                Vector2.DistanceSquared(harvester.Position, Position) > 390f * 390f)
            {
                continue;
            }
            if (harvester.FrontierCombatState is
                DemonFrontierCombatState.HarvesterHookWindup or
                DemonFrontierCombatState.HarvesterHookActive)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateCorruptionPriest(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (HasTaintedGround && _taintedGroundBounds.Intersects(player.Bounds))
            _pendingCorruptionPressure += 8f * elapsed;

        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateCorruptionPriestAction(player, dungeon, enemies, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        if (!_finalRiteUsed && CurrentHealth <= MaxHealth * .24f)
        {
            _finalRiteUsed = true;
            StartCorruptionPriestAbility(
                CorruptionPriestAbilityKind.FinalRite,
                DemonFrontierCombatState.PriestFinalRiteWindup,
                PriestFinalRiteWindupSeconds,
                player.Position.X);
            return;
        }

        int nearbyAllies = CountNearbyDemonAllies(enemies, 285f);
        if (nearbyAllies > 0 && _priestChantCooldown <= 0f)
        {
            _priestChantCooldown = 6.2f;
            StartCorruptionPriestAbility(
                CorruptionPriestAbilityKind.CorruptionChant,
                DemonFrontierCombatState.PriestChantWindup,
                PriestChantWindupSeconds,
                player.Position.X);
            return;
        }

        if (_priestAnchorCooldown <= 0f &&
            HasNearbyAnchorDefinition(dungeon, 370f))
        {
            _priestAnchorCooldown = 7.4f;
            StartCorruptionPriestAbility(
                CorruptionPriestAbilityKind.AnchorBlessing,
                DemonFrontierCombatState.PriestAnchorWindup,
                PriestAnchorWindupSeconds,
                player.Position.X);
            return;
        }

        if (_priestGroundCooldown <= 0f && distance <= 310f &&
            !HasCommittingRiftWarden(enemies) &&
            !HasCommittingPriestGround(enemies))
        {
            _priestGroundCooldown = 6.8f;
            StartCorruptionPriestAbility(
                CorruptionPriestAbilityKind.TaintedGround,
                DemonFrontierCombatState.PriestGroundWindup,
                PriestGroundWindupSeconds,
                player.Position.X);
            return;
        }

        if (distance <= 62f)
        {
            StartCorruptionPriestAbility(
                CorruptionPriestAbilityKind.StaffStrike,
                DemonFrontierCombatState.PriestStaffWindup,
                .31f,
                player.Position.X);
            return;
        }

        if (distance < 185f || nearbyAllies == 0)
            MoveAway(gameTime, player.Position, dungeon, .74f);
        else
            MoveToward(gameTime, player.Position, 245f, dungeon, .48f);
    }

    private void StartCorruptionPriestAbility(
        CorruptionPriestAbilityKind ability,
        DemonFrontierCombatState state,
        float duration,
        float targetX)
    {
        CorruptionPriestAbility = ability;
        LockAttackDirection(targetX);
        StartFrontierAction(state, duration);
    }

    private void UpdateCorruptionPriestAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState == DemonFrontierCombatState.PriestRetreat)
        {
            float direction = Position.X < player.Position.X ? -1f : 1f;
            MoveBy(new Vector2(
                direction * EffectiveMovementSpeed * .88f * elapsed,
                0f), dungeon);
        }

        if (FrontierCombatState == DemonFrontierCombatState.PriestStaffActive &&
            !_actionHitConsumed && ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    Math.Max(1, (int)MathF.Ceiling(EffectiveAttackDamage * .72f)),
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
                _pendingCorruptionPressure += 2f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.PriestChantWindup:
                ApplyPriestSupportBuff(enemies, finalRite: false);
                StartFrontierAction(
                    DemonFrontierCombatState.PriestChantActive,
                    .12f);
                break;
            case DemonFrontierCombatState.PriestChantActive:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestChantRecovery,
                    .58f);
                break;
            case DemonFrontierCombatState.PriestChantRecovery:
                FinishFrontierAction(.36f);
                break;
            case DemonFrontierCombatState.PriestAnchorWindup:
                _anchorBlessingPending = true;
                StartFrontierAction(
                    DemonFrontierCombatState.PriestAnchorActive,
                    .12f);
                break;
            case DemonFrontierCombatState.PriestAnchorActive:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestAnchorRecovery,
                    .52f);
                break;
            case DemonFrontierCombatState.PriestAnchorRecovery:
                FinishFrontierAction(.34f);
                break;
            case DemonFrontierCombatState.PriestGroundWindup:
                PlaceTaintedGround(player, dungeon);
                StartFrontierAction(
                    DemonFrontierCombatState.PriestGroundActive,
                    .12f);
                break;
            case DemonFrontierCombatState.PriestGroundActive:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestGroundRecovery,
                    .48f);
                break;
            case DemonFrontierCombatState.PriestGroundRecovery:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestRetreat,
                    .28f);
                break;
            case DemonFrontierCombatState.PriestStaffWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestStaffActive,
                    .11f);
                break;
            case DemonFrontierCombatState.PriestStaffActive:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestStaffRecovery,
                    .38f);
                break;
            case DemonFrontierCombatState.PriestStaffRecovery:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestRetreat,
                    .34f);
                break;
            case DemonFrontierCombatState.PriestFinalRiteWindup:
                ApplyPriestSupportBuff(enemies, finalRite: true);
                _pendingCorruptionPressure += 6f;
                StartFrontierAction(
                    DemonFrontierCombatState.PriestFinalRiteActive,
                    .18f);
                break;
            case DemonFrontierCombatState.PriestFinalRiteActive:
                StartFrontierAction(
                    DemonFrontierCombatState.PriestFinalRiteRecovery,
                    .72f);
                break;
            case DemonFrontierCombatState.PriestFinalRiteRecovery:
                FinishFrontierAction(.72f);
                break;
            case DemonFrontierCombatState.PriestRetreat:
                FinishFrontierAction(.30f);
                break;
        }
    }

    private void ApplyPriestSupportBuff(EnemyManager enemies, bool finalRite)
    {
        if (enemies == null)
            return;
        float radius = finalRite ? 330f : 285f;
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!enemy.IsAlive || ReferenceEquals(enemy, this) ||
                Vector2.DistanceSquared(enemy.Position, Position) > radiusSquared)
            {
                continue;
            }
            enemy.ApplyTemporaryBuff(
                finalRite ? 20 : 15,
                finalRite ? 9 : 7,
                finalRite ? 4.2f : 3.8f);
        }
    }

    private int CountNearbyDemonAllies(EnemyManager enemies, float radius)
    {
        if (enemies == null)
            return 0;
        int count = 0;
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy.IsAlive && !ReferenceEquals(enemy, this) &&
                enemy is DemonLandsEnemy &&
                Vector2.DistanceSquared(enemy.Position, Position) <= radiusSquared)
            {
                count++;
            }
        }
        return count;
    }

    private bool HasNearbyAnchorDefinition(DungeonMap dungeon, float radius)
    {
        float radiusSquared = radius * radius;
        foreach (CorruptionAnchorDefinition anchor in dungeon.CorruptionAnchors)
        {
            if (Vector2.DistanceSquared(Position, anchor.Position) <= radiusSquared)
                return true;
        }
        return false;
    }

    private void PlaceTaintedGround(
        PlayerCharacter player,
        DungeonMap dungeon)
    {
        const int width = 148;
        const int height = 28;
        int centerX = Math.Clamp(
            (int)MathF.Round(player.Position.X),
            dungeon.WorldBounds.Left + width / 2,
            dungeon.WorldBounds.Right - width / 2);
        _taintedGroundBounds = new Rectangle(
            centerX - width / 2,
            player.Bounds.Bottom - height + 2,
            width,
            height);
        _taintedGroundTimeRemaining = PriestTaintedGroundDurationSeconds;
    }

    private void UpdateRiftWarden(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        UpdateRiftScarPressure(player, elapsed);

        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateRiftWardenAction(player, dungeon, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        if (distance < 64f && _wardenStepCooldown <= 0f)
        {
            _wardenStepCooldown = 4.3f;
            BeginWardenStep(player.Position.X);
            return;
        }

        if (distance >= 112f && distance <= 278f &&
            _wardenGateCooldown <= 0f &&
            !HasCommittingPriestGround(enemies) &&
            !HasCommittingRiftWarden(enemies))
        {
            _wardenGateCooldown = 5.7f;
            RiftWardenAttack = RiftWardenAttackKind.RiftGate;
            LockAttackDirection(player.Position.X);
            StartFrontierAction(
                DemonFrontierCombatState.WardenGateWindup,
                WardenGateWindupSeconds);
            return;
        }

        if (distance <= 108f && _wardenSweepCooldown <= 0f)
        {
            _wardenSweepCooldown = 3.8f;
            RiftWardenAttack = RiftWardenAttackKind.WarpSweep;
            LockAttackDirection(player.Position.X);
            StartFrontierAction(
                DemonFrontierCombatState.WardenSweepWindup,
                WardenSweepWindupSeconds);
            return;
        }

        if (distance <= 142f)
        {
            RiftWardenAttack = RiftWardenAttackKind.RiftThrust;
            LockAttackDirection(player.Position.X);
            StartFrontierAction(
                DemonFrontierCombatState.WardenThrustWindup,
                WardenThrustWindupSeconds);
            return;
        }

        MoveToward(gameTime, player.Position, 122f, dungeon, .78f);
    }

    private void BeginWardenStep(float targetX)
    {
        RiftWardenAttack = RiftWardenAttackKind.WardenStep;
        _riftEntryPosition = Position;
        _riftExitPosition = Vector2.Zero;
        _hasRiftExitPosition = false;
        LockAttackDirection(targetX);
        StartFrontierAction(
            DemonFrontierCombatState.WardenStepEntry,
            .24f);
    }

    private void UpdateRiftWardenAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = RiftWardenAttack switch
            {
                RiftWardenAttackKind.RiftGate =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.22f),
                RiftWardenAttackKind.WarpSweep =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.12f),
                _ => EffectiveAttackDamage
            };
            AttackResolution resolution = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    RiftWardenAttack == RiftWardenAttackKind.RiftGate
                        ? _riftGatePosition
                        : Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (resolution != AttackResolution.Ignored)
                _pendingCorruptionPressure += 5f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.WardenThrustWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenThrustActive,
                    .13f);
                break;
            case DemonFrontierCombatState.WardenThrustActive:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenThrustRecovery,
                    .46f);
                break;
            case DemonFrontierCombatState.WardenThrustRecovery:
                FinishFrontierAction(.34f);
                break;
            case DemonFrontierCombatState.WardenGateWindup:
                if (!TryFindRiftDestination(
                    player,
                    dungeon,
                    defensive: false,
                    out Vector2 gatePosition))
                {
                    FinishFrontierAction(.44f);
                    break;
                }
                _riftGatePosition = gatePosition;
                _riftGateDirection = player.Position.X < gatePosition.X
                    ? -1f
                    : 1f;
                _riftGateTimeRemaining = 2.25f;
                StartFrontierAction(
                    DemonFrontierCombatState.WardenGateOpen,
                    WardenGateReactionSeconds);
                break;
            case DemonFrontierCombatState.WardenGateOpen:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenGateAttackWindup,
                    .28f);
                break;
            case DemonFrontierCombatState.WardenGateAttackWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenGateAttackActive,
                    .14f);
                break;
            case DemonFrontierCombatState.WardenGateAttackActive:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenGateRecovery,
                    .62f);
                break;
            case DemonFrontierCombatState.WardenGateRecovery:
                FinishFrontierAction(.42f);
                break;
            case DemonFrontierCombatState.WardenSweepWindup:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenSweepActive,
                    .16f);
                PlaceRiftScar(dungeon);
                break;
            case DemonFrontierCombatState.WardenSweepActive:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenSweepRecovery,
                    .58f);
                break;
            case DemonFrontierCombatState.WardenSweepRecovery:
                FinishFrontierAction(.38f);
                break;
            case DemonFrontierCombatState.WardenStepEntry:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenStepHidden,
                    .08f);
                break;
            case DemonFrontierCombatState.WardenStepHidden:
                if (!TryFindRiftDestination(
                    player,
                    dungeon,
                    defensive: true,
                    out Vector2 stepDestination))
                {
                    FinishFrontierAction(.40f);
                    break;
                }
                Position = stepDestination;
                _riftExitPosition = stepDestination;
                _hasRiftExitPosition = true;
                LockAttackDirection(player.Position.X);
                StartFrontierAction(
                    DemonFrontierCombatState.WardenStepExit,
                    .25f);
                break;
            case DemonFrontierCombatState.WardenStepExit:
                StartFrontierAction(
                    DemonFrontierCombatState.WardenStepRecovery,
                    .34f);
                break;
            case DemonFrontierCombatState.WardenStepRecovery:
                FinishFrontierAction(.36f);
                break;
        }
    }

    private void PlaceRiftScar(DungeonMap dungeon)
    {
        const int width = 174;
        int centerX = Bounds.Center.X +
            (AttackFacesLeft ? -width / 3 : width / 3);
        centerX = Math.Clamp(
            centerX,
            dungeon.WorldBounds.Left + width / 2,
            dungeon.WorldBounds.Right - width / 2);
        _riftScarBounds = new Rectangle(
            centerX - width / 2,
            Bounds.Bottom - 17,
            width,
            20);
        _riftScarTimeRemaining = 2.1f;
        _riftScarDamageCooldown = 0f;
    }

    private void UpdateRiftScarPressure(PlayerCharacter player, float elapsed)
    {
        if (!HasRiftScar || !_riftScarBounds.Intersects(player.Bounds))
            return;
        _pendingCorruptionPressure += 5f * elapsed;
        if (_riftScarDamageCooldown > 0f)
            return;
        player.ReceiveDamage(3);
        _riftScarDamageCooldown = .82f;
    }

    private void UpdateDemonLegionnaire(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateLegionnaireAction(player, enemies, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        int weakerAllies = CountNearbyWeakerDemonAllies(enemies, 245f);
        if (weakerAllies > 0 && _legionCommandCooldown <= 0f)
        {
            _legionCommandCooldown = 6.4f;
            DemonLegionnaireAttack = DemonLegionnaireAttackKind.CommandGuard;
            _commandBuffApplied = false;
            LockAttackDirection(player.Position.X);
            StartFrontierAction(
                DemonFrontierCombatState.LegionCommandGuard,
                .86f);
            return;
        }

        if (distance > 154f)
        {
            MoveToward(gameTime, player.Position, 112f, dungeon, .62f);
            return;
        }

        _legionPatternIndex++;
        DemonLegionnaireAttackKind attack = distance < 58f &&
            _legionPatternIndex % 3 == 0
                ? DemonLegionnaireAttackKind.InfernalShieldBash
                : distance < 94f && _legionPatternIndex % 2 == 0
                    ? DemonLegionnaireAttackKind.FormationSweep
                    : DemonLegionnaireAttackKind.LegionThrust;
        StartLegionnaireAttack(attack, player.Position.X);
    }

    private void StartLegionnaireAttack(
        DemonLegionnaireAttackKind attack,
        float targetX)
    {
        DemonLegionnaireAttack = attack;
        _legionGuardIntegrity = 105f;
        LockAttackDirection(targetX);
        StartFrontierAction(
            DemonFrontierCombatState.LegionGuardStance,
            .34f);
    }

    private void UpdateLegionnaireAction(
        PlayerCharacter player,
        EnemyManager enemies,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;

        if (FrontierCombatState == DemonFrontierCombatState.LegionCommandGuard &&
            !_commandBuffApplied)
        {
            _commandBuffApplied = true;
            ApplyLegionCommandBuff(enemies);
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = DemonLegionnaireAttack switch
            {
                DemonLegionnaireAttackKind.LegionThrust =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.06f),
                DemonLegionnaireAttackKind.InfernalShieldBash =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * .78f),
                _ => (int)MathF.Ceiling(EffectiveAttackDamage * .94f)
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .35f);
            if (result != AttackResolution.Ignored)
                _pendingCorruptionPressure += 3f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.LegionGuardStance:
                StartFrontierAction(
                    DemonLegionnaireAttack switch
                    {
                        DemonLegionnaireAttackKind.InfernalShieldBash =>
                            DemonFrontierCombatState.LegionBashWindup,
                        DemonLegionnaireAttackKind.FormationSweep =>
                            DemonFrontierCombatState.LegionSweepWindup,
                        _ => DemonFrontierCombatState.LegionThrustWindup
                    },
                    DemonLegionnaireAttack switch
                    {
                        DemonLegionnaireAttackKind.InfernalShieldBash => .38f,
                        DemonLegionnaireAttackKind.FormationSweep => .56f,
                        _ => .48f
                    });
                break;
            case DemonFrontierCombatState.LegionThrustWindup:
                StartFrontierAction(DemonFrontierCombatState.LegionThrustActive, .14f);
                break;
            case DemonFrontierCombatState.LegionThrustActive:
                StartFrontierAction(DemonFrontierCombatState.LegionThrustRecovery, .58f);
                break;
            case DemonFrontierCombatState.LegionThrustRecovery:
                FinishFrontierAction(.34f);
                break;
            case DemonFrontierCombatState.LegionBashWindup:
                StartFrontierAction(DemonFrontierCombatState.LegionBashActive, .13f);
                break;
            case DemonFrontierCombatState.LegionBashActive:
                StartFrontierAction(DemonFrontierCombatState.LegionBashRecovery, .70f);
                break;
            case DemonFrontierCombatState.LegionBashRecovery:
                FinishFrontierAction(.40f);
                break;
            case DemonFrontierCombatState.LegionSweepWindup:
                StartFrontierAction(DemonFrontierCombatState.LegionSweepActive, .17f);
                break;
            case DemonFrontierCombatState.LegionSweepActive:
                StartFrontierAction(DemonFrontierCombatState.LegionSweepRecovery, .66f);
                break;
            case DemonFrontierCombatState.LegionSweepRecovery:
                FinishFrontierAction(.38f);
                break;
            case DemonFrontierCombatState.LegionCommandGuard:
                StartFrontierAction(DemonFrontierCombatState.LegionCommandRecovery, .52f);
                break;
            case DemonFrontierCombatState.LegionCommandRecovery:
                FinishFrontierAction(.46f);
                break;
            case DemonFrontierCombatState.LegionGuardBroken:
                _legionGuardIntegrity = 72f;
                FinishFrontierAction(.62f);
                break;
        }
    }

    private void ApplyLegionCommandBuff(EnemyManager enemies)
    {
        if (enemies == null)
            return;
        const float radiusSquared = 245f * 245f;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!enemy.IsAlive || ReferenceEquals(enemy, this) ||
                enemy is not DemonLandsEnemy ||
                Vector2.DistanceSquared(enemy.Position, Position) > radiusSquared)
            {
                continue;
            }
            enemy.ApplyTemporaryBuff(6, 2, 3.2f);
        }
    }

    private int CountNearbyWeakerDemonAllies(EnemyManager enemies, float radius)
    {
        if (enemies == null)
            return 0;
        int count = 0;
        float radiusSquared = radius * radius;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!enemy.IsAlive || ReferenceEquals(enemy, this) ||
                enemy is not DemonLandsEnemy ||
                enemy.MaxPoise >= MaxPoise ||
                Vector2.DistanceSquared(enemy.Position, Position) > radiusSquared)
            {
                continue;
            }
            count++;
        }
        return count;
    }

    private void EnterLegionGuardBreak()
    {
        if (Type != EnemyType.DemonLegionnaire || !IsAlive ||
            FrontierCombatState == DemonFrontierCombatState.LegionGuardBroken)
        {
            return;
        }
        Attack.Cancel();
        DemonLegionnaireAttack = DemonLegionnaireAttackKind.None;
        _legionGuardIntegrity = 0f;
        _guardReactionTimeRemaining = 0f;
        StartFrontierAction(
            DemonFrontierCombatState.LegionGuardBroken,
            LegionGuardBreakSeconds);
    }

    private void UpdateHornedGeneral(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);
        UpdateGeneralPersistentPressure(player, elapsed);

        float healthRatio = CurrentHealth / (float)MaxHealth;
        if (!_phaseTwoEntered && healthRatio <= .60f)
        {
            _phaseTwoEntered = true;
            CombatPhase = 2;
            HornedGeneralAttack = HornedGeneralAttackKind.None;
            LockAttackDirection(player.Position.X);
            Attack.Cancel();
            StartFrontierAction(
                DemonFrontierCombatState.GeneralPhaseTransition,
                1.08f);
            return;
        }
        if (!_phaseThreeEntered && _phaseTwoEntered &&
            FrontierCombatState == DemonFrontierCombatState.Ready &&
            healthRatio <= .25f)
        {
            _phaseThreeEntered = true;
            CombatPhase = 3;
            HornedGeneralAttack = HornedGeneralAttackKind.None;
            LockAttackDirection(player.Position.X);
            Attack.Cancel();
            StartFrontierAction(
                DemonFrontierCombatState.GeneralPhaseTransition,
                1.16f);
            return;
        }

        if (FrontierCombatState != DemonFrontierCombatState.Ready)
        {
            UpdateHornedGeneralAction(player, dungeon, enemies, elapsed);
            return;
        }

        float distance = MathF.Abs(player.Position.X - Position.X);
        FaceToward(player.Position);
        if (_polishCooldown > 0f)
            return;

        _generalPatternIndex++;
        if (CombatPhase == 1)
        {
            if (_generalGuardCooldown <= 0f && _generalPatternIndex % 5 == 0)
            {
                BeginGeneralGuard(player.Position.X);
                return;
            }
            if (distance < 66f && _generalPatternIndex % 3 == 0)
                StartGeneralAttack(HornedGeneralAttackKind.WarStomp,
                    DemonFrontierCombatState.GeneralStompWindup, .64f, player.Position.X);
            else if (distance < 105f)
                StartGeneralAttack(HornedGeneralAttackKind.HornedSweep,
                    DemonFrontierCombatState.GeneralSweepWindup, .56f, player.Position.X);
            else if (distance <= 190f)
                StartGeneralAttack(HornedGeneralAttackKind.LegionThrust,
                    DemonFrontierCombatState.GeneralThrustWindup, .52f, player.Position.X);
            else
                StartGeneralAttack(HornedGeneralAttackKind.TacticalStep,
                    DemonFrontierCombatState.GeneralTacticalStep, .44f, player.Position.X);
            return;
        }

        if (_generalAnchorCooldown <= 0f && _generalPatternIndex % 6 == 0)
        {
            _generalAnchorCooldown = CombatPhase == 3 ? 10f : 8.5f;
            _generalPulseCooldown = MathF.Max(_generalPulseCooldown, 3.2f);
            _generalChargeCooldown = MathF.Max(_generalChargeCooldown, 2.8f);
            _requestedBossAnchorPosition = player.Position + new Vector2(
                player.Position.X < Position.X ? -118f : 118f,
                0f);
            StartGeneralAttack(HornedGeneralAttackKind.SummonAnchor,
                DemonFrontierCombatState.GeneralAnchorWindup, .92f, player.Position.X);
            return;
        }
        if (_generalPulseCooldown <= 0f && distance <= 210f &&
            _generalPatternIndex % 4 == 0)
        {
            _generalPulseCooldown = 6.2f;
            StartGeneralAttack(HornedGeneralAttackKind.CorruptionPulse,
                DemonFrontierCombatState.GeneralPulseWindup, .82f, player.Position.X);
            return;
        }
        if (_generalChargeCooldown <= 0f && distance >= 150f && distance <= 390f &&
            _generalPatternIndex % 3 == 0)
        {
            _generalChargeCooldown = 6.8f;
            StartGeneralAttack(HornedGeneralAttackKind.InfernalCharge,
                DemonFrontierCombatState.GeneralChargeWindup, .68f, player.Position.X);
            return;
        }
        if (distance <= 220f)
        {
            StartGeneralAttack(HornedGeneralAttackKind.RiftCleave,
                DemonFrontierCombatState.GeneralRiftCleaveWindup, .70f, player.Position.X);
            return;
        }
        MoveToward(gameTime, player.Position, 145f, dungeon,
            CombatPhase == 3 ? .92f : .72f);
    }

    private void BeginGeneralGuard(float targetX)
    {
        _generalGuardCooldown = 6.4f;
        _generalGuardIntegrity = 145f;
        HornedGeneralAttack = HornedGeneralAttackKind.CommandGuard;
        LockAttackDirection(targetX);
        StartFrontierAction(DemonFrontierCombatState.GeneralGuard, .78f);
    }

    private void StartGeneralAttack(
        HornedGeneralAttackKind attack,
        DemonFrontierCombatState state,
        float duration,
        float targetX)
    {
        HornedGeneralAttack = attack;
        LockAttackDirection(targetX);
        StartFrontierAction(state, duration);
    }

    private void UpdateHornedGeneralAction(
        PlayerCharacter player,
        DungeonMap dungeon,
        EnemyManager enemies,
        float elapsed)
    {
        _actionTimeRemaining -= elapsed;
        if (FrontierCombatState is DemonFrontierCombatState.GeneralChargeActive or
            DemonFrontierCombatState.GeneralLegionsEndCharge)
        {
            float speed = FrontierCombatState ==
                DemonFrontierCombatState.GeneralLegionsEndCharge ? 585f : 385f;
            MoveBy(new Vector2(_lockedAttackDirection * speed * elapsed, 0f), dungeon);
        }
        else if (FrontierCombatState == DemonFrontierCombatState.GeneralTacticalStep)
        {
            float direction = MathF.Abs(player.Position.X - Position.X) < 105f
                ? -_lockedAttackDirection
                : _lockedAttackDirection;
            MoveBy(new Vector2(direction * EffectiveMovementSpeed * .72f * elapsed, 0f),
                dungeon);
        }

        if (IsCombatAttackActive && !_actionHitConsumed &&
            ActiveAttackArea.Intersects(player.BodyHurtbox))
        {
            int damage = HornedGeneralAttack switch
            {
                HornedGeneralAttackKind.WarStomp =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * .84f),
                HornedGeneralAttackKind.RiftCleave =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.18f),
                HornedGeneralAttackKind.InfernalCharge =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.14f),
                HornedGeneralAttackKind.LegionsEnd =>
                    (int)MathF.Ceiling(EffectiveAttackDamage * 1.34f),
                _ => EffectiveAttackDamage
            };
            AttackResolution result = player.ReceiveMeleeAttack(
                new AttackContact(
                    damage,
                    Position,
                    blockable: true,
                    unblockable: false,
                    ActiveAttackArea));
            if (result == AttackResolution.PerfectGuard)
                ApplyPoiseDamage(MaxPoise * .18f);
            if (result != AttackResolution.Ignored)
                _pendingCorruptionPressure += CombatPhase + 3f;
            _actionHitConsumed = true;
        }

        if (_actionTimeRemaining > 0f)
            return;

        switch (FrontierCombatState)
        {
            case DemonFrontierCombatState.GeneralPhaseTransition:
                if (CombatPhase == 3 && !_legionsEndUsed)
                    BeginLegionsEnd(player.Position.X);
                else
                    FinishFrontierAction(.55f);
                break;
            case DemonFrontierCombatState.GeneralThrustWindup:
                StartFrontierAction(DemonFrontierCombatState.GeneralThrustActive, .15f);
                break;
            case DemonFrontierCombatState.GeneralThrustActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralThrustRecovery, .58f);
                break;
            case DemonFrontierCombatState.GeneralThrustRecovery:
                FinishFrontierAction(.34f);
                break;
            case DemonFrontierCombatState.GeneralSweepWindup:
                StartFrontierAction(DemonFrontierCombatState.GeneralSweepActive, .18f);
                break;
            case DemonFrontierCombatState.GeneralSweepActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralSweepRecovery, .66f);
                break;
            case DemonFrontierCombatState.GeneralSweepRecovery:
                FinishFrontierAction(.36f);
                break;
            case DemonFrontierCombatState.GeneralGuard:
                StartFrontierAction(DemonFrontierCombatState.GeneralGuardRecovery, .52f);
                break;
            case DemonFrontierCombatState.GeneralGuardRecovery:
                FinishFrontierAction(.48f);
                break;
            case DemonFrontierCombatState.GeneralGuardBroken:
                _generalGuardIntegrity = 95f;
                FinishFrontierAction(.72f);
                break;
            case DemonFrontierCombatState.GeneralStompWindup:
                StartFrontierAction(DemonFrontierCombatState.GeneralStompActive, .18f);
                break;
            case DemonFrontierCombatState.GeneralStompActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralStompRecovery, .72f);
                break;
            case DemonFrontierCombatState.GeneralStompRecovery:
                FinishFrontierAction(.42f);
                break;
            case DemonFrontierCombatState.GeneralTacticalStep:
                FinishFrontierAction(.28f);
                break;
            case DemonFrontierCombatState.GeneralRiftCleaveWindup:
                PlaceGeneralRiftPath();
                StartFrontierAction(DemonFrontierCombatState.GeneralRiftCleaveActive, .17f);
                break;
            case DemonFrontierCombatState.GeneralRiftCleaveActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralRiftCleaveRecovery, .82f);
                break;
            case DemonFrontierCombatState.GeneralRiftCleaveRecovery:
                if (CombatPhase == 3 && _generalPatternIndex % 2 == 0 &&
                    MathF.Abs(player.Position.X - Position.X) <= 190f)
                {
                    StartGeneralAttack(
                        HornedGeneralAttackKind.LegionThrust,
                        DemonFrontierCombatState.GeneralThrustWindup,
                        .38f,
                        player.Position.X);
                }
                else
                {
                    FinishFrontierAction(.42f);
                }
                break;
            case DemonFrontierCombatState.GeneralPulseWindup:
                _pulseCenter = Position;
                _corruptionPulseTimeRemaining = 1.55f;
                StartFrontierAction(DemonFrontierCombatState.GeneralPulseActive, .36f);
                break;
            case DemonFrontierCombatState.GeneralPulseActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralPulseRecovery, .68f);
                break;
            case DemonFrontierCombatState.GeneralPulseRecovery:
                FinishFrontierAction(.48f);
                break;
            case DemonFrontierCombatState.GeneralAnchorWindup:
                AnchorPulsePending = true;
                StartFrontierAction(DemonFrontierCombatState.GeneralAnchorActive, .22f);
                break;
            case DemonFrontierCombatState.GeneralAnchorActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralAnchorRecovery, .74f);
                break;
            case DemonFrontierCombatState.GeneralAnchorRecovery:
                FinishFrontierAction(.52f);
                break;
            case DemonFrontierCombatState.GeneralChargeWindup:
                StartFrontierAction(DemonFrontierCombatState.GeneralChargeActive, .78f);
                break;
            case DemonFrontierCombatState.GeneralChargeActive:
                StartFrontierAction(DemonFrontierCombatState.GeneralChargeRecovery, 1.02f);
                break;
            case DemonFrontierCombatState.GeneralChargeRecovery:
                FinishFrontierAction(.58f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndRaise:
                StartFrontierAction(DemonFrontierCombatState.GeneralLegionsEndWarning, .48f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndWarning:
                StartFrontierAction(DemonFrontierCombatState.GeneralLegionsEndPause, .28f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndPause:
                LockAttackDirection(player.Position.X);
                StartFrontierAction(DemonFrontierCombatState.GeneralLegionsEndCharge, 1.14f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndCharge:
                StartFrontierAction(DemonFrontierCombatState.GeneralLegionsEndCleave, .22f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndCleave:
                StartFrontierAction(DemonFrontierCombatState.GeneralLegionsEndRecovery, 1.42f);
                break;
            case DemonFrontierCombatState.GeneralLegionsEndRecovery:
                FinishFrontierAction(.82f);
                break;
        }
    }

    private void BeginLegionsEnd(float targetX)
    {
        _legionsEndUsed = true;
        HornedGeneralAttack = HornedGeneralAttackKind.LegionsEnd;
        LockAttackDirection(targetX);
        StartFrontierAction(
            DemonFrontierCombatState.GeneralLegionsEndRaise,
            .76f);
    }

    private void UpdateGeneralPersistentPressure(
        PlayerCharacter player,
        float elapsed)
    {
        if (!HasCorruptionPulse ||
            Vector2.DistanceSquared(player.Position, _pulseCenter) >
                GeneralPulseRadius * GeneralPulseRadius)
        {
            return;
        }
        _pendingCorruptionPressure += 11f * elapsed;
    }

    private void PlaceGeneralRiftPath()
    {
        const int reach = 220;
        const int height = 28;
        int x = AttackFacesLeft ? Bounds.Left - reach + 22 : Bounds.Right - 22;
        _generalRiftPathBounds = new Rectangle(
            x,
            Bounds.Bottom - height,
            reach,
            height);
        _generalRiftPathTimeRemaining = 1.25f;
    }

    private void EnterGeneralGuardBreak()
    {
        if (Type != EnemyType.HornedGeneral || !IsAlive ||
            FrontierCombatState == DemonFrontierCombatState.GeneralGuardBroken)
        {
            return;
        }
        Attack.Cancel();
        HornedGeneralAttack = HornedGeneralAttackKind.None;
        _generalGuardIntegrity = 0f;
        _guardReactionTimeRemaining = 0f;
        StartFrontierAction(
            DemonFrontierCombatState.GeneralGuardBroken,
            GeneralGuardBreakSeconds);
    }

    private bool HasCommittingRiftWarden(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy warden ||
                !warden.IsAlive || warden.Type != EnemyType.RiftWarden ||
                Vector2.DistanceSquared(warden.Position, Position) > 420f * 420f)
            {
                continue;
            }
            if (warden.FrontierCombatState is
                DemonFrontierCombatState.WardenGateWindup or
                DemonFrontierCombatState.WardenGateOpen or
                DemonFrontierCombatState.WardenGateAttackWindup or
                DemonFrontierCombatState.WardenGateAttackActive or
                DemonFrontierCombatState.WardenStepEntry or
                DemonFrontierCombatState.WardenStepExit)
            {
                return true;
            }
            if (warden.HasRiftGate)
                return true;
        }
        return false;
    }

    private bool HasCommittingPriestGround(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (enemy is not DemonLandsEnemy priest ||
                !priest.IsAlive || priest.Type != EnemyType.CorruptionPriest ||
                Vector2.DistanceSquared(priest.Position, Position) > 420f * 420f)
            {
                continue;
            }
            if (priest.FrontierCombatState is
                DemonFrontierCombatState.PriestGroundWindup or
                DemonFrontierCombatState.PriestGroundActive or
                DemonFrontierCombatState.PriestFinalRiteWindup or
                DemonFrontierCombatState.PriestFinalRiteActive)
            {
                return true;
            }
            if (priest.HasTaintedGround)
                return true;
        }
        return false;
    }

    private bool HasNearbyImpPartner(EnemyManager enemies)
    {
        if (enemies == null)
            return false;
        foreach (Enemy enemy in enemies.Enemies)
        {
            if (!ReferenceEquals(enemy, this) && enemy.IsAlive &&
                enemy.Type == EnemyType.ImpRaider &&
                Vector2.DistanceSquared(enemy.Position, Position) <= 220f * 220f)
                return true;
        }
        return false;
    }

    private void StartImpAction(
        ImpRaiderAttackKind attack,
        DemonFrontierCombatState state,
        float duration,
        float targetX)
    {
        ImpRaiderAttack = attack;
        LockAttackDirection(targetX);
        StartFrontierAction(state, duration);
    }

    private void StartFrontierAction(
        DemonFrontierCombatState state,
        float duration)
    {
        FrontierCombatState = state;
        _actionDuration = duration;
        _actionTimeRemaining = duration;
        _actionHitConsumed = false;
    }

    private void FinishFrontierAction(float cooldown)
    {
        FrontierCombatState = DemonFrontierCombatState.Ready;
        ImpRaiderAttack = ImpRaiderAttackKind.None;
        HellArcherAttack = HellArcherAttackKind.None;
        SpineHoundAttack = SpineHoundAttackKind.None;
        RiftStalkerAttack = RiftStalkerAttackKind.None;
        BoneHarvesterAttack = BoneHarvesterAttackKind.None;
        HellbruteAttack = HellbruteAttackKind.None;
        CorruptionPriestAbility = CorruptionPriestAbilityKind.None;
        RiftWardenAttack = RiftWardenAttackKind.None;
        DemonLegionnaireAttack = DemonLegionnaireAttackKind.None;
        HornedGeneralAttack = HornedGeneralAttackKind.None;
        _actionDuration = 0f;
        _actionTimeRemaining = 0f;
        _actionHitConsumed = false;
        _frenzyStabsRemaining = 0;
        _twinClawsRemaining = 0;
        _hasRiftExitPosition = false;
        _polishCooldown = MathF.Max(_polishCooldown, cooldown);
    }

    private void CancelFrontierAction(float cooldown)
    {
        Attack.Cancel();
        FinishFrontierAction(cooldown);
    }

    private void LockAttackDirection(float targetX)
    {
        float offset = targetX - Position.X;
        if (MathF.Abs(offset) > 1f)
            _lockedAttackDirection = MathF.Sign(offset);
    }

    private Rectangle CreateImpAttackArea()
    {
        int reach = ImpRaiderAttack switch
        {
            ImpRaiderAttackKind.PouncingCut => 60,
            ImpRaiderAttackKind.FrenziedStab => 48,
            _ => 52
        };
        int height = ImpRaiderAttack == ImpRaiderAttackKind.FrenziedStab
            ? 28
            : 42;
        int y = Bounds.Bottom - height - 3;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 8, y, reach, height)
            : new Rectangle(Bounds.Right - 8, y, reach, height);
    }

    private Rectangle CreateSpineHoundAttackArea()
    {
        if (SpineHoundAttack == SpineHoundAttackKind.SpineBurst)
        {
            Rectangle burst = Bounds;
            burst.Inflate(25, 18);
            return burst;
        }

        int reach = SpineHoundAttack == SpineHoundAttackKind.BodyRush
            ? 54
            : 47;
        int height = SpineHoundAttack == SpineHoundAttackKind.BodyRush
            ? 34
            : 40;
        int y = Bounds.Bottom - height - 1;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 15, y, reach, height)
            : new Rectangle(Bounds.Right - 15, y, reach, height);
    }

    private Rectangle CreateRiftStalkerAttackArea()
    {
        int reach = RiftStalkerAttack == RiftStalkerAttackKind.RiftAmbush
            ? 66
            : 55;
        int height = 62;
        int y = Bounds.Bottom - height - 2;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 9, y, reach, height)
            : new Rectangle(Bounds.Right - 9, y, reach, height);
    }

    private Rectangle CreateBoneHarvesterAttackArea()
    {
        int reach = BoneHarvesterAttack switch
        {
            BoneHarvesterAttackKind.HarvesterHook => 136,
            BoneHarvesterAttackKind.ReapingSweep => 116,
            _ => 72
        };
        int height = BoneHarvesterAttack == BoneHarvesterAttackKind.HarvesterHook
            ? 40
            : 66;
        int y = BoneHarvesterAttack == BoneHarvesterAttackKind.HarvesterHook
            ? Bounds.Center.Y - 8
            : Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 12, y, reach, height)
            : new Rectangle(Bounds.Right - 12, y, reach, height);
    }

    private Rectangle CreateHellbruteAttackArea()
    {
        if (HellbruteAttack == HellbruteAttackKind.BruteSlam)
        {
            Rectangle slam = Bounds;
            slam.Inflate(44, 13);
            slam.Y = Bounds.Bottom - 52;
            slam.Height = 55;
            return slam;
        }
        if (HellbruteAttack == HellbruteAttackKind.SeismicPound)
        {
            int travel = 42 + (int)MathF.Round(ActionProgress * 86f);
            int centerX = Bounds.Center.X +
                (AttackFacesLeft ? -travel : travel);
            return new Rectangle(centerX - 32, Bounds.Bottom - 28, 64, 31);
        }

        int reach = HellbruteAttack == HellbruteAttackKind.InfernalCharge
            ? 64
            : 92;
        int height = HellbruteAttack == HellbruteAttackKind.InfernalCharge
            ? 72
            : 66;
        int y = Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 19, y, reach, height)
            : new Rectangle(Bounds.Right - 19, y, reach, height);
    }

    private Rectangle CreateCorruptionPriestAttackArea()
    {
        const int reach = 58;
        const int height = 54;
        int y = Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 10, y, reach, height)
            : new Rectangle(Bounds.Right - 10, y, reach, height);
    }

    private Rectangle CreateRiftWardenAttackArea()
    {
        if (RiftWardenAttack == RiftWardenAttackKind.RiftGate)
        {
            const int gateReach = 108;
            const int gateHeight = 62;
            int gateY = (int)MathF.Round(_riftGatePosition.Y) - gateHeight / 2;
            int gateX = (int)MathF.Round(_riftGatePosition.X);
            return _riftGateDirection < 0f
                ? new Rectangle(gateX - gateReach, gateY, gateReach, gateHeight)
                : new Rectangle(gateX, gateY, gateReach, gateHeight);
        }

        int reach = RiftWardenAttack == RiftWardenAttackKind.WarpSweep
            ? 118
            : 132;
        int height = RiftWardenAttack == RiftWardenAttackKind.WarpSweep
            ? 70
            : 54;
        int y = Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 16, y, reach, height)
            : new Rectangle(Bounds.Right - 16, y, reach, height);
    }

    private Rectangle CreateLegionnaireAttackArea()
    {
        int reach = DemonLegionnaireAttack switch
        {
            DemonLegionnaireAttackKind.LegionThrust => 126,
            DemonLegionnaireAttackKind.FormationSweep => 110,
            _ => 56
        };
        int height = DemonLegionnaireAttack ==
            DemonLegionnaireAttackKind.InfernalShieldBash ? 58 : 66;
        int y = Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 15, y, reach, height)
            : new Rectangle(Bounds.Right - 15, y, reach, height);
    }

    private Rectangle CreateHornedGeneralAttackArea()
    {
        if (HornedGeneralAttack == HornedGeneralAttackKind.RiftCleave &&
            _generalRiftPathBounds.Width > 0)
        {
            return _generalRiftPathBounds;
        }
        if (HornedGeneralAttack == HornedGeneralAttackKind.WarStomp)
        {
            Rectangle stomp = Bounds;
            stomp.Inflate(58, 8);
            stomp.Y = Bounds.Bottom - 48;
            stomp.Height = 51;
            return stomp;
        }

        int reach = HornedGeneralAttack switch
        {
            HornedGeneralAttackKind.LegionThrust => 158,
            HornedGeneralAttackKind.HornedSweep => 146,
            HornedGeneralAttackKind.InfernalCharge => 86,
            HornedGeneralAttackKind.LegionsEnd => 188,
            _ => 116
        };
        int height = HornedGeneralAttack is
            HornedGeneralAttackKind.InfernalCharge or
            HornedGeneralAttackKind.LegionsEnd ? 92 : 78;
        int y = Bounds.Bottom - height;
        return AttackFacesLeft
            ? new Rectangle(Bounds.Left - reach + 22, y, reach, height)
            : new Rectangle(Bounds.Right - 22, y, reach, height);
    }

    private Vector2 GetHellArrowReleasePosition() => Position + new Vector2(
        AttackFacesLeft ? -30f : 30f,
        -Size.Y * .23f);

    private void UpdateRanged(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        float distance)
    {
        float preferred = Type == EnemyType.RiftWarden ? 250f : 285f;
        if (distance < 145f)
        {
            MoveAway(gameTime, player.Position, dungeon, 1.05f);
            return;
        }
        if (distance > preferred + 80f)
        {
            MoveToward(gameTime, player.Position, preferred, dungeon, .78f);
            return;
        }
        FaceToward(player.Position);
        if (_rangedCooldown > 0f)
            return;

        Vector2 origin = Position + new Vector2(0f, -Size.Y * .12f);
        Vector2 direction = player.Position - origin;
        bool heavy = Type == EnemyType.RiftWarden;
        projectiles.SpawnEnemyBolt(
            origin,
            direction,
            EffectiveAttackDamage,
            RoomId,
            heavy);
        _rangedCooldown = Type switch
        {
            EnemyType.HellArcher => 1.65f,
            EnemyType.RiftWarden => 2.05f,
            _ => 2.25f
        };
    }

    private static Vector2 GetSize(EnemyType type) => type switch
    {
        EnemyType.ImpRaider => new Vector2(38f, 48f),
        EnemyType.HellArcher => new Vector2(42f, 70f),
        EnemyType.SpineHound => new Vector2(62f, 36f),
        EnemyType.RiftStalker => new Vector2(40f, 72f),
        EnemyType.BoneHarvester => new Vector2(58f, 78f),
        EnemyType.Hellbrute => new Vector2(82f, 94f),
        EnemyType.CorruptionPriest => new Vector2(44f, 76f),
        EnemyType.RiftWarden => new Vector2(64f, 84f),
        EnemyType.DemonLegionnaire => new Vector2(58f, 76f),
        EnemyType.HornedGeneral => new Vector2(78f, 102f),
        _ => new Vector2(44f, 58f)
    };

    private static float GetSpeed(EnemyType type) => type switch
    {
        EnemyType.ImpRaider => 158f,
        EnemyType.HellArcher => 132f,
        EnemyType.SpineHound => 172f,
        EnemyType.RiftStalker => 128f,
        EnemyType.BoneHarvester => 76f,
        EnemyType.Hellbrute => 57f,
        EnemyType.CorruptionPriest => 72f,
        EnemyType.RiftWarden => 88f,
        EnemyType.DemonLegionnaire => 104f,
        EnemyType.HornedGeneral => 112f,
        _ => 110f
    };

    private static float GetDetection(EnemyType type) => type is
        EnemyType.HellArcher or EnemyType.CorruptionPriest or
        EnemyType.RiftWarden ? 480f : type == EnemyType.HornedGeneral ? 620f : 380f;

    private static int GetBaseHealth(EnemyType type) => type switch
    {
        EnemyType.ImpRaider => 82,
        EnemyType.HellArcher => 88,
        EnemyType.SpineHound => 132,
        EnemyType.RiftStalker => 118,
        EnemyType.BoneHarvester => 270,
        EnemyType.Hellbrute => 480,
        EnemyType.CorruptionPriest => 175,
        EnemyType.RiftWarden => 430,
        EnemyType.DemonLegionnaire => 470,
        EnemyType.HornedGeneral => 2400,
        _ => 100
    };

    private static int GetBaseDamage(EnemyType type) => type switch
    {
        EnemyType.ImpRaider => 16,
        EnemyType.HellArcher => 14,
        EnemyType.SpineHound => 18,
        EnemyType.RiftStalker => 19,
        EnemyType.BoneHarvester => 26,
        EnemyType.Hellbrute => 32,
        EnemyType.RiftWarden => 26,
        EnemyType.DemonLegionnaire => 29,
        EnemyType.HornedGeneral => 39,
        _ => 18
    };

    private static int GetExperience(EnemyType type) => type switch
    {
        EnemyType.RiftWarden or EnemyType.DemonLegionnaire => 300,
        EnemyType.HornedGeneral => 2200,
        EnemyType.Hellbrute => 220,
        _ => 110
    };

    private static float GetRange(EnemyType type) => type switch
    {
        EnemyType.BoneHarvester => 82f,
        EnemyType.Hellbrute => 76f,
        EnemyType.DemonLegionnaire => 78f,
        EnemyType.HornedGeneral => 106f,
        _ => 58f
    };

    private static float GetCooldown(EnemyType type) => type switch
    {
        EnemyType.SpineHound => .78f,
        EnemyType.Hellbrute => 1.65f,
        EnemyType.HornedGeneral => .98f,
        _ => 1.15f
    };

    private static int Scale(int value, int level, int worldTier, bool health)
    {
        int perLevel = health ? Math.Max(8, value / 8) : 2;
        int leveled = value + (Math.Max(1, level) - 1) * perLevel;
        int percent = health
            ? WorldProgression.GetHealthMultiplierPercent(worldTier)
            : WorldProgression.GetDamageMultiplierPercent(worldTier);
        return WorldProgression.ApplyPercent(leveled, percent);
    }
}
