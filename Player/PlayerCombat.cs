using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
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
    public const float OathDefensiveDurationSeconds = .85f;
    public const float OathPreventedDamageCap = 120f;
    public const float OathStoredForceCoefficient = .50f;
    public const float OathStoredForceCap = 60f;
    public const float PerfectGuardHitstopSeconds = .055f;

    private readonly CombatInputBuffer _inputBuffer = new();
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
    private bool _perfectPhantomStepAwarded;
    private int _duelistRepositionAttackId = -1;
    private float _ultimateMomentumSnapshot;
    private float _perfectPhantomFeedbackRemaining;
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
    private int _techniqueUseCounter;
    private int _currentTechniqueUseId;
    private float _poisePowerMultiplier = 1f;
    private FacingDirection _currentFacing = FacingDirection.Right;
    private float _raiderControlDirection = 1f;
    private float _currentHorizontalDirection;
    private float _spellbladeSkill2HoldSeconds;
    private bool _spellbladeSkill2CurrentlyHeld;
    private RunePlacementDistance _runePlacementDistance =
        RunePlacementDistance.Medium;
    private int _availableGroundRuneCount;
    private float _breakerSkill2HoldSeconds;
    private bool _breakerSkill2CurrentlyHeld;
    private BreakerPendulumStage _breakerPendulumStage =
        BreakerPendulumStage.StageOne;
    private float _breakerInertiaSnapshot;
    private float _breakerPoiseMultiplier = 1f;
    private bool _breakerRhythmPreserved;
    private int _breakerMaceSide = 1;
    private bool _breakerIronCrushFollowupBuffered;
    private bool _reckonerSkill2CurrentlyHeld;
    private bool _reckonerHarpoonWasHeldAfterStart;
    private float _reckonerOrbitHoldSeconds;
    private float _reckonerOrbitHitClock;
    private float _reckonerMomentumSnapshot;
    private int _reckonerCurveSide = 1;
    private int _reckonerHarpoonOutboundHits;
    private float _perfectGuardFeedbackRemaining;
    private float _perfectGuardRewardRemaining;
    private float _oathPreventedDamage;
    private float _hitstopRemaining;
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
    public float UltimateMomentumSnapshot => _ultimateMomentumSnapshot;
    public float RangerChargeRatio => _rangerChargeRatio;
    public RangerDrawState RangerDrawState => _rangerDrawState;
    public SkyfallDistance RangerSkyfallDistance => _skyfallDistance;
    public float RangerUltimateFocusSnapshot =>
        _rangerUltimateFocusSnapshot;
    public float RangerUltimateFocusRatio =>
        RangerTuning.FocusRatio(_rangerUltimateFocusSnapshot);
    public float RangerLockedDirection => _rangerLockedDirection;
    public int CurrentTechniqueUseId => _currentTechniqueUseId;
    public float RaiderControlDirection => _raiderControlDirection;
    public RunePlacementDistance RunePlacementDistance =>
        _runePlacementDistance;
    public int AvailableGroundRuneCount => _availableGroundRuneCount;
    public BreakerPendulumStage BreakerPendulumStage =>
        _breakerPendulumStage;
    public float BreakerInertiaSnapshot => _breakerInertiaSnapshot;
    public float BreakerInertiaSnapshotRatio =>
        BreakerTuning.InertiaRatio(_breakerInertiaSnapshot);
    public bool BreakerRhythmPreserved => _breakerRhythmPreserved;
    public int BreakerMaceSide => _breakerMaceSide;
    public float ReckonerOrbitHoldSeconds => _reckonerOrbitHoldSeconds;
    public int ReckonerOrbitStage =>
        ReckonerTuning.GetOrbitStage(_reckonerOrbitHoldSeconds);
    public float ReckonerMomentumSnapshot => _reckonerMomentumSnapshot;
    public float ReckonerMomentumSnapshotRatio =>
        ReckonerTuning.MomentumRatio(_reckonerMomentumSnapshot);
    public int ReckonerCurveSide => _reckonerCurveSide;
    public bool ReckonerHarpoonCanPierce =>
        _reckonerMomentumSnapshot >= ReckonerTuning.HarpoonPierceThreshold;
    public float RangerWindrunnerDirection =>
        (CurrentTechnique?.Effect is
            WeaponTechniqueEffect.RangerWindrunnerShot or
            WeaponTechniqueEffect.RangerPredatorsHorizon) &&
        _techniqueStageIndex == 0
            ? _rangerWindrunnerDirection
            : 0f;
    public bool IsRangerUltimateCharging =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerHeavensFury &&
        State == CombatState.ChargingHeavy;
    public bool IsRuneBrandCharging =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.SpellbladeRuneBrand &&
        State == CombatState.ChargingHeavy;
    public bool IsPendulumCharging =>
        CurrentTechnique?.Effect ==
            WeaponTechniqueEffect.BreakerPendulumSwing &&
        State == CombatState.ChargingHeavy;
    public bool IsRangerTrajectoryAiming =>
        _weapon.Family == WeaponFamily.HunterBow &&
        CurrentTechnique != null &&
        (State == CombatState.ChargingHeavy || CurrentTechnique.Effect is
            WeaponTechniqueEffect.RangerFallingStar or
            WeaponTechniqueEffect.RangerSkyfallMarker);
    public bool IsWindrunnerEvading =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerWindrunnerShot &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed >= .05f && _stateElapsed <= .19f;
    public float StateElapsed => _stateElapsed;
    public float DodgeDirection { get; private set; } = 1f;
    public WeaponTechnique CurrentTechnique { get; private set; }
    public string TechniqueFeedback { get; private set; } = string.Empty;
    public bool IsTechniqueFeedbackVisible => _techniqueFeedbackRemaining > 0f;
    public bool IsBlocking => State == CombatState.Blocking ||
        IsTechniqueGuardActive;
    public bool IsBlockInputHeld { get; private set; }
    public bool IsDodging => State == CombatState.Dodging;
    public bool IsGuardBroken => State == CombatState.Staggered;
    public bool CanBlock => _weapon.Family.ToOfficialFamily() == WeaponFamily.LongSword;
    public WeaponMoveSet MoveSet => _weapon.MoveSet;
    public float DodgeSpeedMultiplier => _armorItem?.MoveSpeedModifier ??
        _armor.MoveSpeedModifier;
    public bool IsBlockFeedbackActive => _blockFeedbackRemaining > 0f;
    public bool IsPerfectGuardFeedbackActive =>
        _perfectGuardFeedbackRemaining > 0f;
    public bool IsPerfectPhantomFeedbackActive =>
        _perfectPhantomFeedbackRemaining > 0f;
    public float OathPreventedDamage => _oathPreventedDamage;
    public float OathStoredForce => MathF.Min(
        OathStoredForceCap,
        _oathPreventedDamage * OathStoredForceCoefficient);
    public bool IsHitstopActive => _hitstopRemaining > 0f;
    public bool IsDodgeInvulnerable => State == CombatState.Dodging &&
        _stateElapsed >= DodgeStartupSeconds &&
        _stateElapsed < DodgeStartupSeconds + DodgeInvulnerabilitySeconds;
    public bool IsPhantomStepEvading =>
        CurrentTechnique?.Effect == WeaponTechniqueEffect.DuelistPhantomStep &&
        _techniqueSequenceIndex == 0 &&
        _stateElapsed <= .20f;
    public bool IsAttackActive => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed >= CurrentAttack.StartupTime &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime;
    public bool IsAttackWindingUp => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime;
    public bool IsChargingHeavy => State == CombatState.ChargingHeavy;
    public float HeavyChargeProgress => _weapon.Family == WeaponFamily.HunterBow
        ? CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerHeavensFury
            ? Math.Clamp(
                _stateElapsed / RangerTuning.HeavensFuryChargeSeconds,
                0f,
                1f)
            : _rangerChargeRatio
        : _weapon.Family == WeaponFamily.SpikedMace && IsPendulumCharging
            ? Math.Clamp(
                _breakerSkill2HoldSeconds /
                    BreakerTuning.PendulumMaximumSeconds,
                0f,
                1f)
            : 0f;
    public float AttackPowerMultiplier => _attackPowerMultiplier;
    public float ProjectileSpeedMultiplier => _projectileSpeedMultiplier;
    public float AttackAdvanceSpeed => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime
            ? CurrentAttack.ForwardMovementSpeed
            : 0f;
    public bool IsTechniqueGuardActive => CurrentTechnique != null &&
        CurrentAttack != null &&
        CurrentTechnique.ProvidesFrontalGuard &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime &&
        (CurrentTechnique.Effect != WeaponTechniqueEffect.OathStance ||
            _techniqueStageIndex == 0);
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
                    _weapon.Family == WeaponFamily.SpikedMace)
                    return .20f;
                return CanJump ? 1f : 0f;
            }

            return CurrentTechnique?.Effect switch
            {
                WeaponTechniqueEffect.DuelistBladeTempest => .28f,
                WeaponTechniqueEffect.DuelistHundredFangs => .22f,
                WeaponTechniqueEffect.DuelistFinalWaltz => 0f,
                WeaponTechniqueEffect.DuelistMirageCyclone => 0f,
                WeaponTechniqueEffect.DuelistPhantomStep => 0f,
                WeaponTechniqueEffect.DuelistAfterimageExecution => 0f,
                WeaponTechniqueEffect.RangerHuntersDraw => .45f,
                WeaponTechniqueEffect.RangerSkyfallMarker => .28f,
                WeaponTechniqueEffect.RangerPredatorsHorizon => .18f,
                WeaponTechniqueEffect.RangerHeavensFury => .05f,
                WeaponTechniqueEffect.RaiderCrowdCrusher => .05f,
                WeaponTechniqueEffect.RaiderWarbringersDominion => 0f,
                WeaponTechniqueEffect.RaiderHookingAxe => .12f,
                WeaponTechniqueEffect.RaiderExecutionersGrip => .10f,
                WeaponTechniqueEffect.RaiderSkullbreaker => .08f,
                WeaponTechniqueEffect.BreakerIronCrush => .12f,
                WeaponTechniqueEffect.BreakerPendulumSwing => .18f,
                WeaponTechniqueEffect.BreakerEarthbreaker => .05f,
                WeaponTechniqueEffect.BreakerBatteringRush => .08f,
                WeaponTechniqueEffect.BreakerTitansBackhand => .04f,
                WeaponTechniqueEffect.BreakerAnvilFall => .03f,
                WeaponTechniqueEffect.BreakerCataclysmWheel => .02f,
                WeaponTechniqueEffect.BreakerWorldbreaker => 0f,
                WeaponTechniqueEffect.ReckonerOrbitingMaelstrom => .38f,
                WeaponTechniqueEffect.ReckonerVortexSnare => .20f,
                WeaponTechniqueEffect.ReckonerChainsOfJudgment => .04f,
                _ => _weapon.AttackMovementMultiplier
            };
        }
    }

    public void ApplyLoadout(WeaponDefinition weapon, EquipmentItem armorItem)
    {
        _weapon = weapon ?? EquipmentCatalog.KnightLongSword;
        _armorItem = armorItem ?? EquipmentCatalog.CreateStartingArmor();
        _armor = _armorItem.ArmorDefinition ?? EquipmentCatalog.KnightArmor;
        Resources.SetFamily(_weapon.Family);
        _inputBuffer.Clear();
        _dodgeBuffered = false;

        if (!_weapon.UsesShield && State == CombatState.Blocking)
            StartTimedState(CombatState.Idle);
    }

    public void Update(
        GameTime gameTime,
        CombatInput input,
        bool isGrounded,
        FacingDirection facing)
    {
        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 20f);
        _currentFacing = facing;
        _currentHorizontalDirection = input.HorizontalDirection;

        if (_weapon.Family == WeaponFamily.WarAxe &&
            CurrentTechnique?.Effect == WeaponTechniqueEffect.RaiderCrowdCrusher &&
            MathF.Abs(input.HorizontalDirection) > .1f)
        {
            _raiderControlDirection = MathF.Sign(input.HorizontalDirection);
        }

        UpdateRangerInputState(elapsedSeconds, input);
        UpdateSpellbladeInputState(elapsedSeconds, input);
        UpdateBreakerInputState(elapsedSeconds, input);
        UpdateReckonerInputState(elapsedSeconds, input);

        if (_hitstopRemaining > 0f)
        {
            _hitstopRemaining = MathF.Max(0f, _hitstopRemaining - elapsedSeconds);
            return;
        }

        IsBlockInputHeld = input.BlockHeld;
        float actionClockMultiplier = State == CombatState.LightAttack ||
            State == CombatState.HeavyAttack
                ? _weapon.AttackSpeedModifier * GetMomentumSpeedMultiplier()
                : 1f;
        _stateElapsed += elapsedSeconds * actionClockMultiplier;
        _blockFeedbackRemaining = MathF.Max(
            0f,
            _blockFeedbackRemaining - elapsedSeconds);
        _techniqueFeedbackRemaining = MathF.Max(
            0f,
            _techniqueFeedbackRemaining - elapsedSeconds);
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
            UpdateSpellbladeChargeState(input);
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
                _armorItem.StaminaRegenModifier);
            return;
        }
        if (State == CombatState.ChargingHeavy &&
            _weapon.Family == WeaponFamily.SpikedMace)
        {
            UpdateBreakerChargeState(elapsedSeconds, input);
            Stamina.Update(
                elapsedSeconds,
                regenerationAllowed: false,
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
                WeaponTechniqueEffect.BreakerIronCrush)
            {
                float rhythmStart = CurrentAttack.StartupTime +
                    CurrentAttack.ActiveTime * .35f;
                float rhythmEnd = CurrentAttack.StartupTime +
                    CurrentAttack.ActiveTime + .20f;
                _breakerRhythmPreserved = _stateElapsed >= rhythmStart &&
                    _stateElapsed <= rhythmEnd;
            }
        }
        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.BreakerBatteringRush &&
            CurrentAttack != null && input.Skill1Pressed &&
            _stateElapsed >= CurrentAttack.StartupTime * .7f)
        {
            _breakerIronCrushFollowupBuffered = true;
            SetFeedback("IRON CRUSH READY");
        }

        if (CurrentTechnique?.Effect == WeaponTechniqueEffect.IronBastion &&
            _perfectGuardRewardRemaining > 0f &&
            (input.Skill1Pressed || input.Skill2Pressed || input.Skill3Pressed))
        {
            CurrentAttack = null;
            CurrentTechnique = null;
            _techniqueStageIndex = 0;
            StartTimedState(CombatState.Idle);
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

        if (immediatePhantomStep)
        {
            _inputBuffer.Clear();
            _dodgeBuffered = false;
            techniqueStarted = TryStartTechnique(
                WeaponTechniqueInput.Skill2);
        }
        else if (canStartAction && input.DashHeld && dashSkillPressed && isGrounded)
        {
            _inputBuffer.Clear();
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
        else if (!techniqueStarted && canStartAction && input.BlockHeld && isGrounded && CanBlock)
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
            State != CombatState.Blocking;
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

    public AttackResolution ResolveIncomingAttack(
        AttackContact contact,
        Vector2 playerPosition,
        FacingDirection facing)
    {
        if (IsWindrunnerEvading)
        {
            _windrunnerPerfectRewardRemaining = .75f;
            return AttackResolution.Ignored;
        }

        if (IsDodgeInvulnerable || IsPhantomStepEvading)
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

            if (effect == WeaponTechniqueEffect.OathStance &&
                IsTechniqueGuardActive && _techniqueStageIndex == 0)
            {
                _oathPreventedDamage = MathF.Min(
                    OathPreventedDamageCap,
                    _oathPreventedDamage + contact.Damage);
                _blockFeedbackRemaining = FeedbackSeconds;
                return AttackResolution.Blocked;
            }

            if (IsTechniqueGuardActive)
            {
                float normalCost = MathF.Max(
                    BlockMinimumStaminaCost,
                    contact.Damage * BlockStaminaPerDamage) *
                    _armorItem.GuardCostModifier;
                bool canPerfectGuard = effect is WeaponTechniqueEffect.IronBastion or
                    WeaponTechniqueEffect.GuardianReversal;
                bool perfectGuard = canPerfectGuard &&
                    _stateElapsed <= PerfectGuardWindowSeconds;
                float techniqueGuardCost = perfectGuard
                    ? MathF.Max(2f, normalCost * .25f)
                    : normalCost;

                if (Stamina.Current + .001f < techniqueGuardCost)
                {
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
                    _perfectGuardRewardRemaining = 1.2f;
                    _hitstopRemaining = PerfectGuardHitstopSeconds;

                    if (effect == WeaponTechniqueEffect.GuardianReversal)
                    {
                        _attackPowerMultiplier = MathF.Max(
                            _attackPowerMultiplier,
                            1.45f);
                        _stateElapsed = MathF.Max(
                            _stateElapsed,
                            CurrentAttack.StartupTime);
                    }

                    SetFeedback("PERFECT GUARD");
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

            Stamina.SpendUpTo(Stamina.Current);
            StartTimedState(CombatState.Staggered);
            return AttackResolution.GuardBroken;
        }

        if (!touchesBody)
            return AttackResolution.Ignored;

        return AttackResolution.Damaged;
    }

    public void OnDamaged(bool isAlive, int damage = int.MaxValue)
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
        _dodgeBuffered = false;
        ResetRangerActionState();
        ResetRaiderActionState();
        ResetSpellbladeActionState();
        ResetBreakerActionState();
        ResetReckonerActionState();
        Resources.OnOwnerDamaged(damage >= 20);
        IsBlockInputHeld = false;
        StartTimedState(isAlive ? CombatState.Hurt : CombatState.Dead);
    }

    public void CancelActions(bool restoreStamina = false)
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
        _dodgeBuffered = false;
        ResetRangerActionState();
        ResetRaiderActionState();
        ResetSpellbladeActionState();
        ResetBreakerActionState();
        ResetReckonerActionState();
        IsBlockInputHeld = false;
        StartTimedState(CombatState.Idle);

        if (restoreStamina)
            Stamina.Restore();
    }

    private void CompleteExpiredAction(bool isGrounded)
    {
        if ((State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
            CurrentAttack != null && _stateElapsed >= CurrentAttack.TotalTime)
        {
            RegisterBreakerSwingCompletion();

            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainLash &&
                _techniqueSequenceIndex + 1 < _effectiveTechniqueStageCount &&
                !_techniqueStageBuffered)
            {
                Resources.LoseChainMomentum(
                    ReckonerTuning.PoorRhythmMomentumLoss);
            }

            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.BreakerBatteringRush &&
                _breakerIronCrushFollowupBuffered)
            {
                CurrentAttack = null;
                CurrentTechnique = null;
                _techniqueStageIndex = 0;
                _effectiveTechniqueStageCount = 0;
                _techniqueSequenceIndex = 0;
                _techniqueStageBuffered = false;
                _breakerIronCrushFollowupBuffered = false;
                StartTimedState(CombatState.Idle);
                TryStartTechnique(WeaponTechniqueInput.Skill1);
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

                if (CurrentTechnique.Effect ==
                        WeaponTechniqueEffect.ReckonerVortexSnare &&
                    _techniqueStageIndex is >= 1 and <= 5)
                {
                    float consumed = Resources.ConsumeChainMomentum(
                        ReckonerTuning.VortexMomentumDrainPerPass);
                    if (consumed + .001f <
                        ReckonerTuning.VortexMomentumDrainPerPass)
                    {
                        _techniqueSequenceIndex =
                            _effectiveTechniqueStageCount - 1;
                        StartTechniqueStage();
                        return;
                    }
                }

                if (CurrentTechnique.Effect ==
                        WeaponTechniqueEffect.BreakerIronCrush &&
                    !_breakerRhythmPreserved)
                {
                    Resources.LoseBreakerInertia(
                        BreakerTuning.PoorRhythmLoss);
                }
                _techniqueSequenceIndex++;
                if (CurrentTechnique.Effect ==
                    WeaponTechniqueEffect.BreakerIronCrush)
                {
                    _breakerMaceSide = _techniqueSequenceIndex == 1 ? -1 : 0;
                }
                StartTechniqueStage();
                return;
            }

            if (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainLash &&
                _techniqueSequenceIndex == _effectiveTechniqueStageCount - 1)
            {
                Resources.AddChainMomentum(
                    ReckonerTuning.LashCompletionBonus);
            }

            CurrentAttack = null;
            CurrentTechnique = null;
            _techniqueStageIndex = 0;
            _effectiveTechniqueStageCount = 0;
            _techniqueSequenceIndex = 0;
            _techniqueStageBuffered = false;
            _poisePowerMultiplier = 1f;
            if (_weapon.Family == WeaponFamily.HunterBow)
                _rangerChargeRatio = 0f;
            if (_weapon.Family == WeaponFamily.SpikedMace)
                _breakerRhythmPreserved = false;
            StartTimedState(CombatState.Idle);
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
        WeaponTechnique technique = _weapon.CombatProfile.Get(techniqueInput);
        if (technique == null)
            return false;

        if (technique.Effect ==
                WeaponTechniqueEffect.SpellbladeArcaneConvergence &&
            _availableGroundRuneCount < 2)
        {
            SetFeedback("TWO RUNES REQUIRED");
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
        _reckonerMomentumSnapshot = _weapon.Family == WeaponFamily.ChainFlail
            ? Resources.ChainMomentum
            : 0f;
        _effectiveTechniqueStageCount = GetEffectiveStageCount(technique);
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        _techniqueStageBuffered = false;
        _duelistTechniqueGainAwarded = false;
        _perfectPhantomStepAwarded = false;
        _duelistRepositionAttackId = -1;
        _ultimateMomentumSnapshot = 0f;
        _rangerChargeRatio = 0f;
        _rangerWindrunnerDirection = 0f;
        _rangerDrawState = RangerDrawState.Quick;
        _poisePowerMultiplier = 1f;
        _breakerInertiaSnapshot = 0f;
        _breakerPoiseMultiplier = 1f;
        _breakerRhythmPreserved = false;
        _breakerIronCrushFollowupBuffered = false;
        _currentTechniqueUseId = ++_techniqueUseCounter;

        if (_weapon.Family == WeaponFamily.WarAxe)
        {
            _raiderControlDirection = _currentFacing == FacingDirection.Left
                ? -1f
                : 1f;
            if (technique.Effect == WeaponTechniqueEffect.RaiderCrowdCrusher)
            {
                if (MathF.Abs(_currentHorizontalDirection) > .1f)
                {
                    _raiderControlDirection =
                        MathF.Sign(_currentHorizontalDirection);
                }
                SetFeedback("A / D: THROW DIRECTION");
            }
        }

        if (_weapon.Family == WeaponFamily.LongSword &&
            _perfectGuardRewardRemaining > 0f &&
            technique.Effect != WeaponTechniqueEffect.IronBastion)
        {
            _attackPowerMultiplier = 1.15f;
            _perfectGuardRewardRemaining = 0f;
        }

        if (technique.Effect == WeaponTechniqueEffect.OathStance)
            _oathPreventedDamage = 0f;

        if (_weapon.Family == WeaponFamily.DualSwords)
        {
            Resources.MarkCombatActivity();

            if (technique.Effect == WeaponTechniqueEffect.DuelistFinalWaltz)
            {
                _ultimateMomentumSnapshot =
                    Resources.SpendAllDuelistMomentum();
                float ratio = DuelistTuning.GetRatio(
                    _ultimateMomentumSnapshot);
                _attackPowerMultiplier =
                    DuelistTuning.MinimumUltimateDamageMultiplier +
                    ratio * DuelistTuning.MaximumUltimateDamageBonus;
            }
        }

        else if (_weapon.Family == WeaponFamily.HunterBow)
        {
            Resources.MarkRangerCombatActivity();
            _rangerLockedDirection = _currentFacing == FacingDirection.Left
                ? -1f
                : 1f;

            if (technique.Effect == WeaponTechniqueEffect.RangerHeavensFury)
            {
                _rangerUltimateFocusSnapshot = Resources.HunterFocus;
                SetFeedback(technique.Name.ToUpperInvariant());
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }

            if (technique.Effect == WeaponTechniqueEffect.RangerHuntersDraw)
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
        }

        else if (_weapon.Family == WeaponFamily.ArcaneWarStaff &&
            technique.Effect == WeaponTechniqueEffect.SpellbladeRuneBrand)
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
        else if (_weapon.Family == WeaponFamily.SpikedMace)
        {
            ConfigureBreakerTechnique(technique);
            if (technique.Effect ==
                    WeaponTechniqueEffect.BreakerPendulumSwing &&
                _breakerSkill2CurrentlyHeld)
            {
                SetFeedback("PENDULUM STAGE 1");
                StartTimedState(CombatState.ChargingHeavy);
                return true;
            }
        }
        else if (_weapon.Family == WeaponFamily.ChainFlail)
        {
            ConfigureReckonerTechnique(technique);
        }

        SetFeedback(technique.Name.ToUpperInvariant());
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
        StartAttack(stage, state);
        _techniqueStageBuffered = false;
    }

    public void RegisterSuccessfulHit()
    {
        if (CurrentTechnique == null || _resourceGainAttackId == AttackId)
            return;

        _resourceGainAttackId = AttackId;

        if (_weapon.Family == WeaponFamily.SpikedMace)
        {
            RegisterBreakerHit();
            return;
        }

        if (_weapon.Family == WeaponFamily.ChainFlail)
        {
            RegisterReckonerHit();
            return;
        }

        if (_weapon.Family != WeaponFamily.DualSwords)
        {
            Resources.RegisterHit(CurrentTechnique.ResourceGainOnHit);
            return;
        }

        if (CurrentTechnique.Effect == WeaponTechniqueEffect.DuelistFinalWaltz)
            return;

        float gain = CurrentTechnique.ResourceGainOnHit;
        if (CurrentTechnique.Id == "twin-fang" &&
            _techniqueStageIndex == CurrentTechnique.StageCount - 1)
        {
            gain += DuelistTuning.TwinFangFinisherBonus;
        }

        bool oncePerSequence = CurrentTechnique.Effect is
            WeaponTechniqueEffect.DuelistAfterimageExecution or
            WeaponTechniqueEffect.DuelistHundredFangs or
            WeaponTechniqueEffect.DuelistMirageCyclone;
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
            _rangerFocusHitAttackId = sourceAttackId;
        }

        if (gain > 0f)
            Resources.AddHunterFocus(gain);
        else
            Resources.MarkRangerCombatActivity();
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

    public void RegisterRaiderImpactFeedback(bool heavyImpact)
    {
        if (_weapon.Family != WeaponFamily.WarAxe)
            return;

        float duration = heavyImpact ? .052f : .026f;
        _hitstopRemaining = MathF.Max(_hitstopRemaining, duration);
    }

    public void RegisterBreakerImpactFeedback(bool worldbreaker = false)
    {
        if (_weapon.Family != WeaponFamily.SpikedMace)
            return;

        float duration = worldbreaker ? .11f : .058f;
        _hitstopRemaining = MathF.Max(_hitstopRemaining, duration);
    }

    private float GetMomentumSpeedMultiplier()
    {
        if (_weapon.Family == WeaponFamily.DualSwords)
        {
            return 1f + Resources.DuelistMomentumRatio *
                DuelistTuning.MaximumAttackSpeedBonus;
        }
        if (_weapon.Family == WeaponFamily.ChainFlail)
        {
            float ratio = CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainsOfJudgment
                ? ReckonerMomentumSnapshotRatio
                : Resources.ChainMomentumRatio;
            return 1f + ratio * ReckonerTuning.MaximumHeadSpeedBonus;
        }
        return 1f;
    }

    public void RegisterPerfectPhantomStep()
    {
        if (!IsPhantomStepEvading || _perfectPhantomStepAwarded)
            return;

        _perfectPhantomStepAwarded = true;
        Resources.AddDuelistMomentum(
            DuelistTuning.PerfectPhantomStepGain);
        _perfectPhantomFeedbackRemaining = .45f;
        SetFeedback("PERFECT PHANTOM STEP");
    }

    public void RegisterAggressiveReposition()
    {
        if (_weapon.Family != WeaponFamily.DualSwords ||
            CurrentTechnique == null ||
            CurrentTechnique.Effect ==
                WeaponTechniqueEffect.DuelistFinalWaltz ||
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

        if (_weapon.Family == WeaponFamily.ChainFlail)
        {
            return technique.Effect switch
            {
                WeaponTechniqueEffect.ReckonerVortexSnare =>
                    ReckonerTuning.GetVortexPassCount(
                        _reckonerMomentumSnapshot) + 2,
                WeaponTechniqueEffect.ReckonerChainsOfJudgment =>
                    ReckonerTuning.GetUltimateCirclePassCount(
                        _reckonerMomentumSnapshot) + 3,
                _ => technique.StageCount
            };
        }

        if (_weapon.Family != WeaponFamily.DualSwords)
            return technique.StageCount;

        float momentum = Resources.DuelistMomentum;
        return technique.Effect switch
        {
            WeaponTechniqueEffect.DuelistBladeTempest =>
                DuelistTuning.GetBladeTempestHitCount(momentum),
            WeaponTechniqueEffect.DuelistHundredFangs =>
                DuelistTuning.GetHundredFangsHitCount(momentum),
            WeaponTechniqueEffect.DuelistFinalWaltz =>
                DuelistTuning.GetFinalWaltzHitCount(momentum),
            _ => technique.StageCount
        };
    }

    private static int GetPhysicalStageIndex(
        WeaponTechnique technique,
        int sequenceIndex,
        int effectiveCount)
    {
        if (technique == null)
            return 0;

        if (technique.Effect == WeaponTechniqueEffect.ReckonerVortexSnare &&
            sequenceIndex == effectiveCount - 1)
        {
            return technique.StageCount - 1;
        }
        if (technique.Effect ==
                WeaponTechniqueEffect.ReckonerChainsOfJudgment)
        {
            if (sequenceIndex == effectiveCount - 2)
                return technique.StageCount - 2;
            if (sequenceIndex == effectiveCount - 1)
                return technique.StageCount - 1;
        }

        bool preserveFinisher = technique.Effect is
            WeaponTechniqueEffect.DuelistHundredFangs or
            WeaponTechniqueEffect.DuelistFinalWaltz;
        if (preserveFinisher &&
            effectiveCount < technique.StageCount &&
            sequenceIndex == effectiveCount - 1)
        {
            return technique.StageCount - 1;
        }

        return Math.Clamp(sequenceIndex, 0, technique.StageCount - 1);
    }

    private void SetFeedback(string message)
    {
        TechniqueFeedback = message ?? string.Empty;
        _techniqueFeedbackRemaining = .85f;
    }

    private void UpdateRangerInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.HunterBow)
            return;

        if (input.Skill1Pressed)
            _rangerSkill1HoldSeconds = 0f;
        if (input.Skill3Pressed)
            _rangerSkill3HoldSeconds = 0f;

        _rangerSkill1CurrentlyHeld = input.Skill1Held;
        _rangerSkill3CurrentlyHeld = input.Skill3Held;
        if (input.Skill1Held)
            _rangerSkill1HoldSeconds += elapsedSeconds;
        if (input.Skill3Held)
            _rangerSkill3HoldSeconds += elapsedSeconds;

        if (CurrentTechnique?.Effect == WeaponTechniqueEffect.RangerHuntersDraw &&
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

    private void UpdateRangerChargeState(
        float elapsedSeconds,
        CombatInput input)
    {
        WeaponTechniqueEffect effect = CurrentTechnique?.Effect ??
            WeaponTechniqueEffect.None;

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

        if (effect == WeaponTechniqueEffect.RangerHuntersDraw)
        {
            if (input.Skill1Released || !input.Skill1Held)
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
            Resources.AddHunterFocus(RangerTuning.PerfectDrawGain);
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

    private void ResetRangerActionState()
    {
        _rangerChargeRatio = 0f;
        _rangerWindrunnerDirection = 0f;
        _rangerDrawState = RangerDrawState.Quick;
        _rangerUltimateFocusSnapshot = 0f;
        _poisePowerMultiplier = 1f;
    }

    private void ResetRaiderActionState()
    {
        _raiderControlDirection = _currentFacing == FacingDirection.Left
            ? -1f
            : 1f;
    }

    private void ConfigureBreakerTechnique(WeaponTechnique technique)
    {
        _breakerInertiaSnapshot = Resources.BreakerInertia;
        float ratio = BreakerTuning.InertiaRatio(_breakerInertiaSnapshot);
        Resources.MarkCombatActivity();

        switch (technique.Effect)
        {
            case WeaponTechniqueEffect.BreakerIronCrush:
                _breakerPoiseMultiplier = 1f + ratio * .18f;
                _breakerMaceSide = 1;
                break;
            case WeaponTechniqueEffect.BreakerPendulumSwing:
                _breakerPendulumStage = BreakerTuning.GetPendulumStage(
                    _breakerSkill2HoldSeconds);
                ApplyPendulumScaling();
                break;
            case WeaponTechniqueEffect.BreakerEarthbreaker:
                Resources.ConsumeBreakerInertia(
                    BreakerTuning.EarthbreakerConsumption);
                _attackPowerMultiplier = 1f + ratio *
                    BreakerTuning.EarthbreakerMaximumDamageBonus;
                _breakerPoiseMultiplier = 1f + ratio *
                    BreakerTuning.EarthbreakerMaximumPoiseBonus;
                _breakerMaceSide = 0;
                break;
            case WeaponTechniqueEffect.BreakerBatteringRush:
                _breakerPoiseMultiplier = 1f + ratio * .22f;
                _breakerMaceSide = -1;
                break;
            case WeaponTechniqueEffect.BreakerTitansBackhand:
                Resources.ConsumeBreakerInertia(
                    BreakerTuning.TitanBackhandConsumption);
                _attackPowerMultiplier = 1f + ratio *
                    BreakerTuning.TitanBackhandMaximumDamageBonus;
                _breakerPoiseMultiplier = 1f + ratio *
                    BreakerTuning.TitanBackhandMaximumPoiseBonus;
                _breakerMaceSide = -_breakerMaceSide;
                break;
            case WeaponTechniqueEffect.BreakerAnvilFall:
                Resources.ConsumeBreakerInertia(
                    BreakerTuning.AnvilFallConsumption);
                _attackPowerMultiplier = 1f + ratio *
                    BreakerTuning.AnvilFallMaximumDamageBonus;
                _breakerPoiseMultiplier = 1f + ratio *
                    BreakerTuning.AnvilFallMaximumPoiseBonus;
                _breakerMaceSide = 0;
                break;
            case WeaponTechniqueEffect.BreakerCataclysmWheel:
                Resources.ConsumeBreakerInertiaRatio(
                    BreakerTuning.CataclysmConsumptionRatio);
                _attackPowerMultiplier = 1f + ratio * .12f;
                _breakerPoiseMultiplier = 1f + ratio * .30f;
                _breakerMaceSide = 0;
                break;
            case WeaponTechniqueEffect.BreakerWorldbreaker:
                Resources.SpendAllBreakerInertia();
                _attackPowerMultiplier = 1f + ratio *
                    BreakerTuning.WorldbreakerMaximumDamageBonus;
                _breakerPoiseMultiplier = 1f + ratio *
                    BreakerTuning.WorldbreakerMaximumPoiseBonus;
                _breakerMaceSide = 0;
                break;
        }
    }

    private void UpdateBreakerInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.SpikedMace)
            return;

        if (input.Skill2Pressed)
            _breakerSkill2HoldSeconds = 0f;
        _breakerSkill2CurrentlyHeld = input.Skill2Held;
        if (input.Skill2Held)
            _breakerSkill2HoldSeconds = MathF.Min(
                BreakerTuning.PendulumMaximumSeconds,
                _breakerSkill2HoldSeconds + elapsedSeconds);

        if (!IsPendulumCharging)
            return;

        BreakerPendulumStage stage = BreakerTuning.GetPendulumStage(
            _breakerSkill2HoldSeconds);
        if (stage != _breakerPendulumStage)
        {
            _breakerPendulumStage = stage;
            SetFeedback($"PENDULUM STAGE {(int)stage}");
        }
    }

    private void UpdateBreakerChargeState(
        float elapsedSeconds,
        CombatInput input)
    {
        Resources.AddBreakerInertia(
            BreakerTuning.PendulumInertiaGainPerSecond * elapsedSeconds);
        Stamina.SpendUpTo(
            BreakerTuning.PendulumStaminaDrainPerSecond * elapsedSeconds *
            _weapon.StaminaCostModifier);

        bool maximumReached = _breakerSkill2HoldSeconds >=
            BreakerTuning.PendulumMaximumSeconds;
        if (!maximumReached && Stamina.Current > 0f &&
            !input.Skill2Released && input.Skill2Held)
            return;

        _breakerPendulumStage = BreakerTuning.GetPendulumStage(
            _breakerSkill2HoldSeconds);
        ApplyPendulumScaling();
        SetFeedback($"PENDULUM RELEASE {(int)_breakerPendulumStage}");
        StartTechniqueStage();
    }

    private void ApplyPendulumScaling()
    {
        float inertiaRatio = Resources.BreakerInertiaRatio;
        float stageDamage = _breakerPendulumStage switch
        {
            BreakerPendulumStage.StageTwo => 1.16f,
            BreakerPendulumStage.StageThree => 1.38f,
            _ => 1f
        };
        float stagePoise = _breakerPendulumStage switch
        {
            BreakerPendulumStage.StageTwo => 1.28f,
            BreakerPendulumStage.StageThree => 1.62f,
            _ => 1f
        };
        _attackPowerMultiplier = stageDamage;
        _breakerPoiseMultiplier = stagePoise * (1f + inertiaRatio * .18f);
    }

    private void RegisterBreakerHit()
    {
        float gain = CurrentTechnique.Effect switch
        {
            WeaponTechniqueEffect.BreakerIronCrush =>
                _techniqueStageIndex switch
                {
                    0 => BreakerTuning.IronCrushHitOneGain,
                    1 => BreakerTuning.IronCrushHitTwoGain,
                    _ => BreakerTuning.IronCrushHitThreeGain
                } * (_techniqueStageIndex == 0
                    ? 1f
                    : _breakerRhythmPreserved
                        ? BreakerTuning.RhythmGainMultiplier
                        : BreakerTuning.PoorRhythmGainMultiplier),
            WeaponTechniqueEffect.BreakerPendulumSwing =>
                _breakerPendulumStage switch
                {
                    BreakerPendulumStage.StageTwo =>
                        BreakerTuning.PendulumStageTwoHitGain,
                    BreakerPendulumStage.StageThree =>
                        BreakerTuning.PendulumStageThreeHitGain,
                    _ => BreakerTuning.PendulumStageOneHitGain
                },
            WeaponTechniqueEffect.BreakerBatteringRush =>
                BreakerTuning.BatteringRushHitGain,
            _ => 0f
        };

        if (gain > 0f)
            Resources.AddBreakerInertia(gain);
        else
            Resources.MarkCombatActivity();
    }

    private void RegisterBreakerSwingCompletion()
    {
        if (_weapon.Family != WeaponFamily.SpikedMace ||
            CurrentTechnique == null || CurrentAttack == null)
            return;

        if (CurrentTechnique.Effect is
            WeaponTechniqueEffect.BreakerIronCrush or
            WeaponTechniqueEffect.BreakerPendulumSwing or
            WeaponTechniqueEffect.BreakerBatteringRush)
        {
            Resources.AddBreakerInertia(BreakerTuning.SwingCompletionGain);
        }
    }

    private void ResetBreakerActionState()
    {
        _breakerSkill2HoldSeconds = 0f;
        _breakerSkill2CurrentlyHeld = false;
        _breakerPendulumStage = BreakerPendulumStage.StageOne;
        _breakerInertiaSnapshot = 0f;
        _breakerPoiseMultiplier = 1f;
        _breakerRhythmPreserved = false;
        _breakerIronCrushFollowupBuffered = false;
    }

    private void ConfigureReckonerTechnique(WeaponTechnique technique)
    {
        _reckonerOrbitHoldSeconds = 0f;
        _reckonerOrbitHitClock = 0f;
        _reckonerHarpoonOutboundHits = 0;
        _reckonerHarpoonWasHeldAfterStart = false;
        float facingSign = _currentFacing == FacingDirection.Left ? -1f : 1f;
        _reckonerCurveSide = MathF.Abs(_currentHorizontalDirection) > .1f
            ? (int)MathF.Sign(_currentHorizontalDirection * facingSign)
            : 1;
        Resources.MarkCombatActivity();

        if (technique.Effect ==
            WeaponTechniqueEffect.ReckonerChainsOfJudgment)
        {
            Resources.SpendAllChainMomentum();
        }
    }

    private void UpdateReckonerInputState(
        float elapsedSeconds,
        CombatInput input)
    {
        if (_weapon.Family != WeaponFamily.ChainFlail)
            return;

        _reckonerSkill2CurrentlyHeld = input.Skill2Held;
        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ReckonerChainHarpoon &&
            _techniqueStageIndex == 0)
        {
            if (input.Skill3Held)
                _reckonerHarpoonWasHeldAfterStart = true;
            if (_reckonerHarpoonWasHeldAfterStart &&
                (input.Skill3Released || !input.Skill3Held))
            {
                RequestReckonerHarpoonReturn();
                return;
            }
        }
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ReckonerOrbitingMaelstrom ||
            _techniqueStageIndex != 0 || CurrentAttack == null)
        {
            return;
        }

        bool canMaintain = _reckonerSkill2CurrentlyHeld &&
            _reckonerOrbitHoldSeconds < ReckonerTuning.OrbitMaximumSeconds &&
            Stamina.Current > .001f;
        if (!canMaintain)
        {
            BeginReckonerOrbitRelease();
            return;
        }

        float drain = ReckonerTuning.OrbitStaminaDrainPerSecond *
            MathF.Max(0f, elapsedSeconds);
        float spent = Stamina.SpendUpTo(drain);
        if (spent + .001f < drain)
        {
            BeginReckonerOrbitRelease();
            return;
        }

        _reckonerOrbitHoldSeconds = MathF.Min(
            ReckonerTuning.OrbitMaximumSeconds,
            _reckonerOrbitHoldSeconds + elapsedSeconds);
        float uninterruptedRatio = Math.Clamp(
            _reckonerOrbitHoldSeconds / ReckonerTuning.OrbitMaximumSeconds,
            0f,
            1f);
        Resources.AddChainMomentum(
            (ReckonerTuning.OrbitBuildPerSecond +
             ReckonerTuning.OrbitUninterruptedBonusPerSecond *
                uninterruptedRatio) * elapsedSeconds);

        _reckonerOrbitHitClock += elapsedSeconds;
        float interval = ReckonerTuning.GetOrbitHitInterval(
            ReckonerOrbitStage);
        if (_reckonerOrbitHitClock >= interval)
        {
            _reckonerOrbitHitClock -= interval;
            AttackId++;
        }
    }

    private void BeginReckonerOrbitRelease()
    {
        if (CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ReckonerOrbitingMaelstrom ||
            _techniqueStageIndex != 0)
        {
            return;
        }

        _techniqueSequenceIndex = 1;
        StartTechniqueStage();
    }

    private void RegisterReckonerHit()
    {
        if (CurrentTechnique == null)
            return;

        float gain = CurrentTechnique.Effect switch
        {
            WeaponTechniqueEffect.ReckonerChainLash =>
                ReckonerTuning.LashHitGain,
            WeaponTechniqueEffect.ReckonerOrbitingMaelstrom =>
                ReckonerTuning.OrbitHitGain,
            WeaponTechniqueEffect.ReckonerChainHarpoon => 6f,
            WeaponTechniqueEffect.ReckonerReapersPassage =>
                ReckonerTuning.ReapersPassageHitGain,
            WeaponTechniqueEffect.ReckonerCrescentRequiem => 6f,
            WeaponTechniqueEffect.ReckonerSerpentsFang => 8f,
            WeaponTechniqueEffect.ReckonerVortexSnare => 0f,
            WeaponTechniqueEffect.ReckonerChainsOfJudgment => 0f,
            _ => CurrentTechnique.ResourceGainOnHit
        };

        if (gain > 0f)
            Resources.AddChainMomentum(gain);
        else
            Resources.MarkCombatActivity();
    }

    public bool RegisterReckonerHarpoonOutboundTarget(bool heavyOrBoss)
    {
        if (_weapon.Family != WeaponFamily.ChainFlail ||
            CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ReckonerChainHarpoon ||
            _techniqueStageIndex != 0)
        {
            return false;
        }

        _reckonerHarpoonOutboundHits++;
        bool returnNow = heavyOrBoss || !ReckonerHarpoonCanPierce ||
            _reckonerHarpoonOutboundHits >=
                ReckonerTuning.HarpoonMaximumLightTargets;
        if (returnNow)
            RequestReckonerHarpoonReturn();
        return returnNow;
    }

    public void RequestReckonerHarpoonReturn()
    {
        if (_weapon.Family != WeaponFamily.ChainFlail ||
            CurrentTechnique?.Effect !=
                WeaponTechniqueEffect.ReckonerChainHarpoon ||
            _techniqueStageIndex != 0)
        {
            return;
        }

        _techniqueSequenceIndex = 1;
        StartTechniqueStage();
    }

    private void ResetReckonerActionState()
    {
        _reckonerSkill2CurrentlyHeld = false;
        _reckonerHarpoonWasHeldAfterStart = false;
        _reckonerOrbitHoldSeconds = 0f;
        _reckonerOrbitHitClock = 0f;
        _reckonerMomentumSnapshot = 0f;
        _reckonerCurveSide = 1;
        _reckonerHarpoonOutboundHits = 0;
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

        int damage = Math.Max(0, (int)MathF.Round(
            CurrentAttack.CalculateDamage(baseDamage) * _attackPowerMultiplier));

        if (CurrentTechnique?.Effect == WeaponTechniqueEffect.OathStance &&
            _techniqueStageIndex == CurrentTechnique.StageCount - 1)
        {
            damage += (int)MathF.Round(OathStoredForce);
        }

        if (_weapon.Family == WeaponFamily.ChainFlail)
        {
            float ratio = CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainsOfJudgment
                ? ReckonerMomentumSnapshotRatio
                : Resources.ChainMomentumRatio;
            float bonus = CurrentTechnique?.Effect switch
            {
                WeaponTechniqueEffect.ReckonerChainHarpoon
                    when _techniqueStageIndex == 1 => .12f * ratio,
                WeaponTechniqueEffect.ReckonerSerpentsFang => .10f * ratio,
                WeaponTechniqueEffect.ReckonerChainsOfJudgment
                    when _techniqueStageIndex == 7 =>
                        ReckonerTuning.UltimateMaximumDamageBonus * ratio,
                WeaponTechniqueEffect.ReckonerChainsOfJudgment => .12f * ratio,
                _ => 0f
            };
            damage = (int)MathF.Round(damage * (1f + bonus));
        }

        return damage;
    }

    public float CalculatePoiseDamage()
    {
        float powerMultiplier = _weapon.Family switch
        {
            WeaponFamily.HunterBow => _poisePowerMultiplier,
            WeaponFamily.SpikedMace => _breakerPoiseMultiplier,
            _ => _attackPowerMultiplier
        };
        float poise = (CurrentAttack?.PoiseDamage ?? 0f) *
            _weapon.PoiseDamageModifier * powerMultiplier;
        if (CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ReckonerChainsOfJudgment &&
            _techniqueStageIndex >= 6)
        {
            poise *= 1f + ReckonerMomentumSnapshotRatio * .30f;
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
        float momentumRange = _weapon.Family == WeaponFamily.ChainFlail
            ? 1f + (CurrentTechnique?.Effect ==
                    WeaponTechniqueEffect.ReckonerChainsOfJudgment
                ? ReckonerMomentumSnapshotRatio
                : Resources.ChainMomentumRatio) *
                ReckonerTuning.MaximumRadiusBonus
            : 1f;
        if (_weapon.Family == WeaponFamily.SpikedMace &&
            CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.BreakerPendulumSwing)
        {
            momentumRange *= _breakerPendulumStage switch
            {
                BreakerPendulumStage.StageTwo => 1.10f,
                BreakerPendulumStage.StageThree => 1.20f,
                _ => 1f
            };
        }
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
