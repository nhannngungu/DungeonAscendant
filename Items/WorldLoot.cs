using Microsoft.Xna.Framework;

namespace DungeonAscendant.Items;

public sealed class WorldLoot
{
    public static readonly Vector2 Size = new(18f, 18f);

    public EquipmentItem Item { get; }
    public Vector2 Position { get; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)Size.X,
        (int)Size.Y);

    public WorldLoot(EquipmentItem item, Vector2 position)
    {
        Item = item;
        Position = position;
    }
}
