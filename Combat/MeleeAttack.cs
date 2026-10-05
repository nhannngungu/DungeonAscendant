using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// Defines a short-range attack and its brief visual feedback state.
/// </summary>
public sealed class MeleeAttack
{
    private const float FeedbackDurationSeconds = 0.12f;
    private float _feedbackTimeRemaining;
    private float _cooldownTimeRemaining;

    public float Range { get; }
    public float CooldownSeconds { get; }
    public bool IsActive => _feedbackTimeRemaining > 0f;
    public bool IsReady => _cooldownTimeRemaining <= 0f;

    public MeleeAttack(
        float range = 75f,
        float cooldownSeconds = 0f)
    {
        Range = range;
        CooldownSeconds = cooldownSeconds;
    }

    public void Update(GameTime gameTime)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _feedbackTimeRemaining = MathF.Max(
            0f,
            _feedbackTimeRemaining - elapsedSeconds);
        _cooldownTimeRemaining = MathF.Max(
            0f,
            _cooldownTimeRemaining - elapsedSeconds);
    }

    public bool TryStart()
    {
        if (!IsReady)
            return false;

        _feedbackTimeRemaining = FeedbackDurationSeconds;
        _cooldownTimeRemaining = CooldownSeconds;
        return true;
    }

    public void Cancel()
    {
        _feedbackTimeRemaining = 0f;
    }
}
