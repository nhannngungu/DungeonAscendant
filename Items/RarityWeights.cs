namespace DungeonAscendant.Items;

public readonly struct RarityWeights
{
    public int Common { get; }
    public int Uncommon { get; }
    public int Rare { get; }
    public int Epic { get; }
    public int Legendary { get; }

    public RarityWeights(
        int common,
        int uncommon,
        int rare,
        int epic,
        int legendary)
    {
        Common = common;
        Uncommon = uncommon;
        Rare = rare;
        Epic = epic;
        Legendary = legendary;
    }
}
