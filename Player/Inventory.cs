using System.Collections.Generic;
using DungeonAscendant.Items;

namespace DungeonAscendant.Player;

public sealed class Inventory
{
    public const int DefaultCapacity = 12;

    private readonly List<EquipmentItem> _items;

    public int Capacity { get; }
    public int Count => _items.Count;
    public bool IsFull => Count >= Capacity;
    public IReadOnlyList<EquipmentItem> Items => _items;

    public Inventory(int capacity = DefaultCapacity)
    {
        Capacity = capacity > 0 ? capacity : DefaultCapacity;
        _items = new List<EquipmentItem>(Capacity);
    }

    public bool TryAdd(EquipmentItem item)
    {
        if (item == null || IsFull)
            return false;

        _items.Add(item);
        return true;
    }

    public bool Remove(EquipmentItem item)
    {
        return item != null && _items.Remove(item);
    }

    public bool ContainsItemId(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) &&
            _items.Exists(item => item.Id == itemId);
    }

    public int CountMatchingFusionMaterials(EquipmentItem source)
    {
        if (source?.ArmorDefinition == null)
            return 0;

        int count = 0;

        foreach (EquipmentItem item in _items)
        {
            if (IsSameFusionMaterial(item, source))
                count++;
        }

        return count;
    }

    public bool CanFuseArmor(EquipmentItem source)
    {
        return source?.ArmorDefinition != null &&
            ArmorGradeRules.CanAdvance(source.ArmorGrade) &&
            CountMatchingFusionMaterials(source) >=
                ArmorGradeRules.FusionMaterialCount;
    }

    public bool TryFuseArmor(
        EquipmentItem source,
        out EquipmentItem result)
    {
        result = null;

        if (!CanFuseArmor(source))
            return false;

        int remaining = ArmorGradeRules.FusionMaterialCount;

        for (int index = _items.Count - 1;
             index >= 0 && remaining > 0;
             index--)
        {
            if (!IsSameFusionMaterial(_items[index], source))
                continue;

            _items.RemoveAt(index);
            remaining--;
        }

        result = new EquipmentItem(
            source.ArmorDefinition,
            source.Rarity,
            ArmorGradeRules.Next(source.ArmorGrade));
        _items.Add(result);
        return true;
    }

    public EquipmentItem GetItem(int index)
    {
        return index >= 0 && index < Count
            ? _items[index]
            : null;
    }

    private static bool IsSameFusionMaterial(
        EquipmentItem item,
        EquipmentItem source)
    {
        return item?.ArmorDefinition != null &&
            source?.ArmorDefinition != null &&
            item.ArmorDefinition.Id == source.ArmorDefinition.Id &&
            item.ArmorGrade == source.ArmorGrade &&
            item.Rarity == source.Rarity;
    }
}
