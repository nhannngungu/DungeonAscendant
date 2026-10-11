using System;
using System.Collections.Generic;
using DungeonAscendant.Combat;
using DungeonAscendant.Items;
using DungeonAscendant.Player;
using DungeonAscendant.World;

namespace DungeonAscendant.Persistence;

/// <summary>
/// Persistent profile and resumable run snapshot for one character slot.
/// </summary>
public sealed class SaveSlotData
{
    public int SlotIndex { get; set; }
    public bool Occupied { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public CharacterAppearance Appearance { get; set; } =
        CharacterAppearance.CreateDefault();
    public int Level { get; set; } = 1;
    public int CurrentExperience { get; set; }
    public int DungeonDepth { get; set; } = 1;
    public int WorldTier { get; set; } = 1;
    public string CurrentMapId { get; set; } = "map-01";
    public string CurrentArea { get; set; } = "CORRUPTED WILDERNESS";
    public float CheckpointX { get; set; }
    public float CheckpointY { get; set; }
    public bool HasCheckpoint { get; set; }
    public string EquippedWeaponId { get; set; } = "knight-long-sword";
    public string EquippedWeaponName { get; set; } = "Knight Long Sword + Shield";
    public WeaponFamily EquippedWeaponFamily { get; set; } = WeaponFamily.LongSword;
    public int EquippedWeaponTier { get; set; } = 2;
    public int EquippedWeaponDamageBonus { get; set; } = 8;
    public ItemRarity EquippedWeaponRarity { get; set; } = ItemRarity.Uncommon;
    public string EquippedArmorId { get; set; } = "knight-armor";
    public string EquippedArmorName { get; set; } = "Knight Armor";
    public ArmorClass EquippedArmorClass { get; set; } = ArmorClass.Medium;
    public int EquippedArmorTier { get; set; } = 2;
    public int EquippedArmorHealthBonus { get; set; } = 20;
    public ItemRarity EquippedArmorRarity { get; set; } = ItemRarity.Uncommon;
    public ArmorGrade EquippedArmorGrade { get; set; } = ArmorGrade.C1;
    public bool HasInventorySnapshot { get; set; }
    public List<SavedEquipmentItemData> InventoryItems { get; set; } = new();
    public List<SavedLoreNoteData> Notes { get; set; } = new();
    public bool Map01Cleared { get; set; }
    public bool Map02Cleared { get; set; }
    public bool Map03Cleared { get; set; }
    public bool BossDefeated { get; set; }
    public bool LastDiceStarted { get; set; }
    public bool LastDiceCompleted { get; set; }
    public int LastDiceRoundIndex { get; set; }
    public int LastDiceWins { get; set; }
    public bool LastDiceRoundCommitted { get; set; }
    public bool LastDiceRoundResolved { get; set; }
    public bool LastDiceCompletionReached { get; set; }
    public LastDiceChoice LastDiceChoice { get; set; }
    public int LastDiceDie1 { get; set; }
    public int LastDiceDie2 { get; set; }
    public int LastDiceDie3 { get; set; }
    public float LastDiceRevealProgress { get; set; }
    public bool LastDiceRewardGranted { get; set; }
    public int LastDiceRewardWeaponIndex { get; set; } = -1;
    public int KillCount { get; set; }
    public double PlayTimeSeconds { get; set; }
    public DateTimeOffset? LastPlayedUtc { get; set; }

    public static SaveSlotData Empty(int slotIndex)
    {
        return new SaveSlotData { SlotIndex = slotIndex };
    }

    public void Normalize(int expectedIndex)
    {
        SlotIndex = expectedIndex;
        CharacterName = SanitizeName(CharacterName, expectedIndex);
        Appearance ??= CharacterAppearance.CreateDefault();
        Appearance.Normalize();
        Level = Math.Max(1, Level);
        CurrentExperience = Math.Max(0, CurrentExperience);
        DungeonDepth = Math.Max(1, DungeonDepth);
        WorldTier = Math.Max(1, WorldTier);
        CurrentMapId = string.IsNullOrWhiteSpace(CurrentMapId)
            ? $"map-{DungeonDepth:00}"
            : CurrentMapId;
        CurrentArea = string.IsNullOrWhiteSpace(CurrentArea)
            ? "UNKNOWN AREA"
            : CurrentArea;
        EquippedWeaponId = string.IsNullOrWhiteSpace(EquippedWeaponId)
            ? "knight-long-sword"
            : EquippedWeaponId;
        EquippedWeaponName = string.IsNullOrWhiteSpace(EquippedWeaponName)
            ? "Knight Long Sword + Shield"
            : EquippedWeaponName;
        EquippedArmorId = string.IsNullOrWhiteSpace(EquippedArmorId)
            ? "knight-armor"
            : EquippedArmorId;
        EquippedArmorName = string.IsNullOrWhiteSpace(EquippedArmorName)
            ? "Knight Armor"
            : EquippedArmorName;
        EquippedWeaponTier = Math.Max(1, EquippedWeaponTier);
        EquippedWeaponDamageBonus = Math.Max(0, EquippedWeaponDamageBonus);
        EquippedArmorTier = Math.Max(1, EquippedArmorTier);
        EquippedArmorHealthBonus = Math.Max(0, EquippedArmorHealthBonus);
        EquippedArmorGrade = ArmorGradeRules.Clamp(EquippedArmorGrade);
        InventoryItems ??= new List<SavedEquipmentItemData>();
        InventoryItems.RemoveAll(item => item == null);
        foreach (SavedEquipmentItemData item in InventoryItems)
            item.Normalize();
        Notes ??= new List<SavedLoreNoteData>();
        Notes.RemoveAll(note => note == null || string.IsNullOrWhiteSpace(note.Id));
        KillCount = Math.Max(0, KillCount);
        LastDiceRoundIndex = Math.Clamp(
            LastDiceRoundIndex, 0, LastDiceTableSystem.RoundCount - 1);
        LastDiceWins = Math.Clamp(LastDiceWins, 0, LastDiceTableSystem.RoundCount);
        if (!Enum.IsDefined(LastDiceChoice))
            LastDiceChoice = DungeonAscendant.World.LastDiceChoice.Xiu;
        LastDiceRevealProgress = Math.Clamp(LastDiceRevealProgress, 0f, 1f);
        LastDiceRewardWeaponIndex = Math.Clamp(LastDiceRewardWeaponIndex, -1, 6);
        if (!LastDiceRoundCommitted)
        {
            LastDiceRoundResolved = false;
            LastDiceCompletionReached = false;
            LastDiceDie1 = 0;
            LastDiceDie2 = 0;
            LastDiceDie3 = 0;
            LastDiceRevealProgress = 0f;
        }
        else
        {
            LastDiceDie1 = Math.Clamp(LastDiceDie1, 1, 6);
            LastDiceDie2 = Math.Clamp(LastDiceDie2, 1, 6);
            LastDiceDie3 = Math.Clamp(LastDiceDie3, 1, 6);
        }
        if (LastDiceCompleted)
            LastDiceCompletionReached = true;
        PlayTimeSeconds = Math.Max(0d, PlayTimeSeconds);

        if (!Occupied)
        {
            CharacterName = string.Empty;
            HasCheckpoint = false;
            LastPlayedUtc = null;
            HasInventorySnapshot = false;
            InventoryItems.Clear();
            Notes.Clear();
        }
    }

    public static string SanitizeName(string value, int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"ASCENDANT {slotIndex + 1}";

        string trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length <= 18 ? trimmed : trimmed[..18];
    }
}

public sealed class SavedEquipmentItemData
{
    public string DefinitionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EquipmentSlot Slot { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Tier { get; set; } = 1;
    public WeaponFamily WeaponFamily { get; set; } = WeaponFamily.LongSword;
    public int DamageBonus { get; set; }
    public ArmorClass ArmorClass { get; set; } = ArmorClass.Medium;
    public int HealthBonus { get; set; }
    public ArmorGrade ArmorGrade { get; set; } = ArmorGrade.C1;

    public static SavedEquipmentItemData FromItem(EquipmentItem item)
    {
        return new SavedEquipmentItemData
        {
            DefinitionId = item.Definition.Id,
            Name = item.Definition.Name,
            Slot = item.Slot,
            Rarity = item.Rarity,
            Tier = item.Definition.Tier,
            WeaponFamily = item.WeaponDefinition?.Family ?? WeaponFamily.LongSword,
            DamageBonus = item.WeaponDefinition?.DamageBonus ?? 0,
            ArmorClass = item.ArmorDefinition?.Class ?? ArmorClass.Medium,
            HealthBonus = item.ArmorDefinition?.MaxHealthBonus ?? 0,
            ArmorGrade = item.ArmorGrade
        };
    }

    public void Normalize()
    {
        DefinitionId ??= string.Empty;
        Name ??= string.Empty;
        Tier = Math.Max(1, Tier);
        DamageBonus = Math.Max(0, DamageBonus);
        HealthBonus = Math.Max(0, HealthBonus);
        ArmorGrade = ArmorGradeRules.Clamp(ArmorGrade);
    }
}

public sealed class SavedLoreNoteData
{
    public string Id { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
