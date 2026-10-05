using System;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Enemies;

/// <summary>
/// Owns the Goblin's gameplay state and basic chase behavior.
/// </summary>
public sealed class Goblin
{
    private const int BaseMaxHealth = 100;
    private const int HealthIncreasePerLevel = 20;
    private const int BaseAttackDamage = 10;
    private const int DamageIncreasePerLevel = 2;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; }
    public float MovementSpeed { get; }
    public float DetectionRange { get; }
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public int Level { get; }
    public int AttackDamage { get; }
    public int ExperienceReward { get; }

    public Goblin(
        Vector2 position,
        int level = 1,
        float movementSpeed = 110f,
        float detectionRange = 200f)
    {
        Position = position;
        Size = new Vector2(36f, 44f);
        MovementSpeed = movementSpeed;
        DetectionRange = detectionRange;
        Level = Math.Max(1, level);
        MaxHealth = BaseMaxHealth + (Level - 1) * HealthIncreasePerLevel;
        CurrentHealth = MaxHealth;
        AttackDamage = BaseAttackDamage + (Level - 1) * DamageIncreasePerLevel;
        ExperienceReward = 50;
    }

    public void Update(
        GameTime gameTime,
        Vector2 playerPosition,
        float stoppingRange)
    {
        if (!IsAlive)
            return;

        Vector2 toPlayer = playerPosition - Position;
        float distanceSquared = toPlayer.LengthSquared();

        if (distanceSquared > DetectionRange * DetectionRange ||
            distanceSquared <= stoppingRange * stoppingRange)
        {
            return;
        }

        float distance = MathF.Sqrt(distanceSquared);
        Vector2 direction = toPlayer / distance;
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementDistance = MathF.Min(
            MovementSpeed * elapsedSeconds,
            distance - stoppingRange);

        Position += direction * movementDistance;
    }

    public void ReceiveDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }
}
