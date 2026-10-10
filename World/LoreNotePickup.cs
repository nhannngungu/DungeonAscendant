using DungeonAscendant.Lore;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public sealed class LoreNotePickup
{
    public const float InteractionRadius = 76f;
    public static readonly Vector2 Size = new(28f, 22f);

    public LoreNoteDefinition Definition { get; }
    public Vector2 Position { get; }
    public bool IsCollected { get; private set; }
    public Rectangle Bounds => new(
        (int)(Position.X - Size.X / 2f),
        (int)(Position.Y - Size.Y / 2f),
        (int)Size.X,
        (int)Size.Y);

    public LoreNotePickup(LoreNoteDefinition definition, Vector2 position)
    {
        Definition = definition;
        Position = position;
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsCollected &&
        Vector2.DistanceSquared(Position, playerPosition) <=
            InteractionRadius * InteractionRadius;

    public bool TryCollect(
        Vector2 playerPosition,
        LoreCollection collection,
        out LoreNote note)
    {
        note = null;
        if (!CanInteract(playerPosition) || collection == null)
            return false;

        bool added = collection.TryCollect(Definition, out note);
        IsCollected = added || collection.Contains(Definition.Id);
        return added;
    }
}
