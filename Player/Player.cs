using System;
using DungeonAscendant.Items;
using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
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

    public const float DefaultMovementSpeed = 260f;
    public const float Gravity = 1800f;
    public const float JumpVelocity = -660f;
    public const float TerminalFallSpeed = 1000f;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
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
    public PlayerVisualState VisualState => !IsAlive
        ? PlayerVisualState.Death
        : IsHitFlashing
            ? PlayerVisualState.Hurt
            : !IsGrounded
                ? Velocity.Y < 0f ? PlayerVisualState.Jump : PlayerVisualState.Fall
                : MathF.Abs(Velocity.X) > 0.1f
                    ? PlayerVisualState.Run
                    : PlayerVisualState.Idle;

    public Player(
        Vector2 position,
        float movementSpeed = DefaultMovementSpeed,
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
        Facing = FacingDirection.Right;
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

    public void UpdateSideScrollingMovement(
        GameTime gameTime,
        KeyboardState keyboardState,
        bool jumpPressed,
        DungeonMap dungeon)
    {
        if (!IsAlive)
            return;

        float horizontalInput = 0f;

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            horizontalInput -= 1f;

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            horizontalInput += 1f;

        if (horizontalInput < 0f)
            Facing = FacingDirection.Left;
        else if (horizontalInput > 0f)
            Facing = FacingDirection.Right;

        if (jumpPressed && IsGrounded)
        {
            Velocity = new Vector2(Velocity.X, JumpVelocity);
            IsGrounded = false;
        }

        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 20f);
        Velocity = new Vector2(
            horizontalInput * EffectiveMovementSpeed,
            MathF.Min(TerminalFallSpeed, Velocity.Y + Gravity * elapsedSeconds));
        MovementResult result = SideScrollingCollision.Resolve(
            Position,
            Velocity,
            Size,
            elapsedSeconds,
            dungeon);
        Position = result.Position;
        Velocity = result.Velocity;
        IsGrounded = result.IsGrounded;
    }

    public void MoveTo(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        IsGrounded = false;
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

}
