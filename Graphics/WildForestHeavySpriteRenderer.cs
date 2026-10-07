using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Procedural presentation for the Wild Forest's heavy normal and elite
/// enemies. Poses are authored facing right and mirrored as a unit for left.
/// </summary>
public sealed class WildForestHeavySpriteRenderer
{
    private static readonly Color Outline = new(18, 17, 20);
    private static readonly Color BarkBlack = new(42, 33, 27);
    private static readonly Color Bark = new(67, 49, 35);
    private static readonly Color BarkLight = new(91, 65, 43);
    private static readonly Color Corruption = new(126, 39, 91);
    private static readonly Color CorruptionHot = new(214, 52, 87);
    private static readonly Color Fungus = new(33, 25, 38);
    private static readonly Color ChiefSkin = new(72, 104, 52);
    private static readonly Color ChiefSkinDark = new(47, 73, 39);
    private static readonly Color Iron = new(91, 88, 82);
    private static readonly Color IronLight = new(137, 128, 112);
    private static readonly Color Leather = new(94, 57, 39);
    private static readonly Color ChiefRed = new(125, 35, 36);
    private static readonly Color Bone = new(206, 190, 151);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public WildForestHeavySpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(Enemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _controllers.GetValue(
            enemy,
            CreateController);
        animation.Update(gameTime, enemy);

        if (enemy is CorruptedTreant treant)
            DrawTreant(treant, animation);
        else if (enemy is GoblinChief chief)
            DrawChief(chief, animation);
    }

