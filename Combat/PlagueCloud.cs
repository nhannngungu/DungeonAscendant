using System;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public enum PlagueCloudState
{
    Warning,
    Active
}

/// <summary>
/// Small, bounded Map 3 area-denial field. It deliberately uses neither the
/// Catacombs Curse meter nor permanent stacking status effects.
/// </summary>
public sealed class PlagueCloud
{
    public const float FlaskWarningSeconds = .48f;
    public const float FlaskActiveSeconds = 3.25f;
    public const float CollapseWarningSeconds = .68f;
    public const float CollapseActiveSeconds = 2.15f;
    public const float DamageIntervalSeconds = .90f;
    public const float SlowMultiplier = .78f;
    public const float SlowRefreshSeconds = .28f;
    public const int PeriodicDamage = 3;

    public Vector2 Position { get; }
    public Vector2 Size { get; }
    public int RoomId { get; }
    public bool IsCollapseCloud { get; }
    public PlagueCloudState State { get; internal set; } =
        PlagueCloudState.Warning;
    public float TimeRemaining { get; internal set; }
    public float WarningDurationSeconds { get; }
    public float ActiveDurationSeconds { get; }
    public Rectangle Bounds => DungeonCollision.CreateBounds(Position, Size);
    public float PhaseProgress
    {
        get
        {
            float duration = State == PlagueCloudState.Warning
                ? WarningDurationSeconds
                : ActiveDurationSeconds;
            return duration <= 0f
                ? 1f
                : Math.Clamp(1f - TimeRemaining / duration, 0f, 1f);
        }
    }

    public PlagueCloud(Vector2 position, int roomId, bool collapseCloud)
    {
        Position = position;
        RoomId = roomId;
        IsCollapseCloud = collapseCloud;
        Size = collapseCloud
            ? new Vector2(82f, 24f)
            : new Vector2(108f, 30f);
        WarningDurationSeconds = collapseCloud
            ? CollapseWarningSeconds
            : FlaskWarningSeconds;
        ActiveDurationSeconds = collapseCloud
            ? CollapseActiveSeconds
            : FlaskActiveSeconds;
        TimeRemaining = WarningDurationSeconds;
    }

    internal void Refresh(bool collapseCloud)
    {
        if (State == PlagueCloudState.Warning)
        {
            TimeRemaining = MathF.Max(
                TimeRemaining,
                collapseCloud
                    ? CollapseWarningSeconds
                    : FlaskWarningSeconds);
            return;
        }

        TimeRemaining = MathF.Min(
            MathF.Max(TimeRemaining, collapseCloud
                ? CollapseActiveSeconds
                : FlaskActiveSeconds),
            FlaskActiveSeconds);
    }
}
