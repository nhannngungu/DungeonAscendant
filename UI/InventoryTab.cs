using System;
using System.Collections.Generic;
using DungeonAscendant.Items;

namespace DungeonAscendant.UI;

public enum InventoryTab
{
    Weapons,
    Equipment,
    Notes
}

public static class InventoryTabRules
{
    public static bool Includes(InventoryTab tab, EquipmentItem item)
    {
        if (item == null)
            return false;

        return tab switch
        {
            InventoryTab.Weapons => item.Slot == EquipmentSlot.Weapon,
            InventoryTab.Equipment => item.Slot != EquipmentSlot.Weapon,
            _ => false
        };
    }

    public static string GetEmptyMessage(InventoryTab tab)
    {
        return tab == InventoryTab.Weapons
            ? "NO WEAPONS"
            : tab == InventoryTab.Equipment
                ? "NO EQUIPMENT"
                : "NO NOTES";
    }

    public static void PopulateView(
        IReadOnlyList<EquipmentItem> inventoryItems,
        InventoryTab tab,
        List<EquipmentItem> destination)
    {
        destination.Clear();

        if (inventoryItems == null)
            return;

        foreach (EquipmentItem item in inventoryItems)
        {
            if (Includes(tab, item))
                destination.Add(item);
        }
    }

    public static void ValidateOrThrow()
    {
        EquipmentItem weapon = EquipmentCatalog.CreateStartingWeapon();
        EquipmentItem armor = EquipmentCatalog.CreateStartingArmor();

        Require(Includes(InventoryTab.Weapons, weapon), "weapon include");
        Require(!Includes(InventoryTab.Weapons, armor), "weapon exclude");
        Require(Includes(InventoryTab.Equipment, armor), "equipment include");
        Require(!Includes(InventoryTab.Equipment, weapon), "equipment exclude");
        Require(!Includes(InventoryTab.Weapons, null), "null safety");
        Require(!Includes(InventoryTab.Notes, weapon), "notes exclude weapon");
        Require(!Includes(InventoryTab.Notes, armor), "notes exclude armor");

        EquipmentItem[] source = { weapon, armor };
        var view = new List<EquipmentItem>();
        PopulateView(source, InventoryTab.Weapons, view);
        Require(view.Count == 1 && ReferenceEquals(view[0], weapon),
            "weapon view reference");
        PopulateView(source, InventoryTab.Equipment, view);
        Require(view.Count == 1 && ReferenceEquals(view[0], armor),
            "equipment view reference");
        PopulateView(Array.Empty<EquipmentItem>(), InventoryTab.Weapons, view);
        Require(view.Count == 0, "empty view");
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Inventory tab filtering failed: {scenario}.");
        }
    }
}
