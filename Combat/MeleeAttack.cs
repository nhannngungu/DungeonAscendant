using System;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// Defines a short-range attack and its brief visual feedback state.
/// </summary>
public sealed class MeleeAttack
{
    private const float FeedbackDurationSeconds = 0.12f;
    private float _feedbackTimeRemaining;

    public int Damage { get; }
    public float Range { get; }
    public bool IsActive => _feedbackTimeRemaining > 0f;

    public MeleeAttack(int damage = 25, float range = 75f)
    {
        Damage = damage;
        Range = range;
    }

    public void Update(GameTime gameTime)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _feedbackTimeRemaining = MathF.Max(
            0f,
            _feedbackTimeRemaining - elapsedSeconds);
    }

    public bool TryHit(Vector2 attackerPosition, Goblin target)
    {
        _feedbackTimeRemaining = FeedbackDurationSeconds;

        if (!target.IsAlive ||
            Vector2.DistanceSquared(attackerPosition, target.Position) > Range * Range)
        {
            return false;
        }

        target.ReceiveDamage(Damage);
        return true;
    }
}
