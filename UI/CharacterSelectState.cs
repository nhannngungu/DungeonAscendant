using System;
using System.Collections.Generic;
using DungeonAscendant.Core;
using DungeonAscendant.Items;
using DungeonAscendant.Persistence;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.UI;

/// <summary>
/// Selection/delete-confirmation state plus cached renderer-ready previews.
/// </summary>
public sealed class CharacterSelectState
{
    private readonly PlayerCharacter[] _previewPlayers =
        new PlayerCharacter[SaveSlotManager.SlotCount];

    public int SelectedSlotIndex { get; private set; }
    public bool IsDeleteConfirmationOpen { get; private set; }
    public bool DeleteFailed { get; private set; }

    public void Begin(
        IReadOnlyList<SaveSlotData> slots,
        int? preferredSlotIndex = null)
    {
        if (preferredSlotIndex.HasValue)
        {
            SelectedSlotIndex = Math.Clamp(
                preferredSlotIndex.Value,
                0,
                SaveSlotManager.SlotCount - 1);
        }
        else
        {
            SelectedSlotIndex = Math.Clamp(
                SelectedSlotIndex,
                0,
                SaveSlotManager.SlotCount - 1);
        }
        IsDeleteConfirmationOpen = false;
        DeleteFailed = false;
        RefreshPreviews(slots);
    }

    public void Move(int delta)
    {
        SelectedSlotIndex = (SelectedSlotIndex + delta) % SaveSlotManager.SlotCount;
        if (SelectedSlotIndex < 0)
            SelectedSlotIndex += SaveSlotManager.SlotCount;
    }

    public void BeginDelete(SaveSlotData selectedSlot)
    {
        if (selectedSlot?.Occupied == true)
        {
            IsDeleteConfirmationOpen = true;
            DeleteFailed = false;
        }
    }

    public void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
        DeleteFailed = false;
    }

    public void ReportDeleteFailure() => DeleteFailed = true;

    public PlayerCharacter GetPreview(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _previewPlayers.Length
            ? _previewPlayers[slotIndex]
            : null;
    }

    public void RefreshPreviews(IReadOnlyList<SaveSlotData> slots)
    {
        for (int index = 0; index < _previewPlayers.Length; index++)
        {
            SaveSlotData slot = index < slots.Count ? slots[index] : null;
            if (slot?.Occupied != true)
            {
                _previewPlayers[index] = null;
                continue;
            }

            var preview = new PlayerCharacter(
                Vector2.Zero,
                appearance: slot.Appearance);
            preview.RestoreSaveState(
                slot.Level,
                slot.CurrentExperience,
                CreateWeapon(slot),
                CreateArmor(slot));
            _previewPlayers[index] = preview;
        }
    }

    public static EquipmentItem CreateWeapon(SaveSlotData slot)
    {
        if (!EquipmentCatalog.TryResolveSavedWeaponId(
                slot.EquippedWeaponId,
                out WeaponDefinition definition))
        {
            StartupLog.Write(
                EquipmentCatalog.IsRemovedWeaponId(slot.EquippedWeaponId)
                    ? $"[SAVE MIGRATION] Removed equipped weapon ID " +
                        $"'{slot.EquippedWeaponId}' replaced with " +
                        $"'{EquipmentCatalog.KnightLongSword.WeaponId}'."
                    : $"[SAVE MIGRATION] Unknown equipped weapon ID " +
                $"'{slot.EquippedWeaponId}' replaced with " +
                $"'{EquipmentCatalog.KnightLongSword.WeaponId}'.");
            definition = EquipmentCatalog.KnightLongSword;
        }
        return new EquipmentItem(definition, slot.EquippedWeaponRarity);
    }

    public static EquipmentItem CreateArmor(SaveSlotData slot)
    {
        ArmorDefinition definition = slot.EquippedArmorId.StartsWith(
            "generated-",
            StringComparison.OrdinalIgnoreCase)
            ? EquipmentCatalog.CreateGeneratedArmor(
                slot.EquippedArmorClass,
                slot.EquippedArmorTier,
                slot.EquippedArmorHealthBonus)
            : EquipmentCatalog.ResolveSavedArmorId(slot.EquippedArmorId);
        return new EquipmentItem(
            definition,
            slot.EquippedArmorRarity,
            slot.EquippedArmorGrade);
    }

    public static bool TryCreateInventoryItem(
        SavedEquipmentItemData saved,
        out EquipmentItem item)
    {
        item = null;
        if (saved == null)
            return false;

        if (saved.Slot == EquipmentSlot.Weapon)
        {
            if (!EquipmentCatalog.TryResolveSavedWeaponId(
                    saved.DefinitionId,
                    out WeaponDefinition definition))
            {
                StartupLog.Write(
                    EquipmentCatalog.IsRemovedWeaponId(saved.DefinitionId)
                        ? $"[SAVE MIGRATION] Removed obsolete inventory weapon ID " +
                            $"'{saved.DefinitionId}'."
                        : $"[SAVE MIGRATION] Skipped unknown inventory weapon ID " +
                            $"'{saved.DefinitionId}'.");
                return false;
            }

            item = new EquipmentItem(definition, saved.Rarity);
            return true;
        }

        ArmorDefinition armor = saved.DefinitionId.StartsWith(
            "generated-",
            StringComparison.OrdinalIgnoreCase)
            ? EquipmentCatalog.CreateGeneratedArmor(
                saved.ArmorClass,
                saved.Tier,
                saved.HealthBonus)
            : EquipmentCatalog.ResolveSavedArmorId(saved.DefinitionId);
        item = new EquipmentItem(armor, saved.Rarity, saved.ArmorGrade);
        return true;
    }
}
