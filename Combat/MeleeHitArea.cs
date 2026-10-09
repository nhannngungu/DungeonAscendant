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

        if (shape is AttackHitboxShape.Circular or
            AttackHitboxShape.TargetZone)
        {
            int diameter = Math.Max(1, horizontalReach * 2);
            int height = Math.Max(1, verticalThickness);
            return new Rectangle(
                (int)attackerPosition.X - diameter / 2,
                centerY - height / 2,
                diameter,
                height);
        }

        int rearOverlap = shape == AttackHitboxShape.WideArc
            ? Math.Max(8, horizontalReach / 5)
            : shape == AttackHitboxShape.Cross
                ? Math.Max(10, horizontalReach / 4)
            : 0;
        int forwardGap = shape == AttackHitboxShape.Thrust
            ? Math.Max(5, horizontalReach / 12)
            : 0;
        int verticalOffset = shape == AttackHitboxShape.Chop
            ? 8
            : shape == AttackHitboxShape.Thrust ? -5 : 0;
        int width = Math.Max(1, horizontalReach - forwardGap + rearOverlap);

        if (shape == AttackHitboxShape.FrontalFan)
        {
            verticalThickness = Math.Max(
                verticalThickness,
                (int)MathF.Round(reach * .72f));
        }

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
