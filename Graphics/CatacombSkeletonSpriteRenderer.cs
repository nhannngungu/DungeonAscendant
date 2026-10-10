using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only layered renderer for the first two Ancient Catacombs enemies.
/// Poses are authored facing right and mirrored as a complete composition.
/// </summary>
public sealed class CatacombSkeletonSpriteRenderer
{
    private static readonly Color Outline = new(24, 25, 27);
    private static readonly Color BoneDark = new(111, 108, 96);
    private static readonly Color Bone = new(176, 171, 149);
    private static readonly Color BoneLight = new(203, 196, 168);
    private static readonly Color ArcherBone = new(116, 118, 111);
    private static readonly Color ArcherBoneLight = new(151, 153, 142);
    private static readonly Color RustDark = new(66, 48, 43);
    private static readonly Color Rust = new(114, 73, 54);
    private static readonly Color Blade = new(130, 125, 112);
    private static readonly Color Leather = new(69, 49, 37);
    private static readonly Color ClothDark = new(43, 43, 47);
    private static readonly Color Cloth = new(61, 61, 64);
    private static readonly Color BowDark = new(44, 32, 25);
    private static readonly Color BowWood = new(82, 57, 38);
    private static readonly Color RedSoul = new(103, 32, 28, 185);
    private static readonly Color BlueSoul = new(79, 132, 151, 195);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public CatacombSkeletonSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(CatacombEnemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _controllers.GetValue(
            enemy,
            CreateController);
        animation.Update(gameTime, enemy);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool flip = enemy.Facing == EnemyFacingDirection.Left;

        DrawBox(anchor, -19f, -3f, 39f, 5f,
            new Color(8, 10, 13, 135), flip);
        if (animation.State == EnemyVisualState.Death)
        {
            if (enemy.Type == EnemyType.SkeletonArcher)
                DrawArcherDeath(anchor, flip, animation.Progress);
            else
                DrawSkeletonDeath(anchor, flip, animation.Progress);
            return;
        }

        SkeletonPose pose = CreatePose(enemy, animation);
        if (enemy.Type == EnemyType.SkeletonArcher)
            DrawArcher(enemy, anchor, flip, animation, pose);
        else
            DrawSkeleton(enemy, anchor, flip, animation, pose);

        if (animation.State == EnemyVisualState.Hurt)
            DrawImpactDust(anchor, flip, animation.Progress, enemy.EquipmentVariant);
    }

