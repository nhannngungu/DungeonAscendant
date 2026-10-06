using DungeonAscendant.Player;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// Creates the Player's directional rectangular melee region.
/// </summary>
public static class MeleeHitArea
{
    public static Rectangle Create(
        Vector2 attackerPosition,
        Vector2 attackerSize,
        FacingDirection facingDirection,
        float reach,
        float thickness)
    {
        int verticalThickness = (int)thickness;
        int horizontalReach = (int)reach;
        int centerY = (int)attackerPosition.Y;

        if (facingDirection == FacingDirection.Left)
        {
            return new Rectangle(
                (int)(attackerPosition.X - attackerSize.X / 2f) - horizontalReach,
                centerY - verticalThickness / 2,
                horizontalReach,
                verticalThickness);
        }

        return new Rectangle(
            (int)(attackerPosition.X + attackerSize.X / 2f),
            centerY - verticalThickness / 2,
            horizontalReach,
            verticalThickness);
    }
}
