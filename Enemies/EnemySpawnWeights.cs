namespace DungeonAscendant.Enemies;

public readonly struct EnemySpawnWeights
{
    public int Goblin { get; }
    public int DireWolf { get; }
    public int GiantSpider { get; }
    public int GoblinHunter { get; }

    public EnemySpawnWeights(
        int goblin,
        int direWolf,
        int giantSpider,
        int goblinHunter)
    {
        Goblin = goblin;
        DireWolf = direWolf;
        GiantSpider = giantSpider;
        GoblinHunter = goblinHunter;
    }
}
