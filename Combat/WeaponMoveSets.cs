using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

public static class WeaponMoveSets
{
    private static readonly ProjectileDefinition Arrow = new(
        ProjectileType.Arrow, 560f, new Vector2(18f, 6f), 2.8f);
    private static readonly ProjectileDefinition ArcaneBolt = new(
        ProjectileType.ArcaneBolt, 390f, new Vector2(18f, 18f), 3.2f);

    public static readonly WeaponMoveSet LongSword = new(
        "long-sword",
        new[]
        {
            Melee(AttackKind.LightOne, 1f, 14f, 8f, .08f, .10f, .18f, 18f, 72f, 48f, AttackHitboxShape.StandardArc, 48f),
            Melee(AttackKind.LightTwo, 1.1f, 18f, 9f, .09f, .10f, .20f, 22f, 78f, 50f, AttackHitboxShape.StandardArc, 44f),
            Melee(AttackKind.LightThree, 1.4f, 32f, 11f, .14f, .12f, .34f, 38f, 88f, 54f, AttackHitboxShape.StandardArc, 58f)
        },
        Melee(AttackKind.Heavy, 1.9f, 55f, 30f, .32f, .14f, .42f, 64f, 100f, 34f, AttackHitboxShape.Thrust, 72f));

    public static readonly WeaponMoveSet GreatSword = new(
        "great-sword",
        new[]
        {
            Melee(AttackKind.LightOne, 1.28f, 28f, 13f, .22f, .15f, .34f, 42f, 94f, 72f, AttackHitboxShape.WideArc, 18f),
            Melee(AttackKind.LightTwo, 1.48f, 36f, 15f, .25f, .16f, .40f, 56f, 102f, 76f, AttackHitboxShape.WideArc, 16f),
            Melee(AttackKind.LightThree, 1.82f, 54f, 18f, .32f, .18f, .52f, 82f, 108f, 82f, AttackHitboxShape.Chop, 12f)
        },
        Melee(AttackKind.Heavy, 2.75f, 95f, 34f, .58f, .22f, .68f, 124f, 118f, 90f, AttackHitboxShape.WideArc, 8f));

    public static readonly WeaponMoveSet BattleAxe = new(
        "battle-axe",
        new[]
        {
            Melee(AttackKind.LightOne, 1.16f, 34f, 11f, .15f, .11f, .26f, 55f, 68f, 56f, AttackHitboxShape.Chop, 36f),
            Melee(AttackKind.LightTwo, 1.34f, 46f, 13f, .18f, .12f, .31f, 72f, 76f, 58f, AttackHitboxShape.StandardArc, 34f),
            Melee(AttackKind.LightThree, 1.62f, 68f, 16f, .24f, .14f, .43f, 98f, 80f, 66f, AttackHitboxShape.Chop, 42f)
        },
        Melee(AttackKind.Heavy, 2.35f, 115f, 31f, .46f, .18f, .58f, 142f, 84f, 76f, AttackHitboxShape.Chop, 30f));

    public static readonly WeaponMoveSet Spear = new(
        "spear",
        new[]
        {
            Melee(AttackKind.LightOne, .92f, 18f, 8f, .07f, .10f, .18f, 20f, 122f, 22f, AttackHitboxShape.Thrust, 46f),
            Melee(AttackKind.LightTwo, 1.02f, 22f, 9f, .09f, .11f, .20f, 24f, 132f, 24f, AttackHitboxShape.Thrust, 52f),
            Melee(AttackKind.LightThree, 1.18f, 28f, 11f, .13f, .13f, .29f, 34f, 96f, 46f, AttackHitboxShape.StandardArc, 36f)
        },
        Melee(AttackKind.Heavy, 1.9f, 58f, 27f, .34f, .16f, .46f, 52f, 158f, 24f, AttackHitboxShape.Thrust, 150f));

    public static readonly WeaponMoveSet DualDaggers = new(
        "dual-daggers",
        new[]
        {
            Melee(AttackKind.LightOne, .62f, 6f, 4f, .035f, .07f, .09f, 8f, 42f, 34f, AttackHitboxShape.ShortArc, 70f),
            Melee(AttackKind.LightTwo, .65f, 7f, 4f, .035f, .07f, .09f, 9f, 44f, 34f, AttackHitboxShape.ShortArc, 74f),
            Melee(AttackKind.LightThree, .72f, 8f, 5f, .045f, .08f, .10f, 10f, 48f, 38f, AttackHitboxShape.ShortArc, 78f),
            Melee(AttackKind.LightFour, .78f, 9f, 5f, .045f, .08f, .10f, 12f, 52f, 36f, AttackHitboxShape.ShortArc, 92f),
            Melee(AttackKind.LightFive, 1.08f, 16f, 7f, .07f, .10f, .18f, 24f, 56f, 42f, AttackHitboxShape.ShortArc, 106f)
        },
        Melee(AttackKind.Heavy, 1.45f, 28f, 18f, .15f, .19f, .24f, 32f, 58f, 44f, AttackHitboxShape.ShortArc, 96f));

    public static readonly WeaponMoveSet Bow = new(
        "bow",
        new[]
        {
            Ranged(AttackKind.LightOne, .86f, 16f, 8f, .10f, .04f, .20f, 18f, AttackDelivery.Arrow, Arrow)
        },
        Ranged(AttackKind.Heavy, 1.85f, 52f, 25f, .10f, .05f, .32f, 54f, AttackDelivery.Arrow, Arrow),
        heavyChargeSeconds: .8f);

    public static readonly WeaponMoveSet ArcaneStaff = new(
        "arcane-staff",
        new[]
        {
            Ranged(AttackKind.LightOne, 1.18f, 30f, 14f, .20f, .07f, .33f, 34f, AttackDelivery.ArcaneProjectile, ArcaneBolt, AttackResourceKind.MagicReady)
        },
        Ranged(AttackKind.Heavy, 2.35f, 82f, 34f, .16f, .10f, .52f, 76f, AttackDelivery.ArcaneProjectile, ArcaneBolt, AttackResourceKind.MagicReady),
        heavyChargeSeconds: 1.05f);

    private static AttackDefinition Melee(
        AttackKind kind, float damage, float poise, float stamina,
        float startup, float active, float recovery, float knockback,
        float range, float thickness, AttackHitboxShape shape, float movement)
    {
        return new AttackDefinition(kind, damage, poise, stamina, startup, active,
            recovery, knockback, range, thickness, shape, movement);
    }

    private static AttackDefinition Ranged(
        AttackKind kind, float damage, float poise, float stamina,
        float startup, float active, float recovery, float knockback,
        AttackDelivery delivery, ProjectileDefinition projectile,
        AttackResourceKind resource = AttackResourceKind.Stamina)
    {
        return new AttackDefinition(kind, damage, poise, stamina, startup, active,
            recovery, knockback, 1f, 1f, AttackHitboxShape.None, 0f,
            delivery, projectile, resource);
    }
}
