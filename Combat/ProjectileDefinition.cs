using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public sealed class ProjectileDefinition
{
    public ProjectileType Type { get; }
    public float Speed { get; }
    public Vector2 Size { get; }
    public float LifetimeSeconds { get; }
    public ProjectileTrajectoryType Trajectory { get; }
    public float Gravity { get; }
    public int MaximumRicochets { get; }
    public int RemainingPierces { get; }
    public float ImpactRadius { get; }
    public int SplitCount { get; }
    public bool PenetratesTerrain { get; }

    public ProjectileDefinition(
        ProjectileType type,
        float speed,
        Vector2 size,
        float lifetimeSeconds,
        ProjectileTrajectoryType trajectory = ProjectileTrajectoryType.Straight,
        float gravity = 0f,
        int maximumRicochets = 0,
        int remainingPierces = 0,
        float impactRadius = 0f,
        int splitCount = 0,
        bool penetratesTerrain = false)
    {
        Type = type;
        Speed = speed;
        Size = size;
        LifetimeSeconds = lifetimeSeconds;
        Trajectory = trajectory;
        Gravity = gravity;
        MaximumRicochets = maximumRicochets;
        RemainingPierces = remainingPierces;
        ImpactRadius = impactRadius;
        SplitCount = splitCount;
        PenetratesTerrain = penetratesTerrain;
    }
}
