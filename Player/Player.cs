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
    }

    public void Update(
        GameTime gameTime,
        KeyboardState keyboardState,
        Rectangle arenaBounds)
    {
        if (!IsAlive)
            return;

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
            movement.Normalize();

        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += movement * MovementSpeed * elapsedSeconds;
        Position = new Vector2(
            MathHelper.Clamp(
                Position.X,
                arenaBounds.Left + Size.X / 2f,
                arenaBounds.Right - Size.X / 2f),
            MathHelper.Clamp(
                Position.Y,
                arenaBounds.Top + Size.Y / 2f,
                arenaBounds.Bottom - Size.Y / 2f));
    }

    public void ReceiveDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
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
}
