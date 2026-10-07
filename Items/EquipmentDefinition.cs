using System;

namespace DungeonAscendant.Items;

public abstract class EquipmentDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int Tier { get; }
    public EquipmentSlot Slot { get; }

    protected EquipmentDefinition(
        string id,
        string name,
        int tier,
        EquipmentSlot slot)
    {
        Id = string.IsNullOrWhiteSpace(id) ? "equipment" : id;
        Name = string.IsNullOrWhiteSpace(name) ? "Equipment" : name;
        Tier = Math.Max(1, tier);
        Slot = slot;
    }
}
