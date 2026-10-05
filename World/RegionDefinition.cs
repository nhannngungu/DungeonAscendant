namespace DungeonAscendant.World;

public sealed class RegionDefinition
{
    public static RegionDefinition WildForest { get; } = new(
        RegionType.WildForest,
        RegionBossType.AncientTreant,
        isImplemented: true);

    public RegionType Type { get; }
    public RegionBossType BossType { get; }
    public bool IsImplemented { get; }

    private RegionDefinition(
        RegionType type,
        RegionBossType bossType,
        bool isImplemented)
    {
        Type = type;
        BossType = bossType;
        IsImplemented = isImplemented;
    }

    public static RegionDefinition Get(RegionType type)
    {
        return type == RegionType.WildForest
            ? WildForest
            : new RegionDefinition(
                type,
                RegionBossType.AncientTreant,
                isImplemented: false);
    }
}
