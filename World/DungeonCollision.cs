using System;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public static class DungeonCollision
{
    public static Vector2 ResolveMovement(
        Vector2 currentPosition,
        Vector2 desiredPosition,
        Vector2 entitySize,
        DungeonMap dungeon)
    {
        return SideScrollingCollision.ResolveDisplacement(
            currentPosition,
            desiredPosition - currentPosition,
            entitySize,
            dungeon);
    }

    public static bool IsWalkable(Rectangle bounds, DungeonMap dungeon)
    {
        Vector2 position = bounds.Center.ToVector2();
        Vector2 size = new(bounds.Width, bounds.Height);
        return SideScrollingCollision.IsPositionFree(position, size, dungeon);
    }

    public static Rectangle CreateBounds(Vector2 position, Vector2 size)
    {
        return new Rectangle(
            (int)(position.X - size.X / 2f),
            (int)(position.Y - size.Y / 2f),
            (int)size.X,
            (int)size.Y);
    }

}
