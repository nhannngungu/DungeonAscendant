using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// Reusable enemy melee timing with windup, one-hit active frames, recovery,
/// and block metadata.
/// </summary>
public sealed class MeleeAttack
{
    private float _phaseTimeRemaining;
    private bool _hitConsumed;

    public float Range { get; }
    public float CooldownSeconds { get; }
    public float WindupSeconds { get; }
    public float ActiveSeconds { get; }
    public float RecoverySeconds { get; }
    public bool IsBlockable { get; }
    public bool IsUnblockable { get; }
    public EnemyAttackPhase Phase { get; private set; } = EnemyAttackPhase.Ready;
    public bool IsActive => Phase == EnemyAttackPhase.Active;
    public bool IsTelegraphing => Phase == EnemyAttackPhase.Windup;
    public bool IsRecovering => Phase == EnemyAttackPhase.Recovery;
    public bool IsReady => Phase == EnemyAttackPhase.Ready;
    public float PhaseProgress
    {
        get
        {
            float duration = Phase switch
            {
                EnemyAttackPhase.Windup => WindupSeconds,
                EnemyAttackPhase.Active => ActiveSeconds,
                EnemyAttackPhase.Recovery => RecoverySeconds,
                EnemyAttackPhase.Cooldown => CooldownSeconds,
                _ => 0f
            };

            return duration <= 0f
                ? 0f
                : Math.Clamp(1f - _phaseTimeRemaining / duration, 0f, 1f);
        }
    }

    public MeleeAttack(
        float range = 75f,
        float cooldownSeconds = 0f,
        float windupSeconds = 0.18f,
        float activeSeconds = 0.09f,
        float recoverySeconds = 0.22f,
        bool isBlockable = true,
        bool isUnblockable = false)
    {
        Range = range;
        CooldownSeconds = cooldownSeconds;
        WindupSeconds = MathF.Max(0.01f, windupSeconds);
        ActiveSeconds = MathF.Max(0.01f, activeSeconds);
        RecoverySeconds = MathF.Max(0f, recoverySeconds);
        IsBlockable = isBlockable && !isUnblockable;
        IsUnblockable = isUnblockable;
    }

    public void Update(GameTime gameTime)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (Phase == EnemyAttackPhase.Ready)
            return;

        _phaseTimeRemaining -= elapsedSeconds;

        while (_phaseTimeRemaining <= 0f && Phase != EnemyAttackPhase.Ready)
            AdvancePhase();
    }

    public bool TryStart()
    {
        if (Phase == EnemyAttackPhase.Ready)
        {
            StartIfReady();
            return false;
        }

        return TryConsumeActiveHit();
    }

    public bool StartIfReady()
    {
        if (!IsReady)
            return false;

        Phase = EnemyAttackPhase.Windup;
        _phaseTimeRemaining = WindupSeconds;
        _hitConsumed = false;
        return true;
    }

    public bool TryConsumeActiveHit()
    {
        if (Phase != EnemyAttackPhase.Active || _hitConsumed)
            return false;

        _hitConsumed = true;
        return true;
    }

    public void Cancel()
    {
        Phase = CooldownSeconds > 0f
            ? EnemyAttackPhase.Cooldown
            : EnemyAttackPhase.Ready;
        _phaseTimeRemaining = CooldownSeconds;
        _hitConsumed = false;
    }

    private void AdvancePhase()
    {
        float overflow = -_phaseTimeRemaining;

        switch (Phase)
        {
            case EnemyAttackPhase.Windup:
                Phase = EnemyAttackPhase.Active;
                _phaseTimeRemaining = ActiveSeconds - overflow;
                break;
            case EnemyAttackPhase.Active:
                Phase = EnemyAttackPhase.Recovery;
                _phaseTimeRemaining = RecoverySeconds - overflow;
                break;
            case EnemyAttackPhase.Recovery:
                Phase = EnemyAttackPhase.Cooldown;
                _phaseTimeRemaining = CooldownSeconds - overflow;
                break;
            default:
                Phase = EnemyAttackPhase.Ready;
                _phaseTimeRemaining = 0f;
                break;
        }

        if (Phase == EnemyAttackPhase.Cooldown && _phaseTimeRemaining <= 0f)
        {
            Phase = EnemyAttackPhase.Ready;
            _phaseTimeRemaining = 0f;
        }
    }
}
