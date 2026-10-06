using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public readonly struct AttackContact
{
    public int Damage { get; }
    public Vector2 SourcePosition { get; }
    public bool Blockable { get; }
    public bool Unblockable { get; }
    public Rectangle HitArea { get; }

    public AttackContact(
        int damage,
        Vector2 sourcePosition,
        bool blockable = true,
        bool unblockable = false,
        Rectangle hitArea = default)
    {
        Damage = damage;
        SourcePosition = sourcePosition;
        Blockable = blockable;
        Unblockable = unblockable;
        HitArea = hitArea;
    }
}
