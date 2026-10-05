using System;
using DungeonAscendant.Items;
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
    private float _slowTimeRemaining;
    private float _slowMovementMultiplier = 1f;
    private int _baseMaxHealth;
    private int _baseMeleeDamage;

    public Vector2 Position { get; private set; }
    public float MovementSpeed { get; }
    public float EffectiveMovementSpeed => MovementSpeed *
        (IsSlowed ? _slowMovementMultiplier : 1f);
    public Vector2 Size { get; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));
    public int BaseMaxHealth => _baseMaxHealth;
    public int BaseMeleeDamage => _baseMeleeDamage;
    public int MaxHealth => _baseMaxHealth +
        (EquippedItems.Armor?.HealthBonus ?? 0);
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public int MeleeDamage => _baseMeleeDamage +
        (EquippedItems.Weapon?.DamageBonus ?? 0);
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel =>
        BaseExperienceRequirement + (Level - 1) * ExperienceRequirementPerLevel;
    public FacingDirection Facing { get; private set; }
    public bool IsInvulnerable => _invulnerabilityTimeRemaining > 0f;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public bool IsSlowed => _slowTimeRemaining > 0f;
    public float SlowMovementMultiplier => IsSlowed
        ? _slowMovementMultiplier
        : 1f;
    public Inventory Inventory { get; }
    public Equipment EquippedItems { get; }

    public Player(
        Vector2 position,
        float movementSpeed = 220f,
        int maxHealth = 100,
        int meleeDamage = 25)
    {
        Position = position;
        MovementSpeed = movementSpeed;
        Size = new Vector2(40f, 56f);
        _baseMaxHealth = maxHealth;
        _baseMeleeDamage = meleeDamage;
        Inventory = new Inventory();
        EquippedItems = new Equipment();
        CurrentHealth = MaxHealth;
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
        _slowTimeRemaining = MathF.Max(
            0f,
            _slowTimeRemaining - elapsedSeconds);

        if (_slowTimeRemaining <= 0f)
            _slowMovementMultiplier = 1f;
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
        return Position + movement * EffectiveMovementSpeed * elapsedSeconds;
    }

    public void MoveTo(Vector2 position)
    {
        Position = position;
    }

    public bool EquipItem(EquipmentItem item)
    {
        if (!EquippedItems.TryEquip(item, Inventory))
            return false;

        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        return true;
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

    public void ApplySlow(float movementMultiplier, float durationSeconds)
    {
        if (!IsAlive || durationSeconds <= 0f)
            return;

        float clampedMultiplier = MathHelper.Clamp(
            movementMultiplier,
            0.2f,
            1f);
        _slowMovementMultiplier = IsSlowed
            ? MathF.Min(_slowMovementMultiplier, clampedMultiplier)
            : clampedMultiplier;
        _slowTimeRemaining = MathF.Max(
            _slowTimeRemaining,
            durationSeconds);
    }

    public void ClearTemporaryStatus()
    {
        _slowTimeRemaining = 0f;
        _slowMovementMultiplier = 1f;
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
            _baseMaxHealth += HealthIncreasePerLevel;
            _baseMeleeDamage += DamageIncreasePerLevel;
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
