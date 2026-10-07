using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Bosses;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Procedural presentation for the Wild Forest elite nest mother and region
/// boss. All shapes reuse the shared pixel texture and GameTime clocks.
/// </summary>
public sealed class WildForestApexSpriteRenderer
{
    public static readonly Vector2 AncientTreantVisualSize = new(190f, 168f);

    private static readonly Color Outline = new(16, 15, 20);
    private static readonly Color SpiderShell = new(48, 27, 58);
    private static readonly Color SpiderArmor = new(105, 32, 48);
    private static readonly Color SpiderHighlight = new(112, 66, 119);
    private static readonly Color SpiderEye = new(244, 45, 47);
    private static readonly Color Poison = new(57, 92, 58);
    private static readonly Color Silk = new(191, 190, 181);
    private static readonly Color Bone = new(220, 205, 169);
    private static readonly Color BossBark = new(52, 42, 31);
    private static readonly Color BossBarkLight = new(77, 61, 39);
    private static readonly Color Moss = new(50, 78, 42);
    private static readonly Color BossGlow = new(100, 190, 112);
    private static readonly Color BossGlowHot = new(151, 221, 126);
    private static readonly Color Fungus = new(95, 145, 84);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _enemyControllers = new();
    private readonly ConditionalWeakTable<AncientTreant, BossAnimationController>
        _bossControllers = new();

    public WildForestApexSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(Enemy enemy, GameTime gameTime)
    {
        EnemyAnimationController animation = _enemyControllers.GetValue(
            enemy,
            CreateEnemyController);
        animation.Update(gameTime, enemy);

        if (enemy is MotherSpider mother)
            DrawMotherSpider(mother, animation);
        else if (enemy is Spiderling spiderling)
            DrawSpiderling(spiderling, animation);
    }

    public void Draw(AncientTreant boss, GameTime gameTime)
    {
        BossAnimationController animation = _bossControllers.GetValue(
            boss,
            static _ => new BossAnimationController());
        animation.Update(gameTime, boss);
        DrawAncientTreant(boss, animation);
    }

    private static EnemyAnimationController CreateEnemyController(Enemy enemy)
    {
        return new EnemyAnimationController(
            enemy is MotherSpider
                ? EnemyVisualProfile.MotherSpider
                : EnemyVisualProfile.Spiderling);
    }

