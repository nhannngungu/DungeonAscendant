using DungeonAscendant.Dungeon;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Graphics;

public readonly struct RegionTheme
{
    public Color Background { get; }
    public Color Boundary { get; }
    public Color BoundaryAccent { get; }
    public Color CorridorFloor { get; }
    public Color GroundMarking { get; }
    public Color Root { get; }
    public Color Corruption { get; }

    private RegionTheme(
        Color background,
        Color boundary,
        Color boundaryAccent,
        Color corridorFloor,
        Color groundMarking,
        Color root,
        Color corruption)
    {
        Background = background;
        Boundary = boundary;
        BoundaryAccent = boundaryAccent;
        CorridorFloor = corridorFloor;
        GroundMarking = groundMarking;
        Root = root;
        Corruption = corruption;
    }

    public Color GetRoomFloor(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => new Color(35, 55, 39),
            RoomType.Enemy => new Color(48, 43, 31),
            RoomType.Treasure => new Color(53, 53, 31),
            RoomType.Boss => new Color(55, 35, 33),
            RoomType.Exit => new Color(48, 45, 30),
            _ => new Color(42, 48, 34)
        };
    }

    public static RegionTheme For(RegionType region)
    {
        return region switch
        {
            RegionType.WildForest => new RegionTheme(
                new Color(8, 14, 10),
                new Color(31, 42, 30),
                new Color(61, 76, 48),
                new Color(40, 47, 31),
                new Color(75, 91, 49, 92),
                new Color(73, 53, 35),
                new Color(82, 42, 75, 120)),
            _ => new RegionTheme(
                new Color(9, 11, 16),
                new Color(48, 43, 46),
                new Color(82, 71, 71),
                new Color(45, 50, 52),
                new Color(72, 77, 78, 75),
                new Color(82, 63, 47),
                new Color(78, 45, 72, 100))
        };
    }
}
