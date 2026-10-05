using DungeonAscendant.Items;

namespace DungeonAscendant.Player;

public sealed class Equipment
{
    public EquipmentItem Weapon { get; private set; }
    public EquipmentItem Armor { get; private set; }

    public EquipmentItem GetEquipped(EquipmentSlot slot)
    {
        return slot == EquipmentSlot.Weapon ? Weapon : Armor;
    }

    public bool TryEquip(EquipmentItem item, Inventory inventory)
    {
        if (item == null || inventory == null || !inventory.Remove(item))
            return false;

        EquipmentItem previousItem = GetEquipped(item.Slot);
        SetEquipped(item);

        if (previousItem == null || inventory.TryAdd(previousItem))
            return true;

        SetEquipped(previousItem);
        inventory.TryAdd(item);
        return false;
    }

    private void SetEquipped(EquipmentItem item)
    {
        if (item.Slot == EquipmentSlot.Weapon)
            Weapon = item;
        else
            Armor = item;
    }
}
