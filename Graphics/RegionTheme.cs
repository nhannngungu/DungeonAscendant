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
            RegionType.RuinedCity => new RegionTheme(
                new Color(13, 13, 16),
                new Color(53, 51, 52),
                new Color(104, 89, 78),
                new Color(55, 53, 52),
                new Color(105, 84, 68, 70),
                new Color(78, 58, 45),
                new Color(106, 48, 50, 92)),
            RegionType.DemonLands => new RegionTheme(
                new Color(17, 7, 10),
                new Color(55, 31, 28),
                new Color(132, 55, 38),
                new Color(67, 34, 29),
                new Color(130, 52, 36, 90),
                new Color(80, 42, 36),
                new Color(142, 32, 82, 120)),
            RegionType.DemonFortress => new RegionTheme(
                new Color(10, 10, 13),
                new Color(43, 43, 48),
                new Color(116, 56, 45),
                new Color(52, 49, 49),
                new Color(120, 50, 42, 82),
                new Color(70, 54, 45),
                new Color(134, 42, 32, 105)),
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
