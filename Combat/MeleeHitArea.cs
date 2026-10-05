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
        int horizontalThickness = (int)thickness;
        int verticalThickness = (int)thickness;
        int horizontalReach = (int)reach;
        int verticalReach = (int)reach;
        int centerX = (int)attackerPosition.X;
        int centerY = (int)attackerPosition.Y;

        return facingDirection switch
        {
            FacingDirection.Up => new Rectangle(
                centerX - horizontalThickness / 2,
                (int)(attackerPosition.Y - attackerSize.Y / 2f) - verticalReach,
                horizontalThickness,
                verticalReach),
            FacingDirection.Down => new Rectangle(
                centerX - horizontalThickness / 2,
                (int)(attackerPosition.Y + attackerSize.Y / 2f),
                horizontalThickness,
                verticalReach),
            FacingDirection.Left => new Rectangle(
                (int)(attackerPosition.X - attackerSize.X / 2f) - horizontalReach,
                centerY - verticalThickness / 2,
                horizontalReach,
                verticalThickness),
            _ => new Rectangle(
                (int)(attackerPosition.X + attackerSize.X / 2f),
                centerY - verticalThickness / 2,
                horizontalReach,
                verticalThickness)
        };
    }
}
