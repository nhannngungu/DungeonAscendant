using System;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Reusable, elapsed-time animation metadata. A frame can represent a sprite
/// sheet cell or, as in Visual Pass 1, a procedural character pose.
/// </summary>
public sealed class SpriteAnimation
{
    public int FrameCount { get; }
    public float FrameDuration { get; }
    public bool IsLooping { get; }
    public float Duration => FrameCount * FrameDuration;

    public SpriteAnimation(
        int frameCount,
        float frameDuration,
        bool isLooping)
    {
        FrameCount = Math.Max(1, frameCount);
        FrameDuration = MathF.Max(0.001f, frameDuration);
        IsLooping = isLooping;
    }

    public int GetFrame(float elapsedSeconds)
    {
        int frame = (int)(MathF.Max(0f, elapsedSeconds) / FrameDuration);

        if (IsLooping)
            return frame % FrameCount;

        return Math.Min(frame, FrameCount - 1);
    }

    public float GetProgress(float elapsedSeconds)
    {
        if (Duration <= 0f)
            return 0f;

        float progress = MathF.Max(0f, elapsedSeconds) / Duration;
        return IsLooping
            ? progress - MathF.Floor(progress)
            : Math.Clamp(progress, 0f, 1f);
    }
}
