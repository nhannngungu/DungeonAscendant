using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns the Goblin's gameplay state and basic chase behavior.
/// </summary>
public sealed class Goblin
{
    private const float StopDistance = 8f;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; }
    public float MovementSpeed { get; }
    public float DetectionRange { get; }

    public Goblin(
        Vector2 position,
        float movementSpeed = 110f,
        float detectionRange = 200f)
    {
        Position = position;
        Size = new Vector2(36f, 44f);
        MovementSpeed = movementSpeed;
        DetectionRange = detectionRange;
    }

    public void Update(GameTime gameTime, Vector2 playerPosition)
    {
        Vector2 toPlayer = playerPosition - Position;
        float distanceSquared = toPlayer.LengthSquared();

        if (distanceSquared > DetectionRange * DetectionRange ||
            distanceSquared <= StopDistance * StopDistance)
        {
            return;
        }

        float distance = MathF.Sqrt(distanceSquared);
        Vector2 direction = toPlayer / distance;
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementDistance = MathF.Min(
            MovementSpeed * elapsedSeconds,
            distance - StopDistance);

        Position += direction * movementDistance;
    }
}
