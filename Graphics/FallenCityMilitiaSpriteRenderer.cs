using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only renderer for the Fallen City militia pair. Poses are authored
/// facing right and mirrored as a complete composition around a foot anchor.
/// </summary>
public sealed class FallenCityMilitiaSpriteRenderer
{
    public static readonly Vector2 MilitiamanVisualSize = new(82f, 72f);
    public static readonly Vector2 CrossbowmanVisualSize = new(104f, 76f);

    private static readonly Color Outline = new(25, 23, 23);
    private static readonly Color Skin = new(119, 101, 87);
    private static readonly Color DeadSkin = new(91, 87, 75);
    private static readonly Color Scar = new(73, 43, 39);
    private static readonly Color LeatherDark = new(57, 42, 34);
    private static readonly Color Leather = new(91, 64, 46);
    private static readonly Color CityClothDark = new(72, 39, 34);
    private static readonly Color CityCloth = new(112, 58, 46);
    private static readonly Color Patch = new(90, 74, 57);
    private static readonly Color MetalDark = new(62, 61, 59);
    private static readonly Color Metal = new(113, 108, 99);
    private static readonly Color MetalLight = new(153, 145, 129);
    private static readonly Color Boot = new(49, 38, 32);
    private static readonly Color WoodDark = new(55, 38, 29);
    private static readonly Color Wood = new(91, 59, 38);
    private static readonly Color CloakDark = new(53, 45, 43);
    private static readonly Color Cloak = new(74, 61, 55);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<FallenCityEnemy, AnimationClock>
        _clocks = new();

    public FallenCityMilitiaSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(FallenCityEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy,
            static _ => new AnimationClock());
        CityVisualState state = SelectState(enemy);
        clock.Update(gameTime, state);

        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState != FallenCityCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        float shadowWidth = enemy.Type == EnemyType.RuinedCrossbowman
            ? 48f
            : 38f;
        DrawBox(anchor, -shadowWidth / 2f, -3f, shadowWidth, 5f,
            new Color(10, 9, 10, 135), flip);

        if (state == CityVisualState.Death)
        {
            if (enemy.Type == EnemyType.RuinedCrossbowman)
                DrawCrossbowmanDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawMilitiamanDeath(anchor, flip, enemy.DeathPresentationProgress);
            return;
        }

        CityPose pose = CreatePose(enemy, state, clock.Elapsed);
        if (enemy.Type == EnemyType.RuinedCrossbowman)
            DrawCrossbowman(enemy, anchor, flip, state, pose);
        else
            DrawMilitiaman(enemy, anchor, flip, state, pose);

