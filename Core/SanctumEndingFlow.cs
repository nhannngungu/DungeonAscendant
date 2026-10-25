using System;

namespace DungeonAscendant.Core;

/// <summary>One short Map 7 epilogue, with no scene graph or external assets.</summary>
public sealed class SanctumEndingFlow
{
    public const float DurationSeconds = 7.2f;
    public bool IsRunning { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int TriggerCount { get; private set; }
    public float FadeRatio => Math.Clamp(ElapsedSeconds / 1.2f, 0f, 1f);
    public bool FocusThrone => ElapsedSeconds < 2.4f;
    public string TextKey => ElapsedSeconds < 2.4f ? "sanctum.ending.line1" :
        ElapsedSeconds < 4.8f ? "sanctum.ending.line2" : "sanctum.ending.line3";
    public bool TryBegin(bool unlocked, bool seen)
    {
        if (!unlocked || seen || IsRunning) return false;
        IsRunning = true; ElapsedSeconds = 0; TriggerCount++;
        return true;
    }
    public bool Update(float seconds)
    {
        if (!IsRunning) return false;
        ElapsedSeconds = MathF.Min(DurationSeconds, ElapsedSeconds + Math.Clamp(seconds, 0f, .1f));
        if (ElapsedSeconds < DurationSeconds - .0001f) return false;
        IsRunning = false;
        return true;
    }
    public void Reset()
    { IsRunning = false; ElapsedSeconds = 0; TriggerCount = 0; }
}
