using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Player;

/// <summary>
/// Owns the player character's state and movement behavior.
/// </summary>
public sealed class Player
{
    private const int BaseExperienceRequirement = 100;
    private const int ExperienceRequirementPerLevel = 50;
    private const int HealthIncreasePerLevel = 20;
    private const int DamageIncreasePerLevel = 5;
    private const float InvulnerabilityDurationSeconds = 0.5f;
    private const float HitFeedbackDurationSeconds = 0.16f;

    private float _invulnerabilityTimeRemaining;
    private float _hitFeedbackTimeRemaining;

    public Vector2 Position { get; private set; }
    public float MovementSpeed { get; }
    public Vector2 Size { get; }
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public int MeleeDamage { get; private set; }
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel =>
        BaseExperienceRequirement + (Level - 1) * ExperienceRequirementPerLevel;
    public FacingDirection Facing { get; private set; }
    public bool IsInvulnerable => _invulnerabilityTimeRemaining > 0f;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;

    public Player(
        Vector2 position,
        float movementSpeed = 220f,
        int maxHealth = 100,
        int meleeDamage = 25)
    {
        Position = position;
        MovementSpeed = movementSpeed;
        Size = new Vector2(40f, 56f);
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
        MeleeDamage = meleeDamage;
        Level = 1;
        CurrentExperience = 0;
        Facing = FacingDirection.Down;
    }

    public void UpdateTimers(GameTime gameTime)
    {
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _invulnerabilityTimeRemaining = MathF.Max(
            0f,
            _invulnerabilityTimeRemaining - elapsedSeconds);
        _hitFeedbackTimeRemaining = MathF.Max(
            0f,
            _hitFeedbackTimeRemaining - elapsedSeconds);

    }

    public Vector2 GetDesiredPosition(
        GameTime gameTime,
        KeyboardState keyboardState)
    {
        if (!IsAlive)
            return Position;

        Vector2 movement = Vector2.Zero;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            movement.Y -= 1f;

        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            movement.Y += 1f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            movement.X -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            movement.X += 1f;

        if (movement != Vector2.Zero)
        {
            UpdateFacingDirection(movement);
            movement.Normalize();
        }

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        return Position + movement * MovementSpeed * elapsedSeconds;
    }

    public void MoveTo(Vector2 position)
    {
        Position = position;
    }

    public bool ReceiveDamage(int damage)
    {
        if (!IsAlive || IsInvulnerable || damage <= 0)
            return false;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _invulnerabilityTimeRemaining = InvulnerabilityDurationSeconds;
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;
        return true;
    }

    public void GainExperience(int experience)
    {
        if (experience <= 0)
            return;

        CurrentExperience += experience;

        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            Level++;
            MaxHealth += HealthIncreasePerLevel;
            MeleeDamage += DamageIncreasePerLevel;
            CurrentHealth = MaxHealth;
        }
    }

    private void UpdateFacingDirection(Vector2 movement)
    {
        if (MathF.Abs(movement.X) > MathF.Abs(movement.Y))
        {
            Facing = movement.X < 0f
                ? FacingDirection.Left
                : FacingDirection.Right;
            return;
        }

        Facing = movement.Y < 0f
            ? FacingDirection.Up
            : FacingDirection.Down;
    }
}
