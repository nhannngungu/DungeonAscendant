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

    public EquipmentItem GetItem(int index)
    {
        return index >= 0 && index < Count
            ? _items[index]
            : null;
    }
}