    private static EnemyAnimationController CreateController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy.Type == EnemyType.SkeletonArcher
                ? EnemyVisualProfile.SkeletonArcher
                : EnemyVisualProfile.Skeleton);
    }

    private void DrawSkeleton(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation,
        SkeletonPose pose)
    {
        Color bone = enemy.IsHitFlashing ? BoneLight : Bone;
        Vector2 leftFoot = new(-8f, 0f);
        Vector2 rightFoot = new(8f, 0f);
        Vector2 leftKnee = new(-7f + pose.Step, -15f + pose.Crouch);
        Vector2 rightKnee = new(7f - pose.Step, -14f + pose.Crouch);
        Vector2 leftHip = new(-5f + pose.Lean, -29f + pose.Crouch);
        Vector2 rightHip = new(5f + pose.Lean, -29f + pose.Crouch);
        Vector2 spineBase = new(pose.Lean, -30f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -46f + pose.Crouch + pose.Bob);
        Vector2 head = chest + new Vector2(pose.HeadShift, -15f);

        DrawBone(anchor, leftFoot, leftKnee, bone, flip, 4f);
        DrawBone(anchor, leftKnee, leftHip, bone, flip, 4f);
        DrawBone(anchor, rightFoot, rightKnee, bone, flip, 4f);
        DrawBone(anchor, rightKnee, rightHip, bone, flip, 4f);
        DrawJoint(anchor, leftKnee, bone, flip);
        DrawJoint(anchor, rightKnee, bone, flip);
        DrawLineLocal(anchor, leftFoot + new Vector2(-5f, 0f),
            leftFoot + new Vector2(5f, 0f), 4f, BoneDark, flip);
        DrawLineLocal(anchor, rightFoot + new Vector2(-4f, 0f),
            rightFoot + new Vector2(6f, 0f), 4f, BoneDark, flip);
        DrawLineLocal(anchor, leftHip, rightHip, 7f, Outline, flip);
        DrawLineLocal(anchor, leftHip, rightHip, 4f, bone, flip);
        DrawBone(anchor, spineBase, chest, bone, flip, 4f);
        DrawRibCage(anchor, chest, bone, flip, wide: true);

        if (enemy.EquipmentVariant == 2)
        {
            DrawLineLocal(anchor, chest + new Vector2(-9f, -3f),
                spineBase + new Vector2(8f, -1f), 4f, Leather, flip);
            DrawBox(anchor, pose.Lean - 7f, -31f + pose.Crouch,
                15f, 6f, new Color(72, 65, 56), flip);
        }
        else if (enemy.EquipmentVariant == 1)
        {
            DrawBox(anchor, chest.X - 13f, chest.Y - 5f,
                10f, 8f, RustDark, flip);
            DrawBox(anchor, chest.X - 12f, chest.Y - 4f,
                8f, 5f, Rust, flip);
        }

        Vector2 rearShoulder = chest + new Vector2(-10f, -1f);
        Vector2 weaponShoulder = chest + new Vector2(10f, -1f);
        Vector2 rearHand = rearShoulder + new Vector2(
            -4f - pose.LooseArm,
            17f + pose.Crouch * .3f);
        Vector2 weaponDirection = new(
            MathF.Cos(pose.WeaponAngle),
            MathF.Sin(pose.WeaponAngle));
        Vector2 weaponHand = weaponShoulder + weaponDirection * 17f;
        DrawBone(anchor, rearShoulder, rearHand, bone, flip, 4f);
        DrawBone(anchor, weaponShoulder, weaponHand, bone, flip, 4f);
        DrawJoint(anchor, rearShoulder, bone, flip);
        DrawJoint(anchor, weaponShoulder, bone, flip);

        if (enemy.EquipmentVariant == 3)
            DrawBuckler(anchor, rearHand, flip);

        DrawSkull(anchor, head, bone, RedSoul, flip,
            jawDrop: pose.JawDrop,
            helmet: enemy.EquipmentVariant == 0);
        DrawMeleeWeapon(enemy, anchor, weaponHand, weaponDirection, flip);
        DrawCracks(anchor, leftKnee, rearHand, flip);
    }

    private void DrawArcher(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation,
        SkeletonPose pose)
    {
        Color bone = enemy.IsHitFlashing ? BoneLight : ArcherBone;
        DrawArcherCloak(anchor, flip, pose);
        DrawQuiver(anchor, flip, pose);

        Vector2 leftFoot = new(-6f, 0f);
        Vector2 rightFoot = new(7f, 0f);
        Vector2 leftKnee = new(-6f + pose.Step, -17f + pose.Crouch);
        Vector2 rightKnee = new(6f - pose.Step, -16f + pose.Crouch);
        Vector2 leftHip = new(-4f + pose.Lean, -32f + pose.Crouch);
        Vector2 rightHip = new(4f + pose.Lean, -32f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -50f + pose.Crouch + pose.Bob);
        Vector2 head = chest + new Vector2(pose.HeadShift, -15f);

        DrawBone(anchor, leftFoot, leftKnee, bone, flip, 3.5f);
        DrawBone(anchor, leftKnee, leftHip, bone, flip, 3.5f);
        DrawBone(anchor, rightFoot, rightKnee, bone, flip, 3.5f);
        DrawBone(anchor, rightKnee, rightHip, bone, flip, 3.5f);
        DrawLineLocal(anchor, leftHip, rightHip, 5f, bone, flip);
        DrawBone(anchor, new Vector2(pose.Lean, -33f + pose.Crouch),
            chest, bone, flip, 3.5f);
        DrawRibCage(anchor, chest, bone, flip, wide: false);
        DrawBox(anchor, chest.X - 11f, chest.Y - 7f, 18f, 5f,
            ClothDark, flip);
        DrawLineLocal(anchor, chest + new Vector2(-8f, -3f),
            chest + new Vector2(6f, 8f), 3f, Leather, flip);
        DrawSkull(anchor, head, bone, BlueSoul, flip,
            jawDrop: pose.JawDrop, helmet: false,
            eyeBoost: enemy.CombatState == CatacombCombatState.ArcherDraw
                ? .35f + (enemy.IsHeavyArrow ? .25f : 0f)
                : 0f);
        DrawArcherArmsAndBow(enemy, anchor, flip, animation, pose, bone, chest);
    }

    private void DrawArcherCloak(
        Vector2 anchor,
        bool flip,
        SkeletonPose pose)
    {
        float x = pose.Lean - 9f - pose.CloakTrail;
        float y = -53f + pose.Crouch + pose.Bob;
        DrawBox(anchor, x, y, 17f + pose.CloakTrail, 7f,
            ClothDark, flip);
        DrawBox(anchor, x - 1f, y + 6f, 14f + pose.CloakTrail, 28f,
            Cloth, flip);
        DrawBox(anchor, x - 3f, y + 30f, 6f, 12f,
            ClothDark, flip);
        DrawBox(anchor, x + 5f, y + 31f, 5f, 8f,
            Cloth, flip);
        DrawBox(anchor, x + 12f, y + 29f, 5f, 14f,
            ClothDark, flip);
    }

    private void DrawQuiver(
        Vector2 anchor,
        bool flip,
        SkeletonPose pose)
    {
        Vector2 top = new(pose.Lean - 10f, -47f + pose.Crouch);
        Vector2 bottom = new(pose.Lean - 17f, -23f + pose.Crouch);
        DrawLineLocal(anchor, top, bottom, 8f, Outline, flip);
        DrawLineLocal(anchor, top, bottom, 5f, Leather, flip);
        for (int index = 0; index < 3; index++)
        {
            Vector2 shaft = top + new Vector2(index * 3f - 3f, 2f);
            DrawLineLocal(anchor, shaft, shaft + new Vector2(-3f, -15f),
                2f, BowWood, flip);
            DrawBox(anchor, shaft.X - 5f, shaft.Y - 17f, 5f, 3f,
                RustDark, flip);
        }
    }

    private void DrawArcherArmsAndBow(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation,
        SkeletonPose pose,
        Color bone,
        Vector2 chest)
    {
        bool shooting = enemy.CombatState is
            CatacombCombatState.ArcherNock or
            CatacombCombatState.ArcherDraw or
            CatacombCombatState.ArcherRelease or
            CatacombCombatState.ArcherRecovery;
        bool shove = enemy.CombatState is
            CatacombCombatState.BowShoveWindup or
            CatacombCombatState.BowShoveActive or
            CatacombCombatState.BowShoveRecovery;
        float bowX = shove ? 23f + pose.BowPush : shooting ? 18f : 14f;
        float bowY = shooting ? -45f : shove ? -35f : -30f;
        Vector2 bowCenter = new(chest.X + bowX, bowY + pose.Crouch);
        Vector2 bowTop = bowCenter + new Vector2(-4f, -23f);
        Vector2 bowUpper = bowCenter + new Vector2(3f, -11f);
        Vector2 bowLower = bowCenter + new Vector2(3f, 11f);
        Vector2 bowBottom = bowCenter + new Vector2(-4f, 23f);
        float draw = enemy.CombatState == CatacombCombatState.ArcherDraw
            ? MathHelper.Lerp(5f, enemy.IsHeavyArrow ? 20f : 16f,
                enemy.ActionProgress)
            : enemy.CombatState == CatacombCombatState.ArcherNock
                ? 4f
                : 1f;
        Vector2 stringHand = bowCenter + new Vector2(-draw, 0f);
        Vector2 bowShoulder = chest + new Vector2(7f, -1f);
        Vector2 rearShoulder = chest + new Vector2(-7f, -2f);

        if (enemy.CombatState == CatacombCombatState.ArcherNock)
            stringHand = rearShoulder + new Vector2(-10f, -10f);
        if (shove)
            stringHand = bowCenter + new Vector2(-12f, 7f);

        DrawBone(anchor, bowShoulder, bowCenter, bone, flip, 3.5f);
        DrawBone(anchor, rearShoulder, stringHand, bone, flip, 3.5f);
        DrawLineLocal(anchor, bowTop, bowUpper, 5f, BowDark, flip);
        DrawLineLocal(anchor, bowUpper, bowCenter, 4f, BowWood, flip);
        DrawLineLocal(anchor, bowCenter, bowLower, 4f, BowWood, flip);
        DrawLineLocal(anchor, bowLower, bowBottom, 5f, BowDark, flip);
        DrawLineLocal(anchor, bowTop, stringHand, 1.3f,
            new Color(57, 51, 45), flip);
        DrawLineLocal(anchor, stringHand, bowBottom, 1.3f,
            new Color(57, 51, 45), flip);

        if (enemy.CombatState is CatacombCombatState.ArcherNock or
            CatacombCombatState.ArcherDraw)
        {
            Vector2 tip = bowCenter + new Vector2(24f, 0f);
            DrawLineLocal(anchor, stringHand, tip, 2f, BowWood, flip);
            DrawLineLocal(anchor, tip - new Vector2(3f, 0f),
                tip + new Vector2(4f, 0f), 4f, RustDark, flip);
            if (enemy.IsHeavyArrow)
                DrawLineLocal(anchor, tip - new Vector2(8f, 0f), tip,
                    2f, new Color(80, 137, 156, 155), flip);
        }
    }

    private void DrawRibCage(
        Vector2 anchor,
        Vector2 chest,
        Color bone,
        bool flip,
        bool wide)
    {
        float width = wide ? 12f : 9f;
        for (int rib = 0; rib < 4; rib++)
        {
            float y = chest.Y - 2f + rib * 4f;
            float taper = rib * 1.2f;
            DrawLineLocal(anchor,
                new Vector2(chest.X - width + taper, y),
                new Vector2(chest.X - 2f, y + 2f), 2.5f, bone, flip);
            DrawLineLocal(anchor,
                new Vector2(chest.X + 2f, y + 2f),
                new Vector2(chest.X + width - taper, y), 2.5f, bone, flip);
        }
    }

    private void DrawSkull(
        Vector2 anchor,
        Vector2 center,
        Color bone,
        Color eye,
        bool flip,
        float jawDrop,
        bool helmet,
        float eyeBoost = 0f)
    {
        DrawBox(anchor, center.X - 9f, center.Y - 8f, 19f, 15f,
            Outline, flip);
        DrawBox(anchor, center.X - 7f, center.Y - 7f, 15f, 12f,
            bone, flip);
        DrawBox(anchor, center.X - 6f, center.Y - 3f, 5f, 5f,
            new Color(32, 30, 29), flip);
        DrawBox(anchor, center.X + 3f, center.Y - 3f, 5f, 5f,
            new Color(32, 30, 29), flip);
        Color glow = Color.Lerp(eye, new Color(129, 185, 201), eyeBoost);
        DrawBox(anchor, center.X + 4f, center.Y - 2f, 2f, 2f,
            glow, flip);
        DrawBox(anchor, center.X - 5f, center.Y + 6f + jawDrop,
            13f, 5f, Outline, flip);
        DrawBox(anchor, center.X - 4f, center.Y + 6f + jawDrop,
            11f, 3f, BoneDark, flip);
        if (helmet)
        {
            DrawBox(anchor, center.X - 10f, center.Y - 11f, 13f, 5f,
                RustDark, flip);
            DrawBox(anchor, center.X - 8f, center.Y - 10f, 10f, 3f,
                Rust, flip);
        }
    }

    private void DrawMeleeWeapon(
        CatacombEnemy enemy,
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip)
    {
        Vector2 guard = hand + direction * 4f;
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, hand - direction * 6f, guard,
            5f, Outline, flip);
        DrawLineLocal(anchor, hand - direction * 5f, guard,
            3f, Leather, flip);
        if (enemy.EquipmentVariant == 1)
        {
            Vector2 haft = guard + direction * 27f;
            DrawLineLocal(anchor, guard, haft, 4f, Leather, flip);
            DrawLineLocal(anchor, haft - normal * 7f,
                haft + normal * 6f, 8f, RustDark, flip);
            DrawLineLocal(anchor, haft - normal * 5f,
                haft + normal * 5f, 4f, Rust, flip);
        }
        else if (enemy.EquipmentVariant == 2)
        {
            Vector2 end = guard + direction * 25f;
            DrawLineLocal(anchor, guard, end, 4f, Leather, flip);
            DrawBox(anchor, end.X - 5f, end.Y - 5f, 10f, 10f,
                RustDark, flip);
        }
        else
        {
            Vector2 tip = guard + direction * 30f;
            DrawLineLocal(anchor, guard - normal * 6f,
                guard + normal * 6f, 4f, Rust, flip);
            DrawLineLocal(anchor, guard, tip, 7f, RustDark, flip);
            DrawLineLocal(anchor, guard + normal, tip + normal,
                3.5f, Blade, flip);
        }
    }

    private void DrawBuckler(Vector2 anchor, Vector2 hand, bool flip)
    {
        DrawBox(anchor, hand.X - 7f, hand.Y - 8f, 13f, 16f,
            Outline, flip);
        DrawBox(anchor, hand.X - 5f, hand.Y - 6f, 10f, 12f,
            RustDark, flip);
        DrawBox(anchor, hand.X - 1f, hand.Y - 2f, 4f, 4f,
            Rust, flip);
    }

    private void DrawCracks(
        Vector2 anchor,
        Vector2 knee,
        Vector2 hand,
        bool flip)
    {
        DrawLineLocal(anchor, knee + new Vector2(-2f, -4f),
            knee + new Vector2(2f, 0f), 1f, Outline, flip);
        DrawLineLocal(anchor, hand + new Vector2(-2f, -5f),
            hand + new Vector2(2f, -1f), 1f, Outline, flip);
    }

    private void DrawImpactDust(
        Vector2 anchor,
        bool flip,
        float progress,
        int variant)
    {
        float spread = 5f + progress * 12f;
        Color dust = new(184, 177, 151, (int)(180f * (1f - progress)));
        DrawBox(anchor, -spread, -39f - progress * 5f, 3f, 3f, dust, flip);
        DrawBox(anchor, spread - 2f, -31f + progress * 4f, 2f, 2f,
            dust, flip);
        DrawBox(anchor, 1f, -48f - progress * 8f, 2f, 3f, dust, flip);
        if (variant != 0)
            DrawBox(anchor, spread + 2f, -42f, 2f, 2f,
                new Color(168, 91, 55, (int)dust.A), flip);
    }

    private void DrawSkeletonDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 1.45f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .34f) / .66f));
        Color dust = new(160, 153, 132, (int)(115f * (1f - progress)));
        DrawLineLocal(anchor, new Vector2(-13f - settle * 10f, -35f + fall * 31f),
            new Vector2(8f + settle * 13f, -18f + fall * 15f),
            5f, Bone, flip);
        DrawLineLocal(anchor, new Vector2(-4f, -48f + fall * 43f),
            new Vector2(17f, -31f + fall * 27f), 4f, BoneDark, flip);
        DrawLineLocal(anchor, new Vector2(-17f, -23f + fall * 20f),
            new Vector2(-31f, -7f + fall * 4f), 4f, Bone, flip);
        DrawLineLocal(anchor, new Vector2(9f, -24f + fall * 21f),
            new Vector2(29f, -5f + fall * 2f), 4f, Bone, flip);
        DrawBox(anchor, 14f + settle * 10f, -16f + fall * 12f,
            17f, 13f, Outline, flip);
        DrawBox(anchor, 16f + settle * 10f, -14f + fall * 10f,
            13f, 9f, Bone, flip);
        DrawLineLocal(anchor, new Vector2(-7f, -3f),
            new Vector2(30f, -2f), 5f, RustDark, flip);
        DrawBox(anchor, -26f, -4f - progress * 6f, 4f, 3f, dust, flip);
        DrawBox(anchor, 4f, -7f - progress * 9f, 3f, 3f, dust, flip);
        DrawBox(anchor, 25f, -5f - progress * 4f, 3f, 2f, dust, flip);
    }

    private void DrawArcherDeath(Vector2 anchor, bool flip, float progress)
    {
        float buckle = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.7f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .28f) / .72f));
        DrawBox(anchor, -18f, -30f + buckle * 24f, 31f, 27f,
            ClothDark, flip);
        DrawBox(anchor, -15f, -28f + buckle * 23f, 25f, 23f,
            Cloth, flip);
        DrawLineLocal(anchor, new Vector2(-8f, -48f + buckle * 41f),
            new Vector2(13f, -25f + buckle * 18f), 4f,
            ArcherBone, flip);
        DrawBox(anchor, 8f + settle * 8f, -20f + buckle * 15f,
            16f, 12f, BoneDark, flip);
        DrawLineLocal(anchor, new Vector2(20f, -42f + buckle * 34f),
            new Vector2(39f + settle * 8f, -5f), 5f, BowDark, flip);
        DrawLineLocal(anchor, new Vector2(39f + settle * 8f, -5f),
            new Vector2(31f + settle * 10f, -22f + buckle * 18f),
            3f, BowWood, flip);
        Color dust = new(140, 140, 132, (int)(100f * (1f - progress)));
        DrawBox(anchor, -24f, -6f - progress * 7f, 3f, 3f, dust, flip);
        DrawBox(anchor, 18f, -7f - progress * 4f, 2f, 3f, dust, flip);
    }

    private static SkeletonPose CreatePose(
        CatacombEnemy enemy,
        EnemyAnimationController animation)
    {
        float p = animation.Progress;
        float wave = MathF.Sin(p * MathHelper.TwoPi);
        var pose = new SkeletonPose
        {
            WeaponAngle = .88f,
            HeadShift = 1f,
            LooseArm = 1f,
            CloakTrail = 1f
        };

        switch (animation.State)
        {
            case EnemyVisualState.Idle:
                pose.UpperLean = wave * 1.4f;
                pose.HeadShift = MathF.Sin(p * MathHelper.TwoPi + 1.1f) * 2f;
                pose.LooseArm = MathF.Sin(p * MathHelper.TwoPi * .5f) * 2f;
                pose.JawDrop = p > .72f && p < .86f ? 2f : 0f;
                pose.Crouch = MathF.Max(0f, wave) * .8f;
                pose.CloakTrail = 1.5f + MathF.Sin(p * MathHelper.TwoPi) * 1.2f;
                break;
            case EnemyVisualState.Walk:
            case EnemyVisualState.Move:
            case EnemyVisualState.Retreat:
                pose.Step = wave * (enemy.Type == EnemyType.Skeleton ? 5f : 3.5f);
                pose.Bob = MathF.Abs(wave) * 1.5f;
                pose.UpperLean = enemy.Type == EnemyType.Skeleton ? 3f : 1.5f;
                pose.HeadShift = MathF.Sin(p * MathHelper.TwoPi * 1.5f) * 2f;
                pose.WeaponAngle = 1.2f;
                pose.CloakTrail = 4f + MathF.Abs(wave) * 3f;
                break;
            case EnemyVisualState.AttackWindup:
                if (enemy.Type == EnemyType.SkeletonArcher)
                {
                    pose.UpperLean = MathHelper.Lerp(0f, -3f, p);
                    pose.BowPush = MathHelper.Lerp(0f, -7f, p);
                }
                else if (enemy.SkeletonAttack == SkeletonAttackKind.OverheadChop)
                {
                    pose.UpperLean = MathHelper.Lerp(0f, -5f, p);
                    pose.Crouch = p * 2f;
                    pose.WeaponAngle = MathHelper.Lerp(.7f, -1.55f, p);
                }
                else if (enemy.SkeletonAttack == SkeletonAttackKind.ReturnCut)
                {
                    pose.UpperLean = -2f;
                    pose.WeaponAngle = MathHelper.Lerp(.25f, .55f, p);
                }
                else
                {
                    pose.UpperLean = MathHelper.Lerp(0f, -4f, p);
                    pose.WeaponAngle = MathHelper.Lerp(.8f, -2.25f, p);
                }
                break;
            case EnemyVisualState.Attack:
            case EnemyVisualState.HeavyMeleeAttack:
                pose.UpperLean = MathHelper.Lerp(-2f, 7f, p);
                pose.Crouch = 2f;
                if (enemy.Type == EnemyType.SkeletonArcher)
                    pose.BowPush = MathHelper.Lerp(-5f, 14f, p);
                else if (enemy.SkeletonAttack == SkeletonAttackKind.ReturnCut)
                    pose.WeaponAngle = MathHelper.Lerp(.45f, -1.75f, p);
                else if (enemy.SkeletonAttack == SkeletonAttackKind.OverheadChop)
                    pose.WeaponAngle = MathHelper.Lerp(-1.55f, 1.15f, p);
                else
                    pose.WeaponAngle = MathHelper.Lerp(-2.25f, .2f, p);
                break;
            case EnemyVisualState.Aim:
                pose.Crouch = 2f;
                pose.UpperLean = enemy.IsHeavyArrow
                    ? MathHelper.Lerp(0f, -5f, p)
                    : MathHelper.Lerp(0f, -2f, p);
                pose.CloakTrail = 2f;
                break;
            case EnemyVisualState.Shoot:
                pose.UpperLean = 4f * (1f - p);
                pose.CloakTrail = 5f;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.UpperLean = MathHelper.Lerp(6f, 0f, p);
                pose.Crouch = MathHelper.Lerp(3f, 0f, p);
                pose.WeaponAngle = MathHelper.Lerp(.35f, .88f, p);
                pose.CloakTrail = MathHelper.Lerp(5f, 1f, p);
                break;
            case EnemyVisualState.Hurt:
                float hitScale = enemy.IsHeavyHitReaction ? 1.45f :
                    enemy.Type == EnemyType.SkeletonArcher ? 1.18f : 1f;
                pose.UpperLean = -7f * hitScale * (1f - p);
                pose.HeadShift = -4f * hitScale * (1f - p);
                pose.LooseArm = 7f * hitScale;
                pose.Crouch = 2f * hitScale;
                pose.WeaponAngle = 1.5f;
                pose.CloakTrail = 6f;
                break;
            case EnemyVisualState.Stagger:
                pose.UpperLean = MathHelper.Lerp(-10f, -3f, p);
                pose.Lean = -4f;
                pose.Crouch = MathHelper.Lerp(7f, 3f, p);
                pose.HeadShift = -5f;
                pose.LooseArm = 9f;
                pose.WeaponAngle = 1.9f;
                pose.CloakTrail = 8f;
                break;
        }

        if (enemy.Type == EnemyType.Skeleton &&
            enemy.CombatState is CatacombCombatState.MeleeWindup or
                CatacombCombatState.MeleeActive or
                CatacombCombatState.MeleeRecovery)
        {
            pose.WeaponAngle = enemy.MeleeWeaponAngle;
        }

        return pose;
    }

    private void DrawBone(
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

    private void DrawJoint(
        Vector2 anchor,
        Vector2 point,
        Color color,
        bool flip)
    {
        DrawBox(anchor, point.X - 3f, point.Y - 3f, 6f, 6f,
            Outline, flip);
        DrawBox(anchor, point.X - 2f, point.Y - 2f, 4f, 4f,
            color, flip);
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
        DrawLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawLine(
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(
            _pixel,
            from,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, .5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    private struct SkeletonPose
    {
        public float Bob;
        public float Lean;
        public float UpperLean;
        public float Crouch;
        public float Step;
        public float HeadShift;
        public float LooseArm;
        public float WeaponAngle;
        public float JawDrop;
        public float CloakTrail;
        public float BowPush;
    }
}
