using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public readonly struct EnemyDisplacementResult
{
    public Vector2 StartPosition { get; }
    public Vector2 EndPosition { get; }
    public float RequestedDistance { get; }
    public bool HitWall { get; }
    public float DistanceMoved =>
        MathF.Abs(EndPosition.X - StartPosition.X);

    public EnemyDisplacementResult(
        Vector2 startPosition,
        Vector2 endPosition,
        float requestedDistance,
        bool hitWall)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        RequestedDistance = MathF.Max(0f, requestedDistance);
        HitWall = hitWall;
    }
}
