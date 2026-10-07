using System;
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
        float thickness,
        AttackHitboxShape shape)
    {
        int verticalThickness = (int)thickness;
        int horizontalReach = (int)reach;
        int centerY = (int)attackerPosition.Y;
        int bodyEdge = (int)(attackerSize.X / 2f);
        int rearOverlap = shape == AttackHitboxShape.WideArc
            ? Math.Max(8, horizontalReach / 5)
            : 0;
        int forwardGap = shape == AttackHitboxShape.Thrust
            ? Math.Max(5, horizontalReach / 12)
            : 0;
        int verticalOffset = shape == AttackHitboxShape.Chop
            ? 8
            : shape == AttackHitboxShape.Thrust ? -5 : 0;
        int width = Math.Max(1, horizontalReach - forwardGap + rearOverlap);

        if (facingDirection == FacingDirection.Left)
        {
            return new Rectangle(
                (int)attackerPosition.X - bodyEdge - horizontalReach + forwardGap,
                centerY - verticalThickness / 2 + verticalOffset,
                width,
                verticalThickness);
        }

        return new Rectangle(
            (int)attackerPosition.X + bodyEdge + forwardGap - rearOverlap,
            centerY - verticalThickness / 2 + verticalOffset,
            width,
            verticalThickness);
    }
}
