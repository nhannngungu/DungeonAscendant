using System;
using DungeonAscendant.Combat;
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
    public const bool DebugGiveAllTestEquipment = true;
    private const int DebugInventoryCapacity = 20;

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
    public const float BodyHurtboxWidth = 40f;
    public const float BodyHurtboxHeight = 56f;
    public const float DefenseZoneWidth = 80f;
    public const float DefenseZoneHeight = 90f;
    public const float DefenseZoneCenterOffset = 32f;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
    public float MovementSpeed { get; }
    public float EffectiveMovementSpeed => MovementSpeed *
        (EquippedItems.Armor?.MoveSpeedModifier ?? 1f) *
        (IsSlowed ? _slowMovementMultiplier : 1f);
    public Vector2 Size { get; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)MathF.Ceiling(Size.X),
        (int)MathF.Ceiling(Size.Y));
    public Rectangle BodyHurtbox => Bounds;
    public Rectangle DefenseBounds => CreateDefenseBounds(Position, Facing);
    public int BaseMaxHealth => _baseMaxHealth;
    public int BaseMeleeDamage => _baseMeleeDamage;
    public int MaxHealth => _baseMaxHealth +
        (EquippedItems.Armor?.HealthBonus ?? 0);
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public int MeleeDamage => _baseMeleeDamage +
        (EquippedItems.Weapon?.DamageBonus ?? 0);
    public WeaponDefinition EquippedWeapon =>
        EquippedItems.Weapon?.WeaponDefinition ?? EquipmentCatalog.KnightLongSword;
    public ArmorDefinition EquippedArmor =>
        EquippedItems.Armor?.ArmorDefinition ?? EquipmentCatalog.KnightArmor;
    public WeaponFamily WeaponFamily => EquippedWeapon.Family;
    public ArmorClass ArmorClass => EquippedArmor.Class;
    public bool UsesShield => EquippedWeapon.UsesShield;
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel =>
        BaseExperienceRequirement + (Level - 1) * ExperienceRequirementPerLevel;
    public FacingDirection Facing { get; private set; }
    public bool IsInvulnerable => _invulnerabilityTimeRemaining > 0f ||
        Combat.IsDodgeInvulnerable;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public bool IsSlowed => _slowTimeRemaining > 0f;
    public float SlowMovementMultiplier => IsSlowed
        ? _slowMovementMultiplier
        : 1f;
    public Inventory Inventory { get; }
    public Equipment EquippedItems { get; }
    public PlayerCombat Combat { get; }
    public float CurrentStamina => Combat.Stamina.Current;
    public float MaxStamina => Combat.Stamina.Maximum;
    public PlayerVisualState VisualState => !IsAlive ||
        Combat.State == CombatState.Dead
            ? PlayerVisualState.Dead
            : Combat.State == CombatState.Hurt
                ? PlayerVisualState.Hurt
                : Combat.State == CombatState.Staggered
                    ? PlayerVisualState.GuardBreak
                    : Combat.State == CombatState.Dodging
                        ? PlayerVisualState.Dodge
                    : Combat.CurrentTechnique?.Effect is
                        WeaponTechniqueEffect.IronBastion or
                        WeaponTechniqueEffect.OathStance &&
                        Combat.TechniqueStageIndex == 0
                            ? PlayerVisualState.Block
                        : Combat.State == CombatState.ChargingHeavy
                            ? PlayerVisualState.HeavyCharge
                        : Combat.State == CombatState.HeavyAttack
                            ? PlayerVisualState.HeavyAttack
                            : Combat.State == CombatState.LightAttack
                                ? Combat.CurrentAttack?.Kind switch
                                {
                                    AttackKind.LightTwo => PlayerVisualState.LightAttack2,
                                    AttackKind.LightThree => PlayerVisualState.LightAttack3,
                                    AttackKind.LightFour => PlayerVisualState.LightAttack4,
                                    AttackKind.LightFive => PlayerVisualState.LightAttack5,
                                    _ => PlayerVisualState.LightAttack1
                                }
                                : Combat.State == CombatState.Blocking
                                    ? PlayerVisualState.Block
                                    : !IsGrounded
                                        ? Velocity.Y < 0f
                                            ? PlayerVisualState.Jump
                                            : PlayerVisualState.Fall
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
        Inventory = new Inventory(
            DebugGiveAllTestEquipment
                ? DebugInventoryCapacity
                : Inventory.DefaultCapacity);
        EquippedItems = new Equipment();
        Combat = new PlayerCombat();
        EquippedItems.SetStartingItems(
            EquipmentCatalog.CreateStartingWeapon(),
            EquipmentCatalog.CreateStartingArmor());

        if (DebugGiveAllTestEquipment)
        {
            foreach (EquipmentItem item in EquipmentCatalog.CreateSampleInventory())
                Inventory.TryAdd(item);
        }

        ApplyEquipmentToCombat();
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

        if (Combat.CanJump && horizontalInput < 0f)
            Facing = FacingDirection.Left;
        else if (Combat.CanJump && horizontalInput > 0f)
            Facing = FacingDirection.Right;

        if (jumpPressed && IsGrounded && Combat.CanJump)
        {
            Velocity = new Vector2(Velocity.X, JumpVelocity);
            IsGrounded = false;
        }

        float elapsedSeconds = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            1f / 20f);
        float facingSign = Facing == FacingDirection.Left ? -1f : 1f;
        float attackAdvanceDirection =
            MathF.Abs(Combat.RangerWindrunnerDirection) > .1f
                ? Combat.RangerWindrunnerDirection
                : facingSign;
        float horizontalVelocity = Combat.IsDodging
            ? Combat.DodgeDirection * PlayerCombat.DodgeSpeed *
                Combat.DodgeSpeedMultiplier
            : Combat.AttackAdvanceSpeed > 0f
                ? attackAdvanceDirection * Combat.AttackAdvanceSpeed
            : horizontalInput * EffectiveMovementSpeed * Combat.MovementMultiplier;
        Velocity = new Vector2(
            horizontalVelocity,
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
        Combat.SetLocomotion(
            IsGrounded,
            MathF.Abs(Velocity.X) > 0.1f);
    }

    public void UpdateCombat(GameTime gameTime, CombatInput input)
    {
        if (!IsAlive)
            return;

        if (WeaponFamily == WeaponFamily.HunterBow)
        {
            if (Combat.CanJump && input.HorizontalDirection < -.1f)
                Facing = FacingDirection.Left;
            else if (Combat.CanJump && input.HorizontalDirection > .1f)
                Facing = FacingDirection.Right;
        }

        Combat.Update(gameTime, input, IsGrounded, Facing);

        if (Combat.IsDodging)
        {
            Facing = Combat.DodgeDirection < 0f
                ? FacingDirection.Left
                : FacingDirection.Right;
        }
    }

    public void MoveTo(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        IsGrounded = false;
    }

    public void BeginRangerHop(float verticalVelocity)
    {
        if (!IsAlive || !IsGrounded || verticalVelocity >= 0f)
            return;

        Velocity = new Vector2(Velocity.X, verticalVelocity);
        IsGrounded = false;
    }

    public void PullToward(Vector2 target, float distance, DungeonMap dungeon)
    {
        if (!IsAlive || dungeon == null || distance <= 0f)
            return;

        float horizontal = target.X - Position.X;
        if (MathF.Abs(horizontal) <= 1f)
            return;

        Position = DungeonCollision.ResolveMovement(
            Position,
            Position + new Vector2(
                MathF.Sign(horizontal) * MathF.Min(distance, 32f),
                0f),
            Size,
            dungeon);
    }

    public Vector2 MoveThroughCombatTechnique(
        Vector2 desiredPosition,
        DungeonMap dungeon)
    {
        if (!IsAlive || dungeon == null)
            return Vector2.Zero;

        Vector2 start = Position;
        Position = DungeonCollision.ResolveMovement(
            Position,
            desiredPosition,
            Size,
            dungeon);
        Velocity = new Vector2(0f, Velocity.Y);

        if (MathF.Abs(Position.X - start.X) > .5f)
        {
            Facing = Position.X < start.X
                ? FacingDirection.Left
                : FacingDirection.Right;
        }

        return Position - start;
    }

    public void FaceToward(float worldX)
    {
        if (worldX < Position.X - .5f)
            Facing = FacingDirection.Left;
        else if (worldX > Position.X + .5f)
            Facing = FacingDirection.Right;
    }

    public bool EquipItem(EquipmentItem item)
    {
        if (!EquippedItems.TryEquip(
            item,
            Inventory))
            return false;

        ApplyEquipmentToCombat();
        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        return true;
    }

    public void EquipDebugItem(EquipmentItem item)
    {
        EquippedItems.SetDebugItem(item);
        Combat.CancelActions();
        ApplyEquipmentToCombat();
        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
    }

    public bool ReceiveDamage(int damage)
    {
        if (!IsAlive || IsInvulnerable || damage <= 0)
            return false;

        ApplyHealthDamage(ApplyArmorDefense(damage));
        return true;
    }

    public AttackResolution ReceiveMeleeAttack(AttackContact contact)
    {
        return ReceiveAttack(contact);
    }

    public AttackResolution ReceiveProjectileAttack(AttackContact contact)
    {
        return ReceiveAttack(contact);
    }

    private AttackResolution ReceiveAttack(AttackContact contact)
    {
        if (!IsAlive || contact.Damage <= 0)
            return AttackResolution.Ignored;

        AttackResolution resolution = Combat.ResolveIncomingAttack(
            contact,
            Position,
            Facing);

        if (resolution == AttackResolution.Damaged)
        {
            if (IsInvulnerable)
                return AttackResolution.Ignored;

            ApplyHealthDamage(ApplyArmorDefense(contact.Damage));
        }
        else if (resolution == AttackResolution.GuardBroken)
        {
            ApplyHealthDamage(
                ApplyArmorDefense(Math.Max(1, contact.Damage / 2)),
                triggerHurt: false);
        }

        return resolution;
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
        if (IsAlive)
            Combat.CancelActions(restoreStamina: true);
        else
            Combat.OnDamaged(isAlive: false);
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

    private void ApplyHealthDamage(int damage, bool triggerHurt = true)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        _invulnerabilityTimeRemaining = InvulnerabilityDurationSeconds;
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;

        if (triggerHurt || !IsAlive)
            Combat.OnDamaged(IsAlive, damage);
    }

    private int ApplyArmorDefense(int damage)
    {
        return Math.Max(
            1,
            (int)MathF.Round(
                damage * (EquippedItems.Armor?.DamageTakenMultiplier ?? 1f)));
    }

    private void ApplyEquipmentToCombat()
    {
        Combat.ApplyLoadout(EquippedWeapon, EquippedItems.Armor);
    }

    public static Rectangle CreateDefenseBounds(
        Vector2 position,
        FacingDirection facing)
    {
        int width = (int)DefenseZoneWidth;
        int height = (int)DefenseZoneHeight;
        int y = (int)position.Y - height / 2;
        int centerOffset = facing == FacingDirection.Left
            ? -(int)DefenseZoneCenterOffset
            : (int)DefenseZoneCenterOffset;

        return new Rectangle(
            (int)position.X + centerOffset - width / 2,
            y,
            width,
            height);
    }

}
