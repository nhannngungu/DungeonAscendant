namespace DungeonAscendant.Enemies;

public readonly struct EnemySpawnWeights
{
    public int Goblin { get; }
    public int DireWolf { get; }
    public int GiantSpider { get; }
    public int GoblinHunter { get; }
    public int ThornCrawler { get; }
    public int CorruptedTreant { get; }
    public int BloodBat { get; }

    public EnemySpawnWeights(
        int goblin,
        int direWolf,
        int giantSpider,
        int goblinHunter,
        int thornCrawler,
        int corruptedTreant,
        int bloodBat)
    {
        Goblin = goblin;
        DireWolf = direWolf;
        GiantSpider = giantSpider;
        GoblinHunter = goblinHunter;
        ThornCrawler = thornCrawler;
        CorruptedTreant = corruptedTreant;
        BloodBat = bloodBat;
    }
}