    private void DrawMotherSpider(
        MotherSpider mother,
        EnemyAnimationController animation)
    {
        Vector2 anchor = new(mother.Position.X, mother.Bounds.Bottom);
        bool flip = mother.Facing == EnemyFacingDirection.Left;

        if (animation.State == EnemyVisualState.Death)
        {
            DrawMotherDeath(anchor, flip, animation.Progress);
            return;
        }

        MotherPose pose = CreateMotherPose(animation);
        Vector2 body = anchor + new Vector2(pose.BodyX, pose.BodyY);
        Color shell = mother.IsHitFlashing
            ? new Color(220, 207, 198)
            : SpiderShell;

        DrawBox(anchor, -66f, -4f, 132f, 6f,
            new Color(10, 11, 13, 145), flip);

        for (int leg = 0; leg < 4; leg++)
        {
            float direction = leg < 2 ? -1f : 1f;
            int local = leg % 2;
            float rootX = direction * (19f + local * 11f);
            float phase = pose.LegPhase + leg * 0.85f;
            float stride = MathF.Sin(phase) * pose.LegStride;
            float jointX = direction * (46f + local * 9f) + stride;
            float jointY = -20f - local * 5f - MathF.Abs(stride) * 0.12f;
            float tipX = direction * (66f + local * 3f) - stride * 0.7f;
            float tipY = -1f + pose.LegCollapse;
            DrawLine(body, new Vector2(rootX, -25f),
                new Vector2(jointX, jointY), 8f, Outline, flip);
            DrawLine(body, new Vector2(jointX, jointY),
                new Vector2(tipX, tipY), 7f, Outline, flip);
            DrawLine(body, new Vector2(rootX, -25f),
                new Vector2(jointX, jointY), 4f, shell, flip);
            DrawLine(body, new Vector2(jointX, jointY),
                new Vector2(tipX, tipY), 3f, SpiderArmor, flip);

            if (leg == 0 || leg == 3)
            {
                DrawLine(body, new Vector2(jointX, jointY),
                    new Vector2(jointX + direction * 8f, jointY - 9f),
                    3f, Bone, flip);
            }
        }

        float abdomenWidth = 67f + pose.AbdomenPulse;
        DrawBox(body, -52f - pose.AbdomenPulse * 0.5f, -42f,
            abdomenWidth, 35f, Outline, flip);
        DrawBox(body, -49f - pose.AbdomenPulse * 0.5f, -39f,
            abdomenWidth - 6f, 29f, shell, flip);
        DrawBox(body, -39f, -37f, 38f, 8f,
            SpiderHighlight, flip);
        DrawBox(body, -45f, -24f, 49f, 9f,
            SpiderArmor, flip);
        DrawBox(body, -54f, -26f, 10f, 13f,
            new Color(65, 31, 69), flip);

        DrawEggSac(body, flip, -38f, -9f, pose.EggOpen);
        DrawEggSac(body, flip, -22f, -7f, pose.EggOpen);
        DrawEggSac(body, flip, -6f, -9f, pose.EggOpen);
        DrawAttachedSpiderling(body, flip, -31f, -34f,
            animation.Progress * MathHelper.TwoPi);
        DrawAttachedSpiderling(body, flip, -9f, -18f,
            animation.Progress * MathHelper.TwoPi + 1.7f);

        DrawBox(body, 3f, -38f + pose.FrontLift, 39f, 28f,
            Outline, flip);
        DrawBox(body, 6f, -35f + pose.FrontLift, 33f, 22f,
            SpiderArmor, flip);
        DrawBox(body, 13f, -34f + pose.FrontLift, 23f, 9f,
            shell, flip);

        for (int eye = 0; eye < 3; eye++)
        {
            DrawBox(body, 22f + eye * 5f,
                -32f + pose.FrontLift + (eye % 2) * 4f,
                4f, 4f, SpiderEye, flip);
        }

        DrawLine(body, new Vector2(27f, -17f + pose.FrontLift),
            new Vector2(35f + pose.FangReach,
                -5f + pose.FrontLift), 6f, Outline, flip);
        DrawLine(body, new Vector2(29f, -17f + pose.FrontLift),
            new Vector2(35f + pose.FangReach,
                -5f + pose.FrontLift), 3f, Bone, flip);
        DrawLine(body, new Vector2(37f, -17f + pose.FrontLift),
            new Vector2(43f + pose.FangReach,
                -6f + pose.FrontLift), 6f, Outline, flip);
        DrawLine(body, new Vector2(38f, -17f + pose.FrontLift),
            new Vector2(43f + pose.FangReach,
                -6f + pose.FrontLift), 3f, Bone, flip);
        DrawBox(body, 38f + pose.FangReach,
            -7f + pose.FrontLift, 5f, 3f, Poison, flip);

        if (animation.State == EnemyVisualState.WebPrepare ||
            animation.State == EnemyVisualState.WebShoot)
        {
            float silkLength = animation.State == EnemyVisualState.WebShoot
                ? 24f
                : 5f + animation.Progress * 9f;
            DrawLine(body, new Vector2(40f, -13f + pose.FrontLift),
                new Vector2(40f + silkLength, -12f + pose.FrontLift),
                4f, Silk, flip);
            DrawLine(body, new Vector2(40f, -13f + pose.FrontLift),
                new Vector2(39f + silkLength, -17f + pose.FrontLift),
                2f, new Color(225, 220, 210), flip);
        }

        if (animation.State == EnemyVisualState.SummonSpiderlings)
        {
            DrawLine(body, new Vector2(-37f, -8f),
                new Vector2(-51f, -1f), 3f, Silk, flip);
            DrawLine(body, new Vector2(-19f, -7f),
                new Vector2(-29f, 1f), 2f, Silk, flip);
            DrawBox(body, -55f, -3f, 8f, 5f, SpiderShell, flip);
        }
    }

    private void DrawEggSac(
        Vector2 anchor,
        bool flip,
        float x,
        float y,
        float open)
    {
        DrawBox(anchor, x, y - open * 2f, 12f, 9f, Outline, flip);
        DrawBox(anchor, x + 2f, y + 1f - open * 2f, 8f, 6f,
            new Color(151, 139, 117), flip);

        if (open > 0.35f)
            DrawBox(anchor, x + 4f, y + 1f, 4f, 6f,
                new Color(55, 42, 41), flip);
    }

