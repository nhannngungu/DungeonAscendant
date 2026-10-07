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

    private float _stateElapsed;
    private float _comboResetRemaining;
    private float _blockFeedbackRemaining;
    private bool _lightBuffered;
    private int _nextLightIndex;
    private float _attackPowerMultiplier = 1f;
    private float _projectileSpeedMultiplier = 1f;
    private WeaponDefinition _weapon = EquipmentCatalog.KnightLongSword;
    private ArmorDefinition _armor = EquipmentCatalog.KnightArmor;
    private EquipmentItem _armorItem = EquipmentCatalog.CreateStartingArmor();

    public CombatState State { get; private set; } = CombatState.Idle;
    public AttackDefinition CurrentAttack { get; private set; }
    public PlayerStamina Stamina { get; } = new();
    public int AttackId { get; private set; }
    public float StateElapsed => _stateElapsed;
    public float DodgeDirection { get; private set; } = 1f;
    public bool IsBlocking => State == CombatState.Blocking;
    public bool IsBlockInputHeld { get; private set; }
    public bool IsDodging => State == CombatState.Dodging;
    public bool IsGuardBroken => State == CombatState.Staggered;
    public bool CanBlock => _weapon.UsesShield;
    public WeaponMoveSet MoveSet => _weapon.MoveSet;
    public float DodgeSpeedMultiplier => _armorItem?.MoveSpeedModifier ??
        _armor.MoveSpeedModifier;
    public bool IsBlockFeedbackActive => _blockFeedbackRemaining > 0f;
    public bool IsDodgeInvulnerable => State == CombatState.Dodging &&
        _stateElapsed >= DodgeStartupSeconds &&
        _stateElapsed < DodgeStartupSeconds + DodgeInvulnerabilitySeconds;
    public bool IsAttackActive => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed >= CurrentAttack.StartupTime &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime;
    public bool IsAttackWindingUp => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime;
    public bool IsChargingHeavy => State == CombatState.ChargingHeavy;
    public float HeavyChargeProgress => !IsChargingHeavy ||
        MoveSet.HeavyChargeSeconds <= 0f
            ? 0f
            : Math.Clamp(_stateElapsed / MoveSet.HeavyChargeSeconds, 0f, 1f);
    public float AttackPowerMultiplier => _attackPowerMultiplier;
    public float ProjectileSpeedMultiplier => _projectileSpeedMultiplier;
    public float AttackAdvanceSpeed => CurrentAttack != null &&
        (State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
        _stateElapsed < CurrentAttack.StartupTime + CurrentAttack.ActiveTime
            ? CurrentAttack.ForwardMovementSpeed
            : 0f;
    public bool CanJump => State == CombatState.Idle ||
        State == CombatState.Moving ||
        State == CombatState.Jumping;
    public float MovementMultiplier => State == CombatState.LightAttack ||
        State == CombatState.HeavyAttack
        ? _weapon.AttackMovementMultiplier
        : CanJump ? 1f : 0f;

    public void ApplyLoadout(WeaponDefinition weapon, EquipmentItem armorItem)
    {
        _weapon = weapon ?? EquipmentCatalog.KnightLongSword;
        _armorItem = armorItem ?? EquipmentCatalog.CreateStartingArmor();
        _armor = _armorItem.ArmorDefinition ?? EquipmentCatalog.KnightArmor;

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
        IsBlockInputHeld = input.BlockHeld;
        float actionClockMultiplier = State == CombatState.LightAttack ||
            State == CombatState.HeavyAttack
                ? _weapon.AttackSpeedModifier
                : 1f;
        _stateElapsed += elapsedSeconds * actionClockMultiplier;
        _comboResetRemaining = MathF.Max(
            0f,
            _comboResetRemaining - elapsedSeconds);
        _blockFeedbackRemaining = MathF.Max(
            0f,
            _blockFeedbackRemaining - elapsedSeconds);
        if (_comboResetRemaining <= 0f &&
            State != CombatState.LightAttack)
        {
            _nextLightIndex = 0;
        }

        if (State == CombatState.LightAttack &&
            input.LightPressed &&
            CurrentAttack != null &&
            _stateElapsed >= CurrentAttack.StartupTime +
                CurrentAttack.ActiveTime * 0.5f)
        {
            _lightBuffered = true;
        }

        CompleteExpiredAction();

        if (State == CombatState.ChargingHeavy)
        {
            if (input.HeavyReleased || !input.HeavyHeld)
                ReleaseChargedHeavy();

            Stamina.Update(elapsedSeconds, regenerationAllowed: false);
            return;
        }

        bool canStartAction = CanStartAction();

        if (canStartAction && input.DodgePressed && isGrounded &&
            Stamina.TrySpend(DodgeStaminaCost * _armorItem.DodgeCostModifier))
        {
            DodgeDirection = MathF.Abs(input.HorizontalDirection) > 0.1f
                ? MathF.Sign(input.HorizontalDirection)
                : facing == FacingDirection.Left ? -1f : 1f;
            StartTimedState(CombatState.Dodging);
        }
        else if (canStartAction && input.HeavyPressed && isGrounded &&
            MoveSet.HasChargedHeavy &&
            Stamina.Current + .001f >= GetStaminaCost(MoveSet.Heavy))
        {
            CurrentAttack = null;
            _attackPowerMultiplier = 1f;
            _projectileSpeedMultiplier = 1f;
            StartTimedState(CombatState.ChargingHeavy);
        }
        else if (canStartAction && input.HeavyPressed && isGrounded &&
            Stamina.TrySpend(GetStaminaCost(MoveSet.Heavy)))
        {
            _attackPowerMultiplier = 1f;
            _projectileSpeedMultiplier = 1f;
            StartAttack(MoveSet.Heavy, CombatState.HeavyAttack);
        }
        else if (canStartAction && input.LightPressed)
        {
            StartLightAttack();
        }
        else if (canStartAction && input.BlockHeld && isGrounded && CanBlock)
        {
            StartTimedState(CombatState.Blocking);
        }
        else if (State == CombatState.Blocking && !input.BlockHeld)
        {
            StartTimedState(isGrounded ? CombatState.Idle : CombatState.Jumping);
        }

        bool regenerationAllowed = State != CombatState.Dodging &&
            State != CombatState.ChargingHeavy &&
            State != CombatState.HeavyAttack &&
            State != CombatState.Blocking;
        Stamina.Update(
            elapsedSeconds,
            regenerationAllowed,
            _armorItem.StaminaRegenModifier);
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

    public AttackResolution ResolveIncomingAttack(
        AttackContact contact,
        Vector2 playerPosition,
        FacingDirection facing)
    {
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

    public void OnDamaged(bool isAlive)
    {
        CurrentAttack = null;
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        _lightBuffered = false;
        _nextLightIndex = 0;
        IsBlockInputHeld = false;
        StartTimedState(isAlive ? CombatState.Hurt : CombatState.Dead);
    }

    public void CancelActions(bool restoreStamina = false)
    {
        CurrentAttack = null;
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        _lightBuffered = false;
        _nextLightIndex = 0;
        _comboResetRemaining = 0f;
        IsBlockInputHeld = false;
        StartTimedState(CombatState.Idle);

        if (restoreStamina)
            Stamina.Restore();
    }

    private void CompleteExpiredAction()
    {
        if ((State == CombatState.LightAttack || State == CombatState.HeavyAttack) &&
            CurrentAttack != null && _stateElapsed >= CurrentAttack.TotalTime)
        {
            if (State == CombatState.LightAttack && _lightBuffered)
            {
                _lightBuffered = false;
                StartLightAttack();
                return;
            }

            if (State == CombatState.LightAttack)
                _comboResetRemaining = ComboResetSeconds;

            CurrentAttack = null;
            StartTimedState(CombatState.Idle);
            return;
        }

        float duration = State switch
        {
            CombatState.Dodging => DodgeDurationSeconds,
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

    private void StartLightAttack()
    {
        AttackDefinition attack = MoveSet.GetLight(_nextLightIndex);

        if (!Stamina.TrySpend(GetStaminaCost(attack)))
        {
            _lightBuffered = false;
            CurrentAttack = null;
            StartTimedState(CombatState.Idle);
            return;
        }

        _nextLightIndex = (_nextLightIndex + 1) % MoveSet.ComboLength;
        _comboResetRemaining = ComboResetSeconds;
        _attackPowerMultiplier = 1f;
        _projectileSpeedMultiplier = 1f;
        StartAttack(attack, CombatState.LightAttack);
    }

    private void ReleaseChargedHeavy()
    {
        float charge = HeavyChargeProgress;

        if (!Stamina.TrySpend(GetStaminaCost(MoveSet.Heavy)))
        {
            StartTimedState(CombatState.Idle);
            return;
        }

        _attackPowerMultiplier = .85f + charge * .65f;
        _projectileSpeedMultiplier = .90f + charge * .45f;
        StartAttack(MoveSet.Heavy, CombatState.HeavyAttack);
    }

    public float GetStaminaCost(AttackDefinition attack)
    {
        return (attack?.StaminaCost ?? 0f) * _weapon.StaminaCostModifier;
    }

    public int CalculateDamage(int baseDamage)
    {
        return CurrentAttack == null
            ? 0
            : Math.Max(1, (int)MathF.Round(
                CurrentAttack.CalculateDamage(baseDamage) * _attackPowerMultiplier));
    }

    public float CalculatePoiseDamage()
    {
        return (CurrentAttack?.PoiseDamage ?? 0f) *
            _weapon.PoiseDamageModifier * _attackPowerMultiplier;
    }

    public float CalculateKnockback()
    {
        return (CurrentAttack?.Knockback ?? 0f) *
            _weapon.KnockbackModifier * _attackPowerMultiplier;
    }

    public float CalculateRange(AttackDefinition attack)
    {
        return (attack?.Range ?? 1f) * _weapon.RangeModifier;
    }

    private void StartAttack(AttackDefinition attack, CombatState state)
    {
        CurrentAttack = attack;
        AttackId++;
        _lightBuffered = false;
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
