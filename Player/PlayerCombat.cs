using System;
using DungeonAscendant.Combat;
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
    public bool CanJump => State == CombatState.Idle ||
        State == CombatState.Moving ||
        State == CombatState.Jumping;
    public float MovementMultiplier => State == CombatState.LightAttack
        ? 0.35f
        : CanJump ? 1f : 0f;

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
        _stateElapsed += elapsedSeconds;
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

        bool canStartAction = CanStartAction();

        if (canStartAction && input.DodgePressed && isGrounded &&
            Stamina.TrySpend(DodgeStaminaCost))
        {
            DodgeDirection = MathF.Abs(input.HorizontalDirection) > 0.1f
                ? MathF.Sign(input.HorizontalDirection)
                : facing == FacingDirection.Left ? -1f : 1f;
            StartTimedState(CombatState.Dodging);
        }
        else if (canStartAction && input.HeavyPressed && isGrounded &&
            Stamina.TrySpend(SwordAttackSet.Heavy.StaminaCost))
        {
            StartAttack(SwordAttackSet.Heavy, CombatState.HeavyAttack);
        }
        else if (canStartAction && input.LightPressed)
        {
            StartLightAttack();
        }
        else if (canStartAction && input.BlockHeld && isGrounded)
        {
            StartTimedState(CombatState.Blocking);
        }
        else if (State == CombatState.Blocking && !input.BlockHeld)
        {
            StartTimedState(isGrounded ? CombatState.Idle : CombatState.Jumping);
        }

        bool regenerationAllowed = State != CombatState.Dodging &&
            State != CombatState.HeavyAttack &&
            State != CombatState.Blocking;
        Stamina.Update(elapsedSeconds, regenerationAllowed);
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
            !contact.Unblockable && IsBlocking)
        {
            float staminaCost = MathF.Max(
                BlockMinimumStaminaCost,
                contact.Damage * BlockStaminaPerDamage);

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
        _lightBuffered = false;
        _nextLightIndex = 0;
        IsBlockInputHeld = false;
        StartTimedState(isAlive ? CombatState.Hurt : CombatState.Dead);
    }

    public void CancelActions(bool restoreStamina = false)
    {
        CurrentAttack = null;
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
        AttackDefinition attack = SwordAttackSet.GetLight(_nextLightIndex);
        _nextLightIndex = (_nextLightIndex + 1) % 3;
        _comboResetRemaining = ComboResetSeconds;
        StartAttack(attack, CombatState.LightAttack);
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