    private void DrawAttachedSpiderling(
        Vector2 anchor,
        bool flip,
        float x,
        float y,
        float cycle)
    {
        float crawl = MathF.Sin(cycle) * 2f;
        DrawBox(anchor, x + crawl, y, 8f, 5f, Outline, flip);
        DrawBox(anchor, x + 2f + crawl, y + 1f, 5f, 3f,
            SpiderArmor, flip);
        DrawLine(anchor, new Vector2(x + crawl, y + 2f),
            new Vector2(x - 5f + crawl, y + 5f), 1.5f, Outline, flip);
        DrawLine(anchor, new Vector2(x + 8f + crawl, y + 2f),
            new Vector2(x + 13f + crawl, y + 5f), 1.5f, Outline, flip);
    }

    private void DrawMotherDeath(Vector2 anchor, bool flip, float progress)
    {
        float collapse = MathHelper.SmoothStep(0f, 1f, progress);
        DrawBox(anchor, -67f, -4f, 134f, 6f,
            new Color(10, 11, 13, 150), flip);

        for (int leg = 0; leg < 4; leg++)
        {
            float direction = leg < 2 ? -1f : 1f;
            float rootX = direction * (17f + leg % 2 * 10f);
            Vector2 joint = new(direction * MathHelper.Lerp(49f, 33f,
                collapse), MathHelper.Lerp(-20f, -8f, collapse));
            Vector2 tip = new(direction * MathHelper.Lerp(68f, 40f,
                collapse), -1f);
            DrawLine(anchor, new Vector2(rootX, -18f), joint,
                7f, SpiderShell, flip);
            DrawLine(anchor, joint, tip, 6f, SpiderArmor, flip);
        }

        float y = MathHelper.Lerp(-37f, -22f, collapse);
        DrawBox(anchor, -51f, y, 69f, 28f, Outline, flip);
        DrawBox(anchor, -48f, y + 3f, 63f, 22f,
            SpiderShell, flip);
        DrawBox(anchor, 9f, y + 7f, 37f, 18f,
            new Color(63, 28, 43), flip);
        DrawBox(anchor, -37f, -8f, 11f, 6f,
            new Color(105, 95, 80), flip);
        DrawBox(anchor, -20f, -7f, 11f, 5f,
            new Color(105, 95, 80), flip);
    }

    private static MotherPose CreateMotherPose(
        EnemyAnimationController animation)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new MotherPose
        {
            LegPhase = cycle,
            LegStride = 2f,
            AbdomenPulse = (MathF.Sin(cycle) + 1f) * 1.2f
        };

        switch (animation.State)
        {
            case EnemyVisualState.Crawl:
                pose.LegStride = 8f;
                pose.BodyY = MathF.Abs(MathF.Sin(cycle)) * 1.2f;
                pose.AbdomenPulse = 1f;
                break;
            case EnemyVisualState.AttackWindup:
                pose.FrontLift = -9f * animation.Progress;
                pose.FangReach = 6f * animation.Progress;
                pose.LegStride = 1f;
                break;
            case EnemyVisualState.BiteAttack:
                pose.BodyX = 11f * animation.Progress;
                pose.FrontLift = MathHelper.Lerp(-9f, 3f,
                    animation.Progress);
                pose.FangReach = 12f;
                break;
            case EnemyVisualState.AttackRecovery:
                pose.BodyX = MathHelper.Lerp(10f, 0f, animation.Progress);
                pose.FangReach = MathHelper.Lerp(12f, 0f,
                    animation.Progress);
                break;
            case EnemyVisualState.WebPrepare:
                pose.BodyX = -4f * animation.Progress;
                pose.FrontLift = -5f;
                pose.AbdomenPulse = animation.Progress * 4f;
                break;
            case EnemyVisualState.WebShoot:
                pose.BodyX = 5f;
                pose.FrontLift = 2f;
                break;
            case EnemyVisualState.SummonWindup:
                pose.BodyY = 5f * animation.Progress;
                pose.AbdomenPulse = -animation.Progress * 3f;
                pose.EggOpen = animation.Progress * 0.45f;
                pose.LegStride = 0f;
                break;
            case EnemyVisualState.SummonSpiderlings:
                pose.BodyY = 5f;
                pose.AbdomenPulse = 4f;
                pose.EggOpen = 1f;
                pose.LegStride = 0f;
                break;
            case EnemyVisualState.SummonRecovery:
                pose.BodyY = MathHelper.Lerp(5f, 0f, animation.Progress);
                pose.EggOpen = 1f - animation.Progress;
                pose.LegStride = 1f;
                break;
            case EnemyVisualState.Hurt:
                pose.BodyX = -4f * (1f - animation.Progress);
                pose.LegStride = 5f;
                pose.AbdomenPulse = 2f;
                break;
            case EnemyVisualState.Stagger:
                pose.BodyY = 9f;
                pose.FrontLift = 7f;
                pose.LegCollapse = 5f;
                pose.LegStride = 0f;
                break;
        }

