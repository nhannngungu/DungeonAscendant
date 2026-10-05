namespace DungeonAscendant.World;

public sealed class RegionDefinition
{
    public static RegionDefinition WildForest { get; } = new(
        RegionType.WildForest,
        isImplemented: true);

    public RegionType Type { get; }
    public bool IsImplemented { get; }

    private RegionDefinition(RegionType type, bool isImplemented)
    {
        Type = type;
        IsImplemented = isImplemented;
    }

    public static RegionDefinition Get(RegionType type)
    {
        return type == RegionType.WildForest
            ? WildForest
            : new RegionDefinition(type, isImplemented: false);
    }
}
