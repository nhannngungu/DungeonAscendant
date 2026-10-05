using System;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public static class DungeonCollision
{
    private const float MaximumStepDistance = 8f;

    public static Vector2 ResolveMovement(
        Vector2 currentPosition,
        Vector2 desiredPosition,
        Vector2 entitySize,
        DungeonMap dungeon)
    {
        Vector2 movement = desiredPosition - currentPosition;
        int stepCount = Math.Max(
            1,
            (int)MathF.Ceiling(MathF.Max(
                MathF.Abs(movement.X),
                MathF.Abs(movement.Y)) / MaximumStepDistance));
        Vector2 step = movement / stepCount;
        Vector2 resolvedPosition = currentPosition;

        for (int index = 0; index < stepCount; index++)
        {
            Vector2 horizontalPosition = new(
                resolvedPosition.X + step.X,
                resolvedPosition.Y);

            if (IsWalkable(CreateBounds(horizontalPosition, entitySize), dungeon))
                resolvedPosition.X = horizontalPosition.X;

            Vector2 verticalPosition = new(
                resolvedPosition.X,
                resolvedPosition.Y + step.Y);

            if (IsWalkable(CreateBounds(verticalPosition, entitySize), dungeon))
                resolvedPosition.Y = verticalPosition.Y;
        }

        return resolvedPosition;
    }

    public static bool IsWalkable(Rectangle bounds, DungeonMap dungeon)
    {
        foreach (DungeonRoom room in dungeon.Rooms)
        {
            if (Contains(room.Bounds, bounds))
                return true;
        }

        foreach (Rectangle corridor in dungeon.Corridors)
        {
            if (Contains(corridor, bounds))
                return true;
        }

        return false;
    }

    public static Rectangle CreateBounds(Vector2 position, Vector2 size)
    {
        return new Rectangle(
            (int)(position.X - size.X / 2f),
            (int)(position.Y - size.Y / 2f),
            (int)size.X,
            (int)size.Y);
    }

    private static bool Contains(Rectangle container, Rectangle bounds)
    {
        return bounds.Left >= container.Left &&
            bounds.Right <= container.Right &&
            bounds.Top >= container.Top &&
            bounds.Bottom <= container.Bottom;
    }
}
