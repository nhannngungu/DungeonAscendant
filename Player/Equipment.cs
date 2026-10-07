using DungeonAscendant.Items;

namespace DungeonAscendant.Player;

public sealed class Equipment
{
    public EquipmentItem Weapon { get; private set; }
    public EquipmentItem Armor { get; private set; }
    public EquipmentItem Charm1 { get; private set; }
    public EquipmentItem Charm2 { get; private set; }

    public EquipmentItem GetEquipped(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => Weapon,
            EquipmentSlot.Armor => Armor,
            EquipmentSlot.Charm1 => Charm1,
            EquipmentSlot.Charm2 => Charm2,
            _ => null
        };
    }

    public void SetStartingItems(EquipmentItem weapon, EquipmentItem armor)
    {
        Weapon = weapon;
        Armor = armor;
    }

    public void SetDebugItem(EquipmentItem item)
    {
        if (item == null ||
            (item.Slot != EquipmentSlot.Weapon && item.Slot != EquipmentSlot.Armor))
        {
            return;
        }

        SetEquipped(item);
    }

    public bool TryEquip(
        EquipmentItem item,
        Inventory inventory,
        bool avoidDuplicateItemIds = false)
    {
        if (item == null || inventory == null ||
            (item.Slot != EquipmentSlot.Weapon && item.Slot != EquipmentSlot.Armor) ||
            !inventory.Remove(item))
            return false;

        EquipmentItem previousItem = GetEquipped(item.Slot);
        SetEquipped(item);

        if (previousItem == null ||
            (avoidDuplicateItemIds && inventory.ContainsItemId(previousItem.Id)) ||
            inventory.TryAdd(previousItem))
            return true;

        SetEquipped(previousItem);
        inventory.TryAdd(item);
        return false;
    }

    private void SetEquipped(EquipmentItem item)
    {
        switch (item.Slot)
        {
            case EquipmentSlot.Weapon: Weapon = item; break;
            case EquipmentSlot.Armor: Armor = item; break;
            case EquipmentSlot.Charm1: Charm1 = item; break;
            case EquipmentSlot.Charm2: Charm2 = item; break;
        }
    }
}