        return pose;
    }

    private void DrawSpiderling(
        Spiderling spiderling,
        EnemyAnimationController animation)
    {
        Vector2 anchor = new(spiderling.Position.X, spiderling.Bounds.Bottom);
        bool flip = spiderling.Facing == EnemyFacingDirection.Left;
        float cycle = animation.Progress * MathHelper.TwoPi;
        float stride = animation.State == EnemyVisualState.Crawl
            ? MathF.Sin(cycle) * 4f
            : 0f;
        float snap = animation.State == EnemyVisualState.BiteAttack
            ? animation.Progress * 5f
            : 0f;
        Color shell = spiderling.IsHitFlashing
            ? new Color(215, 202, 195)
            : SpiderShell;

        for (int leg = 0; leg < 3; leg++)
        {
            float y = -10f + leg * 3f;
            DrawLine(anchor, new Vector2(-5f, y),
                new Vector2(-17f - stride * (leg % 2 == 0 ? 1f : -1f),
                    -1f), 2f, shell, flip);
            DrawLine(anchor, new Vector2(5f, y),
                new Vector2(17f + stride * (leg % 2 == 0 ? 1f : -1f),
                    -1f), 2f, shell, flip);
        }

        DrawBox(anchor, -10f, -15f, 16f, 11f, Outline, flip);
        DrawBox(anchor, -8f, -13f, 13f, 8f, SpiderShell, flip);
        DrawBox(anchor, 3f + snap, -13f, 10f, 9f,
            SpiderArmor, flip);
        DrawBox(anchor, 9f + snap, -11f, 3f, 3f,
            SpiderEye, flip);
        DrawLine(anchor, new Vector2(10f + snap, -6f),
            new Vector2(14f + snap, -1f), 2f, Bone, flip);
    }

    private void DrawAncientTreant(
        AncientTreant boss,
        BossAnimationController animation)
    {
        Vector2 anchor = new(boss.Position.X, boss.Bounds.Bottom);
        bool flip = boss.FacesLeft;

        if (animation.State == BossVisualState.Death)
        {
            DrawBossDeath(anchor, flip, animation.Progress);
            return;
        }

        BossPose pose = CreateBossPose(animation, boss.Phase);
        Vector2 body = anchor + new Vector2(pose.BodyX, pose.BodyY);
        float phaseHeat = boss.Phase switch
        {
            AncientTreantPhase.PhaseThree => 1f,
            AncientTreantPhase.PhaseTwo => 0.55f,
            _ => 0.18f
        };
        float pulse = 0.5f + MathF.Sin(animation.Elapsed *
            (boss.Phase == AncientTreantPhase.PhaseThree ? 4f : 2f)) * 0.25f;
        Color glow = Color.Lerp(BossGlow, BossGlowHot,
            MathHelper.Clamp(phaseHeat * 0.55f + pulse, 0f, 1f));
        Color bark = boss.IsHitFlashing
            ? new Color(205, 201, 174)
            : Color.Lerp(BossBark, new Color(67, 42, 34), phaseHeat * 0.45f);

        DrawBox(anchor, -91f, -6f, 182f, 8f,
            new Color(9, 13, 10, 165), flip);
        DrawBossRootFoot(body, flip, -31f, pose.LeftRootLift,
            pose.RootSpread, bark);
        DrawBossRootFoot(body, flip, 27f, pose.RightRootLift,
            pose.RootSpread, bark);

        DrawLine(body, new Vector2(-28f, -66f),
            new Vector2(-32f, -8f - pose.LeftRootLift), 25f,
            Outline, flip);
        DrawLine(body, new Vector2(-28f, -64f),
            new Vector2(-32f, -8f - pose.LeftRootLift), 18f,
            BossBark, flip);
        DrawLine(body, new Vector2(28f, -66f),
            new Vector2(32f, -8f - pose.RightRootLift), 26f,
            Outline, flip);
        DrawLine(body, new Vector2(28f, -64f),
            new Vector2(32f, -8f - pose.RightRootLift), 19f,
            bark, flip);

        DrawBossArm(body, flip, new Vector2(-42f, -111f),
            pose.BackElbow, pose.BackHand, BossBark);
        DrawBossArm(body, flip, new Vector2(43f, -111f),
            pose.FrontElbow, pose.FrontHand, bark);

        DrawBox(body, -47f, -126f, 94f, 68f, Outline, flip);
        DrawBox(body, -43f, -122f, 86f, 61f, bark, flip);
        DrawBox(body, -37f, -116f, 74f, 17f, Moss, flip);
        DrawBox(body, -34f, -112f, 68f, 42f,
            new Color(38, 34, 29), flip);

        DrawBox(body, -24f, -108f, 48f, 31f,
            new Color(29, 28, 25), flip);
        DrawBox(body, -18f, -103f, 12f, 8f, glow, flip);
        DrawBox(body, 7f, -103f, 12f, 8f, glow, flip);
        DrawBox(body, -17f, -87f, 35f, 7f, Outline, flip);
        DrawBox(body, -11f, -85f, 23f, 3f,
            new Color(65, 91, 57), flip);

        DrawBossCrown(body, flip, pose.CrownLift, pose.BranchSway,
            bark, glow);
        DrawBossCracks(body, flip, glow, phaseHeat + pose.GlowBoost);
        DrawBossMossAndFungi(body, flip, glow, pose.BranchSway);

        if ((animation.State == BossVisualState.HeavyAttack ||
             animation.State == BossVisualState.AggressiveAttack) &&
            animation.Progress > 0.48f)
        {
            for (int chip = 0; chip < 4; chip++)
            {
                DrawBox(body, 105f + chip * 9f,
                    -8f - (chip % 2) * 5f, 5f, 4f,
                    new Color(111, 78, 44, 145), flip);
            }
        }
    }

    private void DrawBossRootFoot(
        Vector2 anchor,
        bool flip,
        float x,
        float lift,
        float spread,
        Color bark)
    {
        DrawLine(anchor, new Vector2(x, -12f),
            new Vector2(x - 31f - spread, -2f - lift),
            12f, Outline, flip);
        DrawLine(anchor, new Vector2(x, -10f),
            new Vector2(x - 29f - spread, -2f - lift),
            7f, bark, flip);
        DrawLine(anchor, new Vector2(x + 5f, -12f),
            new Vector2(x + 36f + spread, -1f - lift),
            13f, Outline, flip);
        DrawLine(anchor, new Vector2(x + 5f, -10f),
            new Vector2(x + 34f + spread, -1f - lift),
            8f, BossBark, flip);
    }

    private void DrawBossArm(
        Vector2 anchor,
        bool flip,
        Vector2 shoulder,
        Vector2 elbow,
        Vector2 hand,
        Color bark)
    {
        DrawLine(anchor, shoulder, elbow, 28f, Outline, flip);
        DrawLine(anchor, elbow, hand, 23f, Outline, flip);
        DrawLine(anchor, shoulder, elbow, 21f, bark, flip);
        DrawLine(anchor, elbow, hand, 16f, bark, flip);
        DrawLine(anchor, hand, hand + new Vector2(17f, -12f),
            7f, bark, flip);
        DrawLine(anchor, hand, hand + new Vector2(22f, 1f),
            8f, bark, flip);
        DrawLine(anchor, hand, hand + new Vector2(15f, 13f),
            7f, bark, flip);
    }

    private void DrawBossCrown(
        Vector2 anchor,
        bool flip,
        float lift,
        float sway,
        Color bark,
        Color glow)
    {
        Vector2 root = new(0f, -121f - lift);
        Vector2[] tips =
        {
            new(-63f - sway, -157f - lift),
            new(-34f - sway * 0.5f, -168f - lift),
            new(0f, -163f - lift),
            new(35f + sway * 0.5f, -168f - lift),
            new(64f + sway, -154f - lift)
        };

        foreach (Vector2 tip in tips)
        {
            DrawLine(anchor, root, tip, 11f, Outline, flip);
            DrawLine(anchor, root, tip, 6f, bark, flip);
            DrawBox(anchor, tip.X - 3f, tip.Y - 3f, 6f, 6f,
                glow, flip);
        }
    }

    private void DrawBossCracks(
        Vector2 anchor,
        bool flip,
        Color glow,
        float amount)
    {
        float width = 2f + MathHelper.Clamp(amount, 0f, 1.5f);
        DrawLine(anchor, new Vector2(-7f, -76f),
            new Vector2(-17f, -55f), width, glow, flip);
        DrawLine(anchor, new Vector2(-17f, -55f),
            new Vector2(-8f, -34f), width, glow, flip);

        if (amount > 0.45f)
        {
            DrawLine(anchor, new Vector2(16f, -75f),
                new Vector2(24f, -52f), width, glow, flip);
            DrawLine(anchor, new Vector2(24f, -52f),
                new Vector2(17f, -25f), width, glow, flip);
        }

        if (amount > 0.85f)
        {
            DrawLine(anchor, new Vector2(-31f, -93f),
                new Vector2(-38f, -69f), width, glow, flip);
            DrawLine(anchor, new Vector2(34f, -91f),
                new Vector2(39f, -67f), width, glow, flip);
        }
    }

    private void DrawBossMossAndFungi(
        Vector2 anchor,
        bool flip,
        Color glow,
        float sway)
    {
        DrawBox(anchor, -44f, -119f, 29f, 9f, Moss, flip);
        DrawBox(anchor, 19f, -119f, 26f, 8f, Moss, flip);
        DrawLine(anchor, new Vector2(-47f, -111f),
            new Vector2(-58f - sway, -78f), 4f, Moss, flip);
        DrawLine(anchor, new Vector2(46f, -107f),
            new Vector2(55f - sway, -74f), 4f, Moss, flip);
        DrawBox(anchor, -39f, -92f, 8f, 6f, Fungus, flip);
        DrawBox(anchor, -29f, -96f, 6f, 5f, glow, flip);
        DrawBox(anchor, 31f, -83f, 8f, 6f, Fungus, flip);
        DrawBox(anchor, 38f, -88f, 5f, 5f, glow, flip);
    }

    private void DrawBossDeath(Vector2 anchor, bool flip, float progress)
    {
        float collapse = MathHelper.SmoothStep(0f, 1f, progress);
        float trunkY = MathHelper.Lerp(-105f, -43f, collapse);
        Color fadingGlow = Color.Lerp(BossGlowHot, BossBark,
            MathHelper.Clamp(progress * 1.25f, 0f, 1f));
        DrawBox(anchor, -94f, -6f, 188f, 8f,
            new Color(9, 13, 10, 170), flip);
        DrawLine(anchor, new Vector2(-37f, trunkY),
            new Vector2(-88f, -4f), 23f, BossBark, flip);
        DrawLine(anchor, new Vector2(38f, trunkY),
            new Vector2(91f, -3f), 24f, BossBarkLight, flip);
        DrawBox(anchor, -50f, trunkY - 40f, 101f, 50f,
            Outline, flip);
        DrawBox(anchor, -46f, trunkY - 36f, 93f, 43f,
            BossBark, flip);
        DrawBox(anchor, -19f, trunkY - 27f, 13f, 7f,
            fadingGlow, flip);
        DrawBox(anchor, 7f, trunkY - 27f, 13f, 7f,
            fadingGlow, flip);
        DrawLine(anchor, new Vector2(-8f, trunkY - 15f),
            new Vector2(5f, trunkY + 3f), 4f, fadingGlow, flip);
        DrawLine(anchor, new Vector2(-45f, trunkY - 31f),
            new Vector2(-79f, -7f), 13f, BossBark, flip);
        DrawLine(anchor, new Vector2(46f, trunkY - 30f),
            new Vector2(82f, -6f), 13f, BossBarkLight, flip);
    }

    private static BossPose CreateBossPose(
        BossAnimationController animation,
        AncientTreantPhase phase)
    {
        float cycle = animation.Progress * MathHelper.TwoPi;
        var pose = new BossPose
        {
            FrontElbow = new Vector2(67f, -87f),
            FrontHand = new Vector2(82f, -55f),
            BackElbow = new Vector2(-66f, -89f),
            BackHand = new Vector2(-80f, -57f),
            RootSpread = phase == AncientTreantPhase.PhaseThree ? 8f : 0f
        };

        switch (animation.State)
        {
            case BossVisualState.Idle:
                pose.BodyX = MathF.Sin(cycle) * 1f;
                pose.BranchSway = MathF.Sin(cycle) *
                    (phase == AncientTreantPhase.PhaseThree ? 4f : 2f);
                break;
            case BossVisualState.Advance:
                pose.BodyX = MathF.Sin(cycle) * 3f;
                pose.BodyY = MathF.Abs(MathF.Sin(cycle)) * 2f;
                pose.LeftRootLift = MathF.Max(0f, MathF.Sin(cycle)) * 7f;
                pose.RightRootLift = MathF.Max(0f, -MathF.Sin(cycle)) * 7f;
                pose.BranchSway = -MathF.Sin(cycle) * 6f;
                break;
            case BossVisualState.HeavyAttackWindup:
                pose.BodyX = -7f * animation.Progress;
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(67f, -87f), new Vector2(25f, -145f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(82f, -55f), new Vector2(-8f, -155f),
                    animation.Progress);
                pose.RootSpread += animation.Progress * 9f;
                break;
            case BossVisualState.HeavyAttack:
            case BossVisualState.AggressiveAttack:
                pose.BodyX = 8f * animation.Progress;
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(25f, -145f), new Vector2(87f, -80f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(-8f, -155f), new Vector2(139f, -26f),
                    animation.Progress);
                pose.RootSpread += 10f;
                pose.GlowBoost = phase == AncientTreantPhase.PhaseThree
                    ? 0.5f
                    : 0.2f;
                break;
            case BossVisualState.HeavyAttackRecovery:
                pose.BodyX = MathHelper.Lerp(8f, 0f, animation.Progress);
                pose.FrontElbow = Vector2.Lerp(
                    new Vector2(87f, -80f), new Vector2(67f, -87f),
                    animation.Progress);
                pose.FrontHand = Vector2.Lerp(
                    new Vector2(139f, -26f), new Vector2(82f, -55f),
                    animation.Progress);
                pose.RootSpread += MathHelper.Lerp(10f, 0f,
                    animation.Progress);
                break;
            case BossVisualState.RootWindup:
                pose.FrontHand = new Vector2(92f, -39f);
                pose.BackHand = new Vector2(-91f, -40f);
                pose.BodyY = animation.Progress * 3f;
                pose.RootSpread += animation.Progress * 14f;
                pose.GlowBoost = animation.Progress * 0.5f;
                break;
            case BossVisualState.RootStrike:
                pose.FrontHand = new Vector2(101f, -15f);
                pose.BackHand = new Vector2(-100f, -16f);
                pose.BodyY = 6f;
                pose.RootSpread += 16f;
                pose.GlowBoost = 0.7f;
                break;
            case BossVisualState.RootRecovery:
                pose.BodyY = MathHelper.Lerp(6f, 0f, animation.Progress);
                pose.RootSpread += MathHelper.Lerp(16f, 0f,
                    animation.Progress);
                pose.GlowBoost = 0.5f * (1f - animation.Progress);
                break;
            case BossVisualState.PhaseTransitionOne:
            case BossVisualState.PhaseTransitionTwo:
                float rise = MathF.Sin(animation.Progress * MathHelper.Pi);
                pose.BodyY = -5f * rise;
                pose.CrownLift = 14f * rise;
                pose.RootSpread += 21f * rise;
                pose.FrontHand = new Vector2(102f, -79f);
                pose.BackHand = new Vector2(-101f, -80f);
                pose.BranchSway = MathF.Sin(animation.Progress *
                    MathHelper.Pi * 5f) * 7f;
                pose.GlowBoost = 0.8f * rise;
                break;
            case BossVisualState.Hurt:
                pose.BodyX = -3f * (1f - animation.Progress);
                pose.BranchSway = 4f * (1f - animation.Progress);
                break;
            case BossVisualState.Stagger:
                pose.BodyX = -10f;
                pose.BodyY = 15f;
                pose.LeftRootLift = 6f;
                pose.FrontHand = new Vector2(106f, -47f);
                pose.BackHand = new Vector2(-105f, -49f);
                pose.CrownLift = -8f;
                pose.GlowBoost = -0.35f;
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

    private struct MotherPose
    {
        public float BodyX;
        public float BodyY;
        public float FrontLift;
        public float FangReach;
        public float AbdomenPulse;
        public float LegPhase;
        public float LegStride;
        public float LegCollapse;
        public float EggOpen;
    }

    private struct BossPose
    {
        public float BodyX;
        public float BodyY;
        public float LeftRootLift;
        public float RightRootLift;
        public float RootSpread;
        public float BranchSway;
        public float CrownLift;
        public float GlowBoost;
        public Vector2 FrontElbow;
        public Vector2 FrontHand;
        public Vector2 BackElbow;
        public Vector2 BackHand;
    }

    private enum BossVisualState
    {
        Idle,
        Advance,
        HeavyAttackWindup,
        HeavyAttack,
        HeavyAttackRecovery,
        RootWindup,
        RootStrike,
        RootRecovery,
        PhaseTransitionOne,
        PhaseTransitionTwo,
        AggressiveAttack,
        Hurt,
        Stagger,
        Death
    }

    private sealed class BossAnimationController
    {
        private float _elapsed;

        public BossVisualState State { get; private set; }
        public float Elapsed => _elapsed;
        public float Progress { get; private set; }

        public void Update(GameTime gameTime, AncientTreant boss)
        {
            BossVisualState next = SelectState(boss);
            bool changed = next != State;

            if (changed)
            {
                State = next;
                _elapsed = 0f;
            }

            if (State == BossVisualState.Death)
            {
                Progress = boss.DeathPresentationProgress;
                return;
            }

            if (State == BossVisualState.PhaseTransitionOne ||
                State == BossVisualState.PhaseTransitionTwo)
            {
                Progress = boss.PhaseTransitionProgress;
                return;
            }

            if (State == BossVisualState.RootWindup ||
                State == BossVisualState.RootStrike ||
                State == BossVisualState.RootRecovery)
            {
                Progress = boss.RootVisualProgress;
                return;
            }

            if (State == BossVisualState.HeavyAttackWindup ||
                State == BossVisualState.HeavyAttack ||
                State == BossVisualState.AggressiveAttack ||
                State == BossVisualState.HeavyAttackRecovery)
            {
                Progress = boss.Attack.PhaseProgress;
                return;
            }

            float elapsed = MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                1f / 10f);

            if (!changed)
                _elapsed += elapsed;

            float duration = State == BossVisualState.Advance
                ? boss.Phase == AncientTreantPhase.PhaseThree ? 0.66f : 0.92f
                : 1.8f;
            Progress = duration <= 0f ? 0f : _elapsed / duration % 1f;
        }

        private static BossVisualState SelectState(AncientTreant boss)
        {
            if (!boss.IsAlive)
                return BossVisualState.Death;
            if (boss.IsPhaseTransitioning)
            {
                return boss.Phase == AncientTreantPhase.PhaseTwo
                    ? BossVisualState.PhaseTransitionOne
                    : BossVisualState.PhaseTransitionTwo;
            }
            if (boss.IsStaggered)
                return BossVisualState.Stagger;
            if (boss.IsHitFlashing)
                return BossVisualState.Hurt;

            if (boss.Attack.Phase == Combat.EnemyAttackPhase.Windup)
                return BossVisualState.HeavyAttackWindup;
            if (boss.Attack.Phase == Combat.EnemyAttackPhase.Active)
            {
                return boss.Phase == AncientTreantPhase.PhaseThree
                    ? BossVisualState.AggressiveAttack
                    : BossVisualState.HeavyAttack;
            }
            if (boss.Attack.Phase == Combat.EnemyAttackPhase.Recovery)
                return BossVisualState.HeavyAttackRecovery;

            return boss.RootVisualState switch
            {
                AncientTreantRootVisualState.Windup =>
                    BossVisualState.RootWindup,
                AncientTreantRootVisualState.Strike =>
                    BossVisualState.RootStrike,
                AncientTreantRootVisualState.Recovery =>
                    BossVisualState.RootRecovery,
                _ => MathF.Abs(boss.VisualVelocityX) > 1f
                    ? BossVisualState.Advance
                    : BossVisualState.Idle
            };
        }
    }
}
