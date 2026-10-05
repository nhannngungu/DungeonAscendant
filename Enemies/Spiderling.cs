using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using PlayerCharacter = DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

public sealed class Spiderling : Enemy
{
    public MotherSpider Mother { get; }
    public override bool CountsForProgression => false;
    public override bool CanDropLoot => false;
    public override float LootChanceMultiplier => 0f;

    public Spiderling(
        Vector2 position,
        int level,
        int roomId,
        int worldTier,
        MotherSpider mother)
        : base(
            EnemyType.Spiderling,
            position,
            new Vector2(25f, 18f),
            movementSpeed: 165f,
            detectionRange: 225f,
            EnemyStatScaling.Health(25, 5, level, worldTier),
            EnemyStatScaling.Damage(5, 1, level, worldTier),
            experienceReward: 0,
            level,
            worldTier,
            isElite: false,
            roomId,
            attackRange: 30f,
            attackCooldownSeconds: 0.8f)
    {
        Mother = mother;
    }

    protected override void UpdateBehavior(
        GameTime gameTime,
        PlayerCharacter player,
        DungeonMap dungeon,
        ProjectileManager projectiles,
        RootHazardManager rootHazards,
        EnemyManager enemies)
    {
        if (!IsPlayerDetected(player.Position))
            return;

        float distanceSquared = Vector2.DistanceSquared(Position, player.Position);

        if (distanceSquared <= Attack.Range * Attack.Range)
        {
            if (Attack.TryStart())
                player.ReceiveDamage(EffectiveAttackDamage);
        }
        else
        {
            MoveToward(gameTime, player.Position, Attack.Range, dungeon);
        }
    }
}
