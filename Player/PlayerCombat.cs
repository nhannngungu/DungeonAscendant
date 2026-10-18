using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
using DungeonAscendant.Localization;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Player;

/// <summary>
/// Owns mutually exclusive player combat actions, combo buffering, stamina,
/// dodge timing, directional blocking, and guard break behavior.
/// </summary>
public sealed class PlayerCombat
{
    public float ExternalStaminaRegenMultiplier { get; set; } = 1f;
    public const float ComboResetSeconds = 0.55f;
    public const float DodgeStaminaCost = 24f;
    public const float DodgeDurationSeconds = 0.34f;
    public const float DodgeStartupSeconds = 0.05f;
    public const float DodgeInvulnerabilitySeconds = 0.17f;
    public const float DodgeSpeed = 500f;
    public const float BlockMinimumStaminaCost = 10f;
    public const float BlockStaminaPerDamage = 1f;
    public const float GuardBreakSeconds = 0.7f;
    public const float HurtSeconds = 0.22f;

    private const float FeedbackSeconds = 0.18f;
    public const float PerfectGuardWindowSeconds = .22f;
    public const float PerfectGuardHitstopSeconds = .055f;

    private readonly CombatInputBuffer _inputBuffer = new();
    private readonly CombatInputBuffer _knightInputBuffer =
        new(KnightTuning.ChordWindowSeconds);
    private bool _knightDashSkill1Pending;
    private bool _knightAirSkill2Pending;
    private bool _knightForceFallingShield;
    private readonly CombatInputBuffer _duelistInputBuffer =
        new(DuelistTuning.ChordWindowSeconds);
    private float _stateElapsed;
    private float _blockFeedbackRemaining;
    private float _attackPowerMultiplier = 1f;
    private float _projectileSpeedMultiplier = 1f;
    private float _techniqueFeedbackRemaining;
    private int _techniqueStageIndex;
    private int _resourceGainAttackId = -1;
    private bool _dodgeBuffered;
    private float _dodgeBufferRemaining;
    private bool _techniqueStageBuffered;
    private int _techniqueSequenceIndex;
    private int _effectiveTechniqueStageCount;
    private bool _duelistTechniqueGainAwarded;
    private int _duelistRepositionAttackId = -1;
    private float _perfectPhantomFeedbackRemaining;
    private WeaponTechniqueInput? _duelistResolvedInput;
    private bool _duelistDashSkill1Pending;
    private bool _duelistAirSkill2Pending;
    private bool _duelistSkill1Held;
    private bool _duelistSkill3Held;
    private float _duelistHeavenSplitterHoldSeconds;
    private int _duelistHeavenSplitterLevel = 1;
    private float _duelistRiposteWindowRemaining;
    private float _duelistOverdriveRemaining;
    private float _duelistOverdriveCooldownRemaining;
    private bool _duelistUltimateTargetAvailable;
    private int _duelistUltimateProcessedHits;
    private float _rangerSkill1HoldSeconds;
    private float _rangerSkill3HoldSeconds;
    private bool _rangerSkill1CurrentlyHeld;
    private bool _rangerSkill3CurrentlyHeld;
    private float _rangerChargeRatio;
    private float _rangerWindrunnerDirection;
    private RangerDrawState _rangerDrawState;
    private SkyfallDistance _skyfallDistance = SkyfallDistance.Medium;
    private float _rangerUltimateFocusSnapshot;
    private float _rangerLockedDirection = 1f;
    private float _windrunnerPerfectRewardRemaining;
    private int _rangerFocusHitAttackId = -1;
    private int _rangerInputMask;
    private float _rangerChordRemaining;
    private float _rangerEvadeCooldownRemaining;
    private float _rangerBackstepWindowRemaining;
    private float _rangerCounterWindowRemaining;
    private bool _rangerPerfectDodgeActive;
    private float _rangerEvadeDirection;
    private float _rangerFlowGainThisTechnique;
    private bool _rangerUltimateTargetAvailable;
    private int _techniqueUseCounter;
    private int _currentTechniqueUseId;
    private float _poisePowerMultiplier = 1f;
    private FacingDirection _currentFacing = FacingDirection.Right;
    private float _currentHorizontalDirection;
    private float _spellbladeSkill2HoldSeconds;
    private bool _spellbladeSkill2CurrentlyHeld;
    private int _ancientStaffInputMask;
    private float _ancientStaffChordRemaining;
    private bool _ancientStaffDashSkill1Pending;
    private bool _ancientStaffDashSkill2Pending;
    private bool _ancientStaffAirSkill2Pending;
    private bool _ancientStaffSkill2CurrentlyHeld;
    private bool _ancientStaffUltimateTargetAvailable;
    private float _fourthVeilTimeRemaining;
    private float _fourthVeilExitVisualRemaining;
    private float _aegisImpactVerticalOffset;
    private RunePlacementDistance _runePlacementDistance =
        RunePlacementDistance.Medium;
    private int _availableGroundRuneCount;
    private float _perfectGuardFeedbackRemaining;
    private float _perfectGuardRewardRemaining;
    private float _hitstopRemaining;
    private int _greatswordInputMask;
    private float _greatswordChordRemaining;
    private bool _greatswordSkill1CurrentlyHeld;
    private bool _greatswordSkill2CurrentlyHeld;
    private bool _greatswordSkill3CurrentlyHeld;
    private float _greatswordCounterWindowRemaining;
    private bool _greatswordFlowingMomentum;
    private float _greatswordWhirlElapsed;
    private bool _greatswordWhirlEndRequested;
    private bool _greatswordWhirlStopping;
    private bool _greatswordFallingWasAirborne;
    private bool _greatswordFallingPoiseBonus;
    private float _greatswordLandingPulseRemaining;
    private int _greatswordLandingImpactId;
    private float _greatswordChargeRatio;
    private int _greatswordChargeLevel = 1;
    private float _greatswordRecoveryBonusSeconds;
    private bool _greatswordTitanBraced;
    private bool _greatswordBraceProtectedHit;
    private bool _greatswordUltimateTargetAvailable;
    private bool _knightUltimateTargetAvailable;
    private bool _knightFallingWasAirborne;
    private float _knightLandingPulseRemaining;
    private int _knightLandingImpactId;
    private bool _knightFinalLandingWasAirborne;
    private bool _knightRiposteBuffered;
    private int _sovereignInputMask;
    private float _sovereignChordRemaining;
    private float _sovereignStepCooldownRemaining;
    private float _sovereignThroneTimeRemaining;
    private float _sovereignChargeRatio;
    private int _sovereignChargeLevel = 1;
    private bool _sovereignSkill1Held;
    private bool _sovereignSkill3Held;
    private bool _sovereignUltimateTargetAvailable;
    private int _scytheInputMask;
    private float _scytheChordRemaining;
    private bool _scytheDashSkill1Pending;
    private bool _scytheAirSkill2Pending;
    private bool _scytheUltimateTargetAvailable;
    private float _scytheWraithCutWindowRemaining;
    private float _scythePerfectDodgeVisualRemaining;
    private Vector2 _scythePerfectDodgeSource;
    private float _scytheDodgeWeaveRewardRemaining;
    private bool _scytheEnhancedAttackPending;
    private bool _scytheEnhancedThisTechnique;
    private float _scytheMoveDirection = 1f;
    private bool _scytheShadowSeverEnabled;
    private Vector2 _sovereignTransitDirection;
    private bool _sovereignPerfectRiftCounterPending;
    private Vector2 _sovereignPerfectRiftSource;
    private float _sovereignPerfectRiftVisualRemaining;
    private bool _isGrounded;
    private float _verticalVelocity;
    private WeaponDefinition _weapon = EquipmentCatalog.KnightLongSword;
    private ArmorDefinition _armor = EquipmentCatalog.KnightArmor;
    private EquipmentItem _armorItem = EquipmentCatalog.CreateStartingArmor();

