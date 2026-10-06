using System.Collections.Generic;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonRoom
{
    private readonly List<int> _connections = new();
    private readonly List<Platform> _platforms = new();

    public int Id { get; }
    public Rectangle Bounds { get; }
    public RoomType Type { get; internal set; }
    public IReadOnlyList<int> Connections => _connections;
    public IReadOnlyList<Platform> Platforms => _platforms;
    public Vector2 Center => Bounds.Center.ToVector2();
    public int GroundY { get; internal set; }
    public Vector2 Entrance => new(Bounds.Left + 96f, GroundY);
    public Vector2 Exit => new(Bounds.Right - 96f, GroundY);

    public DungeonRoom(int id, Rectangle bounds, RoomType type)
    {
        Id = id;
        Bounds = bounds;
        Type = type;
        GroundY = bounds.Bottom - 72;
    }

    internal void ConnectTo(int roomId)
    {
        if (!_connections.Contains(roomId))
            _connections.Add(roomId);
    }

    internal void AddPlatform(Platform platform)
    {
        _platforms.Add(platform);
    }

    internal void ClearOptionalPlatforms()
    {
        _platforms.RemoveAll(platform =>
            platform.Kind == PlatformKind.Raised ||
            platform.Kind == PlatformKind.Obstacle);
    }

    internal void ClearObstacles()
    {
        _platforms.RemoveAll(platform => platform.Kind == PlatformKind.Obstacle);
    }
}
