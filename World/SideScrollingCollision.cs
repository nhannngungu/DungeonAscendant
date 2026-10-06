using System;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public readonly struct MovementResult
{
    public Vector2 Position { get; }
    public Vector2 Velocity { get; }
    public bool IsGrounded { get; }
    public bool HitWall { get; }
    public bool HitCeiling { get; }

    public MovementResult(
        Vector2 position,
        Vector2 velocity,
        bool isGrounded,
        bool hitWall,
        bool hitCeiling)
    {
        Position = position;
        Velocity = velocity;
        IsGrounded = isGrounded;
        HitWall = hitWall;
        HitCeiling = hitCeiling;
    }
}

/// <summary>
/// Deterministic axis-separated rectangle collision for side-view entities.
/// Movement is subdivided to avoid tunnelling through thin platforms.
/// </summary>
public static class SideScrollingCollision
{
    private const float MaximumStepDistance = 6f;
    private const int GroundProbeDistance = 2;

    public static MovementResult Resolve(
        Vector2 position,
        Vector2 velocity,
        Vector2 size,
        float elapsedSeconds,
        DungeonMap dungeon)
    {
        Vector2 movement = velocity * elapsedSeconds;
        int stepCount = Math.Max(
            1,
            (int)MathF.Ceiling(MathF.Max(
                MathF.Abs(movement.X),
                MathF.Abs(movement.Y)) / MaximumStepDistance));
        Vector2 step = movement / stepCount;
        bool hitWall = false;
        bool hitCeiling = false;
        bool landed = false;

        for (int index = 0; index < stepCount; index++)
        {
            if (step.X != 0f)
            {
                Vector2 horizontal = new(position.X + step.X, position.Y);

                if (IsPositionFree(horizontal, size, dungeon))
                    position.X = horizontal.X;
                else
                {
                    position.X = MoveUntilContact(position, size, step.X, true, dungeon).X;
                    velocity.X = 0f;
                    hitWall = true;
                }
            }

            if (step.Y != 0f)
            {
                Vector2 vertical = new(position.X, position.Y + step.Y);

                if (IsPositionFree(vertical, size, dungeon))
                    position.Y = vertical.Y;
                else
                {
                    position.Y = MoveUntilContact(position, size, step.Y, false, dungeon).Y;
                    landed |= step.Y > 0f;
                    hitCeiling |= step.Y < 0f;
                    velocity.Y = 0f;
                }
            }
        }

        bool grounded = landed || IsSupported(position, size, dungeon);

        if (grounded && velocity.Y > 0f)
            velocity.Y = 0f;

        return new MovementResult(position, velocity, grounded, hitWall, hitCeiling);
    }

    public static Vector2 ResolveDisplacement(
        Vector2 position,
        Vector2 displacement,
        Vector2 size,
        DungeonMap dungeon)
    {
        return Resolve(position, displacement, size, 1f, dungeon).Position;
    }

    public static bool IsSupported(
        Vector2 position,
        Vector2 size,
        DungeonMap dungeon)
    {
        return !IsPositionFree(
            position + new Vector2(0f, GroundProbeDistance),
            size,
            dungeon);
    }

    public static bool IsPositionFree(
        Vector2 position,
        Vector2 size,
        DungeonMap dungeon)
    {
        Rectangle bounds = DungeonCollision.CreateBounds(position, size);
        Rectangle world = dungeon.WorldBounds;

        if (bounds.Left < world.Left || bounds.Right > world.Right ||
            bounds.Top < world.Top || bounds.Bottom > world.Bottom)
        {
            return false;
        }

        foreach (Platform platform in dungeon.Platforms)
        {
            if (bounds.Intersects(platform.Bounds))
                return false;
        }

        return true;
    }

    public static Vector2 PlaceOnGround(
        float x,
        Vector2 size,
        DungeonRoom room)
    {
        float halfWidth = size.X / 2f;
        float clampedX = MathHelper.Clamp(
            x,
            room.Bounds.Left + halfWidth + 8f,
            room.Bounds.Right - halfWidth - 8f);
        return new Vector2(clampedX, room.GroundY - size.Y / 2f);
    }

    private static Vector2 MoveUntilContact(
        Vector2 position,
        Vector2 size,
        float amount,
        bool horizontal,
        DungeonMap dungeon)
    {
        float direction = MathF.Sign(amount);
        float remaining = MathF.Abs(amount);

        while (remaining > 0.01f)
        {
            float distance = MathF.Min(1f, remaining) * direction;
            Vector2 candidate = horizontal
                ? position + new Vector2(distance, 0f)
                : position + new Vector2(0f, distance);

            if (!IsPositionFree(candidate, size, dungeon))
                break;

            position = candidate;
            remaining -= MathF.Abs(distance);
        }

        return position;
    }
}