    private static EnemyAnimationController CreateController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy is CorruptedTreant
                ? EnemyVisualProfile.CorruptedTreant
                : EnemyVisualProfile.GoblinChief);
    }

    private void DrawTreant(
        CorruptedTreant treant,
        EnemyAnimationController animation)
    {
        Vector2 anchor = new(treant.Position.X, treant.Bounds.Bottom);
        bool flip = treant.Facing == EnemyFacingDirection.Left;

        if (animation.State == EnemyVisualState.Death)
        {
            DrawTreantDeath(anchor, flip, animation.Progress);
            return;
        }

        TreantPose pose = CreateTreantPose(animation);
        Vector2 body = anchor + new Vector2(pose.BodyX, pose.BodyY);
        Color bark = treant.IsHitFlashing
            ? new Color(208, 196, 171)
            : Bark;
        float pulse = 0.55f + 0.45f * MathF.Sin(
            animation.Elapsed * 2.2f);
        Color crack = Color.Lerp(Corruption, CorruptionHot,
            MathHelper.Clamp(pulse + pose.CrackHeat, 0f, 1f));

        DrawBox(anchor, -45f, -5f, 90f, 7f,
            new Color(12, 13, 12, 145), flip);
        DrawTreantRootFoot(body, flip, -17f, pose.LeftRootLift);
        DrawTreantRootFoot(body, flip, 15f, pose.RightRootLift);

        DrawLine(body, new Vector2(-16f, -38f),
            new Vector2(-18f, -4f - pose.LeftRootLift), 15f,
            Outline, flip);
        DrawLine(body, new Vector2(-16f, -37f),
            new Vector2(-18f, -4f - pose.LeftRootLift), 10f,
            BarkBlack, flip);
        DrawLine(body, new Vector2(15f, -38f),
            new Vector2(18f, -4f - pose.RightRootLift), 16f,
            Outline, flip);
        DrawLine(body, new Vector2(15f, -37f),
            new Vector2(18f, -4f - pose.RightRootLift), 11f,
            bark, flip);

        DrawTreantArm(body, flip, front: false,
            new Vector2(-24f, -65f), pose.BackElbow, pose.BackHand,
            BarkBlack);
        DrawTreantArm(body, flip, front: true,
            new Vector2(25f, -65f), pose.FrontElbow, pose.FrontHand,
            bark);

        DrawBox(body, -29f, -78f, 58f, 48f, Outline, flip);
        DrawBox(body, -26f, -76f, 52f, 44f, bark, flip);
        DrawBox(body, -21f, -84f, 43f, 27f, Outline, flip);
        DrawBox(body, -18f, -81f, 37f, 24f, BarkBlack, flip);
        DrawBox(body, -13f, -68f, 26f, 14f,
            new Color(29, 23, 25), flip);
        DrawBox(body, -10f, -67f, 5f, 4f, CorruptionHot, flip);
        DrawBox(body, 6f, -67f, 5f, 4f, CorruptionHot, flip);
        DrawBox(body, -9f, -58f, 19f, 5f, Outline, flip);
        DrawBox(body, -6f, -57f, 13f, 2f, Corruption, flip);

        DrawLine(body, new Vector2(-4f, -53f),
            new Vector2(-11f, -38f), 4f, crack, flip);
        DrawLine(body, new Vector2(-11f, -38f),
            new Vector2(-4f, -24f), 3f, crack, flip);
        DrawLine(body, new Vector2(8f, -49f),
            new Vector2(13f, -33f), 3f, crack, flip);
        DrawLine(body, new Vector2(13f, -33f),
            new Vector2(8f, -16f), 2f, crack, flip);

        DrawTreantShoulderSpikes(body, flip, pose.SpikeSpread);
        DrawDeadVines(body, flip, pose.BranchLag);
        DrawBox(body, -28f, -49f, 8f, 6f, Fungus, flip);
        DrawBox(body, -32f, -43f, 12f, 5f, Fungus, flip);
        DrawBox(body, 19f, -29f, 7f, 5f, Fungus, flip);

        if (animation.State == EnemyVisualState.HeavyMeleeAttack &&
            animation.Progress > 0.42f)
        {
            DrawBox(body, 74f, -8f, 5f, 4f,
                new Color(102, 73, 45, 150), flip);
            DrawBox(body, 84f, -5f, 4f, 3f,
                new Color(126, 88, 50, 125), flip);
        }
    }

    private void DrawTreantArm(
        Vector2 anchor,
        bool flip,
        bool front,
        Vector2 shoulder,
        Vector2 elbow,
        Vector2 hand,
        Color color)
    {
        float upperWidth = front ? 18f : 16f;
        DrawLine(anchor, shoulder, elbow, upperWidth + 6f, Outline, flip);
        DrawLine(anchor, elbow, hand, upperWidth + 2f, Outline, flip);
        DrawLine(anchor, shoulder, elbow, upperWidth, color, flip);
        DrawLine(anchor, elbow, hand, upperWidth - 4f, color, flip);
        DrawLine(anchor, hand, hand + new Vector2(9f, -8f), 5f,
            Outline, flip);
        DrawLine(anchor, hand, hand + new Vector2(13f, 1f), 5f,
            color, flip);
        DrawLine(anchor, hand, hand + new Vector2(8f, 8f), 4f,
            color, flip);
    }

    private void DrawTreantRootFoot(
        Vector2 anchor,
        bool flip,
        float x,
        float lift)
    {
        float y = -2f - lift;
        DrawLine(anchor, new Vector2(x, -8f),
            new Vector2(x - 17f, y), 7f, Outline, flip);
        DrawLine(anchor, new Vector2(x, -7f),
            new Vector2(x - 15f, y), 4f, BarkBlack, flip);
        DrawLine(anchor, new Vector2(x + 2f, -7f),
            new Vector2(x + 19f, y + 1f), 7f, Outline, flip);
        DrawLine(anchor, new Vector2(x + 2f, -6f),
            new Vector2(x + 17f, y + 1f), 4f, Bark, flip);
    }

    private void DrawTreantShoulderSpikes(
        Vector2 anchor,
        bool flip,
        float spread)
    {
        DrawLine(anchor, new Vector2(-22f, -77f),
            new Vector2(-37f - spread, -101f), 7f, Outline, flip);
        DrawLine(anchor, new Vector2(-22f, -77f),
            new Vector2(-36f - spread, -99f), 4f, BarkBlack, flip);
        DrawLine(anchor, new Vector2(-29f, -72f),
            new Vector2(-48f - spread, -87f), 6f, Outline, flip);
        DrawLine(anchor, new Vector2(-29f, -72f),
            new Vector2(-46f - spread, -86f), 3f, Bark, flip);
    }

    private void DrawDeadVines(Vector2 anchor, bool flip, float lag)
    {
        DrawLine(anchor, new Vector2(-30f, -68f),
            new Vector2(-42f - lag, -51f), 3f, Fungus, flip);
        DrawLine(anchor, new Vector2(-42f - lag, -51f),
            new Vector2(-38f - lag, -38f), 2f, Fungus, flip);
        DrawLine(anchor, new Vector2(28f, -60f),
            new Vector2(37f - lag, -43f), 2f, Fungus, flip);
    }

    private void DrawTreantDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f, progress);
        float bodyY = MathHelper.Lerp(-67f, -24f, fall);
        Color crack = Color.Lerp(CorruptionHot, BarkBlack, fall);
        DrawBox(anchor, -48f, -5f, 96f, 7f,
            new Color(12, 13, 12, 150), flip);
        DrawLine(anchor, new Vector2(-23f, bodyY),
            new Vector2(-55f, -7f), 15f, BarkBlack, flip);
        DrawLine(anchor, new Vector2(21f, bodyY),
            new Vector2(58f, -5f), 16f, Bark, flip);
        DrawBox(anchor, -30f, bodyY - 28f, 61f, 35f, Outline, flip);
        DrawBox(anchor, -27f, bodyY - 25f, 55f, 29f, BarkBlack, flip);
        DrawLine(anchor, new Vector2(-5f, bodyY - 21f),
            new Vector2(7f, bodyY - 5f), 3f, crack, flip);
        DrawLine(anchor, new Vector2(-28f, bodyY - 12f),
            new Vector2(-52f, -4f), 8f, Bark, flip);
        DrawLine(anchor, new Vector2(29f, bodyY - 11f),
            new Vector2(56f, -3f), 8f, BarkBlack, flip);
    }

    private static TreantPose CreateTreantPose(
        EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new TreantPose
        {
            FrontElbow = new Vector2(38f, -48f),
            FrontHand = new Vector2(47f, -27f),
            BackElbow = new Vector2(-37f, -49f),
            BackHand = new Vector2(-46f, -28f)
        };

        switch (animation.State)
        {
            case EnemyVisualState.Idle:
                pose.BodyX = MathF.Sin(cycle) * 0.8f;
                pose.BranchLag = MathF.Sin(cycle) * 1.5f;
                break;
            case EnemyVisualState.Walk:
                pose.BodyX = MathF.Sin(cycle) * 2f;
                pose.BodyY = MathF.Abs(MathF.Sin(cycle)) * 1.5f;
                pose.LeftRootLift = MathF.Max(0f, MathF.Sin(cycle)) * 5f;
                pose.RightRootLift = MathF.Max(0f, -MathF.Sin(cycle)) * 5f;
                pose.BranchLag = -MathF.Sin(cycle) * 4f;
                break;
            case EnemyVisualState.AttackWindup:
                pose.BodyX = -5f * animation.Progress;
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(38f, -48f), new Vector2(17f, -84f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(47f, -27f), new Vector2(-5f, -94f),
                    animation.Progress);
                pose.SpikeSpread = animation.Progress * 3f;
                break;
            case EnemyVisualState.HeavyMeleeAttack:
                pose.BodyX = 5f * animation.Progress;
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(17f, -84f), new Vector2(55f, -50f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(-5f, -94f), new Vector2(89f, -18f),
                    animation.Progress);
                break;
            case EnemyVisualState.AttackRecovery:
                pose.BodyX = MathHelper.Lerp(5f, 0f, animation.Progress);
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(55f, -50f), new Vector2(42f, -38f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(89f, -18f), new Vector2(51f, -17f),
                    animation.Progress);
                break;
            case EnemyVisualState.RootStrikeWindup:
                pose.BodyY = 2f * animation.Progress;
                pose.FrontHand = new Vector2(49f, -61f);
                pose.BackHand = new Vector2(-48f, -60f);
                pose.CrackHeat = animation.Progress * 0.45f;
                pose.SpikeSpread = animation.Progress * 5f;
                break;
            case EnemyVisualState.RootStrike:
                pose.BodyY = 5f;
                pose.FrontHand = new Vector2(57f, -14f);
                pose.BackHand = new Vector2(-55f, -15f);
                pose.CrackHeat = 0.6f;
                break;
            case EnemyVisualState.RootStrikeRecovery:
                pose.BodyY = MathHelper.Lerp(5f, 0f, animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(57f, -14f), new Vector2(47f, -27f),
                    animation.Progress);
                pose.BackHand = Vector2.Lerp(
                    new Vector2(-55f, -15f), new Vector2(-46f, -28f),
                    animation.Progress);
                pose.CrackHeat = 0.4f * (1f - animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -3f * (1f - animation.Progress);
                pose.BranchLag = 4f * (1f - animation.Progress);
                break;
            case EnemyVisualState.Stagger:
                pose.BodyX = -7f;
                pose.BodyY = 10f;
                pose.LeftRootLift = 4f;
                pose.FrontHand = new Vector2(58f, -23f);
                pose.BackHand = new Vector2(-57f, -24f);
                pose.SpikeSpread = 5f;
                break;
        }

        return pose;
    }

    private void DrawChief(
        GoblinChief chief,
        EnemyAnimationController animation)
    {
        Vector2 anchor = new(chief.Position.X, chief.Bounds.Bottom);
        bool flip = chief.Facing == EnemyFacingDirection.Left;

        if (animation.State == EnemyVisualState.Death)
        {
            DrawChiefDeath(anchor, flip, animation.Progress);
            return;
        }

        ChiefPose pose = CreateChiefPose(animation);
        Vector2 body = anchor + new Vector2(pose.BodyX, pose.BodyY);
        Color skin = chief.IsHitFlashing
            ? new Color(215, 206, 170)
            : ChiefSkin;

        DrawBox(anchor, -31f, -4f, 63f, 6f,
            new Color(12, 13, 12, 145), flip);
        DrawChiefBanner(body, flip, pose.BannerLag, pose.BannerDrop);

        DrawLine(body, new Vector2(-13f, -25f),
            new Vector2(-17f + pose.LeftStep, -2f), 10f,
            Outline, flip);
        DrawLine(body, new Vector2(-13f, -24f),
            new Vector2(-17f + pose.LeftStep, -2f), 6f,
            Leather, flip);
        DrawLine(body, new Vector2(13f, -25f),
            new Vector2(17f + pose.RightStep, -2f), 10f,
            Outline, flip);
        DrawLine(body, new Vector2(13f, -24f),
            new Vector2(17f + pose.RightStep, -2f), 6f,
            Leather, flip);

        DrawBox(body, -23f, -48f, 46f, 29f, Outline, flip);
        DrawBox(body, -20f, -46f, 40f, 25f, Leather, flip);
        DrawBox(body, -17f, -44f, 34f, 7f, ChiefRed, flip);
        DrawBox(body, -9f, -40f, 18f, 17f, Iron, flip);
        DrawBox(body, -5f, -38f, 10f, 13f, ChiefSkinDark, flip);

        DrawLine(body, new Vector2(-19f, -42f),
            new Vector2(-31f, -28f), 11f, Outline, flip);
        DrawLine(body, new Vector2(-19f, -41f),
            new Vector2(-30f, -28f), 7f, skin, flip);
        DrawLine(body, new Vector2(19f, -42f), pose.WeaponHand,
            12f, Outline, flip);
        DrawLine(body, new Vector2(19f, -41f), pose.WeaponHand,
            8f, skin, flip);

        DrawBox(body, -30f, -51f, 20f, 20f, Outline, flip);
        DrawBox(body, -28f, -49f, 17f, 16f, Iron, flip);
        DrawBox(body, -27f, -47f, 13f, 5f, ChiefRed, flip);
        DrawBox(body, 13f, -46f, 10f, 13f, Bone, flip);

        DrawBox(body, -18f, -64f, 37f, 22f, Outline, flip);
        DrawBox(body, -15f, -61f, 31f, 18f, skin, flip);
        DrawBox(body, -17f, -66f, 35f, 8f, Iron, flip);
        DrawBox(body, -14f, -64f, 28f, 4f, IronLight, flip);
        DrawChiefHorn(body, flip, -12f, -64f, -1f);
        DrawChiefHorn(body, flip, 12f, -64f, 1f);
        DrawBox(body, 8f, -55f, 5f, 4f,
            new Color(230, 67, 42), flip);
        DrawLine(body, new Vector2(5f, -59f),
            new Vector2(13f, -48f), 2f, ChiefRed, flip);
        DrawBox(body, 11f, -48f, 7f, 4f, ChiefSkinDark, flip);

        DrawBox(body, -8f, -28f, 3f, 5f, Bone, flip);
        DrawBox(body, -2f, -27f, 3f, 6f, Bone, flip);
        DrawBox(body, 4f, -28f, 3f, 5f, Bone, flip);

        DrawChiefAxe(body, flip, pose);

        if (animation.State == EnemyVisualState.Attack &&
            animation.Progress > 0.45f)
        {
            DrawBox(body, 51f, -4f, 5f, 3f,
                new Color(122, 81, 48, 150), flip);
            DrawBox(body, 61f, -2f, 4f, 2f,
                new Color(153, 101, 55, 120), flip);
        }

        if (animation.State == EnemyVisualState.WarCry)
            DrawWarCryPulse(body, flip, animation.Progress);
    }

    private void DrawChiefBanner(
        Vector2 anchor,
        bool flip,
        float lag,
        float drop)
    {
        DrawLine(anchor, new Vector2(-22f, -15f),
            new Vector2(-24f, -70f + drop), 4f, Outline, flip);
        DrawLine(anchor, new Vector2(-22f, -15f),
            new Vector2(-24f, -70f + drop), 2f, Bone, flip);
        DrawLine(anchor, new Vector2(-24f, -67f + drop),
            new Vector2(-39f - lag, -61f + drop), 16f,
            Outline, flip);
        DrawLine(anchor, new Vector2(-24f, -67f + drop),
            new Vector2(-37f - lag, -61f + drop), 12f,
            ChiefRed, flip);
        DrawLine(anchor, new Vector2(-37f - lag, -61f + drop),
            new Vector2(-30f - lag, -53f + drop), 7f,
            ChiefRed, flip);
    }

    private void DrawChiefHorn(
        Vector2 anchor,
        bool flip,
        float x,
        float y,
        float direction)
    {
        DrawLine(anchor, new Vector2(x, y),
            new Vector2(x + direction * 10f, y - 10f), 7f,
            Outline, flip);
        DrawLine(anchor, new Vector2(x, y),
            new Vector2(x + direction * 9f, y - 9f), 4f,
            Bone, flip);
        DrawLine(anchor, new Vector2(x + direction * 9f, y - 9f),
            new Vector2(x + direction * 13f, y - 4f), 3f,
            Bone, flip);
    }

    private void DrawChiefAxe(
        Vector2 anchor,
        bool flip,
        ChiefPose pose)
    {
        DrawLine(anchor, pose.WeaponHand, pose.WeaponHead,
            7f, Outline, flip);
        DrawLine(anchor, pose.WeaponHand, pose.WeaponHead,
            4f, new Color(90, 52, 31), flip);
        Vector2 bladeRoot = pose.WeaponHead + new Vector2(-2f, -1f);
        DrawLine(anchor, bladeRoot,
            bladeRoot + pose.BladeDirection * 13f, 12f,
            Outline, flip);
        DrawLine(anchor, bladeRoot,
            bladeRoot + pose.BladeDirection * 12f, 8f,
            IronLight, flip);
    }

    private void DrawWarCryPulse(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float expansion = MathHelper.Lerp(25f, 47f, progress);
        byte alpha = (byte)MathHelper.Lerp(150f, 25f, progress);
        Color pulse = new((byte)171, (byte)59, (byte)35, alpha);
        DrawLine(anchor, new Vector2(18f, -53f),
            new Vector2(expansion, -58f), 3f, pulse, flip);
        DrawLine(anchor, new Vector2(19f, -48f),
            new Vector2(expansion + 6f, -44f), 2f, pulse, flip);
        DrawLine(anchor, new Vector2(17f, -43f),
            new Vector2(expansion, -33f), 2f, pulse, flip);
    }

    private void DrawChiefDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f, progress);
        float y = MathHelper.Lerp(-42f, -15f, fall);
        DrawBox(anchor, -35f, -4f, 70f, 6f,
            new Color(12, 13, 12, 150), flip);
        DrawLine(anchor, new Vector2(-22f, -55f + 34f * fall),
            new Vector2(-40f, -3f), 3f, Bone, flip);
        DrawLine(anchor, new Vector2(-38f, -38f + 25f * fall),
            new Vector2(-50f, -4f), 10f, ChiefRed, flip);
        DrawBox(anchor, -25f, y - 18f, 51f, 27f, Outline, flip);
        DrawBox(anchor, -22f, y - 15f, 45f, 21f, Leather, flip);
        DrawBox(anchor, 4f, y - 22f, 28f, 17f, ChiefSkinDark, flip);
        DrawLine(anchor, new Vector2(22f, y - 8f),
            new Vector2(57f, -2f), 6f, new Color(90, 52, 31), flip);
        DrawLine(anchor, new Vector2(52f, -3f),
            new Vector2(63f, -10f), 9f, Iron, flip);
    }

    private static ChiefPose CreateChiefPose(
        EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new ChiefPose
        {
            WeaponHand = new Vector2(28f, -34f),
            WeaponHead = new Vector2(39f, -8f),
            BladeDirection = new Vector2(0.7f, -0.7f)
        };

        switch (animation.State)
        {
            case EnemyVisualState.Idle:
                pose.BodyY = MathF.Abs(MathF.Sin(cycle)) * 0.7f;
                pose.BannerLag = MathF.Sin(cycle) * 1.5f;
                break;
            case EnemyVisualState.Walk:
                pose.BodyY = MathF.Abs(MathF.Sin(cycle)) * 2f;
                pose.LeftStep = MathF.Sin(cycle) * 4f;
                pose.RightStep = -pose.LeftStep;
                pose.BannerLag = -MathF.Sin(cycle) * 4f;
                pose.WeaponHead += new Vector2(
                    -MathF.Sin(cycle) * 2f,
                    MathF.Abs(MathF.Sin(cycle)) * 2f);
                break;
            case EnemyVisualState.AttackWindup:
                pose.BodyX = -4f * animation.Progress;
                pose.WeaponHand = Vector2.Lerp(
                    new Vector2(28f, -34f), new Vector2(12f, -54f),
                    animation.Progress);
                pose.WeaponHead = Vector2.Lerp(
                    new Vector2(39f, -8f), new Vector2(-8f, -72f),
                    animation.Progress);
                pose.BladeDirection = new Vector2(-0.8f, -0.5f);
                pose.BannerLag = animation.Progress * 5f;
                break;
            case EnemyVisualState.Attack:
                pose.BodyX = 5f * animation.Progress;
                pose.WeaponHand = Vector2.Lerp(
                    new Vector2(12f, -54f), new Vector2(34f, -24f),
                    animation.Progress);
                pose.WeaponHead = Vector2.Lerp(
                    new Vector2(-8f, -72f), new Vector2(65f, -7f),
                    animation.Progress);
                pose.BladeDirection = new Vector2(0.85f, 0.45f);
                pose.BannerLag = 6f;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.BodyX = MathHelper.Lerp(5f, 0f, animation.Progress);
                pose.WeaponHand = Vector2.Lerp(
                    new Vector2(34f, -24f), new Vector2(28f, -34f),
                    animation.Progress);
                pose.WeaponHead = Vector2.Lerp(
                    new Vector2(65f, -7f), new Vector2(39f, -8f),
                    animation.Progress);
                pose.BladeDirection = new Vector2(0.7f, -0.7f);
                pose.BannerLag = MathHelper.Lerp(6f, 0f,
                    animation.Progress);
                break;
            case EnemyVisualState.WarCryWindup:
                pose.BodyY = 3f * animation.Progress;
                pose.LeftStep = -4f * animation.Progress;
                pose.RightStep = 4f * animation.Progress;
                pose.WeaponHand = new Vector2(26f, -50f);
                pose.WeaponHead = new Vector2(35f, -72f);
                pose.BannerLag = -6f * animation.Progress;
                break;
            case EnemyVisualState.WarCry:
                pose.BodyX = 4f;
                pose.BodyY = -3f;
                pose.LeftStep = -4f;
                pose.RightStep = 4f;
                pose.WeaponHand = new Vector2(30f, -56f);
                pose.WeaponHead = new Vector2(40f, -75f);
                pose.BannerLag = 9f;
                break;
            case EnemyVisualState.WarCryRecovery:
                pose.BodyX = MathHelper.Lerp(4f, 0f, animation.Progress);
                pose.BodyY = MathHelper.Lerp(-3f, 0f, animation.Progress);
                pose.WeaponHand = Vector2.Lerp(
                    new Vector2(30f, -56f), new Vector2(28f, -34f),
                    animation.Progress);
                pose.WeaponHead = Vector2.Lerp(
                    new Vector2(40f, -75f), new Vector2(39f, -8f),
                    animation.Progress);
                pose.BannerLag = MathHelper.Lerp(9f, 0f,
                    animation.Progress);
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -3f * (1f - animation.Progress);
                pose.WeaponHead += new Vector2(-5f, 4f);
                pose.BannerLag = 4f;
                break;
            case EnemyVisualState.Stagger:
                pose.BodyX = -7f;
                pose.BodyY = 7f;
                pose.WeaponHand = new Vector2(24f, -22f);
                pose.WeaponHead = new Vector2(37f, -2f);
                pose.BannerLag = 10f;
                pose.BannerDrop = 7f;
                break;
        }

        return pose;
    }

    private void DrawBox(
        Vector2 anchor,
        float localX,
        float localY,
        float width,
        float height,
        Color color,
        bool flip)
    {
        float x = flip ? -localX - width : localX;
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                (int)MathF.Round(anchor.X + x),
                (int)MathF.Round(anchor.Y + localY),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))),
            color);
    }

    private void DrawLine(
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

        if (length <= 0.01f)
            return;

        _spriteBatch.Draw(
            _pixel,
            start,
            sourceRectangle: null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            layerDepth: 0f);
    }

    private struct TreantPose
    {
        public float BodyX;
        public float BodyY;
        public float LeftRootLift;
        public float RightRootLift;
        public float BranchLag;
        public float SpikeSpread;
        public float CrackHeat;
        public Vector2 FrontElbow;
        public Vector2 FrontHand;
        public Vector2 BackElbow;
        public Vector2 BackHand;
    }

    private struct ChiefPose
    {
        public float BodyX;
        public float BodyY;
        public float LeftStep;
        public float RightStep;
        public float BannerLag;
        public float BannerDrop;
        public Vector2 WeaponHand;
        public Vector2 WeaponHead;
        public Vector2 BladeDirection;
    }
}
