namespace DungeonAscendant.Combat;

/// <summary>
/// Default sword moveset. Equipment-specific movesets can replace this catalog later.
/// </summary>
public static class SwordAttackSet
{
    public static readonly AttackDefinition LightOne = new(
        AttackKind.LightOne, 1f, 14f, 0f,
        startupTime: 0.08f, activeTime: 0.10f, recoveryTime: 0.18f,
        knockback: 18f, range: 72f, thickness: 48f);

    public static readonly AttackDefinition LightTwo = new(
        AttackKind.LightTwo, 1.1f, 18f, 0f,
        startupTime: 0.09f, activeTime: 0.10f, recoveryTime: 0.20f,
        knockback: 22f, range: 78f, thickness: 50f);

    public static readonly AttackDefinition LightThree = new(
        AttackKind.LightThree, 1.4f, 32f, 0f,
        startupTime: 0.14f, activeTime: 0.12f, recoveryTime: 0.34f,
        knockback: 38f, range: 88f, thickness: 54f);

    public static readonly AttackDefinition Heavy = new(
        AttackKind.Heavy, 1.9f, 55f, 30f,
        startupTime: 0.32f, activeTime: 0.14f, recoveryTime: 0.42f,
        knockback: 64f, range: 96f, thickness: 58f);

    public static AttackDefinition GetLight(int index)
    {
        return index switch
        {
            1 => LightTwo,
            2 => LightThree,
            _ => LightOne
        };
    }
}
