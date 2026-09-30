using System;

namespace DungeonAscendant.Core;

/// <summary>
/// Owns the platform-independent game state and update flow.
/// </summary>
public sealed class GameSession
{
    public void Update(TimeSpan elapsedTime)
    {
        // Game systems will be updated here as they are introduced.
    }
}
