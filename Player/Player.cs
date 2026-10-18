using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
using DungeonAscendant.Lore;
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
    public const bool DebugGodModeDefault = true;
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
    public bool DebugGodMode { get; set; } = DebugGodModeDefault;
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
	public bool IsKnightSwordAndShield => string.Equals(
		EquippedWeapon.WeaponId,
		EquipmentCatalog.KnightLongSword.WeaponId,
		StringComparison.Ordinal);
    public ArmorClass ArmorClass => EquippedArmor.Class;
    public bool UsesShield => EquippedWeapon.UsesShield;
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel =>
        BaseExperienceRequirement + (Level - 1) * ExperienceRequirementPerLevel;
    public FacingDirection Facing { get; private set; }
    public bool IsInvulnerable => _invulnerabilityTimeRemaining > 0f ||
        Combat.IsDodgeInvulnerable || Combat.IsFourthVeilActive ||
        Combat.IsScytheShadowEvading || Combat.IsScytheDodgeWeaving ||
        Combat.IsUmbralAttached;
    public bool IsTargetable => IsAlive && !Combat.IsFourthVeilActive &&
        !Combat.IsUmbralAttached;
    public bool IsHitFlashing => _hitFeedbackTimeRemaining > 0f;
    public bool IsSlowed => _slowTimeRemaining > 0f;
    public float SlowMovementMultiplier => IsSlowed
        ? _slowMovementMultiplier
        : 1f;
    public Inventory Inventory { get; }
    public LoreCollection Notes { get; }
    public Equipment EquippedItems { get; }
    public PlayerCombat Combat { get; }
    public CharacterAppearance Appearance { get; private set; }
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
                    : (Combat.CurrentTechnique?.Effect is
                            WeaponTechniqueEffect.KnightShieldGuard or
                            WeaponTechniqueEffect.KnightShieldWall ||
                       Combat.CurrentTechnique?.Effect ==
                            WeaponTechniqueEffect.KnightFinalOath &&
                            Combat.TechniqueStageIndex is 0 or 8)
                            ? PlayerVisualState.Block
                        : Combat.State == CombatState.ChargingHeavy
                            ? PlayerVisualState.HeavyCharge
                        : Combat.State == CombatState.HeavyAttack
                            ? PlayerVisualState.HeavyAttack
                            : Combat.State is CombatState.LightAttack or
                                CombatState.UmbralAttached
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
        int meleeDamage = 25,
        CharacterAppearance appearance = null)
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
        Notes = new LoreCollection();
        EquippedItems = new Equipment();
        Combat = new PlayerCombat();
        Appearance = appearance?.Clone() ?? CharacterAppearance.CreateDefault();
        Appearance.Normalize();
        EquippedItems.SetStartingItems(
            DebugGiveAllTestEquipment
                ? EquipmentCatalog.CreateDevelopmentWeapon(0)
                : EquipmentCatalog.CreateStartingWeapon(),
            EquipmentCatalog.CreateStartingArmor());

        if (DebugGiveAllTestEquipment)
        {
            EnsureCanonicalTestWeapons();
            foreach (EquipmentItem item in
                     EquipmentCatalog.CreateDevelopmentArmorInventory())
            {
                Inventory.TryAdd(item);
            }
        }

        ApplyEquipmentToCombat();
        CurrentHealth = MaxHealth;
        Level = 1;
        CurrentExperience = 0;
        Facing = FacingDirection.Right;
    }

    public void SetAppearance(CharacterAppearance appearance)
    {
        Appearance = appearance?.Clone() ?? CharacterAppearance.CreateDefault();
        Appearance.Normalize();
    }

    public void RestoreSaveState(
        int level,
        int currentExperience,
        EquipmentItem weapon,
        EquipmentItem armor)
    {
        Level = Math.Max(1, level);
        _baseMaxHealth = 100 + (Level - 1) * HealthIncreasePerLevel;
        _baseMeleeDamage = 25 + (Level - 1) * DamageIncreasePerLevel;
        CurrentExperience = Math.Clamp(
            currentExperience,
            0,
            Math.Max(0, ExperienceToNextLevel - 1));
        EquippedItems.SetStartingItems(
            weapon ?? EquipmentCatalog.CreateStartingWeapon(),
            armor ?? EquipmentCatalog.CreateStartingArmor());
		Combat.CancelActions(restoreStamina: true);
        ApplyEquipmentToCombat();
        CurrentHealth = MaxHealth;
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
                : WeaponFamily == WeaponFamily.Scythe &&
                    Combat.CurrentTechnique?.Effect is
                        WeaponTechniqueEffect.ScytheShadowSlip or
                        WeaponTechniqueEffect.ScytheShadowPass
                    ? Combat.ScytheMoveDirection
                : facingSign;
        float horizontalVelocity = Combat.IsDodging
            ? Combat.DodgeDirection * PlayerCombat.DodgeSpeed *
                Combat.DodgeSpeedMultiplier
            : Combat.AttackAdvanceSpeed > 0f
                ? attackAdvanceDirection * Combat.AttackAdvanceSpeed
            : horizontalInput * EffectiveMovementSpeed * Combat.MovementMultiplier;
        float verticalVelocity = Velocity.Y + Gravity * elapsedSeconds;
        if (Combat.IsGreatswordFallingBastion)
        {
            verticalVelocity +=
                GreatswordTuning.FallingBastionDownwardBoost * elapsedSeconds;
        }
        if (Combat.IsKnightFallingShield)
            verticalVelocity = MathF.Max(
                verticalVelocity,
                KnightTuning.FallingShieldDownwardSpeed);
        if (Combat.IsAstralDescending)
            verticalVelocity = MathF.Max(verticalVelocity, 760f);
        if (Combat.CurrentTechnique?.Effect ==
                WeaponTechniqueEffect.ScytheCrescentDive)
            verticalVelocity = MathF.Max(verticalVelocity, 690f);
        Velocity = new Vector2(
            horizontalVelocity,
            MathF.Min(TerminalFallSpeed, verticalVelocity));
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

        Combat.Update(gameTime, input, IsGrounded, Facing, Velocity.Y);

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

    public void BeginGreatswordUltimateLeap(float verticalVelocity)
    {
        if (!IsAlive || !IsGrounded || verticalVelocity >= 0f)
            return;

        Velocity = new Vector2(Velocity.X, verticalVelocity);
        IsGrounded = false;
    }

    public void PullToward(Vector2 target, float distance, DungeonMap dungeon)
    {
        if (!IsAlive || Combat.IsUltimateControlImmune ||
            dungeon == null || distance <= 0f)
            return;

        Combat.HardInterrupt();
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

    public void PushAwayFrom(
        Vector2 source,
        float distance,
        DungeonMap dungeon)
    {
        if (!IsAlive || Combat.IsUltimateControlImmune ||
            dungeon == null || distance <= 0f)
            return;

        Combat.HardInterrupt();
        float horizontal = Position.X - source.X;
        if (MathF.Abs(horizontal) <= 1f)
            horizontal = Facing == FacingDirection.Left ? -1f : 1f;
        Position = DungeonCollision.ResolveMovement(
            Position,
            Position + new Vector2(
                MathF.Sign(horizontal) * MathF.Min(distance, 28f),
                0f),
            Size,
            dungeon);
        Velocity = new Vector2(0f, Velocity.Y);
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

		Combat.CancelActions();
        ApplyEquipmentToCombat();
        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        return true;
    }

    public int GrantAllWeapons()
    {
        return EnsureCanonicalTestWeapons();
    }

    public int EnsureCanonicalTestWeapons()
    {
        int granted = 0;

        for (int index = 0;
             index < EquipmentCatalog.AllImplementedWeapons.Count;
             index++)
        {
            WeaponDefinition definition =
                EquipmentCatalog.AllImplementedWeapons[index];
            if (EquippedItems.Weapon?.WeaponId == definition.WeaponId ||
                Inventory.ContainsItemId(definition.WeaponId))
            {
                continue;
            }

            if (!Inventory.TryAdd(
                    EquipmentCatalog.CreateDevelopmentWeapon(index)))
                break;

            granted++;
        }

        return granted;
    }

    public bool NormalizeDevelopmentTestWeapons()
    {
        var ownedCanonicalIds = new HashSet<string>(StringComparer.Ordinal);
        if (EquipmentCatalog.GetImplementedWeaponOrder(
                EquippedItems.Weapon?.WeaponId) != int.MaxValue)
        {
            ownedCanonicalIds.Add(EquippedItems.Weapon.WeaponId);
        }

        foreach (EquipmentItem item in Inventory.Items)
        {
            if (EquipmentCatalog.GetImplementedWeaponOrder(item.WeaponId) !=
                int.MaxValue)
            {
                ownedCanonicalIds.Add(item.WeaponId);
            }
        }

        // Only exact historical debug rosters are normalized. Partial current
        // inventories and legitimate duplicate rewards remain untouched.
        if (!IsRecognizedLegacyDevelopmentRoster(ownedCanonicalIds))
            return false;

        bool equippedIsCanonical = EquipmentCatalog.GetImplementedWeaponOrder(
            EquippedItems.Weapon?.WeaponId) != int.MaxValue;
        var preserved = new List<EquipmentItem>();
        foreach (EquipmentItem item in Inventory.Items)
        {
            if (EquipmentCatalog.GetImplementedWeaponOrder(item.WeaponId) ==
                int.MaxValue)
            {
                preserved.Add(item);
            }
        }

        int canonicalInventoryCount = EquipmentCatalog.AllImplementedWeapons.Count -
            (equippedIsCanonical ? 1 : 0);
        if (preserved.Count + canonicalInventoryCount > Inventory.Capacity)
            return false;

        string equippedId = EquippedItems.Weapon?.WeaponId ?? string.Empty;
        if (equippedIsCanonical)
        {
            int equippedIndex = EquipmentCatalog.GetImplementedWeaponOrder(
                equippedId);
            EquippedItems.SetStartingItems(
                EquipmentCatalog.CreateDevelopmentWeapon(equippedIndex),
                EquippedItems.Armor);
        }

        var normalized = new List<EquipmentItem>(
            preserved.Count + canonicalInventoryCount);
        for (int index = 0;
             index < EquipmentCatalog.AllImplementedWeapons.Count;
             index++)
        {
            WeaponDefinition definition =
                EquipmentCatalog.AllImplementedWeapons[index];
            if (equippedIsCanonical && definition.WeaponId == equippedId)
                continue;
            normalized.Add(EquipmentCatalog.CreateDevelopmentWeapon(index));
        }
        normalized.AddRange(preserved);
        Inventory.Restore(normalized);

        ApplyEquipmentToCombat();
        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        return true;
    }

    private static bool IsRecognizedLegacyDevelopmentRoster(
        HashSet<string> ownedWeaponIds)
    {
        if (ownedWeaponIds == null)
            return false;

        bool IsCurrentExcept(params WeaponDefinition[] laterAdditions)
        {
            var excluded = new HashSet<string>(StringComparer.Ordinal);
            foreach (WeaponDefinition definition in laterAdditions)
                excluded.Add(definition.WeaponId);

            if (ownedWeaponIds.Count !=
                EquipmentCatalog.AllImplementedWeapons.Count - excluded.Count)
            {
                return false;
            }

            foreach (WeaponDefinition definition in
                     EquipmentCatalog.AllImplementedWeapons)
            {
                if (!excluded.Contains(definition.WeaponId) &&
                    !ownedWeaponIds.Contains(definition.WeaponId))
                {
                    return false;
                }
            }

            return true;
        }

        return IsCurrentExcept(EquipmentCatalog.ShadowReaperScythe) ||
            IsCurrentExcept(
                EquipmentCatalog.IronVanguardGreatsword,
                EquipmentCatalog.ForbiddenSovereignAbyssRing,
                EquipmentCatalog.ShadowReaperScythe);
    }

    public int CountOwnedImplementedWeapons()
    {
        int count = 0;
        foreach (WeaponDefinition definition in
                 EquipmentCatalog.AllImplementedWeapons)
        {
            if (EquippedItems.Weapon?.WeaponId == definition.WeaponId ||
                Inventory.ContainsItemId(definition.WeaponId))
            {
                count++;
            }
        }

        return count;
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
        return ReceiveAttack(contact, isProjectile: false);
    }

    public AttackResolution ReceiveProjectileAttack(AttackContact contact)
    {
        return ReceiveAttack(contact, isProjectile: true);
    }

    private AttackResolution ReceiveAttack(
        AttackContact contact,
        bool isProjectile)
    {
        if (!IsAlive || contact.Damage <= 0)
            return AttackResolution.Ignored;

        AttackResolution resolution = Combat.ResolveIncomingAttack(
            contact,
            Position,
            Facing,
            isProjectile);

        if (resolution == AttackResolution.Damaged)
        {
            if (IsInvulnerable)
                return AttackResolution.Ignored;

            ApplyHealthDamage(
                ApplyArmorDefense(contact.Damage),
                hardInterrupt: contact.Unblockable);
        }
        else if (resolution == AttackResolution.BladeGuarded)
        {
            if (IsInvulnerable)
                return AttackResolution.Ignored;

            int guardedDamage = Math.Max(
                1,
                (int)MathF.Round(
                    contact.Damage *
                    (WeaponFamily == WeaponFamily.ArcaneWarStaff
                        ? AncientStaffTuning.AegisDamageTakenMultiplier
                        : GreatswordTuning.IronResolveDamageTakenMultiplier)));
            ApplyHealthDamage(
                ApplyArmorDefense(guardedDamage),
                hardInterrupt: false);
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
        if (!IsAlive || Combat.IsUltimateControlImmune ||
            durationSeconds <= 0f)
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

    public void RestoreHealth()
    {
        CurrentHealth = MaxHealth;
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

    private void ApplyHealthDamage(
        int damage,
        bool triggerHurt = true,
        bool hardInterrupt = false)
    {
        int minimumHealth = DebugGodMode ? 1 : 0;
        CurrentHealth = Math.Max(minimumHealth, CurrentHealth - damage);
        _invulnerabilityTimeRemaining = InvulnerabilityDurationSeconds;
        _hitFeedbackTimeRemaining = HitFeedbackDurationSeconds;

        if (triggerHurt || !IsAlive)
            Combat.OnDamaged(IsAlive, damage, hardInterrupt);
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
