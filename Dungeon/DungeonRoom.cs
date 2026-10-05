using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class DungeonRoom
{
    private readonly List<int> _connections = new();

    public int Id { get; }
    public Rectangle Bounds { get; }
    public RoomType Type { get; internal set; }
    public IReadOnlyList<int> Connections => _connections;
    public Vector2 Center => Bounds.Center.ToVector2();

    public DungeonRoom(int id, Rectangle bounds, RoomType type)
    {
        Id = id;
        Bounds = bounds;
        Type = type;
    }

    internal void ConnectTo(int roomId)
    {
        if (!_connections.Contains(roomId))
            _connections.Add(roomId);
    }
}