    public CombatState State { get; private set; } = CombatState.Idle;
    public AttackDefinition CurrentAttack { get; private set; }
    public PlayerStamina Stamina { get; } = new();
    public WeaponResourceState Resources { get; } = new();
    public int AttackId { get; private set; }
    public int TechniqueStageIndex => _techniqueStageIndex;
    public int EffectiveTechniqueStageCount => _effectiveTechniqueStageCount;
    public bool IsDuelistOverdriveActive =>
        _weapon.Family == WeaponFamily.DualSwords &&
        _duelistOverdriveRemaining > 0f;
    public float DuelistOverdriveRemaining => _duelistOverdriveRemaining;
    public float DuelistOverdriveCooldownRemaining =>
        _duelistOverdriveCooldownRemaining;
    public int DuelistHeavenSplitterLevel => _duelistHeavenSplitterLevel;
    public bool IsDuelistUltimateReady =>
        _weapon.Family == WeaponFamily.DualSwords && Stamina.IsFull &&
        _duelistUltimateTargetAvailable;
    public bool IsDuelistUltimateActive =>
        CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.DuelistInfiniteStarburst;
    public int DuelistUltimateHitNumber => _duelistUltimateProcessedHits;
    public bool IsDuelistCelestialX => IsDuelistUltimateActive &&
        _techniqueSequenceIndex >= DuelistTuning.UltimateStrikeCount;
    public float DuelistUltimateCinematicRatio =>
        IsDuelistUltimateActive && State == CombatState.ChargingHeavy
            ? Math.Clamp(_stateElapsed / DuelistTuning.UltimateIntroSeconds,
                0f, 1f)
            : 0f;
    public float RangerChargeRatio => _rangerChargeRatio;
    public RangerDrawState RangerDrawState => _rangerDrawState;
    public SkyfallDistance RangerSkyfallDistance => _skyfallDistance;
    public float RangerUltimateFocusSnapshot =>
        _rangerUltimateFocusSnapshot;
    public float RangerUltimateFocusRatio =>
        RangerTuning.FocusRatio(_rangerUltimateFocusSnapshot);
    public float RangerLockedDirection => _rangerLockedDirection;
    public bool IsRangerPerfectDodgeActive => _rangerPerfectDodgeActive;
    public float RangerCounterWindowRemaining => _rangerCounterWindowRemaining;
    public int RangerFlowTier => RangerTuning.FocusTier(Resources.HunterFocus);
    public bool IsRangerUltimateReady =>
        _weapon.Family == WeaponFamily.HunterBow &&
        Stamina.IsFull && _rangerUltimateTargetAvailable;
    public float RangerUltimateCinematicRatio =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerPhantomHunt &&
        State == CombatState.ChargingHeavy
            ? Math.Clamp(_stateElapsed / RangerTuning.PhantomHuntIntroSeconds, 0f, 1f)
            : 0f;
    public float RangerUltimateWorldTimeScale =>
        RangerUltimateCinematicRatio > 0f
            ? RangerTuning.PhantomHuntWorldSlowScale
            : 1f;
    public int CurrentTechniqueUseId => _currentTechniqueUseId;
    public RunePlacementDistance RunePlacementDistance =>
        _runePlacementDistance;
    public int AvailableGroundRuneCount => _availableGroundRuneCount;
    public int ElderSigils => Resources.ElderSigils;
    public bool IsFourthVeilActive =>
        _weapon.Family == WeaponFamily.ArcaneWarStaff &&
        _fourthVeilTimeRemaining > 0f;
    public bool IsAstralDescending =>
        CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.AncientStaffAstralDescent;
    public float FourthVeilRatio => Math.Clamp(
        _fourthVeilTimeRemaining /
            AncientStaffTuning.FourthVeilDurationSeconds,
        0f,
        1f);
    public float FourthVeilVisualStrength => IsFourthVeilActive
        ? Math.Clamp(
            (AncientStaffTuning.FourthVeilDurationSeconds -
                _fourthVeilTimeRemaining) / .28f,
            0f,
            1f)
        : Math.Clamp(_fourthVeilExitVisualRemaining / .34f, 0f, 1f);
    public bool IsFourthVeilVisualActive =>
        IsFourthVeilActive || _fourthVeilExitVisualRemaining > 0f;
    public float AegisImpactVerticalOffset => _aegisImpactVerticalOffset;
    public float AegisWardStress => 1f - Math.Clamp(
        Stamina.Current / MathF.Max(1f, Stamina.Maximum),
        0f,
        1f);
    public bool IsAncientStaffUltimateReady =>
        _weapon.Family == WeaponFamily.ArcaneWarStaff &&
        Stamina.IsFull && _ancientStaffUltimateTargetAvailable &&
        !IsFourthVeilActive;
    public float AncientStaffUltimateCinematicRatio =>
        CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate &&
            State == CombatState.ChargingHeavy
            ? Math.Clamp(
                _stateElapsed / AncientStaffTuning.UltimateIntroSeconds,
                0f,
                1f)
            : 0f;
    public float AncientStaffUltimateWorldTimeScale =>
        AncientStaffUltimateCinematicRatio > 0f
            ? AncientStaffTuning.UltimateWorldSlowScale
            : 1f;
    public float RangerWindrunnerDirection
    {
        get
        {
            WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None;
            bool movementStage = _techniqueStageIndex == 0 ||
                effect == WeaponTechniqueEffect.RangerPhantomBarrage &&
                _techniqueStageIndex == 3;
            bool mobileEffect = effect is
                WeaponTechniqueEffect.RangerWindrunnerShot or
                WeaponTechniqueEffect.RangerPredatorsHorizon or
                WeaponTechniqueEffect.RangerEvasiveStep or
                WeaponTechniqueEffect.RangerEvasiveShot or
                WeaponTechniqueEffect.RangerTwinFang or
                WeaponTechniqueEffect.RangerPhantomRetreat or
                WeaponTechniqueEffect.RangerBackstepShot or
                WeaponTechniqueEffect.RangerSlidingShot or
                WeaponTechniqueEffect.RangerPhantomBarrage or
                WeaponTechniqueEffect.RangerPhantomHunt;
            return mobileEffect && movementStage
                ? _rangerWindrunnerDirection
                : 0f;
        }
    }
    public bool IsRangerUltimateCharging =>
        CurrentTechnique?.Effect is (WeaponTechniqueEffect.RangerHeavensFury or
            WeaponTechniqueEffect.RangerPhantomHunt) &&
        State == CombatState.ChargingHeavy;
    public bool IsRuneBrandCharging =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.SpellbladeRuneBrand &&
        State == CombatState.ChargingHeavy;
    public bool IsRangerTrajectoryAiming =>
        _weapon.Family == WeaponFamily.HunterBow &&
        CurrentTechnique != null &&
        (State == CombatState.ChargingHeavy || CurrentTechnique.Effect is
            WeaponTechniqueEffect.RangerFallingStar or
            WeaponTechniqueEffect.RangerSkyfallMarker);
    public bool IsWindrunnerEvading =>
        CurrentTechnique?.Effect is (WeaponTechniqueEffect.RangerWindrunnerShot or
            WeaponTechniqueEffect.RangerEvasiveStep or
            WeaponTechniqueEffect.RangerEvasiveShot) &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed >= .02f &&
        _stateElapsed <= RangerTuning.PerfectDodgeWindowSeconds;
    public float StateElapsed => _stateElapsed;
    public float DodgeDirection { get; private set; } = 1f;
    public WeaponTechnique CurrentTechnique { get; private set; }
    public bool IsUltimateControlImmune =>
        CurrentTechnique?.Input == WeaponTechniqueInput.Ultimate;
    public string TechniqueFeedback { get; private set; } = string.Empty;
    public bool IsTechniqueFeedbackVisible => _techniqueFeedbackRemaining > 0f;
    public bool IsBlocking => State == CombatState.Blocking ||
        IsTechniqueGuardActive;
    public bool IsBlockInputHeld { get; private set; }
    public bool IsDodging => State == CombatState.Dodging;
    public bool IsGuardBroken => State == CombatState.Staggered;
    public bool CanBlock => _weapon.Family.ToOfficialFamily() is
        WeaponFamily.LongSword or WeaponFamily.GreatSword or
        WeaponFamily.ArcaneWarStaff or WeaponFamily.DualSwords;
    public WeaponMoveSet MoveSet => _weapon.MoveSet;
    public float DodgeSpeedMultiplier => _armorItem?.MoveSpeedModifier ??
        _armor.MoveSpeedModifier;
    public bool IsBlockFeedbackActive => _blockFeedbackRemaining > 0f;
    public bool IsPerfectGuardFeedbackActive =>
        _perfectGuardFeedbackRemaining > 0f;
	private bool IsKnightSwordAndShield => string.Equals(
		_weapon.WeaponId,
		EquipmentCatalog.KnightLongSword.WeaponId,
		StringComparison.Ordinal);
    public bool IsKnightFinalOathReady =>
		IsKnightSwordAndShield && Stamina.IsFull &&
        _knightUltimateTargetAvailable;
    public bool IsKnightFinalOathActive =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFinalOath;
    public float KnightUltimateCinematicRatio =>
        IsKnightFinalOathActive && State == CombatState.ChargingHeavy
            ? Math.Clamp(_stateElapsed / KnightTuning.UltimateIntroSeconds, 0f, 1f)
            : 0f;
    public float KnightUltimateWorldTimeScale =>
        KnightUltimateCinematicRatio > 0f
            ? KnightTuning.UltimateWorldSlowScale
            : 1f;
    public float KnightLandingPulse => _knightLandingPulseRemaining;
    public int KnightLandingImpactId => _knightLandingImpactId;
    public bool IsPerfectPhantomFeedbackActive =>
        _perfectPhantomFeedbackRemaining > 0f;
    public bool IsBladeMomentumFlowing => _greatswordFlowingMomentum;
    public bool IsGreatswordWhirling =>
        CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.GreatswordWhirlingJudgment &&
        !_greatswordWhirlStopping;
    public bool IsGreatswordWhirlStopping => _greatswordWhirlStopping;
    public float GreatswordWhirlElapsed => _greatswordWhirlElapsed;
    public bool IsFinalVerdictReady =>
        _weapon.Family == WeaponFamily.GreatSword &&
        Stamina.IsFull && _greatswordUltimateTargetAvailable;
    public float GreatswordChargeRatio => _greatswordChargeRatio;
    public int GreatswordChargeLevel => _greatswordChargeLevel;
    public bool IsGreatswordCharging =>
        _weapon.Family == WeaponFamily.GreatSword &&
        State == CombatState.ChargingHeavy &&
        CurrentTechnique?.Effect is
            WeaponTechniqueEffect.GreatswordTitanReckoning or
            WeaponTechniqueEffect.GreatswordKingsRupture;
    public float GreatswordUltimateCinematicRatio =>
        CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFinalVerdict &&
            State == CombatState.ChargingHeavy
                ? Math.Clamp(
                    _stateElapsed / GreatswordTuning.FinalVerdictIntroSeconds,
                    0f,
                    1f)
                : 0f;
    public float GreatswordUltimateWorldTimeScale =>
        GreatswordUltimateCinematicRatio > 0f
            ? GreatswordTuning.FinalVerdictWorldSlowScale
            : 1f;
    public bool IsSovereignThroneActive =>
        _weapon.Family == WeaponFamily.AbyssRing &&
        _sovereignThroneTimeRemaining > 0f;
    public float SovereignThroneRatio => Math.Clamp(
        _sovereignThroneTimeRemaining /
            AbyssRingTuning.ThroneDurationSeconds, 0f, 1f);
    public Vector2 SovereignTransitDirection => _sovereignTransitDirection;
    public float SovereignChargeRatio => _sovereignChargeRatio;
    public int SovereignChargeLevel => _sovereignChargeLevel;
    public bool IsSovereignUltimateReady =>
        _weapon.Family == WeaponFamily.AbyssRing &&
        Stamina.IsFull && _sovereignUltimateTargetAvailable;
    public float SovereignUltimateCinematicRatio =>
        CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm &&
            State == CombatState.ChargingHeavy
                ? Math.Clamp(_stateElapsed /
                    AbyssRingTuning.UltimateIntroSeconds, 0f, 1f)
                : 0f;
    public float SovereignUltimateWorldTimeScale =>
        SovereignUltimateCinematicRatio > 0f
            ? AbyssRingTuning.UltimateWorldSlowScale
            : 1f;
    public bool IsSovereignRiftEvading =>
        (CurrentTechnique?.Effect is
            WeaponTechniqueEffect.SovereignOmnipresentStep or
            WeaponTechniqueEffect.SovereignHeavenReversal) &&
        _stateElapsed <= AbyssRingTuning.PerfectRiftWindowSeconds;
    public float SovereignPerfectRiftVisualRatio => Math.Clamp(
        _sovereignPerfectRiftVisualRemaining /
            AbyssRingTuning.PerfectRiftVisualSeconds,
        0f,
        1f);
    public float GreatswordLandingPulse => _greatswordLandingPulseRemaining;
    public int GreatswordLandingImpactId => _greatswordLandingImpactId;
    public bool IsGreatswordFallingBastion =>
        CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.GreatswordFallingBastion &&
        !_isGrounded;
    public bool IsKnightFallingShield => IsKnightSwordAndShield &&
        CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFallingShield &&
        !_isGrounded;
    public bool IsHitstopActive => _hitstopRemaining > 0f;
    public bool IsDodgeInvulnerable => State == CombatState.Dodging &&
        _stateElapsed >= DodgeStartupSeconds &&
        _stateElapsed < DodgeStartupSeconds + DodgeInvulnerabilitySeconds;
    public bool IsScytheShadowEvading =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.ScytheShadowSlip &&
        _stateElapsed >= ScytheTuning.ShadowSlipIframeStartSeconds &&
        _stateElapsed <= ScytheTuning.ShadowSlipIframeEndSeconds;
    public bool IsScytheDodgeWeaving =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.ScytheEclipseWaltz &&
        _techniqueStageIndex is 0 or 2 &&
        _stateElapsed <= ScytheTuning.DodgeWeaveWindowSeconds;
    public bool IsUmbralAttached => State == CombatState.UmbralAttached;
    public bool IsScytheUltimateReady =>
        _weapon.Family == WeaponFamily.Scythe && Stamina.IsFull &&
        _scytheUltimateTargetAvailable;
    public float ScytheUltimateCinematicRatio =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.ScytheUmbraDevourer &&
        State == CombatState.ChargingHeavy
            ? Math.Clamp(_stateElapsed / ScytheTuning.UltimateIntroSeconds, 0f, 1f)
            : 0f;
    public float ScytheUltimateWorldTimeScale =>
        ScytheUltimateCinematicRatio > 0f
            ? ScytheTuning.UltimateWorldSlowScale
            : 1f;
    public float ScytheMoveDirection => _scytheMoveDirection;
    public float ScythePerfectDodgeVisualRatio => Math.Clamp(
        _scythePerfectDodgeVisualRemaining / .18f, 0f, 1f);
    public Vector2 ScythePerfectDodgeSource => _scythePerfectDodgeSource;
    public bool ScytheShadowSeverEnabled => _scytheShadowSeverEnabled;
    public bool IsScytheEnhancedAttack => _scytheEnhancedThisTechnique;
    public bool IsAttackActive => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack ||
            State == CombatState.UmbralAttached) &&
        !(CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion &&
            _greatswordFallingWasAirborne) &&
        !(CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFallingShield &&
            _knightFallingWasAirborne) &&
        !(CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFinalOath &&
            _techniqueStageIndex == 7 && !_isGrounded) &&
        _stateElapsed >= CurrentAttack.StartupTime &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime;
    public bool IsAttackWindingUp => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime;
    public bool IsChargingHeavy => State == CombatState.ChargingHeavy;
    public bool HasCommittedAction =>
        State == CombatState.ChargingHeavy ||
        State == CombatState.UmbralAttached ||
        CurrentAttack != null &&
            State is CombatState.LightAttack or CombatState.HeavyAttack;
    public float HeavyChargeProgress => _weapon.Family == WeaponFamily.HunterBow
        ? CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerHeavensFury
            ? Math.Clamp(
                _stateElapsed / RangerTuning.HeavensFuryChargeSeconds,
                0f,
                1f)
            : _rangerChargeRatio
        : 0f;
    public float AttackPowerMultiplier => _attackPowerMultiplier;
    public float ProjectileSpeedMultiplier => _projectileSpeedMultiplier;
    public float AttackAdvanceSpeed => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime
            ? CurrentAttack.ForwardMovementSpeed
            : 0f;
    public bool IsTechniqueGuardActive => CurrentTechnique != null &&
        CurrentTechnique.ProvidesFrontalGuard &&
        (CurrentTechnique.Effect ==
                WeaponTechniqueEffect.GreatswordTitanReckoning &&
            State == CombatState.ChargingHeavy ||
         CurrentAttack != null &&
            (State == CombatState.LightAttack ||
                State == CombatState.HeavyAttack) &&
            _stateElapsed < CurrentAttack.StartupTime +
                CurrentAttack.ActiveTime &&
            (CurrentTechnique.Effect != WeaponTechniqueEffect.KnightFinalOath ||
                _techniqueStageIndex is 0 or 8) &&
            (CurrentTechnique.Effect !=
                    WeaponTechniqueEffect.GreatswordIronDominion ||
                _techniqueStageIndex == 0));
    public bool CanJump => State == CombatState.Idle ||
        State == CombatState.Moving ||
        State == CombatState.Jumping;
    public float MovementMultiplier
    {
        get
        {
            if (State != CombatState.LightAttack &&
                State != CombatState.HeavyAttack)
            {
                if (State == CombatState.ChargingHeavy &&
                    _weapon.Family == WeaponFamily.HunterBow)
                    return IsRangerUltimateCharging ? .06f : .22f;
                if (State == CombatState.ChargingHeavy &&
                    _weapon.Family == WeaponFamily.ArcaneWarStaff)
                    return .25f;
                if (State == CombatState.ChargingHeavy &&
                    _weapon.Family == WeaponFamily.GreatSword)
                    return CurrentTechnique?.Effect ==
                            WeaponTechniqueEffect.GreatswordFinalVerdict
                        ? 0f
                        : .08f;
                return CanJump ? 1f : 0f;
            }

            return CurrentTechnique?.Effect switch
            {
                WeaponTechniqueEffect.RangerHuntersDraw => .45f,
                WeaponTechniqueEffect.RangerMobileShot =>
                    _techniqueStageIndex >= 3 ? .72f : 1f,
                WeaponTechniqueEffect.RangerPiercingDraw => .34f,
                WeaponTechniqueEffect.RangerEvasiveStep => 0f,
                WeaponTechniqueEffect.RangerEvasiveShot => .82f,
                WeaponTechniqueEffect.RangerTwinFang => .80f,
                WeaponTechniqueEffect.RangerPhantomRetreat => .74f,
                WeaponTechniqueEffect.RangerPiercingVolley => .48f,
                WeaponTechniqueEffect.RangerBackstepShot => .82f,
                WeaponTechniqueEffect.RangerCounterShot => .78f,
                WeaponTechniqueEffect.RangerSlidingShot => .18f,
                WeaponTechniqueEffect.RangerPhantomBarrage => .64f,
                WeaponTechniqueEffect.RangerArrowStorm => .16f,
                WeaponTechniqueEffect.RangerAerialShot => 1f,
                WeaponTechniqueEffect.RangerSkyfallVolley => .88f,
                WeaponTechniqueEffect.RangerSkyfallShot => 1f,
                WeaponTechniqueEffect.RangerPhantomHunt => .08f,
                WeaponTechniqueEffect.RangerSkyfallMarker => .28f,
                WeaponTechniqueEffect.RangerPredatorsHorizon => .18f,
                WeaponTechniqueEffect.RangerHeavensFury => .05f,
                WeaponTechniqueEffect.AncientStaffArcaneBoltChain => .72f,
                WeaponTechniqueEffect.AncientStaffAegisWard => .45f,
                WeaponTechniqueEffect.AncientStaffSpiritBastion => .28f,
                WeaponTechniqueEffect.AncientStaffElderInvocation => .10f,
                WeaponTechniqueEffect.AncientStaffAstralDescent => .08f,
                WeaponTechniqueEffect.AncientStaffBlinkCast => .18f,
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate => 0f,
                WeaponTechniqueEffect.GreatswordIronResolve => .24f,
                WeaponTechniqueEffect.GreatswordTitanReckoning => .08f,
                WeaponTechniqueEffect.GreatswordIronDominion => .10f,
                WeaponTechniqueEffect.GreatswordKingsRupture => .06f,
                WeaponTechniqueEffect.GreatswordFallingBastion => .16f,
                WeaponTechniqueEffect.GreatswordVanguardAssault => .14f,
                WeaponTechniqueEffect.GreatswordWhirlingJudgment =>
                    _greatswordWhirlStopping
                        ? .12f
                        : GreatswordTuning.WhirlMovementMultiplier,
                WeaponTechniqueEffect.GreatswordFinalVerdict => 0f,
                _ => _weapon.AttackMovementMultiplier
            };
        }
    }

    public void ApplyLoadout(WeaponDefinition weapon, EquipmentItem armorItem)
    {
		WeaponDefinition requestedWeapon =
			weapon ?? EquipmentCatalog.KnightLongSword;
		WeaponDefinition nextWeapon = EquipmentCatalog.TryResolveSavedWeaponId(
				requestedWeapon.WeaponId,
				out WeaponDefinition canonicalWeapon)
			? canonicalWeapon
			: requestedWeapon;
        bool weaponChanged = !string.Equals(
                _weapon.WeaponId,
                nextWeapon.WeaponId,
                StringComparison.Ordinal) ||
            _weapon.WeaponType != nextWeapon.WeaponType;
        _weapon = nextWeapon;
        _armorItem = armorItem ?? EquipmentCatalog.CreateStartingArmor();
        _armor = _armorItem.ArmorDefinition ?? EquipmentCatalog.KnightArmor;
        Resources.SetFamily(_weapon.Family);
        _inputBuffer.Clear();
        _dodgeBuffered = false;

        if (weaponChanged)
        {
            ClearActionState();
            if (State != CombatState.Dead)
                StartTimedState(CombatState.Idle);
            Resources.Reset();
            _resourceGainAttackId = -1;
            _duelistTechniqueGainAwarded = false;
            _duelistRepositionAttackId = -1;
            _perfectPhantomFeedbackRemaining = 0f;
            ResetDuelistTemporaryState();
			ResetKnightTemporaryState();
            _windrunnerPerfectRewardRemaining = 0f;
            _rangerFocusHitAttackId = -1;
            _rangerEvadeCooldownRemaining = 0f;
            _rangerFlowGainThisTechnique = 0f;
            _sovereignThroneTimeRemaining = 0f;
            _sovereignPerfectRiftCounterPending = false;
            _fourthVeilTimeRemaining = 0f;
            _fourthVeilExitVisualRemaining = 0f;
            ClearAncientStaffPendingInput();
        }

        if (!_weapon.UsesShield && State == CombatState.Blocking)
            StartTimedState(CombatState.Idle);
    }

    public void Update(
        GameTime gameTime,
        CombatInput input,
        bool isGrounded,
        FacingDirection facing,
        float verticalVelocity = 0f)
    {
        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 20f);
        _currentFacing = facing;
        _currentHorizontalDirection = input.HorizontalDirection;
        _isGrounded = isGrounded;
        _verticalVelocity = verticalVelocity;

        UpdateGreatswordInputState(elapsedSeconds, input);
        UpdateRangerInputState(elapsedSeconds, input);
        UpdateSovereignInputState(elapsedSeconds, input);
        UpdateAncientStaffInputState(elapsedSeconds, input);
        UpdateScytheInputState(elapsedSeconds, input);
        UpdateDuelistInputState(elapsedSeconds, input);
        _sovereignStepCooldownRemaining = MathF.Max(
            0f, _sovereignStepCooldownRemaining - elapsedSeconds);
        _sovereignThroneTimeRemaining = MathF.Max(
            0f, _sovereignThroneTimeRemaining - elapsedSeconds);
        bool fourthVeilWasActive = IsFourthVeilActive;
        _fourthVeilExitVisualRemaining = MathF.Max(
            0f, _fourthVeilExitVisualRemaining - elapsedSeconds);
        _fourthVeilTimeRemaining = MathF.Max(
            0f, _fourthVeilTimeRemaining - elapsedSeconds);
        if (fourthVeilWasActive && _fourthVeilTimeRemaining <= 0f)
            _fourthVeilExitVisualRemaining = .34f;

        if (_weapon.Family == WeaponFamily.ArcaneWarStaff &&
            IsFourthVeilActive && input.DashHeld && input.Skill2Pressed)
        {
            _fourthVeilTimeRemaining = 0f;
            _fourthVeilExitVisualRemaining = .34f;
            ClearActionState();
            StartTimedState(isGrounded
                ? CombatState.Idle
                : CombatState.Jumping);
            SetFeedback("FOURTH VEIL RELEASED");
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }

        if (_weapon.Family == WeaponFamily.GreatSword &&
            CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion &&
            _verticalVelocity > 40f)
        {
            _greatswordFallingPoiseBonus = true;
        }

        if (_weapon.Family == WeaponFamily.HunterBow &&
            CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerPiercingDraw &&
            State == CombatState.ChargingHeavy &&
            (input.Skill2Pressed || input.DashHeld))
        {
            ClearActionState();
            StartTimedState(isGrounded ? CombatState.Idle : CombatState.Jumping);
            SetFeedback("PIERCING DRAW CANCELLED");
            return;
        }

        if (_weapon.Family == WeaponFamily.HunterBow &&
            CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerArrowStorm &&
            CurrentAttack != null)
        {
            bool beforeRelease = _stateElapsed < CurrentAttack.StartupTime;
            if (beforeRelease && (input.Skill2Pressed || input.DashHeld))
            {
                ClearActionState();
                StartTimedState(isGrounded ? CombatState.Idle : CombatState.Jumping);
                SetFeedback("ARROW STORM CANCELLED");
                return;
            }

            if (!beforeRelease && MathF.Abs(input.HorizontalDirection) > .1f)
            {
                ClearActionState();
                StartTimedState(isGrounded ? CombatState.Moving : CombatState.Jumping);
                return;
            }
        }

        if (_weapon.Family == WeaponFamily.GreatSword &&
            CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion &&
            _greatswordFallingWasAirborne && isGrounded)
        {
            _greatswordFallingWasAirborne = false;
            _greatswordLandingPulseRemaining = .32f;
            _greatswordLandingImpactId++;
            _hitstopRemaining = MathF.Max(_hitstopRemaining, .045f);
            StartAttack(CurrentAttack, CombatState.HeavyAttack);
        }

		if (IsKnightSwordAndShield &&
            CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFallingShield &&
            _knightFallingWasAirborne && isGrounded)
        {
            _knightFallingWasAirborne = false;
            _knightLandingPulseRemaining = .30f;
            _knightLandingImpactId++;
            _hitstopRemaining = MathF.Max(_hitstopRemaining, .045f);
            StartAttack(CurrentAttack, CombatState.HeavyAttack);
        }

        if (IsKnightFinalOathActive && _techniqueStageIndex == 7)
        {
            if (!isGrounded)
            {
                _knightFinalLandingWasAirborne = true;
            }
            else if (_knightFinalLandingWasAirborne)
            {
                _knightFinalLandingWasAirborne = false;
                _knightLandingPulseRemaining = .34f;
                _knightLandingImpactId++;
                _hitstopRemaining = MathF.Max(_hitstopRemaining, .05f);
                StartAttack(CurrentAttack, CombatState.LightAttack);
            }
        }

        if (input.MovementCancelPressed &&
            MathF.Abs(input.HorizontalDirection) > .1f &&
            HasCommittedAction &&
            !IsRangerMovementCompatibleAction() &&
            (_weapon.Family != WeaponFamily.GreatSword ||
                IsGreatswordWhirling))
        {
            if (IsGreatswordWhirling)
            {
                BeginGreatswordWhirlStop();
                return;
            }
            ClearActionState();
            StartTimedState(isGrounded
                ? CombatState.Moving
                : CombatState.Jumping);
            return;
        }

        UpdateSpellbladeInputState(elapsedSeconds, input);

        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffAegisWard &&
            (input.Skill2Released || !_ancientStaffSkill2CurrentlyHeld))
        {
            ClearActionState();
            StartTimedState(isGrounded
                ? CombatState.Idle
                : CombatState.Jumping);
        }

        if (_weapon.Family == WeaponFamily.GreatSword &&
            CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordIronResolve)
        {
            if ((input.Skill2Released || !input.Skill2Held) &&
                _stateElapsed >= GreatswordTuning.SteelDeflectWindowSeconds)
            {
                float counterRemaining = _greatswordCounterWindowRemaining;
                ClearActionState();
                _greatswordCounterWindowRemaining = counterRemaining;
                StartTimedState(isGrounded
                    ? CombatState.Idle
                    : CombatState.Jumping);
                return;
            }
        }

        if (IsGreatswordWhirling)
        {
            _greatswordWhirlElapsed += elapsedSeconds;
            if (input.Skill3Released || !_greatswordSkill3CurrentlyHeld)
                _greatswordWhirlEndRequested = true;
            if ((_greatswordWhirlEndRequested ||
                    _greatswordWhirlElapsed >=
                        GreatswordTuning.WhirlMaximumSeconds) &&
                _greatswordWhirlElapsed >=
                    GreatswordTuning.WhirlMinimumCommitmentSeconds)
            {
                BeginGreatswordWhirlEndingSweep();
            }
        }

        if (_weapon.Family == WeaponFamily.GreatSword &&
            State == CombatState.ChargingHeavy)
        {
            _stateElapsed += elapsedSeconds;
            UpdateGreatswordChargeState(input);
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }

		if (IsKnightSwordAndShield &&
            State == CombatState.ChargingHeavy && IsKnightFinalOathActive)
        {
            _stateElapsed += elapsedSeconds;
            if (_stateElapsed >= KnightTuning.UltimateIntroSeconds)
            {
                SetFeedback("I WILL NOT FALL.");
                StartTechniqueStage();
            }
            Stamina.Update(elapsedSeconds, regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }

        if (_hitstopRemaining > 0f)
        {
			if (IsKnightSwordAndShield &&
                _perfectGuardRewardRemaining > 0f && input.Skill1Pressed)
                _knightRiposteBuffered = true;
            _hitstopRemaining = MathF.Max(0f, _hitstopRemaining - elapsedSeconds);
            if (_hitstopRemaining <= 0f && _knightRiposteBuffered)
            {
                _knightRiposteBuffered = false;
                CurrentAttack = null;
                CurrentTechnique = null;
                _techniqueStageIndex = 0;
                _effectiveTechniqueStageCount = 0;
                _techniqueSequenceIndex = 0;
                StartTimedState(CombatState.Idle);
                TryStartTechnique(WeaponTechniqueInput.Skill1);
            }
            return;
        }

        IsBlockInputHeld = input.BlockHeld;
        float actionClockMultiplier = State == CombatState.LightAttack ||
            State == CombatState.HeavyAttack
                ? _weapon.AttackSpeedModifier * GetMomentumSpeedMultiplier() *
                    (_weapon.Family == WeaponFamily.HunterBow
                        ? RangerTuning.FlowSpeedMultiplier(Resources.HunterFocus)
                        : 1f)
                : 1f;
        _stateElapsed += elapsedSeconds * actionClockMultiplier;
        _blockFeedbackRemaining = MathF.Max(
            0f,
            _blockFeedbackRemaining - elapsedSeconds);
        _techniqueFeedbackRemaining = MathF.Max(
            0f,
            _techniqueFeedbackRemaining - elapsedSeconds);
        _sovereignPerfectRiftVisualRemaining = MathF.Max(
            0f,
            _sovereignPerfectRiftVisualRemaining - elapsedSeconds);
        _perfectGuardFeedbackRemaining = MathF.Max(
            0f,
            _perfectGuardFeedbackRemaining - elapsedSeconds);
        _perfectPhantomFeedbackRemaining = MathF.Max(
            0f,
            _perfectPhantomFeedbackRemaining - elapsedSeconds);
        _perfectGuardRewardRemaining = MathF.Max(
            0f,
            _perfectGuardRewardRemaining - elapsedSeconds);
        _windrunnerPerfectRewardRemaining = MathF.Max(
            0f,
            _windrunnerPerfectRewardRemaining - elapsedSeconds);
        _greatswordCounterWindowRemaining = MathF.Max(
            0f,
            _greatswordCounterWindowRemaining - elapsedSeconds);
        _greatswordLandingPulseRemaining = MathF.Max(
            0f,
            _greatswordLandingPulseRemaining - elapsedSeconds);
        _knightLandingPulseRemaining = MathF.Max(
            0f, _knightLandingPulseRemaining - elapsedSeconds);
        _scytheWraithCutWindowRemaining = MathF.Max(
            0f, _scytheWraithCutWindowRemaining - elapsedSeconds);
        _scythePerfectDodgeVisualRemaining = MathF.Max(
            0f, _scythePerfectDodgeVisualRemaining - elapsedSeconds);
        _scytheDodgeWeaveRewardRemaining = MathF.Max(
            0f, _scytheDodgeWeaveRewardRemaining - elapsedSeconds);
        _duelistRiposteWindowRemaining = MathF.Max(
            0f, _duelistRiposteWindowRemaining - elapsedSeconds);
        _duelistOverdriveRemaining = MathF.Max(
            0f, _duelistOverdriveRemaining - elapsedSeconds);
        _duelistOverdriveCooldownRemaining = MathF.Max(
            0f, _duelistOverdriveCooldownRemaining - elapsedSeconds);
        if (_scytheDodgeWeaveRewardRemaining <= 0f)
            _scytheEnhancedAttackPending = false;
        Resources.Update(elapsedSeconds);

        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.HunterBow)
        {
            UpdateRangerChargeState(elapsedSeconds, input);
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }
        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.ArcaneWarStaff)
        {
            if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate)
            {
                if (_stateElapsed >= AncientStaffTuning.UltimateIntroSeconds)
                {
                    SetFeedback("RISE... AND BE JUDGED!");
                    StartTechniqueStage();
                }
            }
            else
            {
                UpdateSpellbladeChargeState(input);
            }
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }
        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.AbyssRing)
        {
            UpdateSovereignChargeState();
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }
        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.Scythe)
        {
            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ScytheUmbraDevourer &&
                _stateElapsed >= ScytheTuning.UltimateIntroSeconds)
            {
                SetFeedback("YOUR SHADOW IS MINE.");
                StartTechniqueStage();
            }
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }
        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.DualSwords)
        {
            if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.DuelistInfiniteStarburst)
            {
                if (_stateElapsed >= DuelistTuning.UltimateIntroSeconds)
                {
                    SetFeedback("INFINITE STARBURST ASCENSION");
                    StartTechniqueStage();
                }
            }
            else if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.DuelistHeavenSplitter)
            {
                _duelistHeavenSplitterHoldSeconds = MathF.Min(
                    DuelistTuning.HeavenSplitterLevelThreeSeconds,
                    _duelistHeavenSplitterHoldSeconds + elapsedSeconds);
                if ((!_duelistSkill1Held && !_duelistSkill3Held) ||
                    _duelistHeavenSplitterHoldSeconds >=
                        DuelistTuning.HeavenSplitterLevelThreeSeconds)
                {
                    ReleaseDuelistHeavenSplitter();
                }
            }
            Stamina.Update(elapsedSeconds, regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }

        if (CurrentTechnique?.RequiresRepeatedInput == true &&
            input.Skill1Pressed &&
            _techniqueSequenceIndex + 1 < _effectiveTechniqueStageCount &&
            CurrentAttack != null &&
            _stateElapsed >= CurrentAttack.StartupTime * .5f)
        {
            _techniqueStageBuffered = true;
            if (CurrentTechnique.Effect ==
                WeaponTechniqueEffect.GreatswordCombo)
            {
                float flowStart = MathF.Max(
                    CurrentAttack.StartupTime * .65f,
                    CurrentAttack.TotalTime -
                        GreatswordTuning.ComboWindowSeconds);
                float flowEnd = CurrentAttack.TotalTime - .04f;
                _greatswordFlowingMomentum =
                    _stateElapsed >= flowStart &&
                    _stateElapsed <= flowEnd;
            }
        }

        if (CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightShieldGuard &&
            _perfectGuardRewardRemaining > 0f &&
            (input.Skill1Pressed || input.Skill2Pressed || input.Skill3Pressed))
        {
            CurrentAttack = null;
            CurrentTechnique = null;
            _techniqueStageIndex = 0;
            StartTimedState(CombatState.Idle);
        }

        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.AncientStaffAegisWard &&
            CurrentAttack != null && _ancientStaffSkill2CurrentlyHeld)
        {
            float heldWardTime = CurrentAttack.StartupTime +
                CurrentAttack.ActiveTime - .001f;
            _stateElapsed = MathF.Min(_stateElapsed, heldWardTime);
        }

        if (CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightShieldGuard &&
            CurrentAttack != null && input.Skill2Held)
        {
            float heldGuardTime = CurrentAttack.StartupTime +
                CurrentAttack.ActiveTime - .001f;
            _stateElapsed = MathF.Min(_stateElapsed, heldGuardTime);
        }

        CompleteExpiredAction(isGrounded);
        bool canStartAction = CanStartAction();
        bool dashSkillPressed = IsDashSkillPressed(input);
        WeaponTechniqueInput? bufferedInput = null;
        bool techniqueStarted = false;
        bool dodgeReady = false;

        if (canStartAction && input.DodgePressed)
        {
            _dodgeBuffered = true;
            _dodgeBufferRemaining = .12f;
        }

        bool immediatePhantomStep = canStartAction && isGrounded &&
            _weapon.Family == WeaponFamily.DualSwords &&
            input.PerfectEvadeOpportunity &&
            input.Skill2Pressed && !input.Skill1Pressed &&
            !input.Skill3Pressed;

        if (canStartAction && _weapon.Family == WeaponFamily.DualSwords)
        {
            _inputBuffer.Clear();
            _dodgeBuffered = false;
            techniqueStarted = ResolveDuelistActionInput();
        }
		else if (canStartAction && IsKnightSwordAndShield)
        {
            _inputBuffer.Clear();
            if (input.Skill1Pressed && input.DashHeld)
                _knightDashSkill1Pending = true;
            if (input.Skill2Pressed && !isGrounded)
                _knightAirSkill2Pending = true;

            WeaponTechniqueInput? knightInput = _knightInputBuffer.Update(
                elapsedSeconds,
                input.Skill1Pressed,
                input.Skill2Pressed,
                input.Skill3Pressed);
            if (_knightDashSkill1Pending &&
                _knightInputBuffer.HasPendingInput)
            {
                // Shift is both the shared dash modifier and the dodge key.
                // Let the Knight chord resolve before the generic dodge buffer.
                _dodgeBuffered = false;
            }
            if (knightInput.HasValue)
            {
                WeaponTechniqueInput resolved = knightInput.Value;
                bool dashSkill1 = _knightDashSkill1Pending;
                bool airSkill2 = _knightAirSkill2Pending;
                _knightDashSkill1Pending = false;
                _knightAirSkill2Pending = false;
                if (resolved == WeaponTechniqueInput.Skill1 && dashSkill1 &&
                    isGrounded)
                    resolved = WeaponTechniqueInput.DashSkill;
                _knightForceFallingShield =
                    resolved == WeaponTechniqueInput.Skill2 && airSkill2;
                techniqueStarted = TryStartTechnique(resolved);
                _knightForceFallingShield = false;
            }
        }
        else if (immediatePhantomStep)
        {
            _inputBuffer.Clear();
            _dodgeBuffered = false;
            techniqueStarted = TryStartTechnique(
                WeaponTechniqueInput.Skill2);
        }
        else if (canStartAction &&
            _weapon.Family == WeaponFamily.ArcaneWarStaff)
        {
            _inputBuffer.Clear();
            techniqueStarted = ResolveAncientStaffActionInput(input);
        }
        else if (canStartAction && _weapon.Family == WeaponFamily.Scythe)
        {
            _inputBuffer.Clear();
            techniqueStarted = ResolveScytheActionInput();
        }
        else if (canStartAction && input.DashHeld && dashSkillPressed && isGrounded)
        {
            _inputBuffer.Clear();
            if (_weapon.Family == WeaponFamily.HunterBow)
                ClearRangerPendingInput();
            if (_weapon.Family == WeaponFamily.GreatSword)
                ClearGreatswordPendingInput();
            if (_weapon.Family == WeaponFamily.AbyssRing)
                ClearSovereignPendingInput();
            _dodgeBuffered = false;
            techniqueStarted = TryStartTechnique(WeaponTechniqueInput.DashSkill);
            if (techniqueStarted && CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.RangerWindrunnerShot)
            {
                _rangerWindrunnerDirection = facing == FacingDirection.Left
                    ? 1f
                    : -1f;
            }
        }
        else if (canStartAction &&
            _weapon.Family == WeaponFamily.GreatSword)
        {
            _inputBuffer.Clear();
            techniqueStarted = ResolveGreatswordActionInput();
        }
        else if (canStartAction &&
            _weapon.Family == WeaponFamily.HunterBow)
        {
            _inputBuffer.Clear();
            techniqueStarted = ResolveRangerActionInput(
                elapsedSeconds,
                input);
        }
        else if (canStartAction &&
            _weapon.Family == WeaponFamily.AbyssRing)
        {
            _inputBuffer.Clear();
            techniqueStarted = ResolveSovereignActionInput();
        }
        else if (canStartAction)
        {
            bufferedInput = _inputBuffer.Update(
                elapsedSeconds,
                input.Skill1Pressed,
                input.Skill2Pressed,
                input.Skill3Pressed);
        }

        if (_dodgeBuffered && !techniqueStarted)
        {
            _dodgeBufferRemaining -= elapsedSeconds;
            if (_dodgeBufferRemaining <= 0f)
            {
                _dodgeBuffered = false;
                dodgeReady = true;
            }
        }

        if (canStartAction && bufferedInput.HasValue)
        {
            techniqueStarted = TryStartTechnique(bufferedInput.Value);
        }
        else if (!techniqueStarted && canStartAction && dodgeReady &&
            isGrounded &&
            Stamina.TrySpend(DodgeStaminaCost * _armorItem.DodgeCostModifier))
        {
            _inputBuffer.Clear();
            DodgeDirection = MathF.Abs(input.HorizontalDirection) > 0.1f
                ? MathF.Sign(input.HorizontalDirection)
                : facing == FacingDirection.Left ? -1f : 1f;
            StartTimedState(CombatState.Dodging);
        }
        else if (!techniqueStarted && canStartAction && input.BlockHeld &&
            isGrounded && CanBlock && _weapon.UsesShield)
        {
            StartTimedState(CombatState.Blocking);
        }
        else if (State == CombatState.Blocking && !input.BlockHeld)
        {
            StartTimedState(isGrounded ? CombatState.Idle : CombatState.Jumping);
        }

        bool regenerationAllowed = State != CombatState.Dodging &&
            State != CombatState.HeavyAttack &&
            State != CombatState.LightAttack &&
            State != CombatState.Blocking &&
            !IsFourthVeilActive;
        Stamina.Update(
            elapsedSeconds,
            regenerationAllowed,
            _armorItem.StaminaRegenModifier * ExternalStaminaRegenMultiplier);
    }

    public void SetLocomotion(bool isGrounded, bool isMoving)
    {
        if (State != CombatState.Idle &&
            State != CombatState.Moving &&
            State != CombatState.Jumping)
        {
            return;
        }

        State = !isGrounded
            ? CombatState.Jumping
            : isMoving ? CombatState.Moving : CombatState.Idle;
    }

    public void SetSpellbladeRuneCount(int count)
    {
        _availableGroundRuneCount = Math.Clamp(
            count,
            0,
            SpellbladeTuning.MaximumGroundRunes);
    }

    public void SetAncientStaffUltimateTargetAvailable(bool available)
    {
        _ancientStaffUltimateTargetAvailable = available;
    }

    public void RequestAncientStaffUltimateReturn()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate ||
            _techniqueSequenceIndex >= CurrentTechnique.StageCount - 1)
            return;
        _techniqueSequenceIndex = CurrentTechnique.StageCount - 1;
        StartTechniqueStage();
    }

    public void SetRangerUltimateTargetAvailable(bool available)
    {
        _rangerUltimateTargetAvailable =
            _weapon.Family == WeaponFamily.HunterBow && available;
    }

    public void SetSovereignUltimateTargetAvailable(bool available)
    {
        _sovereignUltimateTargetAvailable =
            _weapon.Family == WeaponFamily.AbyssRing && available;
    }

    public void RequestSovereignSafeExit()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.SovereignGoldenCataclysm)
            return;
        _techniqueSequenceIndex = Math.Max(
            0, _effectiveTechniqueStageCount - 1);
        _hitstopRemaining = 0f;
        StartTechniqueStage();
        SetFeedback("SAFE EXIT");
    }

    public void CancelSovereignTargetedSequence()
    {
        WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (effect is not (WeaponTechniqueEffect.SovereignParadoxImpact or
            WeaponTechniqueEffect.SovereignInfiniteRiftTheatre or
            WeaponTechniqueEffect.SovereignZeroDistanceStrike))
        {
            return;
        }

        ClearActionState();
        StartTimedState(_isGrounded
            ? CombatState.Idle
            : CombatState.Jumping);
        SetFeedback("RIFT TARGET LOST");
    }

    public bool TryConsumePerfectRiftCounter(out Vector2 source)
    {
        source = _sovereignPerfectRiftSource;
        if (!_sovereignPerfectRiftCounterPending)
            return false;
        _sovereignPerfectRiftCounterPending = false;
        return true;
    }

    public void RequestRangerUltimateLanding()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.RangerPhantomHunt)
            return;

        _techniqueSequenceIndex = Math.Max(
            0,
            _effectiveTechniqueStageCount - 1);
        _hitstopRemaining = 0f;
        StartTechniqueStage();
        SetFeedback("PHANTOM LANDING");
    }

    public void SetGreatswordUltimateTargetAvailable(bool available)
    {
        _greatswordUltimateTargetAvailable =
            _weapon.Family == WeaponFamily.GreatSword && available;
    }

    public void SetKnightUltimateTargetAvailable(bool available)
    {
        _knightUltimateTargetAvailable =
			IsKnightSwordAndShield && available;
    }

    public void SetScytheUltimateTargetAvailable(bool available)
    {
        _scytheUltimateTargetAvailable =
            _weapon.Family == WeaponFamily.Scythe && available;
    }

    public void SetDuelistUltimateTargetAvailable(bool available)
    {
        _duelistUltimateTargetAvailable =
            _weapon.Family == WeaponFamily.DualSwords && available;
    }

    public void CancelDuelistUltimateForLostTarget()
    {
        if (!IsDuelistUltimateActive)
            return;
        ClearActionState();
        StartTimedState(_isGrounded ? CombatState.Idle : CombatState.Jumping);
        SetFeedback("ASTRAL TARGET LOST");
    }

    public void RequestScytheSafeReturn()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ScytheUmbraDevourer)
            return;
        _techniqueSequenceIndex = Math.Max(0, _effectiveTechniqueStageCount - 1);
        _hitstopRemaining = 0f;
        StartTechniqueStage();
        SetFeedback("SAFE SHADOW RETURN");
    }

    public void EnableScytheShadowSever()
    {
        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ScytheAbyssalRequiem)
            _scytheShadowSeverEnabled = true;
    }

    public void RequestGreatswordUltimateLanding()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.GreatswordFinalVerdict)
            return;

        _techniqueSequenceIndex = Math.Max(
            0,
            _effectiveTechniqueStageCount - 1);
        _hitstopRemaining = 0f;
        StartTechniqueStage();
        SetFeedback("SAFE LANDING");
    }

    public void RequestKnightUltimateLanding()
    {
        if (!IsKnightFinalOathActive)
            return;
        _techniqueSequenceIndex = Math.Max(0, _effectiveTechniqueStageCount - 1);
        _hitstopRemaining = 0f;
        StartTechniqueStage();
        SetFeedback("DEFENSIVE LANDING");
    }

    public AttackResolution ResolveIncomingAttack(
        AttackContact contact,
        Vector2 playerPosition,
        FacingDirection facing,
        bool isProjectile = false)
    {
        if (IsFourthVeilActive)
            return AttackResolution.Ignored;

        if (IsSovereignRiftEvading && !contact.Unblockable)
        {
            _sovereignPerfectRiftSource = contact.SourcePosition;
            _sovereignPerfectRiftCounterPending = true;
            _sovereignPerfectRiftVisualRemaining =
                AbyssRingTuning.PerfectRiftVisualSeconds;
            _hitstopRemaining = MathF.Max(_hitstopRemaining, .035f);
            SetFeedback("PERFECT RIFT");
            return AttackResolution.Ignored;
        }

        if ((IsScytheShadowEvading || IsScytheDodgeWeaving) &&
            !contact.Unblockable)
        {
            if (IsScytheShadowEvading)
            {
                _scytheWraithCutWindowRemaining =
                    ScytheTuning.WraithCutWindowSeconds;
                _scythePerfectDodgeVisualRemaining = .18f;
                _scythePerfectDodgeSource = contact.SourcePosition;
                SetFeedback("PERFECT DODGE â€” WRAITH CUT READY");
            }
            else
            {
                _scytheEnhancedAttackPending = true;
                _scytheDodgeWeaveRewardRemaining =
                    ScytheTuning.DodgeWeaveFollowupSeconds;
                SetFeedback("DODGE WEAVE");
            }
            _hitstopRemaining = MathF.Max(_hitstopRemaining, .03f);
            return AttackResolution.Ignored;
        }

        if (IsWindrunnerEvading)
        {
            _windrunnerPerfectRewardRemaining = .75f;
            if (_weapon.Family == WeaponFamily.HunterBow &&
                CurrentTechnique?.Effect is
                    WeaponTechniqueEffect.RangerEvasiveStep or
                    WeaponTechniqueEffect.RangerEvasiveShot)
            {
                _rangerPerfectDodgeActive = true;
                Resources.AddHunterFocus(RangerTuning.PerfectEvadeBonus);
                SetFeedback("PERFECT EVADE");
                _hitstopRemaining = MathF.Max(_hitstopRemaining, .035f);
            }
            return AttackResolution.Ignored;
        }

        if (IsDodgeInvulnerable)
            return AttackResolution.Ignored;

        float horizontalOffset = contact.SourcePosition.X - playerPosition.X;
        bool fromFront = MathF.Abs(horizontalOffset) <= 2f ||
            (horizontalOffset < 0f && facing == FacingDirection.Left) ||
            (horizontalOffset > 0f && facing == FacingDirection.Right);

        bool touchesDefenseZone = contact.HitArea.Width > 0 &&
            contact.HitArea.Height > 0 &&
            contact.HitArea.Intersects(
                CreateDefenseBounds(playerPosition, facing));
        bool touchesBody = contact.HitArea.Width <= 0 ||
            contact.HitArea.Height <= 0 ||
            contact.HitArea.Intersects(CreateBodyBounds(playerPosition));

        if (fromFront && touchesDefenseZone && contact.Blockable &&
            !contact.Unblockable && IsBlocking && CanBlock)
        {
            WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
                WeaponTechniqueEffect.None;

            if (_weapon.Family == WeaponFamily.DualSwords &&
                effect is (WeaponTechniqueEffect.DuelistPhantomParry or
                    WeaponTechniqueEffect.DuelistCrossRequiem) &&
                IsTechniqueGuardActive)
            {
                float guardCost = MathF.Max(3f,
                    contact.Damage * .32f) * _armorItem.GuardCostModifier;
                bool perfectParry = effect ==
                        WeaponTechniqueEffect.DuelistPhantomParry &&
                    _stateElapsed <=
                        DuelistTuning.PerfectParryWindowSeconds;
                if (Stamina.Current + .001f < guardCost)
                {
                    if (IsUltimateControlImmune)
                        return AttackResolution.Damaged;
                    Stamina.SpendUpTo(Stamina.Current);
                    CurrentAttack = null;
                    CurrentTechnique = null;
                    StartTimedState(CombatState.Staggered);
                    return AttackResolution.GuardBroken;
                }

                Stamina.SpendUpTo(perfectParry
                    ? MathF.Max(2f, guardCost * .35f)
                    : guardCost);
                _blockFeedbackRemaining = FeedbackSeconds;
                if (perfectParry)
                {
                    Resources.AddDuelistMomentum(
                        DuelistTuning.PerfectParryGain);
                    _duelistRiposteWindowRemaining =
                        DuelistTuning.RiposteWindowSeconds;
                    _perfectPhantomFeedbackRemaining = .45f;
                    _hitstopRemaining = PerfectGuardHitstopSeconds;
                    SetFeedback("PHANTOM PARRY â€” RIPOSTE READY");
                    return AttackResolution.PerfectGuard;
                }
                return AttackResolution.BladeGuarded;
            }

            if (_weapon.Family == WeaponFamily.ArcaneWarStaff &&
                effect is WeaponTechniqueEffect.AncientStaffAegisWard or
                    WeaponTechniqueEffect.AncientStaffSpiritBastion &&
                IsTechniqueGuardActive)
            {
                _aegisImpactVerticalOffset = Math.Clamp(
                    contact.HitArea.Center.Y - playerPosition.Y,
                    -31f,
                    31f);
                float guardCost = MathF.Max(
                    BlockMinimumStaminaCost,
                    contact.Damage * BlockStaminaPerDamage) *
                    AncientStaffTuning.AegisPressureMultiplier *
                    _armorItem.GuardCostModifier;
                bool arcaneDeflect = effect ==
                        WeaponTechniqueEffect.AncientStaffAegisWard &&
                    _stateElapsed <=
                        AncientStaffTuning.AegisDeflectWindowSeconds;
                if (Stamina.Current + .001f < guardCost)
                {
                    if (IsUltimateControlImmune)
                        return AttackResolution.Damaged;
                    Stamina.SpendUpTo(Stamina.Current);
                    CurrentAttack = null;
                    CurrentTechnique = null;
                    StartTimedState(CombatState.Staggered);
                    return AttackResolution.GuardBroken;
                }

                Stamina.SpendUpTo(arcaneDeflect
                    ? MathF.Max(2f, guardCost * .25f)
                    : guardCost);
                _blockFeedbackRemaining = FeedbackSeconds;
                if (arcaneDeflect)
                {
                    Resources.AddElderSigil();
                    _perfectGuardFeedbackRemaining = .32f;
                    _perfectGuardRewardRemaining = .75f;
                    _hitstopRemaining = PerfectGuardHitstopSeconds;
                    SetFeedback("ARCANE DEFLECT");
                    return AttackResolution.PerfectGuard;
                }

                return AttackResolution.BladeGuarded;
            }

            if (_weapon.Family == WeaponFamily.GreatSword &&
                effect is (WeaponTechniqueEffect.GreatswordIronResolve or
                    WeaponTechniqueEffect.GreatswordTitanReckoning or
                    WeaponTechniqueEffect.GreatswordIronDominion) &&
                IsTechniqueGuardActive)
            {
                float guardCost = MathF.Max(
                    BlockMinimumStaminaCost,
                    contact.Damage * BlockStaminaPerDamage) *
                    GreatswordTuning.IronResolveGuardPressureMultiplier *
                    _armorItem.GuardCostModifier;
                bool steelDeflect = effect ==
                        WeaponTechniqueEffect.GreatswordIronResolve &&
                    !isProjectile &&
                    _stateElapsed <=
                        GreatswordTuning.SteelDeflectWindowSeconds;

                if (Stamina.Current + .001f < guardCost)
                {
                    if (IsUltimateControlImmune)
                        return AttackResolution.Damaged;
                    Stamina.SpendUpTo(Stamina.Current);
                    CurrentAttack = null;
                    CurrentTechnique = null;
                    StartTimedState(CombatState.Staggered);
                    return AttackResolution.GuardBroken;
                }

                Stamina.SpendUpTo(steelDeflect
                    ? MathF.Max(3f, guardCost * .35f)
                    : guardCost);
                _blockFeedbackRemaining = FeedbackSeconds;

                if (steelDeflect)
                {
                    _greatswordCounterWindowRemaining =
                        GreatswordTuning.SteelDeflectCounterWindowSeconds;
                    _perfectGuardFeedbackRemaining = .28f;
                    _hitstopRemaining = PerfectGuardHitstopSeconds;
                    SetFeedback("STEEL DEFLECT");
                    return AttackResolution.PerfectGuard;
                }

                if (effect ==
                    WeaponTechniqueEffect.GreatswordTitanReckoning)
                {
                    _greatswordTitanBraced = true;
                    _greatswordBraceProtectedHit = true;
                    SetFeedback("POWER TRANSFER");
                }

                return AttackResolution.BladeGuarded;
            }

            if (IsTechniqueGuardActive)
            {
                float normalCost = MathF.Max(
                    BlockMinimumStaminaCost,
                    contact.Damage * BlockStaminaPerDamage) *
                    _armorItem.GuardCostModifier;
                if (effect == WeaponTechniqueEffect.KnightShieldWall)
                    normalCost *= KnightTuning.ShieldWallGuardPressureMultiplier;
                bool canPerfectGuard = effect ==
                    WeaponTechniqueEffect.KnightShieldGuard;
                bool perfectGuard = canPerfectGuard &&
                    _stateElapsed <= KnightTuning.PerfectGuardWindowSeconds;
                float techniqueGuardCost = perfectGuard
                    ? MathF.Max(2f, normalCost * .25f)
                    : normalCost;

                if (Stamina.Current + .001f < techniqueGuardCost)
                {
                    if (IsUltimateControlImmune)
                        return AttackResolution.Damaged;
                    Stamina.SpendUpTo(Stamina.Current);
                    CurrentAttack = null;
                    CurrentTechnique = null;
                    StartTimedState(CombatState.Staggered);
                    return AttackResolution.GuardBroken;
                }

                Stamina.SpendUpTo(techniqueGuardCost);
                _blockFeedbackRemaining = FeedbackSeconds;

                if (perfectGuard)
                {
                    _perfectGuardFeedbackRemaining = .32f;
                    _perfectGuardRewardRemaining = KnightTuning.RiposteWindowSeconds;
                    _hitstopRemaining = PerfectGuardHitstopSeconds;

                    SetFeedback("PERFECT GUARD — RIPOSTE READY");
                    return AttackResolution.PerfectGuard;
                }

                return AttackResolution.Blocked;
            }

            float staminaCost = MathF.Max(
                BlockMinimumStaminaCost,
                contact.Damage * BlockStaminaPerDamage) *
                _armorItem.GuardCostModifier;

            if (Stamina.Current >= staminaCost)
            {
                Stamina.SpendUpTo(staminaCost);
                _blockFeedbackRemaining = FeedbackSeconds;
                return AttackResolution.Blocked;
            }

            if (IsUltimateControlImmune)
                return AttackResolution.Damaged;
            Stamina.SpendUpTo(Stamina.Current);
            StartTimedState(CombatState.Staggered);
            return AttackResolution.GuardBroken;
        }

        if (!touchesBody)
            return AttackResolution.Ignored;

        return AttackResolution.Damaged;
    }

    public void OnDamaged(
        bool isAlive,
        int damage = int.MaxValue,
        bool hardInterrupt = false)
    {
        Resources.OnOwnerDamaged(damage >= 20);

        if (isAlive && IsUltimateControlImmune)
            return;

        bool interruptibleGreatswordCharge =
            _weapon.Family == WeaponFamily.GreatSword &&
            State == CombatState.ChargingHeavy &&
            CurrentTechnique?.Effect is
                WeaponTechniqueEffect.GreatswordTitanReckoning or
                WeaponTechniqueEffect.GreatswordKingsRupture;
        bool preserveBracedTitan = _greatswordBraceProtectedHit;
        _greatswordBraceProtectedHit = false;
        if (isAlive && !hardInterrupt && HasCommittedAction &&
            (!interruptibleGreatswordCharge || preserveBracedTitan))
            return;

        ClearActionState();
        if (!isAlive)
        {
            ResetDuelistTemporaryState();
			ResetKnightTemporaryState();
            _sovereignThroneTimeRemaining = 0f;
            _fourthVeilTimeRemaining = 0f;
            _fourthVeilExitVisualRemaining = 0f;
        }
        else if (hardInterrupt)
        {
            _fourthVeilTimeRemaining = 0f;
            _fourthVeilExitVisualRemaining = 0f;
        }
        StartTimedState(isAlive ? CombatState.Hurt : CombatState.Dead);
    }

    public void HardInterrupt()
    {
        if (IsUltimateControlImmune ||
            State is CombatState.Dead or CombatState.Staggered or
            CombatState.Hurt)
        {
            return;
        }

        _fourthVeilTimeRemaining = 0f;
        _fourthVeilExitVisualRemaining = 0f;
        ClearActionState();
        StartTimedState(CombatState.Hurt);
    }

    public void CancelActions(bool restoreStamina = false)
    {
        ClearActionState();
        ResetDuelistTemporaryState();
		ResetKnightTemporaryState();
        _sovereignThroneTimeRemaining = 0f;
        _sovereignPerfectRiftCounterPending = false;
        _fourthVeilTimeRemaining = 0f;
        _fourthVeilExitVisualRemaining = 0f;
        StartTimedState(CombatState.Idle);

        if (restoreStamina)
            Stamina.Restore();
    }

    public void DebugCycleRangerFocus()
    {
        float next = Resources.HunterFocus < 1f ? 35f :
            Resources.HunterFocus < 36f ? 70f :
            Resources.HunterFocus < 71f ? 100f : 0f;
        Resources.SetHunterFocusForDebug(next);
    }

    public void DebugToggleRangerUltimate()
    {
        Stamina.Restore();
        SetFeedback("PHANTOM HUNT STAMINA READY");
    }

    public void DebugForceRangerPerfectDodge()
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;
        _rangerPerfectDodgeActive = true;
        _rangerCounterWindowRemaining = RangerTuning.CounterWindowSeconds;
        Resources.AddHunterFocus(RangerTuning.PerfectEvadeBonus);
        SetFeedback("PERFECT DODGE â€” COUNTER READY");
    }

    public void DebugResetRanger()
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;
        CancelActions();
        Resources.SetHunterFocusForDebug(0f);
        Resources.SetHunterUltimateForDebug(0f);
    }

    private void ClearActionState(bool preserveRangerWindows = false)
    {
        CurrentAttack = null;
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        CurrentTechnique = null;
        _techniqueStageIndex = 0;
        _effectiveTechniqueStageCount = 0;
        _techniqueSequenceIndex = 0;
        _techniqueStageBuffered = false;
        _inputBuffer.Clear();
        _knightInputBuffer.Clear();
        ClearDuelistPendingInput();
        _duelistHeavenSplitterHoldSeconds = 0f;
        _duelistHeavenSplitterLevel = 1;
        _dodgeBuffered = false;
        ResetRangerActionState(preserveRangerWindows);
        ResetSpellbladeActionState();
        ClearAncientStaffPendingInput();
        ResetGreatswordActionState();
        ResetSovereignActionState();
        ResetScytheActionState();
        _knightFinalLandingWasAirborne = false;
        _knightRiposteBuffered = false;
        IsBlockInputHeld = false;
        _hitstopRemaining = 0f;
    }

    private void CompleteExpiredAction(bool isGrounded)
    {
        if ((State == CombatState.LightAttack || State == CombatState.HeavyAttack ||
                State == CombatState.UmbralAttached) &&
            CurrentAttack != null &&
            _stateElapsed >= CurrentAttack.TotalTime +
                GetGreatswordRecoveryBonus())
        {
            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.GreatswordFallingBastion &&
                !isGrounded)
            {
                return;
            }
            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.KnightFallingShield && !isGrounded)
                return;
            if (CurrentTechnique?.Effect == WeaponTechniqueEffect.KnightFinalOath &&
                _techniqueStageIndex == 7 && !isGrounded)
                return;

            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.RangerMobileShot &&
                _techniqueSequenceIndex == 3)
            {
                _techniqueSequenceIndex++;
                StartTechniqueStage();
                return;
            }

            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.RangerPredatorsHorizon &&
                _techniqueSequenceIndex == 0 && !isGrounded)
                return;

            if (CurrentTechnique != null &&
                _techniqueSequenceIndex + 1 < _effectiveTechniqueStageCount &&
                (!CurrentTechnique.RequiresRepeatedInput ||
                    _techniqueStageBuffered))
            {
                if (CurrentTechnique.AdditionalStageStaminaCost > 0f &&
                    !Stamina.TrySpend(
                        CurrentTechnique.AdditionalStageStaminaCost *
                        _weapon.StaminaCostModifier))
                {
                    SetFeedback("INSUFFICIENT STAMINA");
                    CurrentAttack = null;
                    CurrentTechnique = null;
                    _techniqueStageIndex = 0;
                    _effectiveTechniqueStageCount = 0;
                    _techniqueSequenceIndex = 0;
                    _techniqueStageBuffered = false;
                    StartTimedState(CombatState.Idle);
                    return;
                }

                _techniqueSequenceIndex++;
                StartTechniqueStage();
                if (CurrentTechnique?.Effect ==
                        WeaponTechniqueEffect.GreatswordCombo &&
                    _greatswordFlowingMomentum)
                {
                    _stateElapsed = MathF.Min(
                        CurrentAttack.StartupTime * .55f,
                        GreatswordTuning.FlowTransitionAdvanceSeconds);
                }
                return;
            }

            bool cinematicLanding = CurrentTechnique?.Effect is
                WeaponTechniqueEffect.RangerPhantomHunt or
                WeaponTechniqueEffect.GreatswordFinalVerdict or
                WeaponTechniqueEffect.KnightFinalOath or
                WeaponTechniqueEffect.SovereignGoldenCataclysm or
                WeaponTechniqueEffect.ScytheUmbraDevourer;
            CurrentAttack = null;
            CurrentTechnique = null;
            _techniqueStageIndex = 0;
            _effectiveTechniqueStageCount = 0;
            _techniqueSequenceIndex = 0;
            _techniqueStageBuffered = false;
            _poisePowerMultiplier = 1f;
            if (_weapon.Family == WeaponFamily.HunterBow)
                _rangerChargeRatio = 0f;
            _greatswordFlowingMomentum = false;
            _greatswordWhirlStopping = false;
            _greatswordRecoveryBonusSeconds = 0f;
            StartTimedState(cinematicLanding && !isGrounded
                ? CombatState.Jumping
                : CombatState.Idle);
            return;
        }

        float duration = State switch
        {
            CombatState.Dodging => DodgeDurationSeconds -
                Resources.DuelistMomentumRatio * .04f,
            CombatState.Staggered => GuardBreakSeconds,
            CombatState.Hurt => HurtSeconds,
            _ => float.MaxValue
        };

        if (_stateElapsed >= duration)
            StartTimedState(CombatState.Idle);
    }

    private bool CanStartAction()
    {
        return State == CombatState.Idle ||
            State == CombatState.Moving ||
            State == CombatState.Jumping ||
            State == CombatState.Blocking;
    }

    private bool IsDashSkillPressed(CombatInput input)
    {
        return _weapon.CombatProfile.DashInput switch
        {
            WeaponTechniqueInput.Skill1 => input.Skill1Pressed,
            WeaponTechniqueInput.Skill2 => input.Skill2Pressed,
            WeaponTechniqueInput.Skill3 => input.Skill3Pressed,
            _ => false
        };
    }

    private bool TryStartTechnique(WeaponTechniqueInput techniqueInput)
    {
        WeaponTechnique technique;
		if (IsKnightSwordAndShield)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill1 &&
                _perfectGuardRewardRemaining > 0f)
                technique = WeaponTechniqueCatalog.KnightGuardRiposte;
            else if (techniqueInput == WeaponTechniqueInput.Skill2 &&
                (!_isGrounded || _knightForceFallingShield))
                technique = WeaponTechniqueCatalog.KnightFallingShield;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.DualSwords)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill1 &&
                _duelistRiposteWindowRemaining > 0f)
                technique = WeaponTechniqueCatalog.DuelistPhantomRiposte;
            else if (techniqueInput == WeaponTechniqueInput.Skill2 &&
                !_isGrounded)
                technique = WeaponTechniqueCatalog.DuelistFallingStar;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.GreatSword)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill2 && !_isGrounded)
                technique = WeaponTechniqueCatalog.IronSovereignFallingBastion;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.HunterBow)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill1 && !_isGrounded)
                technique = WeaponTechniqueCatalog.RangerAerialShot;
            else if (techniqueInput == WeaponTechniqueInput.Skill2 && !_isGrounded)
                technique = WeaponTechniqueCatalog.RangerSkyfallShot;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.AbyssRing)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill2 && !_isGrounded)
                technique = WeaponTechniqueCatalog.SovereignHeavenReversal;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.ArcaneWarStaff)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill2 && !_isGrounded)
                technique = WeaponTechniqueCatalog.AncientStaffAstralDescent;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else if (_weapon.Family == WeaponFamily.Scythe)
        {
            if (techniqueInput == WeaponTechniqueInput.Skill1 &&
                _scytheWraithCutWindowRemaining > 0f)
                technique = WeaponTechniqueCatalog.ScytheWraithCut;
            else if (techniqueInput == WeaponTechniqueInput.Skill2 && !_isGrounded)
                technique = WeaponTechniqueCatalog.ScytheCrescentDive;
            else
                technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        else
        {
            technique = _weapon.CombatProfile.Get(techniqueInput);
        }
        if (technique == null)
            return false;

        if (technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneConvergence &&
            _availableGroundRuneCount < 2)
        {
            SetFeedback("TWO RUNES REQUIRED");
            return false;
        }

        if (_weapon.Family == WeaponFamily.HunterBow &&
            technique.Effect is (WeaponTechniqueEffect.RangerEvasiveStep or
                WeaponTechniqueEffect.RangerEvasiveShot) &&
            _rangerEvadeCooldownRemaining > 0f)
        {
            SetFeedback("EVASIVE SHOT RECOVERING");
            return false;
        }

        bool rangerUltimate = _weapon.Family == WeaponFamily.HunterBow &&
            technique.Effect == WeaponTechniqueEffect.RangerPhantomHunt;
        if (rangerUltimate && !_rangerUltimateTargetAvailable)
        {
            SetFeedback("NO VALID TARGET IN RANGE");
            return false;
        }

        bool greatswordUltimate =
            _weapon.Family == WeaponFamily.GreatSword &&
            technique.Effect == WeaponTechniqueEffect.GreatswordFinalVerdict;
        if (greatswordUltimate && !_greatswordUltimateTargetAvailable)
        {
            SetFeedback("NO SAFE TARGET IN RANGE");
            return false;
        }

		bool knightUltimate = IsKnightSwordAndShield &&
            technique.Effect == WeaponTechniqueEffect.KnightFinalOath;
        if (knightUltimate && !_knightUltimateTargetAvailable)
        {
            SetFeedback("NO SAFE TARGET IN RANGE");
            return false;
        }

        bool sovereignUltimate =
            _weapon.Family == WeaponFamily.AbyssRing &&
            technique.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm;
        if (sovereignUltimate && !_sovereignUltimateTargetAvailable)
        {
            SetFeedback("NO SAFE TARGET IN RANGE");
            return false;
        }
        bool ancientStaffUltimate =
            _weapon.Family == WeaponFamily.ArcaneWarStaff &&
            technique.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate;
        if (ancientStaffUltimate && !_ancientStaffUltimateTargetAvailable)
        {
            SetFeedback("NO VALID DEPLOYMENT AREA");
            return false;
        }
        bool scytheUltimate = _weapon.Family == WeaponFamily.Scythe &&
            technique.Effect == WeaponTechniqueEffect.ScytheUmbraDevourer;
        if (scytheUltimate && !_scytheUltimateTargetAvailable)
        {
            SetFeedback("NO SAFE TARGET IN RANGE");
            return false;
        }
        bool duelistUltimate = _weapon.Family == WeaponFamily.DualSwords &&
            technique.Effect ==
                WeaponTechniqueEffect.DuelistInfiniteStarburst;
        if (duelistUltimate && !_duelistUltimateTargetAvailable)
        {
            SetFeedback("NO VALID TARGET IN RANGE");
            return false;
        }
        if (technique.Effect ==
                WeaponTechniqueEffect.DuelistBladeOverdrive &&
            (_duelistOverdriveCooldownRemaining > 0f ||
                _duelistOverdriveRemaining > 0f))
        {
            SetFeedback("BLADE OVERDRIVE RECOVERING");
            return false;
        }
        if (_weapon.Family == WeaponFamily.AbyssRing &&
            (technique.Effect is
                WeaponTechniqueEffect.SovereignOmnipresentStep or
                WeaponTechniqueEffect.SovereignHeavenReversal) &&
            _sovereignStepCooldownRemaining > 0f)
        {
            SetFeedback("OMNIPRESENT STEP RECOVERING");
            return false;
        }
        if (_weapon.Family == WeaponFamily.AbyssRing &&
            technique.Effect ==
                WeaponTechniqueEffect.SovereignThroneManifestation &&
            IsSovereignThroneActive)
        {
            SetFeedback("THRONE ALREADY MANIFEST");
            return false;
        }

        float staminaCost = technique.RequiresFullStamina
            ? Stamina.Maximum
            : technique.StaminaCost * _weapon.StaminaCostModifier;

        if (technique.RequiresFullStamina && !Stamina.IsFull)
        {
            SetFeedback("FULL STAMINA REQUIRED");
            return false;
        }

        if (!Stamina.TrySpend(staminaCost))
        {
            SetFeedback("INSUFFICIENT STAMINA");
            return false;
        }

        CurrentTechnique = technique;
        _techniqueStageIndex = 0;
        _techniqueSequenceIndex = 0;
        _effectiveTechniqueStageCount = GetEffectiveStageCount(technique);
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        _techniqueStageBuffered = false;
        _duelistTechniqueGainAwarded = false;
        _duelistRepositionAttackId = -1;
        _rangerChargeRatio = 0f;
        _rangerWindrunnerDirection = 0f;
        _rangerDrawState = RangerDrawState.Quick;
        _poisePowerMultiplier = 1f;
        _currentTechniqueUseId = ++_techniqueUseCounter;
        _rangerFlowGainThisTechnique = 0f;
        _sovereignChargeRatio = 0f;
        _sovereignChargeLevel = 1;
        _scytheEnhancedThisTechnique = _scytheEnhancedAttackPending;
        if (_scytheEnhancedThisTechnique)
            _attackPowerMultiplier *= 1f + ScytheTuning.DodgeWeaveDamageBonus;

		if (IsKnightSwordAndShield)
        {
            if (technique.Effect == WeaponTechniqueEffect.KnightGuardRiposte)
            {
                _perfectGuardRewardRemaining = 0f;
                SetFeedback("GUARD RIPOSTE");
            }
            else if (technique.Effect == WeaponTechniqueEffect.KnightFallingShield)
            {
                _knightFallingWasAirborne = !_isGrounded;
            }
            else if (technique.Effect == WeaponTechniqueEffect.KnightFinalOath)
            {
                _knightFinalLandingWasAirborne = false;
                SetFeedback("KNIGHT'S FINAL OATH");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }

        if (_weapon.Family == WeaponFamily.GreatSword)
        {
            _greatswordFlowingMomentum = false;
            _greatswordWhirlStopping = false;
            _greatswordWhirlEndRequested = false;
            _greatswordChargeRatio = 0f;
            _greatswordChargeLevel = 1;
            _greatswordRecoveryBonusSeconds = 0f;
            _greatswordTitanBraced = false;

            if (technique.Effect == WeaponTechniqueEffect.GreatswordCombo &&
                _greatswordCounterWindowRemaining > 0f)
            {
                _greatswordCounterWindowRemaining = 0f;
                _poisePowerMultiplier = 1.35f;
                SetFeedback("STEEL COUNTER");
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.GreatswordWhirlingJudgment)
            {
                _greatswordWhirlElapsed = 0f;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion)
            {
                _greatswordFallingWasAirborne = !_isGrounded;
                _greatswordFallingPoiseBonus = _verticalVelocity > 40f;
            }
            else if (technique.Effect is
                WeaponTechniqueEffect.GreatswordTitanReckoning or
                WeaponTechniqueEffect.GreatswordKingsRupture)
            {
                SetFeedback(technique.Effect ==
                        WeaponTechniqueEffect.GreatswordTitanReckoning
                    ? "TITAN STANCE"
                    : "ROYAL WINDUP");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.GreatswordFinalVerdict)
            {
                SetFeedback("FINAL VERDICT");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }

        if (_weapon.Family == WeaponFamily.DualSwords)
        {
            Resources.MarkCombatActivity();
            if (technique.Effect ==
                WeaponTechniqueEffect.DuelistPhantomRiposte)
            {
                _duelistRiposteWindowRemaining = 0f;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.DuelistBladeOverdrive)
            {
                _duelistOverdriveRemaining =
                    DuelistTuning.OverdriveDurationSeconds;
                _duelistOverdriveCooldownRemaining =
                    DuelistTuning.OverdriveCooldownSeconds;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.DuelistHeavenSplitter)
            {
                _duelistHeavenSplitterHoldSeconds = 0f;
                _duelistHeavenSplitterLevel = 1;
                SetFeedback("HEAVEN SPLITTER");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.DuelistInfiniteStarburst)
            {
                _duelistUltimateProcessedHits = 0;
                SetFeedback("INFINITE STARBURST ASCENSION");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }

        else if (_weapon.Family == WeaponFamily.HunterBow)
        {
            Resources.MarkRangerCombatActivity();
            _rangerLockedDirection = _currentFacing == FacingDirection.Left
                ? -1f
                : 1f;

            if (technique.Effect == WeaponTechniqueEffect.RangerPhantomHunt)
            {
                _rangerUltimateFocusSnapshot = 0f;
                _rangerWindrunnerDirection = -_rangerLockedDirection;
                SetFeedback("PHANTOM HUNT");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }

            if (technique.Effect == WeaponTechniqueEffect.RangerHeavensFury)
            {
                _rangerUltimateFocusSnapshot = Resources.HunterFocus;
                SetFeedback(technique.Name.ToUpperInvariant());
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }

            if (technique.Effect is WeaponTechniqueEffect.RangerHuntersDraw or
                WeaponTechniqueEffect.RangerPiercingDraw)
            {
                _rangerChargeRatio = Math.Clamp(
                    _rangerSkill1HoldSeconds /
                        RangerTuning.HunterDrawMaximumSeconds,
                    0f,
                    1f);
                if (_rangerSkill1CurrentlyHeld)
                {
                    SetFeedback(technique.Name.ToUpperInvariant());
                    StartTimedState(CombatState.ChargingHeavy);
                    return true;
                }

                ReleaseRangerDraw();
                SetFeedback(technique.Name.ToUpperInvariant());
                return true;
            }

            if (technique.Effect == WeaponTechniqueEffect.RangerSkyfallMarker)
            {
                _skyfallDistance = GetSkyfallDistance(
                    _rangerSkill3HoldSeconds,
                    _rangerSkill3CurrentlyHeld);
                if (_rangerSkill3CurrentlyHeld)
                {
                    SetFeedback(technique.Name.ToUpperInvariant());
                    StartTimedState(CombatState.ChargingHeavy);
                    return true;
                }

                StartTechniqueStage();
                SetFeedback(technique.Name.ToUpperInvariant());
                return true;
            }

            if (technique.Effect ==
                WeaponTechniqueEffect.RangerPredatorsHorizon)
            {
                _rangerWindrunnerDirection = -_rangerLockedDirection;
            }

            if (technique.Effect is WeaponTechniqueEffect.RangerEvasiveStep or
                    WeaponTechniqueEffect.RangerEvasiveShot)
            {
                _rangerEvadeCooldownRemaining =
                    RangerTuning.EvasiveStepCooldownSeconds;
                _rangerBackstepWindowRemaining =
                    RangerTuning.BackstepFollowupWindowSeconds;
                _rangerEvadeDirection = MathF.Abs(_currentHorizontalDirection) > .1f
                    ? MathF.Sign(_currentHorizontalDirection)
                    : -_rangerLockedDirection;
                _rangerWindrunnerDirection = _rangerEvadeDirection;
            }
            else if (technique.Effect == WeaponTechniqueEffect.RangerTwinFang)
            {
                _rangerWindrunnerDirection = MathF.Abs(
                    _currentHorizontalDirection) > .1f
                        ? -MathF.Sign(_currentHorizontalDirection)
                        : -_rangerLockedDirection;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.RangerPhantomRetreat)
            {
                _rangerWindrunnerDirection = -_rangerLockedDirection;
            }
            else if (technique.Effect == WeaponTechniqueEffect.RangerBackstepShot)
            {
                _rangerWindrunnerDirection = -_rangerLockedDirection;
                _rangerBackstepWindowRemaining = 0f;
            }
            else if (technique.Effect == WeaponTechniqueEffect.RangerSlidingShot)
            {
                _rangerWindrunnerDirection = MathF.Abs(_currentHorizontalDirection) > .1f
                    ? MathF.Sign(_currentHorizontalDirection)
                    : _rangerLockedDirection;
            }
            else if (technique.Effect == WeaponTechniqueEffect.RangerPhantomBarrage)
            {
                _effectiveTechniqueStageCount = Resources.HunterFocus >=
                    RangerTuning.PhantomFlowThreshold ? 6 : 4;
                _rangerWindrunnerDirection = MathF.Abs(_currentHorizontalDirection) > .1f
                    ? MathF.Sign(_currentHorizontalDirection)
                    : -_rangerLockedDirection;
                SetFeedback(_effectiveTechniqueStageCount == 6
                    ? "TEMPEST BARRAGE"
                    : "PHANTOM BARRAGE");
            }
            else if (technique.Effect == WeaponTechniqueEffect.RangerAerialShot)
            {
                _effectiveTechniqueStageCount = Resources.HunterFocus >=
                    RangerTuning.PhantomFlowThreshold ? 2 : 1;
            }
        }

        else if (_weapon.Family == WeaponFamily.ArcaneWarStaff)
        {
            if (technique.Effect ==
                WeaponTechniqueEffect.AncientStaffFourthVeil)
            {
                _fourthVeilTimeRemaining =
                    AncientStaffTuning.FourthVeilDurationSeconds;
                _fourthVeilExitVisualRemaining = 0f;
                SetFeedback("FOURTH VEIL");
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate)
            {
                SetFeedback("HEAVENFALL THUNDER GATE");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
            else if (technique.Effect == WeaponTechniqueEffect.SpellbladeRuneBrand)
            {
                _runePlacementDistance = GetRunePlacementDistance(
                    _spellbladeSkill2HoldSeconds,
                    _spellbladeSkill2CurrentlyHeld);
                if (_spellbladeSkill2CurrentlyHeld)
                {
                    SetFeedback(technique.Name.ToUpperInvariant());
                    StartTimedState(CombatState.ChargingHeavy);
                    return true;
                }
            }
        }
        else if (_weapon.Family == WeaponFamily.AbyssRing)
        {
            if (technique.Effect is
                WeaponTechniqueEffect.SovereignOmnipresentStep or
                WeaponTechniqueEffect.SovereignHeavenReversal)
            {
                _sovereignStepCooldownRemaining =
                    AbyssRingTuning.OmnipresentStepCooldownSeconds;
                if (technique.Effect ==
                    WeaponTechniqueEffect.SovereignHeavenReversal)
                {
                    _sovereignTransitDirection = new Vector2(
                        _currentFacing == FacingDirection.Left ? -1f : 1f,
                        -1f);
                }
                else if (_sovereignTransitDirection.LengthSquared() < .01f)
                {
                    float facing = _currentFacing == FacingDirection.Left
                        ? -1f
                        : 1f;
                    _sovereignTransitDirection = new Vector2(-facing, 0f);
                }
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.SovereignThroneManifestation)
            {
                _sovereignThroneTimeRemaining =
                    AbyssRingTuning.ThroneDurationSeconds;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.SovereignEventHorizonPalm)
            {
                SetFeedback("EVENT HORIZON CHARGE");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
            else if (technique.Effect ==
                WeaponTechniqueEffect.SovereignGoldenCataclysm)
            {
                SetFeedback("GOLDEN SOVEREIGN CATACLYSM");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }
        else if (_weapon.Family == WeaponFamily.Scythe)
        {
            float facing = _currentFacing == FacingDirection.Left ? -1f : 1f;
            _scytheMoveDirection = MathF.Abs(_currentHorizontalDirection) > .1f
                ? MathF.Sign(_currentHorizontalDirection)
                : -facing;
            _scytheShadowSeverEnabled = false;
            if (technique.Effect == WeaponTechniqueEffect.ScytheWraithCut)
                _scytheWraithCutWindowRemaining = 0f;
            if (technique.Effect == WeaponTechniqueEffect.ScytheShadowPass)
                _scytheMoveDirection = facing;
            if (technique.Effect == WeaponTechniqueEffect.ScytheUmbraDevourer)
            {
                _scytheEnhancedThisTechnique = false;
                SetFeedback("UMBRA DEVOURER");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }

        SetFeedback(_weapon.Family == WeaponFamily.GreatSword &&
                technique.Effect ==
                    WeaponTechniqueEffect.GreatswordWhirlingJudgment
            ? Loc.Get("weapon.whirling_judgment.name")
            : technique.Name.ToUpperInvariant());
        StartTechniqueStage();
        return true;
    }

    private void StartTechniqueStage()
    {
        _techniqueStageIndex = GetPhysicalStageIndex(
            CurrentTechnique,
            _techniqueSequenceIndex,
            _effectiveTechniqueStageCount);
        AttackDefinition stage = CurrentTechnique?.GetStage(_techniqueStageIndex);
        if (stage == null)
        {
            CurrentTechnique = null;
            CurrentAttack = null;
            StartTimedState(CombatState.Idle);
            return;
        }

        CombatState state = stage.Kind == AttackKind.Heavy
            ? CombatState.HeavyAttack
            : CombatState.LightAttack;
        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ScytheUmbraDevourer &&
            _techniqueStageIndex is >= 1 and <= 3)
            state = CombatState.UmbralAttached;
        StartAttack(stage, state);
        _techniqueStageBuffered = false;
    }

    public void RegisterSuccessfulHit()
    {
        if (CurrentTechnique == null || _resourceGainAttackId == AttackId)
            return;

        _resourceGainAttackId = AttackId;

        if (_weapon.Family == WeaponFamily.Scythe &&
            _scytheEnhancedThisTechnique)
        {
            _scytheEnhancedThisTechnique = false;
            _scytheEnhancedAttackPending = false;
            _scytheDodgeWeaveRewardRemaining = 0f;
        }

        if (_weapon.Family == WeaponFamily.GreatSword)
            return;

        if (_weapon.Family != WeaponFamily.DualSwords)
        {
            Resources.RegisterHit(CurrentTechnique.ResourceGainOnHit);
            return;
        }

        if (CurrentTechnique.Effect ==
            WeaponTechniqueEffect.DuelistInfiniteStarburst)
        {
            if (_techniqueStageIndex < DuelistTuning.UltimateStrikeCount)
                _duelistUltimateProcessedHits = Math.Min(
                    DuelistTuning.UltimateStrikeCount,
                    _duelistUltimateProcessedHits + 1);
            return;
        }

        float gain = CurrentTechnique.Effect switch
        {
            WeaponTechniqueEffect.DuelistStarburstSwordArt =>
                DuelistTuning.BasicHitGain +
                (_techniqueStageIndex == CurrentTechnique.StageCount - 1
                    ? DuelistTuning.StarburstCompletionBonus : 0f),
            WeaponTechniqueEffect.DuelistCrossRequiem
                when _techniqueStageIndex == CurrentTechnique.StageCount - 1 =>
                    DuelistTuning.CrossRequiemGain,
            WeaponTechniqueEffect.DuelistThousandEdgeWaltz
                when _techniqueStageIndex == CurrentTechnique.StageCount - 1 =>
                    DuelistTuning.ThousandEdgeWaltzGain,
            WeaponTechniqueEffect.DuelistPhantomRiposte or
            WeaponTechniqueEffect.DuelistFallingStar or
            WeaponTechniqueEffect.DuelistFlashBlade
                when _techniqueStageIndex == CurrentTechnique.StageCount - 1 =>
                    DuelistTuning.DodgeCounterGain,
            _ => 0f
        };

        bool oncePerSequence = CurrentTechnique.Effect is
            WeaponTechniqueEffect.DuelistCrossRequiem or
            WeaponTechniqueEffect.DuelistThousandEdgeWaltz or
            WeaponTechniqueEffect.DuelistPhantomRiposte or
            WeaponTechniqueEffect.DuelistFallingStar or
            WeaponTechniqueEffect.DuelistFlashBlade;
        if (oncePerSequence && _duelistTechniqueGainAwarded)
            gain = 0f;

        if (gain > 0f)
        {
            Resources.AddDuelistMomentum(gain);
            if (oncePerSequence)
                _duelistTechniqueGainAwarded = true;
        }
    }

    public void RegisterExternalHit(int resourceGain)
    {
        Resources.RegisterHit(resourceGain);
    }

    public void RegisterRangerProjectileHit(
        int sourceAttackId,
        WeaponTechniqueEffect effect,
        RangerDrawState drawState,
        bool safeSpacing)
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;

        bool firstHitFromShot = sourceAttackId < 0 ||
            _rangerFocusHitAttackId != sourceAttackId;
        float gain = 0f;
        if (firstHitFromShot)
        {
            gain += RangerTuning.ConsistentHitGain;
            if (MathF.Abs(_currentHorizontalDirection) > .1f)
                gain += RangerTuning.MovingHitGain;
            if (safeSpacing)
                gain += RangerTuning.SafeSpacingHitGain;
            if (drawState is RangerDrawState.Full or RangerDrawState.Perfect)
                gain += RangerTuning.FullDrawHitGain;
            if (effect == WeaponTechniqueEffect.RangerWindrunnerShot)
            {
                gain += RangerTuning.WindrunnerHitGain;
                if (_windrunnerPerfectRewardRemaining > 0f)
                {
                    gain += RangerTuning.WindrunnerPerfectEvadeBonus;
                    _windrunnerPerfectRewardRemaining = 0f;
                    SetFeedback("WINDRUNNER EVADE");
                }
            }
            if (effect is WeaponTechniqueEffect.RangerSlidingShot or
                WeaponTechniqueEffect.RangerBackstepShot or
                WeaponTechniqueEffect.RangerEvasiveShot or
                WeaponTechniqueEffect.RangerTwinFang or
                WeaponTechniqueEffect.RangerPhantomRetreat)
                gain += RangerTuning.EvasiveHitGain;
            if (effect == WeaponTechniqueEffect.RangerCounterShot)
                gain += RangerTuning.CounterHitGain;
            if (effect is WeaponTechniqueEffect.RangerAerialShot or
                WeaponTechniqueEffect.RangerSkyfallVolley or
                WeaponTechniqueEffect.RangerSkyfallShot)
                gain += RangerTuning.AerialHitGain;
            _rangerFocusHitAttackId = sourceAttackId;
        }

        float allowedFocus = MathF.Max(
            0f,
            RangerTuning.FlowGainPerTechniqueCap -
                _rangerFlowGainThisTechnique);
        gain = MathF.Min(gain, allowedFocus);
        if (effect == WeaponTechniqueEffect.RangerPhantomHunt)
            gain = 0f;

        if (gain > 0f)
        {
            Resources.AddHunterFocus(gain);
            _rangerFlowGainThisTechnique += gain;
        }
        else
            Resources.MarkRangerCombatActivity();

    }

    public void RegisterRangerProjectileMiss(int sourceAttackId)
    {
        if (_weapon.Family != WeaponFamily.HunterBow ||
            sourceAttackId < 0 || _rangerFocusHitAttackId == sourceAttackId)
            return;

        Resources.LoseHunterFocus(RangerTuning.MissFocusLoss);
        Resources.MarkRangerCombatActivity();
    }

    public void RegisterRangerWeakPointHit()
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;

        if (CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.RangerPhantomHunt)
        {
            SetFeedback("WEAK POINT");
            return;
        }

        float focus = MathF.Min(
            RangerTuning.WeakPointHitGain,
            MathF.Max(0f, RangerTuning.FlowGainPerTechniqueCap -
                _rangerFlowGainThisTechnique));
        if (focus > 0f)
        {
            Resources.AddHunterFocus(focus);
            _rangerFlowGainThisTechnique += focus;
        }
        SetFeedback("WEAK POINT");
        _hitstopRemaining = MathF.Max(_hitstopRemaining, .025f);
    }

    public void UpdateRangerClosePressure(float elapsedSeconds, bool pressured)
    {
        if (_weapon.Family == WeaponFamily.HunterBow && pressured)
        {
            Resources.LoseHunterFocus(
                RangerTuning.ClosePressureLossPerSecond *
                MathF.Max(0f, elapsedSeconds));
        }
    }

    public void RegisterRangerImpactFeedback(
        bool strongHit,
        bool ultimate)
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;

        float duration = ultimate
            ? .055f
            : strongHit ? .022f
            : 0f;
        _hitstopRemaining = MathF.Max(_hitstopRemaining, duration);
    }

    public void RegisterGreatswordImpactFeedback(bool ultimate)
    {
        if (_weapon.Family != WeaponFamily.GreatSword)
            return;
        _hitstopRemaining = MathF.Max(
            _hitstopRemaining,
            ultimate ? .075f : .045f);
    }

    private float GetMomentumSpeedMultiplier()
    {
        if (_weapon.Family == WeaponFamily.DualSwords)
        {
            float multiplier = DuelistTuning.GetTierSpeedMultiplier(
                Resources.DuelistMomentum);
            if (IsDuelistOverdriveActive)
                multiplier += DuelistTuning.OverdriveSpeedBonus;
            return MathF.Min(1.25f, multiplier);
        }
        return 1f;
    }

    public void RegisterAggressiveReposition()
    {
        if (_weapon.Family != WeaponFamily.DualSwords ||
            CurrentTechnique == null ||
            _duelistRepositionAttackId == AttackId)
            return;

        _duelistRepositionAttackId = AttackId;
        Resources.AddDuelistMomentum(
            DuelistTuning.AggressiveRepositionGain);
    }

    private int GetEffectiveStageCount(WeaponTechnique technique)
    {
        if (technique == null)
            return 0;

        return technique.StageCount;
    }

    private static int GetPhysicalStageIndex(
        WeaponTechnique technique,
        int sequenceIndex,
        int effectiveCount)
    {
        if (technique == null)
            return 0;

        return Math.Clamp(sequenceIndex, 0, technique.StageCount - 1);
    }

    private void SetFeedback(string message)
    {
        TechniqueFeedback = message ?? string.Empty;
        _techniqueFeedbackRemaining = .85f;
    }

    private void UpdateGreatswordInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.GreatSword)
        {
            ClearGreatswordPendingInput();
            _greatswordSkill1CurrentlyHeld = false;
            _greatswordSkill2CurrentlyHeld = false;
            _greatswordSkill3CurrentlyHeld = false;
            return;
        }

        _greatswordSkill1CurrentlyHeld = input.Skill1Held;
        _greatswordSkill2CurrentlyHeld = input.Skill2Held;
        _greatswordSkill3CurrentlyHeld = input.Skill3Held;

        int pressedMask = (input.Skill1Pressed ? 1 : 0) |
            (input.Skill2Pressed ? 2 : 0) |
            (input.Skill3Pressed ? 4 : 0);
        if (pressedMask != 0 && !HasCommittedAction)
        {
            if (_greatswordInputMask == 0)
            {
                _greatswordChordRemaining =
                    GreatswordTuning.ChordWindowSeconds;
            }
            _greatswordInputMask |= pressedMask;
        }

        if (_greatswordInputMask != 0)
        {
            _greatswordChordRemaining = MathF.Max(
                0f,
                _greatswordChordRemaining - elapsedSeconds);
        }
    }

    private bool ResolveGreatswordActionInput()
    {
        if (_greatswordInputMask == 7)
        {
            ClearGreatswordPendingInput();
            return TryStartTechnique(WeaponTechniqueInput.Ultimate);
        }

        if (_greatswordChordRemaining > 0f)
            return false;

        int mask = _greatswordInputMask;
        ClearGreatswordPendingInput();
        if (!_isGrounded && mask == 2)
            return TryStartTechnique(WeaponTechniqueInput.Skill2);

        return mask switch
        {
            3 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill2),
            6 => TryStartTechnique(WeaponTechniqueInput.Skill2Skill3),
            5 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill3),
            1 => TryStartTechnique(WeaponTechniqueInput.Skill1),
            2 => TryStartTechnique(WeaponTechniqueInput.Skill2),
            4 => TryStartTechnique(WeaponTechniqueInput.Skill3),
            _ => false
        };
    }

    private void ClearGreatswordPendingInput()
    {
        _greatswordInputMask = 0;
        _greatswordChordRemaining = 0f;
    }

    private void UpdateGreatswordChargeState(CombatInput input)
    {
        WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (effect == WeaponTechniqueEffect.GreatswordFinalVerdict)
        {
            if (_stateElapsed >= GreatswordTuning.FinalVerdictIntroSeconds)
                StartTechniqueStage();
            return;
        }

        bool titan = effect ==
            WeaponTechniqueEffect.GreatswordTitanReckoning;
        bool rupture = effect ==
            WeaponTechniqueEffect.GreatswordKingsRupture;
        if (!titan && !rupture)
            return;

        float maximum = titan
            ? GreatswordTuning.TitanMaximumChargeSeconds
            : GreatswordTuning.RuptureMaximumChargeSeconds;
        float levelTwo = titan
            ? GreatswordTuning.TitanLevelTwoSeconds
            : GreatswordTuning.RuptureLevelTwoSeconds;
        float levelThree = titan
            ? GreatswordTuning.TitanLevelThreeSeconds
            : GreatswordTuning.RuptureLevelThreeSeconds;
        bool held = titan
            ? _greatswordSkill1CurrentlyHeld &&
                _greatswordSkill2CurrentlyHeld
            : _greatswordSkill1CurrentlyHeld &&
                _greatswordSkill3CurrentlyHeld;
        _greatswordChargeRatio = Math.Clamp(
            _stateElapsed / maximum,
            0f,
            1f);
        if (held && _stateElapsed < maximum)
            return;

        _greatswordChargeLevel = _stateElapsed >= levelThree
            ? 3
            : _stateElapsed >= levelTwo ? 2 : 1;
        _attackPowerMultiplier = _greatswordChargeLevel switch
        {
            3 => titan
                ? GreatswordTuning.ChargeLevelThreeMultiplier
                : GreatswordTuning.RuptureLevelThreeMultiplier,
            2 => titan
                ? GreatswordTuning.ChargeLevelTwoMultiplier
                : GreatswordTuning.RuptureLevelTwoMultiplier,
            _ => 1f
        };
        _poisePowerMultiplier = _greatswordChargeLevel switch
        {
            3 => GreatswordTuning.ChargeLevelThreePoiseMultiplier,
            2 => GreatswordTuning.ChargeLevelTwoPoiseMultiplier,
            _ => 1f
        };
        if (titan && _greatswordTitanBraced)
        {
            _poisePowerMultiplier *=
                GreatswordTuning.TitanBracePoiseBonus;
        }
        _greatswordRecoveryBonusSeconds = _greatswordChargeLevel switch
        {
            3 => GreatswordTuning.ChargeLevelThreeRecoveryBonus,
            2 => GreatswordTuning.ChargeLevelTwoRecoveryBonus,
            _ => 0f
        };
        SetFeedback($"SOVEREIGN FORCE {_greatswordChargeLevel}");
        StartTechniqueStage();
    }

    private void BeginGreatswordWhirlEndingSweep()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.GreatswordWhirlingJudgment ||
            _greatswordWhirlStopping ||
            _techniqueSequenceIndex >= CurrentTechnique.StageCount - 1)
        {
            return;
        }

        _techniqueSequenceIndex = CurrentTechnique.StageCount - 1;
        SetFeedback("ENDING SWEEP");
        StartTechniqueStage();
    }

    private void BeginGreatswordWhirlStop()
    {
        CurrentTechnique = WeaponTechniqueCatalog.IronVanguardSpinStop;
        _techniqueSequenceIndex = 0;
        _techniqueStageIndex = 0;
        _effectiveTechniqueStageCount = 1;
        _techniqueStageBuffered = false;
        _greatswordWhirlStopping = true;
        SetFeedback("CONTROLLED STOP");
        StartTechniqueStage();
    }

    private float GetGreatswordRecoveryBonus()
    {
        if (_weapon.Family != WeaponFamily.GreatSword ||
            CurrentTechnique?.Effect is not
                (WeaponTechniqueEffect.GreatswordTitanReckoning or
                 WeaponTechniqueEffect.GreatswordKingsRupture) ||
            _techniqueSequenceIndex + 1 < _effectiveTechniqueStageCount)
            return 0f;
        return _greatswordRecoveryBonusSeconds;
    }

    private void ResetGreatswordActionState()
    {
        ClearGreatswordPendingInput();
        _greatswordSkill1CurrentlyHeld = false;
        _greatswordSkill2CurrentlyHeld = false;
        _greatswordSkill3CurrentlyHeld = false;
        _greatswordCounterWindowRemaining = 0f;
        _greatswordFlowingMomentum = false;
        _greatswordWhirlElapsed = 0f;
        _greatswordWhirlEndRequested = false;
        _greatswordWhirlStopping = false;
        _greatswordFallingWasAirborne = false;
        _greatswordFallingPoiseBonus = false;
        _greatswordLandingPulseRemaining = 0f;
        _greatswordChargeRatio = 0f;
        _greatswordChargeLevel = 1;
        _greatswordRecoveryBonusSeconds = 0f;
        _greatswordTitanBraced = false;
        _greatswordBraceProtectedHit = false;
    }

    private void UpdateSovereignInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.AbyssRing)
        {
            ClearSovereignPendingInput();
            return;
        }

        _sovereignSkill1Held = input.Skill1Held;
        _sovereignSkill3Held = input.Skill3Held;
        int pressedMask = (input.Skill1Pressed ? 1 : 0) |
            (input.Skill2Pressed ? 2 : 0) |
            (input.Skill3Pressed ? 4 : 0);
        if (pressedMask != 0 && !HasCommittedAction)
        {
            if (_sovereignInputMask == 0)
                _sovereignChordRemaining = AbyssRingTuning.ChordWindowSeconds;
            _sovereignInputMask |= pressedMask;
            if (input.Skill2Pressed)
            {
                float facing = _currentFacing == FacingDirection.Left ? -1f : 1f;
                float horizontal = MathF.Abs(_currentHorizontalDirection) > .1f
                    ? MathF.Sign(_currentHorizontalDirection)
                    : 0f;
                float vertical = input.VerticalDirection;
                if (MathF.Abs(horizontal) < .1f &&
                    MathF.Abs(vertical) < .1f)
                {
                    horizontal = -facing;
                }
                _sovereignTransitDirection = new Vector2(
                    horizontal,
                    vertical);
            }
        }

        if (_sovereignInputMask != 0)
            _sovereignChordRemaining = MathF.Max(
                0f, _sovereignChordRemaining - elapsedSeconds);
    }

    private bool ResolveSovereignActionInput()
    {
        if (_sovereignInputMask == 7)
        {
            ClearSovereignPendingInput();
            return TryStartTechnique(WeaponTechniqueInput.Ultimate);
        }
        if (_sovereignChordRemaining > 0f)
            return false;

        int mask = _sovereignInputMask;
        ClearSovereignPendingInput();
        if (!_isGrounded && mask == 2)
            return TryStartTechnique(WeaponTechniqueInput.Skill2);
        return mask switch
        {
            3 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill2),
            6 => TryStartTechnique(WeaponTechniqueInput.Skill2Skill3),
            5 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill3),
            1 => TryStartTechnique(WeaponTechniqueInput.Skill1),
            2 => TryStartTechnique(WeaponTechniqueInput.Skill2),
            4 => TryStartTechnique(WeaponTechniqueInput.Skill3),
            _ => false
        };
    }

    private void ClearSovereignPendingInput()
    {
        _sovereignInputMask = 0;
        _sovereignChordRemaining = 0f;
    }

    private void UpdateSovereignChargeState()
    {
        WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;
        if (effect == WeaponTechniqueEffect.SovereignGoldenCataclysm)
        {
            if (_stateElapsed >= AbyssRingTuning.UltimateIntroSeconds)
                StartTechniqueStage();
            return;
        }
        if (effect != WeaponTechniqueEffect.SovereignEventHorizonPalm)
            return;

        _sovereignChargeRatio = Math.Clamp(
            _stateElapsed / AbyssRingTuning.EventHorizonMaximumSeconds,
            0f, 1f);
        bool held = _sovereignSkill1Held && _sovereignSkill3Held;
        if (held && _stateElapsed < AbyssRingTuning.EventHorizonMaximumSeconds)
            return;
        _sovereignChargeLevel = _stateElapsed >=
                AbyssRingTuning.EventHorizonLevelThreeSeconds
            ? 3
            : _stateElapsed >= AbyssRingTuning.EventHorizonLevelTwoSeconds
                ? 2
                : 1;
        _attackPowerMultiplier = _sovereignChargeLevel switch
        {
            3 => 3.90f / 1.90f,
            2 => 2.80f / 1.90f,
            _ => 1f
        };
        _poisePowerMultiplier = 1f + (_sovereignChargeLevel - 1) * .20f;
        SetFeedback($"EVENT HORIZON LEVEL {_sovereignChargeLevel}");
        StartTechniqueStage();
    }

    private void ResetSovereignActionState()
    {
        ClearSovereignPendingInput();
        _sovereignSkill1Held = false;
        _sovereignSkill3Held = false;
        _sovereignChargeRatio = 0f;
        _sovereignChargeLevel = 1;
        _sovereignTransitDirection = Vector2.Zero;
    }

    private void UpdateRangerInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;

        _rangerEvadeCooldownRemaining = MathF.Max(
            0f,
            _rangerEvadeCooldownRemaining - elapsedSeconds);
        _rangerBackstepWindowRemaining = MathF.Max(
            0f,
            _rangerBackstepWindowRemaining - elapsedSeconds);
        _rangerCounterWindowRemaining = MathF.Max(
            0f,
            _rangerCounterWindowRemaining - elapsedSeconds);
        if (_rangerCounterWindowRemaining <= 0f)
            _rangerPerfectDodgeActive = false;

        int pressedMask = (input.Skill1Pressed ? 1 : 0) |
            (input.Skill2Pressed ? 2 : 0) |
            (input.Skill3Pressed ? 4 : 0);
        bool acceptsNewRangerAction = !HasCommittedAction ||
            CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerEvasiveStep ||
            CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerEvasiveShot;
        if (pressedMask != 0 && acceptsNewRangerAction)
        {
            if (_rangerInputMask == 0)
                _rangerChordRemaining = RangerTuning.UltimateChordWindowSeconds;
            _rangerInputMask |= pressedMask;
        }
        if (_rangerInputMask != 0)
            _rangerChordRemaining = MathF.Max(
                0f,
                _rangerChordRemaining - elapsedSeconds);

        if (input.Skill1Pressed)
        {
            _rangerSkill1HoldSeconds = 0f;
        }
        if (input.Skill3Pressed)
        {
            _rangerSkill3HoldSeconds = 0f;
        }

        _rangerSkill1CurrentlyHeld = input.Skill1Held;
        _rangerSkill3CurrentlyHeld = input.Skill3Held;
        if (input.Skill1Held)
            _rangerSkill1HoldSeconds += elapsedSeconds;
        if (input.Skill3Held)
            _rangerSkill3HoldSeconds += elapsedSeconds;

        if (CurrentTechnique?.Effect is WeaponTechniqueEffect.RangerHuntersDraw or
                WeaponTechniqueEffect.RangerPiercingDraw &&
            State == CombatState.ChargingHeavy)
        {
            _rangerChargeRatio = Math.Clamp(
                _rangerSkill1HoldSeconds /
                    RangerTuning.HunterDrawMaximumSeconds,
                0f,
                1f);
            _rangerDrawState = GetRangerDrawState(
                _rangerSkill1HoldSeconds);
        }
        else if (CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.RangerSkyfallMarker &&
            State == CombatState.ChargingHeavy)
        {
            _skyfallDistance = GetSkyfallDistance(
                _rangerSkill3HoldSeconds,
                held: true);
        }
    }

    private bool ResolveRangerActionInput(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_rangerInputMask == 7)
        {
            ClearRangerPendingInput();
            return TryStartTechnique(WeaponTechniqueInput.Ultimate);
        }

        if (_rangerChordRemaining > 0f)
            return false;

        int mask = _rangerInputMask;
        ClearRangerPendingInput();
        return mask switch
        {
            3 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill2),
            6 => TryStartTechnique(WeaponTechniqueInput.Skill2Skill3),
            5 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill3),
            1 => TryStartTechnique(WeaponTechniqueInput.Skill1),
            2 => TryStartTechnique(WeaponTechniqueInput.Skill2),
            4 => TryStartTechnique(WeaponTechniqueInput.Skill3),
            _ => false
        };
    }

    private void ClearRangerPendingInput()
    {
        _rangerInputMask = 0;
        _rangerChordRemaining = 0f;
    }

    private bool IsRangerMovementCompatibleAction()
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return false;

        return CurrentTechnique?.Effect is
            WeaponTechniqueEffect.RangerMobileShot or
            WeaponTechniqueEffect.RangerPiercingVolley or
            WeaponTechniqueEffect.RangerEvasiveShot or
            WeaponTechniqueEffect.RangerTwinFang or
            WeaponTechniqueEffect.RangerPhantomRetreat or
            WeaponTechniqueEffect.RangerBackstepShot or
            WeaponTechniqueEffect.RangerCounterShot or
            WeaponTechniqueEffect.RangerSlidingShot or
            WeaponTechniqueEffect.RangerPhantomBarrage or
            WeaponTechniqueEffect.RangerArrowStorm or
            WeaponTechniqueEffect.RangerAerialShot or
            WeaponTechniqueEffect.RangerSkyfallVolley or
            WeaponTechniqueEffect.RangerSkyfallShot;
    }

    private void UpdateRangerChargeState(
        float elapsedSeconds,
        CombatInput input)
    {
        WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;

        if (effect == WeaponTechniqueEffect.RangerPhantomHunt)
        {
            if (_stateElapsed >= RangerTuning.PhantomHuntIntroSeconds)
            {
                SetFeedback("I AM THE HUNTER!");
                StartTechniqueStage();
            }
            return;
        }

        if (effect == WeaponTechniqueEffect.RangerHeavensFury)
        {
            if (_stateElapsed >= RangerTuning.HeavensFuryChargeSeconds)
            {
                float focusSpent = Resources.SpendAllHunterFocus();
                _rangerUltimateFocusSnapshot = focusSpent;
                float focusRatio = RangerTuning.FocusRatio(focusSpent);
                _attackPowerMultiplier = 1f + focusRatio *
                    RangerTuning.HeavensFuryMaximumFocusDamageBonus;
                _poisePowerMultiplier = 1f + focusRatio *
                    RangerTuning.HeavensFuryMaximumFocusPoiseBonus;
                SetFeedback("HEAVEN'S FURY");
                StartTechniqueStage();
            }
            return;
        }

        if (effect is WeaponTechniqueEffect.RangerHuntersDraw or
            WeaponTechniqueEffect.RangerPiercingDraw)
        {
            if (input.Skill1Released || !input.Skill1Held ||
                _rangerSkill1HoldSeconds >=
                    RangerTuning.FullChargeStabilitySeconds)
                ReleaseRangerDraw();
            return;
        }

        if (effect == WeaponTechniqueEffect.RangerSkyfallMarker &&
            (input.Skill3Released || !input.Skill3Held))
        {
            _skyfallDistance = GetSkyfallDistance(
                _rangerSkill3HoldSeconds,
                held: false);
            StartTechniqueStage();
        }
    }

    private void ReleaseRangerDraw()
    {
        _rangerDrawState = GetRangerDrawState(_rangerSkill1HoldSeconds);
        if (_rangerDrawState == RangerDrawState.Perfect)
        {
            Stamina.SpendUpTo(RangerTuning.HunterDrawPerfectSurcharge);
            _attackPowerMultiplier = RangerTuning.PerfectDrawDamageMultiplier;
            _projectileSpeedMultiplier = RangerTuning.PerfectDrawSpeedMultiplier;
            _poisePowerMultiplier = RangerTuning.PerfectDrawPoiseMultiplier;
            SetFeedback("PERFECT DRAW");
        }
        else if (_rangerDrawState == RangerDrawState.Full)
        {
            Stamina.SpendUpTo(RangerTuning.HunterDrawFullSurcharge);
            _attackPowerMultiplier = RangerTuning.FullDrawDamageMultiplier;
            _projectileSpeedMultiplier = RangerTuning.FullDrawSpeedMultiplier;
            _poisePowerMultiplier = RangerTuning.FullDrawPoiseMultiplier;
            SetFeedback("FULL DRAW");
        }
        StartTechniqueStage();
    }

    private static RangerDrawState GetRangerDrawState(float holdSeconds)
    {
        if (holdSeconds >= RangerTuning.PerfectDrawStartSeconds &&
            holdSeconds <= RangerTuning.PerfectDrawEndSeconds)
            return RangerDrawState.Perfect;
        return holdSeconds >= RangerTuning.FullDrawStartSeconds
            ? RangerDrawState.Full
            : RangerDrawState.Quick;
    }

    private static SkyfallDistance GetSkyfallDistance(
        float holdSeconds,
        bool held)
    {
        if (!held && holdSeconds < RangerTuning.SkyfallTapThresholdSeconds)
            return SkyfallDistance.Medium;
        if (holdSeconds < RangerTuning.SkyfallNearEndSeconds)
            return SkyfallDistance.Near;
        return holdSeconds < RangerTuning.SkyfallMediumEndSeconds
            ? SkyfallDistance.Medium
            : SkyfallDistance.Far;
    }

    private void ResetRangerActionState(bool preserveWindows = false)
    {
        _rangerChargeRatio = 0f;
        _rangerWindrunnerDirection = 0f;
        _rangerDrawState = RangerDrawState.Quick;
        _rangerUltimateFocusSnapshot = 0f;
        _poisePowerMultiplier = 1f;
        ClearRangerPendingInput();
        if (!preserveWindows)
        {
            _rangerBackstepWindowRemaining = 0f;
            _rangerCounterWindowRemaining = 0f;
            _rangerPerfectDodgeActive = false;
        }
    }

    private void UpdateScytheInputState(float elapsedSeconds, CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.Scythe)
        {
            ClearScythePendingInput();
            return;
        }

        if (HasCommittedAction)
            return;

        int pressedMask = (input.Skill1Pressed ? 1 : 0) |
            (input.Skill2Pressed ? 2 : 0) |
            (input.Skill3Pressed ? 4 : 0);
        if (pressedMask != 0)
        {
            if (_scytheInputMask == 0)
                _scytheChordRemaining = ScytheTuning.ChordWindowSeconds;
            _scytheInputMask |= pressedMask;
            if (input.Skill1Pressed && input.DashHeld)
                _scytheDashSkill1Pending = true;
            if (input.Skill2Pressed && !_isGrounded)
                _scytheAirSkill2Pending = true;
        }

        if (_scytheInputMask != 0)
            _scytheChordRemaining = MathF.Max(
                0f, _scytheChordRemaining - elapsedSeconds);
    }

    private void UpdateDuelistInputState(float elapsedSeconds, CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.DualSwords)
        {
            ClearDuelistPendingInput();
            return;
        }

        _duelistSkill1Held = input.Skill1Held;
        _duelistSkill3Held = input.Skill3Held;
        if (HasCommittedAction)
            return;

        if (input.Skill1Pressed && input.DashHeld)
            _duelistDashSkill1Pending = true;
        if (input.Skill2Pressed && !_isGrounded)
            _duelistAirSkill2Pending = true;

        WeaponTechniqueInput? resolved = _duelistInputBuffer.Update(
            elapsedSeconds, input.Skill1Pressed, input.Skill2Pressed,
            input.Skill3Pressed);
        if (resolved.HasValue)
            _duelistResolvedInput = resolved;
    }

    private bool ResolveDuelistActionInput()
    {
        if (!_duelistResolvedInput.HasValue)
            return false;

        WeaponTechniqueInput input = _duelistResolvedInput.Value;
        bool dashJ = _duelistDashSkill1Pending;
        bool airK = _duelistAirSkill2Pending;
        ClearDuelistPendingInput();

        if (input == WeaponTechniqueInput.Ultimate)
            return TryStartTechnique(input);
        if (airK && input == WeaponTechniqueInput.Skill2)
            return TryStartTechnique(WeaponTechniqueInput.Skill2);
        if (input is WeaponTechniqueInput.Skill1Skill2 or
            WeaponTechniqueInput.Skill2Skill3 or
            WeaponTechniqueInput.Skill1Skill3)
            return TryStartTechnique(input);
        if (dashJ && input == WeaponTechniqueInput.Skill1)
            return TryStartTechnique(WeaponTechniqueInput.DashSkill);
        return TryStartTechnique(input);
    }

    private void ClearDuelistPendingInput()
    {
        _duelistInputBuffer.Clear();
        _duelistResolvedInput = null;
        _duelistDashSkill1Pending = false;
        _duelistAirSkill2Pending = false;
    }

    private void ResetDuelistTemporaryState()
    {
        ClearDuelistPendingInput();
        _duelistRiposteWindowRemaining = 0f;
        _duelistOverdriveRemaining = 0f;
        _duelistOverdriveCooldownRemaining = 0f;
        _duelistHeavenSplitterHoldSeconds = 0f;
        _duelistHeavenSplitterLevel = 1;
        _duelistUltimateTargetAvailable = false;
        _duelistUltimateProcessedHits = 0;
    }

	private void ResetKnightTemporaryState()
	{
		_knightInputBuffer.Clear();
		_knightDashSkill1Pending = false;
		_knightAirSkill2Pending = false;
		_knightForceFallingShield = false;
		_perfectGuardRewardRemaining = 0f;
		_perfectGuardFeedbackRemaining = 0f;
		_knightUltimateTargetAvailable = false;
		_knightFallingWasAirborne = false;
		_knightLandingPulseRemaining = 0f;
		_knightFinalLandingWasAirborne = false;
		_knightRiposteBuffered = false;
	}

    private void ReleaseDuelistHeavenSplitter()
    {
        _duelistHeavenSplitterLevel =
            _duelistHeavenSplitterHoldSeconds >=
                DuelistTuning.HeavenSplitterLevelThreeSeconds ? 3 :
            _duelistHeavenSplitterHoldSeconds >=
                DuelistTuning.HeavenSplitterLevelTwoSeconds ? 2 : 1;
        _attackPowerMultiplier = _duelistHeavenSplitterLevel switch
        {
            3 => 4.10f / 2.20f,
            2 => 3.10f / 2.20f,
            _ => 1f
        };
        SetFeedback($"HEAVEN SPLITTER LV {_duelistHeavenSplitterLevel}");
        StartTechniqueStage();
    }

    private bool ResolveScytheActionInput()
    {
        if (_scytheInputMask == 7)
        {
            ClearScythePendingInput();
            return TryStartTechnique(WeaponTechniqueInput.Ultimate);
        }
        if (_scytheChordRemaining > 0f)
            return false;

        int mask = _scytheInputMask;
        bool dashJ = _scytheDashSkill1Pending;
        bool airK = _scytheAirSkill2Pending;
        ClearScythePendingInput();
        if (airK)
            return TryStartTechnique(WeaponTechniqueInput.Skill2);
        return mask switch
        {
            3 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill2),
            6 => TryStartTechnique(WeaponTechniqueInput.Skill2Skill3),
            5 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill3),
            1 when dashJ => TryStartTechnique(WeaponTechniqueInput.DashSkill),
            1 => TryStartTechnique(WeaponTechniqueInput.Skill1),
            2 => TryStartTechnique(WeaponTechniqueInput.Skill2),
            4 => TryStartTechnique(WeaponTechniqueInput.Skill3),
            _ => false
        };
    }

    private void ClearScythePendingInput()
    {
        _scytheInputMask = 0;
        _scytheChordRemaining = 0f;
        _scytheDashSkill1Pending = false;
        _scytheAirSkill2Pending = false;
    }

    private void ResetScytheActionState()
    {
        ClearScythePendingInput();
        _scytheMoveDirection = 1f;
        _scytheShadowSeverEnabled = false;
        _scytheEnhancedThisTechnique = false;
    }

    private void UpdateSpellbladeInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.ArcaneWarStaff)
            return;

        if (input.Skill2Pressed)
            _spellbladeSkill2HoldSeconds = 0f;
        _spellbladeSkill2CurrentlyHeld = input.Skill2Held;
        if (input.Skill2Held)
            _spellbladeSkill2HoldSeconds += elapsedSeconds;

        if (IsRuneBrandCharging)
        {
            _runePlacementDistance = GetRunePlacementDistance(
                _spellbladeSkill2HoldSeconds,
                held: true);
        }
    }

    private void UpdateAncientStaffInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.ArcaneWarStaff)
        {
            ClearAncientStaffPendingInput();
            return;
        }

        _ancientStaffSkill2CurrentlyHeld = input.Skill2Held;
        if (HasCommittedAction || IsFourthVeilActive)
            return;

        int pressedMask = (input.Skill1Pressed ? 1 : 0) |
            (input.Skill2Pressed ? 2 : 0) |
            (input.Skill3Pressed ? 4 : 0);
        if (pressedMask != 0)
        {
            if (_ancientStaffInputMask == 0)
            {
                _ancientStaffChordRemaining =
                    AncientStaffTuning.ChordWindowSeconds;
            }
            _ancientStaffInputMask |= pressedMask;
            if (input.Skill1Pressed && input.DashHeld)
                _ancientStaffDashSkill1Pending = true;
            if (input.Skill2Pressed)
            {
                _ancientStaffDashSkill2Pending |= input.DashHeld;
                _ancientStaffAirSkill2Pending |= !_isGrounded;
            }
        }

        if (_ancientStaffInputMask != 0)
        {
            _ancientStaffChordRemaining = MathF.Max(
                0f,
                _ancientStaffChordRemaining - elapsedSeconds);
        }
    }

    private bool ResolveAncientStaffActionInput(CombatInput input)
    {
        if (IsFourthVeilActive)
        {
            ClearAncientStaffPendingInput();
            return false;
        }

        if (_ancientStaffInputMask == 7)
        {
            ClearAncientStaffPendingInput();
            return TryStartTechnique(WeaponTechniqueInput.Ultimate);
        }

        if (_ancientStaffChordRemaining > 0f)
            return false;

        int mask = _ancientStaffInputMask;
        bool dashSkill1 = _ancientStaffDashSkill1Pending;
        bool dashSkill2 = _ancientStaffDashSkill2Pending;
        bool airSkill2 = _ancientStaffAirSkill2Pending;
        ClearAncientStaffPendingInput();
        if (dashSkill2)
            return TryStartTechnique(WeaponTechniqueInput.DashSkill2);
        if (airSkill2)
            return TryStartTechnique(WeaponTechniqueInput.Skill2);
        return mask switch
        {
            3 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill2),
            6 => TryStartTechnique(WeaponTechniqueInput.Skill2Skill3),
            5 => TryStartTechnique(WeaponTechniqueInput.Skill1Skill3),
            1 when dashSkill1 =>
                TryStartTechnique(WeaponTechniqueInput.DashSkill),
            1 => TryStartTechnique(WeaponTechniqueInput.Skill1),
            2 => TryStartTechnique(WeaponTechniqueInput.Skill2),
            4 => TryStartTechnique(WeaponTechniqueInput.Skill3),
            _ => false
        };
    }

    private void ClearAncientStaffPendingInput()
    {
        _ancientStaffInputMask = 0;
        _ancientStaffChordRemaining = 0f;
        _ancientStaffDashSkill1Pending = false;
        _ancientStaffDashSkill2Pending = false;
        _ancientStaffAirSkill2Pending = false;
    }

    private void UpdateSpellbladeChargeState(CombatInput input)
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.SpellbladeRuneBrand)
            return;
        if (!input.Skill2Released && input.Skill2Held)
            return;

        _runePlacementDistance = GetRunePlacementDistance(
            _spellbladeSkill2HoldSeconds,
            held: false);
        StartTechniqueStage();
    }

    private static RunePlacementDistance GetRunePlacementDistance(
        float holdSeconds,
        bool held)
    {
        if (!held && holdSeconds < SpellbladeTuning.RuneBrandTapThresholdSeconds)
            return RunePlacementDistance.Medium;
        if (holdSeconds < SpellbladeTuning.RuneBrandNearEndSeconds)
            return RunePlacementDistance.Near;
        return holdSeconds < SpellbladeTuning.RuneBrandMediumEndSeconds
            ? RunePlacementDistance.Medium
            : RunePlacementDistance.Far;
    }

    private void ResetSpellbladeActionState()
    {
        _spellbladeSkill2HoldSeconds = 0f;
        _spellbladeSkill2CurrentlyHeld = false;
        _runePlacementDistance = RunePlacementDistance.Medium;
    }

    public float GetStaminaCost(AttackDefinition attack)
    {
        return (attack?.StaminaCost ?? 0f) * _weapon.StaminaCostModifier;
    }

    public int CalculateDamage(int baseDamage)
    {
        if (CurrentAttack == null)
            return 0;

        float duelistBuff = IsDuelistOverdriveActive &&
            CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.DuelistInfiniteStarburst
            ? 1f + DuelistTuning.OverdriveDamageBonus
            : 1f;
        int damage = Math.Max(0, (int)MathF.Round(
            CurrentAttack.CalculateDamage(baseDamage) *
                _attackPowerMultiplier * duelistBuff));

        if (IsSovereignThroneActive)
            damage = (int)MathF.Round(
                damage * AbyssRingTuning.ThroneDamageMultiplier);

        return damage;
    }

    public float GetTargetDamageMultiplier(float healthRatio)
    {
        return 1f;
    }

    public float CalculatePoiseDamage()
    {
        float powerMultiplier = _weapon.Family switch
        {
            WeaponFamily.HunterBow => _poisePowerMultiplier,
            WeaponFamily.GreatSword => _poisePowerMultiplier,
            _ => _attackPowerMultiplier
        };
        float poise = (CurrentAttack?.PoiseDamage ?? 0f) *
            _weapon.PoiseDamageModifier * powerMultiplier;
        if (IsSovereignThroneActive)
            poise *= AbyssRingTuning.ThronePoiseMultiplier;
        if (_weapon.Family == WeaponFamily.GreatSword &&
            CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.GreatswordFallingBastion &&
            _greatswordFallingPoiseBonus)
        {
            poise *= 1.18f;
        }
        return poise;
    }

    public float CalculateKnockback()
    {
        return (CurrentAttack?.Knockback ?? 0f) *
            _weapon.KnockbackModifier * _attackPowerMultiplier;
    }

    public float CalculateRange(AttackDefinition attack)
    {
        float momentumRange = 1f;
        if (IsSovereignThroneActive)
            momentumRange *= AbyssRingTuning.ThronePortalRangeMultiplier;
        return (attack?.Range ?? 1f) * _weapon.RangeModifier * momentumRange;
    }

    private void StartAttack(AttackDefinition attack, CombatState state)
    {
        CurrentAttack = attack;
        AttackId++;
        StartTimedState(state);
    }

    private void StartTimedState(CombatState state)
    {
        State = state;
        _stateElapsed = 0f;
    }

    private static Rectangle CreateBodyBounds(Vector2 playerPosition)
    {
        return new Rectangle(
            (int)(playerPosition.X - Player.BodyHurtboxWidth / 2f),
            (int)(playerPosition.Y - Player.BodyHurtboxHeight / 2f),
            (int)Player.BodyHurtboxWidth,
            (int)Player.BodyHurtboxHeight);
    }

    private static Rectangle CreateDefenseBounds(
        Vector2 playerPosition,
        FacingDirection facing)
    {
        return Player.CreateDefenseBounds(playerPosition, facing);
    }
}