        DrawRestrainedFeedback(enemy, anchor, flip, state, clock.Elapsed);
    }

    private void DrawMilitiaman(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        CityVisualState state,
        CityPose pose)
    {
        Color skin = enemy.IsHitFlashing ? new Color(190, 169, 143) :
            DeadSkin;
        Vector2 rearFoot = new(-7f - pose.Step, 0f);
        Vector2 frontFoot = new(8f + pose.Step, 0f);
        Vector2 rearKnee = new(-6f + pose.Step, -17f + pose.Crouch);
        Vector2 frontKnee = new(7f - pose.Step, -18f + pose.Crouch);
        Vector2 hip = new(pose.Lean, -33f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -50f + pose.Crouch + pose.Bob);
        Vector2 head = chest + new Vector2(pose.HeadShift, -14f);

        DrawLimb(anchor, rearFoot, rearKnee, CityClothDark, flip, 6f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f),
            CityCloth, flip, 7f);
        DrawLimb(anchor, frontFoot, frontKnee, CityClothDark, flip, 6f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 0f),
            CityCloth, flip, 7f);
        DrawLineLocal(anchor, rearFoot + new Vector2(-6f, 0f),
            rearFoot + new Vector2(5f, 0f), 5f, Boot, flip);
        DrawLineLocal(anchor, frontFoot + new Vector2(-4f, 0f),
            frontFoot + new Vector2(7f, 0f), 5f, Boot, flip);

        DrawBox(anchor, chest.X - 12f, chest.Y - 7f, 24f, 25f,
            Outline, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y - 5f, 20f, 22f,
            LeatherDark, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y - 1f, 16f, 12f,
            Leather, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y + 14f, 21f, 7f,
            CityClothDark, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y + 15f, 8f, 5f,
            Patch, flip);

        // Broken city insignia: a faded shield missing its lower corner.
        DrawBox(anchor, chest.X + 1f, chest.Y + 1f, 7f, 7f,
            new Color(133, 69, 50), flip);
        DrawBox(anchor, chest.X + 3f, chest.Y + 3f, 3f, 5f,
            new Color(174, 111, 67), flip);
        DrawBox(anchor, chest.X + 6f, chest.Y + 6f, 3f, 3f,
            LeatherDark, flip);

        Vector2 rearShoulder = chest + new Vector2(-10f, -1f);
        Vector2 rearHand = rearShoulder + new Vector2(-4f, 18f);
        Vector2 weaponShoulder = chest + new Vector2(10f, -1f);
        Vector2 weaponDirection = new(
            MathF.Cos(pose.WeaponAngle),
            MathF.Sin(pose.WeaponAngle));
        Vector2 weaponHand = weaponShoulder + weaponDirection * 17f;
        DrawLimb(anchor, rearShoulder, rearHand, Leather, flip, 6f);
        DrawLimb(anchor, weaponShoulder, weaponHand, Leather, flip, 6f);
        DrawBox(anchor, rearShoulder.X - 7f, rearShoulder.Y - 5f,
            12f, 9f, CityClothDark, flip);

        DrawDecayedFace(anchor, head, skin, flip, hooded: false);
        DrawOldSword(anchor, weaponHand, weaponDirection, flip);

        float clothTrail = pose.ClothTrail;
        DrawBox(anchor, chest.X - 11f - clothTrail, chest.Y + 17f,
            8f + clothTrail, 14f, CityClothDark, flip);
        DrawBox(anchor, chest.X + 3f, chest.Y + 17f,
            8f + clothTrail * .35f, 12f, CityCloth, flip);
    }

    private void DrawCrossbowman(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        CityVisualState state,
        CityPose pose)
    {
        Color skin = enemy.IsHitFlashing ? new Color(190, 169, 143) : Skin;
        Vector2 rearFoot = new(-10f - pose.Step, 0f);
        Vector2 frontFoot = new(10f + pose.Step, 0f);
        Vector2 rearKnee = new(-8f + pose.Step, -17f + pose.Crouch);
        Vector2 frontKnee = new(9f - pose.Step, -18f + pose.Crouch);
        Vector2 hip = new(pose.Lean, -34f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -51f + pose.Crouch + pose.Bob);
        Vector2 head = chest + new Vector2(pose.HeadShift, -15f);

        DrawCrossbowCloak(anchor, chest, pose, flip);
        DrawBoltPouch(anchor, hip, flip);
        DrawLimb(anchor, rearFoot, rearKnee, CloakDark, flip, 7f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-6f, 0f),
            Leather, flip, 8f);
        DrawLimb(anchor, frontFoot, frontKnee, CloakDark, flip, 7f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(6f, 0f),
            Leather, flip, 8f);
        DrawLineLocal(anchor, rearFoot + new Vector2(-7f, 0f),
            rearFoot + new Vector2(6f, 0f), 6f, Boot, flip);
        DrawLineLocal(anchor, frontFoot + new Vector2(-5f, 0f),
            frontFoot + new Vector2(8f, 0f), 6f, Boot, flip);

        DrawBox(anchor, chest.X - 15f, chest.Y - 8f, 30f, 27f,
            Outline, flip);
        DrawBox(anchor, chest.X - 13f, chest.Y - 6f, 26f, 24f,
            LeatherDark, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 4f, 22f, 18f,
            new Color(89, 75, 63), flip);
        DrawLineLocal(anchor, chest + new Vector2(-11f, -4f),
            chest + new Vector2(9f, 14f), 4f, CityClothDark, flip);

        // One intact shoulder plate and one visibly broken plate.
        DrawBox(anchor, chest.X + 8f, chest.Y - 9f, 13f, 8f,
            Outline, flip);
        DrawBox(anchor, chest.X + 9f, chest.Y - 8f, 10f, 6f,
            Metal, flip);
        DrawBox(anchor, chest.X - 19f, chest.Y - 7f, 9f, 6f,
            MetalDark, flip);
        DrawBox(anchor, chest.X - 20f, chest.Y - 3f, 5f, 4f,
            LeatherDark, flip);

        DrawDecayedFace(anchor, head, skin, flip, hooded: true);
        DrawCrossbowAndArms(enemy, anchor, chest, flip, state, pose);
    }

    private void DrawCrossbowCloak(
        Vector2 anchor,
        Vector2 chest,
        CityPose pose,
        bool flip)
    {
        float trail = pose.ClothTrail;
        DrawBox(anchor, chest.X - 17f - trail, chest.Y - 5f,
            18f + trail, 8f, CloakDark, flip);
        DrawBox(anchor, chest.X - 18f - trail, chest.Y + 2f,
            18f + trail, 29f, Cloak, flip);
        DrawBox(anchor, chest.X - 20f - trail, chest.Y + 25f,
            8f, 12f, CloakDark, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y + 28f,
            7f, 10f, Cloak, flip);
    }

    private void DrawBoltPouch(Vector2 anchor, Vector2 hip, bool flip)
    {
        DrawBox(anchor, hip.X - 17f, hip.Y - 7f, 11f, 18f,
            Outline, flip);
        DrawBox(anchor, hip.X - 15f, hip.Y - 5f, 8f, 15f,
            Leather, flip);
        for (int index = 0; index < 3; index++)
        {
            float x = hip.X - 15f + index * 3f;
            DrawLineLocal(anchor, new Vector2(x, hip.Y - 4f),
                new Vector2(x - 2f, hip.Y - 18f), 2f, Wood, flip);
            DrawBox(anchor, x - 4f, hip.Y - 20f, 4f, 3f,
                MetalDark, flip);
        }
    }

    private void DrawCrossbowAndArms(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        CityVisualState state,
        CityPose pose)
    {
        bool reload = state == CityVisualState.Reload;
        float crossbowX = chest.X + 20f + pose.WeaponPush;
        float crossbowY = chest.Y + 5f + pose.ReloadDrop;
        Vector2 stock = new(crossbowX - 22f, crossbowY + 4f);
        Vector2 grip = new(crossbowX - 6f, crossbowY);
        Vector2 nose = new(crossbowX + 25f, crossbowY - 1f);
        Vector2 rearShoulder = chest + new Vector2(-10f, 0f);
        Vector2 frontShoulder = chest + new Vector2(11f, 0f);
        Vector2 rearHand = reload
            ? chest + new Vector2(
                -15f + enemy.ActionProgress * 12f,
                20f - enemy.ActionProgress * 17f)
            : grip + new Vector2(-9f, 5f);
        Vector2 frontHand = grip + new Vector2(9f, 1f);

        DrawLimb(anchor, rearShoulder, rearHand, Leather, flip, 6f);
        DrawLimb(anchor, frontShoulder, frontHand, Leather, flip, 6f);
        DrawLineLocal(anchor, stock, nose, 7f, Outline, flip);
        DrawLineLocal(anchor, stock + new Vector2(1f, -1f),
            nose, 4f, Wood, flip);
        DrawLineLocal(anchor, grip + new Vector2(5f, 0f),
            grip + new Vector2(9f, 11f), 5f, WoodDark, flip);
        DrawLineLocal(anchor, nose + new Vector2(-4f, -17f),
            nose + new Vector2(2f, 17f), 6f, Outline, flip);
        DrawLineLocal(anchor, nose + new Vector2(-3f, -15f),
            nose + new Vector2(1f, 15f), 3f, Wood, flip);
        DrawLineLocal(anchor, nose + new Vector2(-3f, -15f),
            grip + new Vector2(2f, 0f), 1.5f, MetalDark, flip);
        DrawLineLocal(anchor, grip + new Vector2(2f, 0f),
            nose + new Vector2(1f, 15f), 1.5f, MetalDark, flip);

        if (state is CityVisualState.Aim or CityVisualState.Fire)
        {
            DrawLineLocal(anchor, grip + new Vector2(-5f, -2f),
                nose + new Vector2(15f, -2f),
                enemy.IsHeavyBolt ? 3f : 2f,
                WoodDark, flip);
            DrawBox(anchor, nose.X + 11f, nose.Y - 5f,
                enemy.IsHeavyBolt ? 9f : 7f,
                enemy.IsHeavyBolt ? 7f : 5f,
                Metal, flip);
        }
        else if (reload)
        {
            float boltX = MathHelper.Lerp(
                chest.X - 13f,
                nose.X + 9f,
                enemy.ActionProgress);
            float boltY = MathHelper.Lerp(
                chest.Y + 18f,
                nose.Y - 2f,
                enemy.ActionProgress);
            DrawLineLocal(anchor, new Vector2(boltX - 14f, boltY),
                new Vector2(boltX + 7f, boltY), 2f, Wood, flip);
            DrawBox(anchor, boltX + 5f, boltY - 3f, 6f, 5f,
                MetalDark, flip);
        }
    }

    private void DrawDecayedFace(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip,
        bool hooded)
    {
        if (hooded)
        {
            DrawBox(anchor, head.X - 12f, head.Y - 11f, 23f, 19f,
                Outline, flip);
            DrawBox(anchor, head.X - 10f, head.Y - 10f, 20f, 16f,
                CloakDark, flip);
        }
        DrawBox(anchor, head.X - 8f, head.Y - 7f, 17f, 16f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 6f, 15f, 14f,
            skin, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 2f, 3f, 3f,
            new Color(45, 39, 36), flip);
        DrawLineLocal(anchor, head + new Vector2(-5f, -4f),
            head + new Vector2(2f, 5f), 1.5f, Scar, flip);
        DrawBox(anchor, head.X - 4f, head.Y + 5f, 7f, 2f,
            new Color(62, 45, 40), flip);

        if (!hooded)
        {
            DrawBox(anchor, head.X - 10f, head.Y - 10f, 15f, 5f,
                MetalDark, flip);
            DrawBox(anchor, head.X - 8f, head.Y - 9f, 11f, 3f,
                Metal, flip);
        }
    }

    private void DrawOldSword(
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 guard = hand + direction * 4f;
        Vector2 tip = guard + direction * 32f;
        DrawLineLocal(anchor, hand - direction * 7f, guard,
            5f, Outline, flip);
        DrawLineLocal(anchor, hand - direction * 6f, guard,
            3f, Leather, flip);
        DrawLineLocal(anchor, guard - normal * 7f,
            guard + normal * 7f, 5f, MetalDark, flip);
        DrawLineLocal(anchor, guard, tip, 7f, Outline, flip);
        DrawLineLocal(anchor, guard + normal, tip + normal,
            3.5f, Metal, flip);
        DrawBox(anchor, tip.X - 2f, tip.Y - 2f, 3f, 3f,
            MetalLight, flip);
    }

    private void DrawRestrainedFeedback(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        CityVisualState state,
        float elapsed)
    {
        if (state == CityVisualState.Walk &&
            (int)(elapsed * 10f) % 5 == 0)
        {
            DrawBox(anchor, -13f, -4f, 3f, 2f,
                new Color(132, 113, 88, 105), flip);
            DrawBox(anchor, 10f, -3f, 2f, 2f,
                new Color(155, 137, 107, 90), flip);
        }

        if (state == CityVisualState.Hurt)
        {
            DrawBox(anchor, 14f, -45f, 3f, 2f,
                new Color(112, 39, 35, 175), flip);
            DrawBox(anchor, 20f, -40f, 2f, 2f,
                new Color(91, 32, 31, 125), flip);
        }
        else if (state == CityVisualState.Stagger)
        {
            DrawBox(anchor, -18f, -4f, 3f, 2f,
                new Color(150, 130, 98, 130), flip);
            DrawBox(anchor, 15f, -6f, 2f, 3f,
                new Color(127, 111, 87, 115), flip);
        }
    }

    private void DrawMilitiamanDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.45f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .30f) / .70f));
        DrawLineLocal(anchor,
            new Vector2(-10f, -47f + fall * 42f),
            new Vector2(18f + settle * 12f, -27f + fall * 22f),
            14f, Outline, flip);
        DrawLineLocal(anchor,
            new Vector2(-9f, -46f + fall * 41f),
            new Vector2(17f + settle * 12f, -27f + fall * 21f),
            10f, LeatherDark, flip);
        DrawBox(anchor, 15f + settle * 11f, -42f + fall * 35f,
            16f, 14f, DeadSkin, flip);
        DrawLineLocal(anchor, new Vector2(-18f, -3f),
            new Vector2(30f + settle * 13f, -2f), 5f,
            MetalDark, flip);
        DrawDeathDust(anchor, flip, progress);
    }

    private void DrawCrossbowmanDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float buckle = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.65f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .28f) / .72f));
        DrawBox(anchor, -20f, -34f + buckle * 29f, 39f, 30f,
            Outline, flip);
        DrawBox(anchor, -18f, -32f + buckle * 28f, 35f, 27f,
            CloakDark, flip);
        DrawBox(anchor, 10f + settle * 8f, -52f + buckle * 45f,
            18f, 16f, DeadSkin, flip);
        DrawLineLocal(anchor,
            new Vector2(-4f, -35f + buckle * 28f),
            new Vector2(43f + settle * 13f, -8f),
            8f, Outline, flip);
        DrawLineLocal(anchor,
            new Vector2(-2f, -34f + buckle * 27f),
            new Vector2(42f + settle * 13f, -8f),
            4f, Wood, flip);
        DrawLineLocal(anchor,
            new Vector2(35f + settle * 13f, -24f + buckle * 16f),
            new Vector2(43f + settle * 13f, 1f),
            4f, MetalDark, flip);
        DrawDeathDust(anchor, flip, progress);
    }

    private void DrawDeathDust(Vector2 anchor, bool flip, float progress)
    {
        int alpha = (int)(125f * (1f - progress));
        Color dust = new(145, 124, 96, alpha);
        DrawBox(anchor, -27f - progress * 7f, -5f, 4f, 3f, dust, flip);
        DrawBox(anchor, 5f, -7f - progress * 8f, 3f, 3f, dust, flip);
        DrawBox(anchor, 26f + progress * 8f, -4f, 4f, 2f, dust, flip);
    }

    private static CityPose CreatePose(
        FallenCityEnemy enemy,
        CityVisualState state,
        float elapsed)
    {
        float p = enemy.CombatState == FallenCityCombatState.Ready
            ? elapsed
            : enemy.ActionProgress;
        float wave = MathF.Sin(elapsed * MathHelper.TwoPi * .85f);
        var pose = new CityPose
        {
            WeaponAngle = .92f,
            ClothTrail = 1.5f,
            ReloadDrop = 0f
        };

        switch (state)
        {
            case CityVisualState.Idle:
                pose.Bob = MathF.Max(0f, wave) * .7f;
                pose.UpperLean = -2f + wave * .7f;
                pose.HeadShift = -1f + wave * .4f;
                pose.WeaponAngle = enemy.Type == EnemyType.FallenMilitiaman
                    ? 1.18f
                    : .15f;
                pose.ClothTrail = 2f + wave * .6f;
                break;
            case CityVisualState.Walk:
                pose.Step = wave * (enemy.Type == EnemyType.FallenMilitiaman
                    ? 5f
                    : 3.5f);
                pose.Bob = MathF.Abs(wave) * 1.4f;
                pose.UpperLean = 2f;
                pose.HeadShift = wave * 1.2f;
                pose.WeaponAngle = .65f;
                pose.ClothTrail = 4f + MathF.Abs(wave) * 2f;
                break;
            case CityVisualState.Windup:
                pose.Crouch = p * 2f;
                pose.UpperLean = MathHelper.Lerp(0f, -5f, p);
                pose.WeaponAngle = enemy.MilitiaAttack switch
                {
                    MilitiaAttackKind.ShortThrust =>
                        MathHelper.Lerp(.9f, -.08f, p),
                    MilitiaAttackKind.DesperateSwing =>
                        MathHelper.Lerp(.9f, -1.75f, p),
                    _ => MathHelper.Lerp(.9f, -2.2f, p)
                };
                break;
            case CityVisualState.Attack:
                pose.Crouch = 2f;
                pose.UpperLean = MathHelper.Lerp(-3f, 7f, p);
                pose.WeaponAngle = enemy.MilitiaAttack switch
                {
                    MilitiaAttackKind.ShortThrust => -.08f,
                    MilitiaAttackKind.DesperateSwing =>
                        MathHelper.Lerp(-1.75f, 1.22f, p),
                    _ => MathHelper.Lerp(-2.2f, .32f, p)
                };
                break;
            case CityVisualState.Recovery:
                pose.Crouch = MathHelper.Lerp(3f, 0f, p);
                pose.UpperLean = MathHelper.Lerp(6f, -1f, p);
                pose.WeaponAngle = MathHelper.Lerp(.3f, .92f, p);
                break;
            case CityVisualState.DefensiveStep:
                pose.Step = -3f;
                pose.Crouch = 3f;
                pose.UpperLean = -5f;
                pose.WeaponAngle = .12f;
                pose.ClothTrail = 6f;
                break;
            case CityVisualState.Aim:
                pose.Crouch = 2f;
                pose.UpperLean = MathHelper.Lerp(0f, -4f, p);
                pose.WeaponPush = MathHelper.Lerp(-5f, 8f, p);
                pose.ClothTrail = 2f;
                break;
            case CityVisualState.Fire:
                pose.UpperLean = 4f * (1f - p);
                pose.WeaponPush = MathHelper.Lerp(12f, 4f, p);
                pose.ClothTrail = 5f;
                break;
            case CityVisualState.Reload:
                pose.Crouch = 4f;
                pose.UpperLean = 5f;
                pose.ReloadDrop = MathHelper.Lerp(2f, 17f,
                    MathF.Sin(p * MathF.PI));
                pose.WeaponPush = -7f;
                pose.HeadShift = 3f;
                pose.ClothTrail = 2f;
                break;
            case CityVisualState.ShoveWindup:
                pose.Crouch = 4f;
                pose.UpperLean = -6f;
                pose.WeaponPush = MathHelper.Lerp(0f, -11f, p);
                break;
            case CityVisualState.Shove:
                pose.Crouch = 3f;
                pose.UpperLean = 7f;
                pose.WeaponPush = MathHelper.Lerp(-8f, 17f, p);
                break;
            case CityVisualState.Hurt:
                pose.UpperLean = -8f;
                pose.HeadShift = -4f;
                pose.Crouch = 3f;
                pose.WeaponAngle = 1.45f;
                pose.WeaponPush = -4f;
                pose.ClothTrail = 6f;
                break;
            case CityVisualState.Stagger:
                pose.UpperLean = -11f;
                pose.Lean = -4f;
                pose.Crouch = 7f;
                pose.HeadShift = -5f;
                pose.WeaponAngle = 1.85f;
                pose.ReloadDrop = 10f;
                pose.ClothTrail = 8f;
                break;
        }

        return pose;
    }

    private static CityVisualState SelectState(FallenCityEnemy enemy)
    {
        if (!enemy.IsAlive)
            return CityVisualState.Death;
        if (enemy.IsStaggered)
            return CityVisualState.Stagger;
        if (enemy.IsHitFlashing)
            return CityVisualState.Hurt;

        return enemy.CombatState switch
        {
            FallenCityCombatState.MilitiaWindup => CityVisualState.Windup,
            FallenCityCombatState.MilitiaActive => CityVisualState.Attack,
            FallenCityCombatState.MilitiaRecovery => CityVisualState.Recovery,
            FallenCityCombatState.MilitiaDefensiveStep =>
                CityVisualState.DefensiveStep,
            FallenCityCombatState.CrossbowAim => CityVisualState.Aim,
            FallenCityCombatState.CrossbowFire => CityVisualState.Fire,
            FallenCityCombatState.CrossbowReload => CityVisualState.Reload,
            FallenCityCombatState.CrossbowReposition =>
                CityVisualState.DefensiveStep,
            FallenCityCombatState.CrossbowShoveWindup =>
                CityVisualState.ShoveWindup,
            FallenCityCombatState.CrossbowShoveActive => CityVisualState.Shove,
            FallenCityCombatState.CrossbowShoveRecovery =>
                CityVisualState.Recovery,
            _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                ? CityVisualState.Walk
                : CityVisualState.Idle
        };
    }

    private void DrawLimb(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        Color color,
        bool flip,
        float thickness)
    {
        DrawLineLocal(anchor, from, to, thickness + 2f, Outline, flip);
        DrawLineLocal(anchor, from, to, thickness, color, flip);
    }

    private void DrawBox(
        Vector2 anchor,
        float x,
        float y,
        float width,
        float height,
        Color color,
        bool flip)
    {
        float worldX = flip ? anchor.X - x - width : anchor.X + x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(worldX),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLineLocal(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color,
        bool flip)
    {
        if (flip)
        {
            from.X = -from.X;
            to.X = -to.X;
        }
        Vector2 start = anchor + from;
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, .5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    private sealed class AnimationClock
    {
        public CityVisualState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, CityVisualState state)
        {
            if (State != state)
            {
                State = state;
                Elapsed = 0f;
                return;
            }

            Elapsed += MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                .10f);
        }
    }

    private enum CityVisualState
    {
        Idle,
        Walk,
        Windup,
        Attack,
        Recovery,
        DefensiveStep,
        Aim,
        Fire,
        Reload,
        ShoveWindup,
        Shove,
        Hurt,
        Stagger,
        Death
    }

    private struct CityPose
    {
        public float Bob;
        public float Lean;
        public float UpperLean;
        public float Crouch;
        public float Step;
        public float HeadShift;
        public float WeaponAngle;
        public float WeaponPush;
        public float ReloadDrop;
        public float ClothTrail;
    }
}
