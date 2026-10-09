using System;
using System.Collections.Generic;
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
    public const float DefaultEntryDropOffset = 40f;
    public const float DefaultEntrySearchRadius = 128f;
    private const float EntrySearchStep = 16f;
    private const float EntryClearanceProbeStep = 4f;

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

    public static Vector2 ResolveSafeEntrySpawn(
        float intendedX,
        Vector2 size,
        DungeonMap dungeon,
        float dropOffset = DefaultEntryDropOffset,
        float horizontalSearchRadius = DefaultEntrySearchRadius,
        IReadOnlyList<Rectangle> blockedBounds = null)
    {
        if (dungeon == null)
            throw new ArgumentNullException(nameof(dungeon));
        if (size.X <= 0f || size.Y <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size));

        dropOffset = MathF.Max(0f, dropOffset);
        horizontalSearchRadius = MathF.Max(0f, horizontalSearchRadius);
        float halfWidth = size.X / 2f;
        float minimumX = dungeon.WorldBounds.Left + halfWidth;
        float maximumX = dungeon.WorldBounds.Right - halfWidth;
        int searchSteps = (int)MathF.Ceiling(
            horizontalSearchRadius / EntrySearchStep);

        for (int step = 0; step <= searchSteps; step++)
        {
            if (step == 0)
            {
                float x = MathHelper.Clamp(intendedX, minimumX, maximumX);
                if (TryResolveSafeEntryAtX(
                    x, size, dungeon, dropOffset, blockedBounds,
                    out Vector2 spawn))
                {
                    return spawn;
                }

                continue;
            }

            float distance = MathF.Min(
                horizontalSearchRadius,
                step * EntrySearchStep);
            float right = MathHelper.Clamp(
                intendedX + distance, minimumX, maximumX);
            if (TryResolveSafeEntryAtX(
                right, size, dungeon, dropOffset, blockedBounds,
                out Vector2 rightSpawn))
            {
                return rightSpawn;
            }

            float left = MathHelper.Clamp(
                intendedX - distance, minimumX, maximumX);
            if (left != right && TryResolveSafeEntryAtX(
                left, size, dungeon, dropOffset, blockedBounds,
                out Vector2 leftSpawn))
            {
                return leftSpawn;
            }
        }

        throw new InvalidOperationException(
            $"No safe entry floor was found within {horizontalSearchRadius:0} px of X={intendedX:0}.");
    }

    private static bool TryResolveSafeEntryAtX(
        float x,
        Vector2 size,
        DungeonMap dungeon,
        float dropOffset,
        IReadOnlyList<Rectangle> blockedBounds,
        out Vector2 spawn)
    {
        spawn = Vector2.Zero;
        float halfWidth = size.X / 2f;
        int bestFloorTop = int.MaxValue;

        foreach (Platform platform in dungeon.Platforms)
        {
            if (x - halfWidth < platform.Bounds.Left ||
                x + halfWidth > platform.Bounds.Right ||
                platform.Bounds.Top >= bestFloorTop)
            {
                continue;
            }

            Vector2 standing = new(
                x,
                platform.Bounds.Top - size.Y / 2f);
            Vector2 candidate = standing - new Vector2(0f, dropOffset);
            if (!HasClearEntryDrop(
                candidate, standing, size, dungeon, blockedBounds))
            {
                continue;
            }

            bestFloorTop = platform.Bounds.Top;
            spawn = candidate;
        }

        return bestFloorTop != int.MaxValue;
    }

    private static bool HasClearEntryDrop(
        Vector2 spawn,
        Vector2 standing,
        Vector2 size,
        DungeonMap dungeon,
        IReadOnlyList<Rectangle> blockedBounds)
    {
        float distance = standing.Y - spawn.Y;
        int steps = Math.Max(
            1,
            (int)MathF.Ceiling(distance / EntryClearanceProbeStep));
        for (int step = 0; step <= steps; step++)
        {
            Vector2 position = Vector2.Lerp(
                spawn,
                standing,
                step / (float)steps);
            if (!IsPositionFree(position, size, dungeon) ||
                IntersectsAny(position, size, blockedBounds))
            {
                return false;
            }
        }

        return IsSupported(standing, size, dungeon);
    }

    private static bool IntersectsAny(
        Vector2 position,
        Vector2 size,
        IReadOnlyList<Rectangle> blockedBounds)
    {
        if (blockedBounds == null)
            return false;

        Rectangle bounds = DungeonCollision.CreateBounds(position, size);
        foreach (Rectangle blocked in blockedBounds)
        {
            if (bounds.Intersects(blocked))
                return true;
        }

        return false;
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
