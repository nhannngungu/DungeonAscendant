using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only presentation for the War Tomb guardian and Fallen Hall boss.
/// Their shapes, weapons, phase language, and deaths intentionally differ.
/// </summary>
public sealed class CatacombKnightSpriteRenderer
{
    private static readonly Color Outline = new(13, 14, 18);
    private static readonly Color BlackIron = new(39, 40, 46);
    private static readonly Color Iron = new(61, 60, 67);
    private static readonly Color IronEdge = new(93, 86, 88);
    private static readonly Color DeathCape = new(49, 31, 38);
    private static readonly Color RoyalDark = new(39, 41, 49);
    private static readonly Color RoyalPlate = new(79, 78, 86);
    private static readonly Color RoyalEdge = new(129, 120, 112);
    private static readonly Color RoyalGold = new(132, 99, 56);
    private static readonly Color RoyalCape = new(55, 36, 52);
    private static readonly Color Blade = new(143, 139, 139);
    private static readonly Color Curse = new(101, 42, 85);
    private static readonly Color CurseHot = new(157, 51, 84);
    private static readonly Color Violet = new(111, 68, 137);
    private static readonly Color Dust = new(91, 78, 68);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<Enemy, EnemyAnimationController>
        _controllers = new();

    public CatacombKnightSpriteRenderer(
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
        bool lockedAttackPose = (enemy.CombatState is
                CatacombCombatState.KnightWindup or
                CatacombCombatState.KnightActive) &&
            enemy.FallenKnightAttack != FallenKnightAttackKind.GraveStep;
        if (lockedAttackPose)
            flip = enemy.LockedAttackDirection < 0f;

        DrawShadow(anchor, enemy.Type == EnemyType.DeathKnight ? 43f : 37f);
        if (enemy.Type == EnemyType.DeathKnight)
        {
            DrawDeathKnightTelegraph(enemy, anchor, flip, animation.Elapsed);
            if (enemy.IsAlive)
                DrawDeathKnight(enemy, anchor, flip, animation);
            else
                DrawDeathKnightDeath(anchor, flip,
                    enemy.DeathPresentationProgress);
            return;
        }

        DrawFallenTelegraph(enemy, anchor, flip, animation.Elapsed);
        if (enemy.IsAlive)
            DrawFallenKnight(enemy, anchor, flip, animation);
        else
            DrawFallenKnightDeath(anchor, flip,
                enemy.DeathPresentationProgress);
    }

    private static EnemyAnimationController CreateController(Enemy enemy) =>
        new(enemy.Type == EnemyType.DeathKnight
            ? EnemyVisualProfile.DeathKnight
            : EnemyVisualProfile.FallenKnight);

    private void DrawDeathKnight(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        float step = animation.State == EnemyVisualState.Walk
            ? MathF.Sin(animation.Elapsed * 3.7f) * 5f
            : 0f;
        float bob = animation.State == EnemyVisualState.Walk
            ? MathF.Abs(MathF.Sin(animation.Elapsed * 3.7f)) * 2f
            : 0f;
        float crouch = animation.State == EnemyVisualState.KnightDormant
            ? 31f * (1f - progress)
            : 0f;
        float lean = 0f;
        float weaponAngle = .58f;
        float weaponReach = 0f;

        if (animation.State == EnemyVisualState.AttackWindup)
        {
            crouch = 5f + progress * 6f;
            lean = -8f * progress;
            weaponAngle = enemy.DeathKnightAttack switch
            {
                DeathKnightAttackKind.ExecutionCrush =>
                    MathHelper.Lerp(.55f, -1.58f, progress),
                DeathKnightAttackKind.DreadCharge =>
                    MathHelper.Lerp(.6f, -.08f, progress),
                DeathKnightAttackKind.TombbreakerSlam =>
                    MathHelper.Lerp(.5f, -1.72f, progress),
                _ => MathHelper.Lerp(.45f, -2.48f, progress)
            };
        }
        else if (animation.State == EnemyVisualState.WarSweep)
        {
            lean = 13f; crouch = 8f;
            weaponAngle = MathHelper.Lerp(-2.48f, .18f, progress);
        }
        else if (animation.State == EnemyVisualState.ExecutionCrush)
        {
            lean = 10f; crouch = 10f;
            weaponAngle = MathHelper.Lerp(-1.58f, 1.18f, progress);
        }
        else if (animation.State == EnemyVisualState.DreadCharge)
        {
            lean = 18f; crouch = 12f; weaponAngle = -.04f;
            weaponReach = 17f;
        }
        else if (animation.State == EnemyVisualState.TombbreakerSlam)
        {
            lean = 8f; crouch = 14f;
            weaponAngle = MathHelper.Lerp(-1.72f, 1.44f, progress);
        }
        else if (animation.State == EnemyVisualState.AttackRecovery)
        {
            lean = 12f * (1f - progress);
            crouch = 12f * (1f - progress);
            weaponAngle = MathHelper.Lerp(1.0f, .58f, progress);
        }
        else if (animation.State == EnemyVisualState.Stagger)
        {
            crouch = 25f;
            lean = -12f + MathF.Sin(progress * 10f) * 3f;
            weaponAngle = 1.35f;
        }
        else if (animation.State == EnemyVisualState.Hurt)
        {
            lean = -6f * (1f - progress);
        }

        Color plate = enemy.IsHitFlashing ? IronEdge : Iron;
        Vector2 hips = new(lean * .18f, -43f + crouch + bob);
        Vector2 chest = new(lean, -79f + crouch + bob);
        Vector2 head = chest + new Vector2(0f, -25f);

        DrawCape(anchor, chest, DeathCape, flip, 1f);
        DrawArmoredLeg(anchor, new Vector2(-18f - step * .4f, 0f),
            hips + new Vector2(-13f, 4f), BlackIron, flip, 18f);
        DrawArmoredLeg(anchor, new Vector2(18f + step, 0f),
            hips + new Vector2(13f, 3f), plate, flip, 19f);

        DrawBox(anchor, chest.X - 31f, chest.Y - 13f, 63f, 53f,
            Outline, flip);
        DrawBox(anchor, chest.X - 28f, chest.Y - 10f, 57f, 47f,
            BlackIron, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 7f, 44f, 39f,
            plate, flip);
        DrawBox(anchor, chest.X - 34f, chest.Y - 10f, 14f, 25f,
            IronEdge, flip);
        DrawBox(anchor, chest.X + 21f, chest.Y - 10f, 14f, 25f,
            IronEdge, flip);
        DrawBrokenInsignia(anchor, chest, flip);

        Vector2 shoulder = chest + new Vector2(24f, -2f);
        Vector2 hand = shoulder + new Vector2(10f + weaponReach, 25f);
        DrawLineLocal(anchor, chest + new Vector2(-25f, 1f), hand,
            17f, Outline, flip);
        DrawLineLocal(anchor, chest + new Vector2(-25f, 1f), hand,
            12f, BlackIron, flip);
        DrawLineLocal(anchor, shoulder, hand, 18f, Outline, flip);
        DrawLineLocal(anchor, shoulder, hand, 13f, plate, flip);
        DrawWarBlade(anchor, hand, weaponAngle, flip);

        DrawBox(anchor, head.X - 19f, head.Y - 13f, 39f, 34f,
            Outline, flip);
        DrawBox(anchor, head.X - 16f, head.Y - 10f, 33f, 28f,
            BlackIron, flip);
        DrawBox(anchor, head.X - 20f, head.Y - 10f, 41f, 10f,
            plate, flip);
        DrawBox(anchor, head.X - 12f, head.Y + 3f, 25f, 6f,
            Outline, flip);
        float visorPulse = .65f + MathF.Sin(animation.Elapsed * 6f) * .18f;
        DrawBox(anchor, head.X - 8f, head.Y + 4f, 17f, 3f,
            CurseHot * visorPulse, flip);
        DrawDeathCracks(anchor, chest, animation.Elapsed, flip);

        if (animation.State is EnemyVisualState.WarSweep or
            EnemyVisualState.ExecutionCrush or EnemyVisualState.DreadCharge or
            EnemyVisualState.TombbreakerSlam)
        {
            DrawWeaponTrail(anchor, hand, weaponAngle, 72f,
                Curse * (.45f * (1f - progress * .55f)), flip);
        }
        if (animation.State is EnemyVisualState.ExecutionCrush or
            EnemyVisualState.TombbreakerSlam && progress > .45f)
        {
            DrawImpactDust(anchor, progress, 1.15f);
        }
    }

    private void DrawFallenKnight(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        EnemyAnimationController animation)
    {
        float progress = animation.Progress;
        int phase = enemy.IsPhaseTransitioning
            ? enemy.CombatPhase
            : enemy.ActiveFallenPhase;
        float step = animation.State == EnemyVisualState.Walk
            ? MathF.Sin(animation.Elapsed * 5f) * 5f
            : 0f;
        float capeMotion = MathF.Sin(animation.Elapsed * (phase + 3f)) *
            (phase == 1 ? 2f : 4f + phase);
        float crouch = animation.State == EnemyVisualState.KnightDormant
            ? 25f * (1f - progress)
            : 0f;
        float lean = 0f;
        float swordAngle = .48f;
        float reach = 0f;

        if (animation.State == EnemyVisualState.KnightPhaseTransition)
        {
            crouch = enemy.CombatPhase == 3 ? 20f : 7f;
            lean = -6f + MathF.Sin(progress * 18f) * 3f;
            swordAngle = 1.3f;
        }
        else if (animation.State == EnemyVisualState.AttackWindup)
        {
            FallenKnightAttackKind attack = enemy.FallenKnightAttack;
            crouch = 3f + progress * 5f;
            swordAngle = attack switch
            {
                FallenKnightAttackKind.HeavyOverhead or
                    FallenKnightAttackKind.FinalOathCleave =>
                    MathHelper.Lerp(.45f, -1.62f, progress),
                FallenKnightAttackKind.AdvancingThrust or
                    FallenKnightAttackKind.OathbreakerRush =>
                    MathHelper.Lerp(.45f, -.06f, progress),
                FallenKnightAttackKind.GuardedCounter or
                    FallenKnightAttackKind.CursedCounter =>
                    MathHelper.Lerp(.5f, -2.0f, progress),
                FallenKnightAttackKind.LastJudgment =>
                    MathHelper.Lerp(.5f, -1.52f, progress),
                _ => MathHelper.Lerp(.4f, -2.34f, progress)
            };
        }
        else if (animation.State is EnemyVisualState.RoyalSlash or
            EnemyVisualState.CursedExtensionSlash)
        {
            lean = 12f; crouch = 6f;
            swordAngle = MathHelper.Lerp(-2.34f, .14f, progress);
        }
        else if (animation.State == EnemyVisualState.KnightCounter)
        {
            lean = 13f; crouch = 8f;
            swordAngle = MathHelper.Lerp(-2.0f, .35f, progress);
        }
        else if (animation.State == EnemyVisualState.AdvancingThrust)
        {
            lean = 16f; crouch = 6f; swordAngle = -.05f; reach = 16f;
        }
        else if (animation.State == EnemyVisualState.ExecutionCrush)
        {
            lean = 8f; crouch = 9f;
            swordAngle = MathHelper.Lerp(-1.62f, 1.12f, progress);
        }
        else if (animation.State == EnemyVisualState.GraveStep)
        {
            lean = -13f; crouch = 9f; swordAngle = .18f;
        }
        else if (animation.State == EnemyVisualState.FinalOathCleave)
        {
            lean = 15f; crouch = 12f;
            swordAngle = MathHelper.Lerp(-1.62f, .82f, progress);
        }
        else if (animation.State == EnemyVisualState.OathbreakerRush)
        {
            lean = 19f; crouch = 10f;
            swordAngle = -.35f + MathF.Sin(progress * MathF.PI * 6f) * .62f;
            reach = 14f;
        }
        else if (animation.State == EnemyVisualState.LastJudgment)
        {
            crouch = 14f; swordAngle = 1.46f;
        }
        else if (animation.State == EnemyVisualState.AttackRecovery)
        {
            lean = 9f * (1f - progress);
            crouch = 8f * (1f - progress);
            swordAngle = MathHelper.Lerp(.9f, .48f, progress);
        }
        else if (animation.State == EnemyVisualState.Stagger)
        {
            crouch = 19f; lean = -10f; swordAngle = 1.32f;
        }
        else if (animation.State == EnemyVisualState.Hurt)
        {
            lean = -5f * (1f - progress);
        }

        Color plate = enemy.IsHitFlashing ? RoyalEdge : RoyalPlate;
        Vector2 hips = new(lean * .18f, -42f + crouch);
        Vector2 chest = new(lean, -80f + crouch);
        Vector2 head = chest + new Vector2(-1f, -24f);
        DrawRoyalCape(anchor, chest, capeMotion, phase, flip);
        DrawArmoredLeg(anchor, new Vector2(-14f - step * .35f, 0f),
            hips + new Vector2(-10f, 4f), RoyalDark, flip, 14f);
        DrawArmoredLeg(anchor, new Vector2(15f + step, 0f),
            hips + new Vector2(10f, 3f), plate, flip, 15f);

        DrawBox(anchor, chest.X - 25f, chest.Y - 12f, 51f, 50f,
            Outline, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 9f, 45f, 44f,
            RoyalDark, flip);
        DrawBox(anchor, chest.X - 16f, chest.Y - 6f, 33f, 37f,
            plate, flip);
        DrawBox(anchor, chest.X - 3f, chest.Y - 7f, 6f, 40f,
            RoyalGold, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y + 16f, 45f, 4f,
            RoyalGold * .75f, flip);

        Vector2 shoulder = chest + new Vector2(19f, -1f);
        Vector2 hand = shoulder + new Vector2(9f + reach, 22f);
        DrawLineLocal(anchor, chest + new Vector2(-19f, 0f), hand,
            13f, Outline, flip);
        DrawLineLocal(anchor, chest + new Vector2(-19f, 0f), hand,
            9f, RoyalDark, flip);
        DrawLineLocal(anchor, shoulder, hand, 14f, Outline, flip);
        DrawLineLocal(anchor, shoulder, hand, 10f, plate, flip);
        DrawRoyalGreatsword(anchor, hand, swordAngle, phase, flip);

        DrawBox(anchor, head.X - 15f, head.Y - 12f, 31f, 29f,
            Outline, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 9f, 25f, 23f,
            RoyalDark, flip);
        DrawBox(anchor, head.X - 16f, head.Y - 9f, 33f, 8f,
            plate, flip);
        DrawBox(anchor, head.X - 8f, head.Y + 3f, 17f, 4f,
            phase == 1 ? Outline : Curse * (.6f + phase * .12f), flip);
        DrawCrownCrest(anchor, head, flip);
        DrawRoyalCracks(anchor, chest, phase, animation.Elapsed, flip);

        if (phase >= 2 && animation.State is
            EnemyVisualState.CursedExtensionSlash or
            EnemyVisualState.KnightCounter or
            EnemyVisualState.FinalOathCleave or
            EnemyVisualState.OathbreakerRush)
        {
            DrawWeaponTrail(anchor, hand, swordAngle,
                phase == 3 ? 91f : 79f,
                Violet * (.56f * (1f - progress * .5f)), flip);
        }
        if (phase == 3)
            DrawSoulFragments(anchor, chest, animation.Elapsed, flip);
    }

    private void DrawDeathKnightTelegraph(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        if (!enemy.IsAlive ||
            enemy.CombatState != CatacombCombatState.KnightWindup)
            return;
        float pulse = .38f + MathF.Sin(elapsed * 10f) * .12f;
        if (enemy.DeathKnightAttack == DeathKnightAttackKind.TombbreakerSlam)
        {
            DrawGroundBand(anchor, -168f, 168f, CurseHot * pulse);
            DrawLine(anchor + new Vector2(0f, -5f),
                anchor + new Vector2(0f, -84f), 3f, Curse * pulse);
        }
        else if (enemy.DeathKnightAttack == DeathKnightAttackKind.DreadCharge)
        {
            float direction = enemy.LockedAttackDirection;
            DrawGroundBand(anchor, direction * 25f, direction * 155f,
                Dust * .55f);
        }
        else if (enemy.DeathKnightAttack == DeathKnightAttackKind.WarSweep)
        {
            DrawArcTicks(anchor + new Vector2(0f, -49f), 80f, 138f,
                Curse * pulse, flip);
        }
    }

    private void DrawFallenTelegraph(
        CatacombEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        if (!enemy.IsAlive)
            return;
        float pulse = .42f + MathF.Sin(elapsed * 9f) * .14f;
        if (enemy.IsPhaseTransitioning)
        {
            float radius = 34f + enemy.ActionProgress * 74f;
            DrawDiamond(anchor + new Vector2(0f, -62f), radius,
                CurseHot * ((1f - enemy.ActionProgress * .55f) * .55f));
            return;
        }
        if (enemy.CombatState != CatacombCombatState.KnightWindup)
            return;

        switch (enemy.FallenKnightAttack)
        {
            case FallenKnightAttackKind.CursedCounter:
            case FallenKnightAttackKind.GuardedCounter:
                DrawDiamond(anchor + new Vector2(0f, -64f), 42f,
                    Violet * pulse);
                break;
            case FallenKnightAttackKind.CursedExtensionSlash:
                DrawArcTicks(anchor + new Vector2(0f, -54f), 96f, 171f,
                    Violet * pulse, flip);
                break;
            case FallenKnightAttackKind.FinalOathCleave:
                DrawArcTicks(anchor + new Vector2(0f, -52f), 122f, 216f,
                    CurseHot * pulse, flip);
                break;
            case FallenKnightAttackKind.LastJudgment:
                DrawGroundBand(anchor, -335f, -92f, CurseHot * pulse);
                DrawGroundBand(anchor, 92f, 335f, CurseHot * pulse);
                DrawDiamond(anchor + new Vector2(0f, -4f), 34f,
                    new Color(126, 126, 118) * .42f);
                break;
            case FallenKnightAttackKind.OathbreakerRush:
                DrawGroundBand(anchor, enemy.LockedAttackDirection * 22f,
                    enemy.LockedAttackDirection * 176f, Violet * pulse);
                break;
        }
    }

    private void DrawDeathKnightDeath(Vector2 anchor, bool flip, float progress)
    {
        float knee = MathHelper.Clamp((progress - .18f) / .36f, 0f, 1f);
        float collapse = MathHelper.Clamp((progress - .55f) / .40f, 0f, 1f);
        float fade = 1f - MathHelper.Clamp((progress - .88f) / .12f, 0f, 1f);
        Vector2 chest = new(12f * collapse, -73f + knee * 23f + collapse * 39f);
        DrawCape(anchor, chest, DeathCape * fade, flip, 1f - collapse * .3f);
        DrawLineLocal(anchor, new Vector2(-18f, 0f),
            chest + new Vector2(-12f, 30f), 18f, BlackIron * fade, flip);
        DrawLineLocal(anchor, new Vector2(18f + collapse * 26f, 0f),
            chest + new Vector2(13f, 30f), 18f, Iron * fade, flip);
        DrawBox(anchor, chest.X - 29f, chest.Y - 12f, 59f, 52f,
            Iron * fade, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 38f, 35f, 28f,
            BlackIron * fade, flip);
        Vector2 droppedHand = chest + new Vector2(24f, 22f + progress * 18f);
        DrawWarBlade(anchor, droppedHand,
            MathHelper.Lerp(.65f, 1.5f, MathF.Min(1f, progress * 2f)), flip);
        if (progress > .58f)
            DrawImpactDust(anchor, (progress - .58f) / .42f, 1.45f);
    }

    private void DrawFallenKnightDeath(Vector2 anchor, bool flip, float progress)
    {
        float stop = MathHelper.Clamp(progress / .18f, 0f, 1f);
        float knee = MathHelper.Clamp((progress - .18f) / .34f, 0f, 1f);
        float collapse = MathHelper.Clamp((progress - .56f) / .35f, 0f, 1f);
        float soulFade = 1f - MathHelper.Clamp((progress - .72f) / .28f, 0f, 1f);
        Vector2 chest = new(10f * collapse, -78f + knee * 22f + collapse * 41f);
        DrawRoyalCape(anchor, chest, 0f, 3, flip, soulFade);
        DrawLineLocal(anchor, new Vector2(-14f, 0f),
            chest + new Vector2(-10f, 35f), 14f, RoyalDark, flip);
        DrawLineLocal(anchor, new Vector2(15f + collapse * 22f, 0f),
            chest + new Vector2(10f, 34f), 15f, RoyalPlate, flip);
        DrawBox(anchor, chest.X - 23f, chest.Y - 10f, 47f, 48f,
            RoyalPlate, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 37f, 29f, 27f,
            RoyalDark, flip);
        Vector2 swordHand = chest + new Vector2(22f, 20f + knee * 18f);
        DrawRoyalGreatsword(anchor, swordHand,
            MathHelper.Lerp(.35f, 1.52f, MathF.Min(1f, stop + knee)),
            3, flip, soulFade);
        for (int index = 0; index < 6; index++)
        {
            float side = index % 2 == 0 ? -1f : 1f;
            float rise = progress * (28f + index * 5f);
            DrawBox(anchor, chest.X + side * (12f + index * 4f),
                chest.Y - 8f - rise, 4f, 4f,
                Violet * (soulFade * (.65f - index * .05f)), flip);
        }
        if (progress > .58f)
            DrawImpactDust(anchor, (progress - .58f) / .42f, 1.05f);
    }

    private void DrawWarBlade(Vector2 anchor, Vector2 hand, float angle,
        bool flip)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 head = hand + direction * 73f;
        DrawLineLocal(anchor, hand - direction * 15f, head, 9f,
            Outline, flip);
        DrawLineLocal(anchor, hand - direction * 13f, head, 5f,
            new Color(80, 59, 53), flip);
        DrawLineLocal(anchor, head - normal * 22f, head + normal * 22f,
            15f, Outline, flip);
        DrawLineLocal(anchor, head - normal * 18f, head + normal * 18f,
            10f, Blade, flip);
        DrawLineLocal(anchor, head + normal * 17f,
            head + normal * 26f - direction * 9f, 7f, Blade, flip);
    }

    private void DrawRoyalGreatsword(Vector2 anchor, Vector2 hand,
        float angle, int phase, bool flip, float alpha = 1f)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        float bladeLength = 70f + (phase >= 2 ? 6f : 0f);
        Vector2 guard = hand + direction * 6f;
        Vector2 tip = hand + direction * bladeLength;
        DrawLineLocal(anchor, hand - direction * 13f, tip, 9f,
            Outline * alpha, flip);
        DrawLineLocal(anchor, guard, tip, 5f, Blade * alpha, flip);
        DrawLineLocal(anchor, guard + direction * 4f,
            tip - direction * 5f, 2f,
            (phase == 1 ? RoyalEdge : Violet) * alpha, flip);
        DrawLineLocal(anchor, guard - normal * 13f, guard + normal * 13f,
            5f, RoyalGold * alpha, flip);
    }

    private void DrawCape(Vector2 anchor, Vector2 chest, Color color,
        bool flip, float scale)
    {
        DrawLineLocal(anchor, chest + new Vector2(-21f, -4f),
            chest + new Vector2(-31f, 49f * scale), 20f, Outline, flip);
        DrawLineLocal(anchor, chest + new Vector2(-18f, -2f),
            chest + new Vector2(-29f, 48f * scale), 14f, color, flip);
        DrawLineLocal(anchor, chest + new Vector2(-6f, 13f),
            chest + new Vector2(-15f, 54f * scale), 12f, color * .78f, flip);
    }

    private void DrawRoyalCape(Vector2 anchor, Vector2 chest, float motion,
        int phase, bool flip, float alpha = 1f)
    {
        float tear = phase == 3 ? 11f : 0f;
        DrawLineLocal(anchor, chest + new Vector2(-19f, -4f),
            chest + new Vector2(-35f - motion, 63f - tear),
            18f, Outline * alpha, flip);
        DrawLineLocal(anchor, chest + new Vector2(-16f, -2f),
            chest + new Vector2(-34f - motion, 61f - tear),
            13f, RoyalCape * alpha, flip);
        DrawLineLocal(anchor, chest + new Vector2(-7f, 11f),
            chest + new Vector2(-17f - motion * .6f, 68f),
            phase == 3 ? 7f : 11f, RoyalCape * (.78f * alpha), flip);
    }

    private void DrawArmoredLeg(Vector2 anchor, Vector2 foot, Vector2 hip,
        Color color, bool flip, float thickness)
    {
        DrawLineLocal(anchor, foot, hip, thickness + 5f, Outline, flip);
        DrawLineLocal(anchor, foot, hip, thickness, color, flip);
    }

    private void DrawBrokenInsignia(Vector2 anchor, Vector2 chest, bool flip)
    {
        DrawLineLocal(anchor, chest + new Vector2(-7f, 0f),
            chest + new Vector2(0f, 12f), 3f, RoyalGold * .55f, flip);
        DrawLineLocal(anchor, chest + new Vector2(0f, 12f),
            chest + new Vector2(-5f, 22f), 3f, RoyalGold * .42f, flip);
        DrawLineLocal(anchor, chest + new Vector2(0f, 12f),
            chest + new Vector2(8f, 7f), 2f, RoyalGold * .40f, flip);
    }

    private void DrawCrownCrest(Vector2 anchor, Vector2 head, bool flip)
    {
        DrawLineLocal(anchor, head + new Vector2(-12f, -10f),
            head + new Vector2(-8f, -21f), 4f, RoyalGold, flip);
        DrawLineLocal(anchor, head + new Vector2(-8f, -21f),
            head + new Vector2(-1f, -12f), 4f, RoyalGold, flip);
        DrawLineLocal(anchor, head + new Vector2(-1f, -12f),
            head + new Vector2(5f, -24f), 4f, RoyalGold, flip);
        DrawLineLocal(anchor, head + new Vector2(5f, -24f),
            head + new Vector2(12f, -10f), 4f, RoyalGold, flip);
    }

    private void DrawDeathCracks(Vector2 anchor, Vector2 chest,
        float elapsed, bool flip)
    {
        Color glow = CurseHot * (.52f + MathF.Sin(elapsed * 7f) * .16f);
        DrawLineLocal(anchor, chest + new Vector2(-8f, -4f),
            chest + new Vector2(-1f, 8f), 2f, glow, flip);
        DrawLineLocal(anchor, chest + new Vector2(-1f, 8f),
            chest + new Vector2(-5f, 20f), 2f, glow, flip);
        DrawLineLocal(anchor, chest + new Vector2(13f, 4f),
            chest + new Vector2(19f, 19f), 2f, Curse * .65f, flip);
    }

    private void DrawRoyalCracks(Vector2 anchor, Vector2 chest, int phase,
        float elapsed, bool flip)
    {
        if (phase <= 1)
            return;
        float strength = phase == 3 ? .88f : .56f;
        Color glow = CurseHot * (strength + MathF.Sin(elapsed * 8f) * .10f);
        DrawLineLocal(anchor, chest + new Vector2(-9f, -5f),
            chest + new Vector2(-2f, 8f), 2f, glow, flip);
        DrawLineLocal(anchor, chest + new Vector2(-2f, 8f),
            chest + new Vector2(-7f, 23f), 2f, glow, flip);
        DrawLineLocal(anchor, chest + new Vector2(8f, 1f),
            chest + new Vector2(15f, 16f), 2f, glow, flip);
        if (phase == 3)
            DrawLineLocal(anchor, chest + new Vector2(15f, 16f),
                chest + new Vector2(9f, 29f), 2f, glow, flip);
    }

    private void DrawSoulFragments(Vector2 anchor, Vector2 chest,
        float elapsed, bool flip)
    {
        for (int index = 0; index < 5; index++)
        {
            float angle = elapsed * (.75f + index * .08f) + index * 1.26f;
            float radius = 31f + index * 4f;
            DrawBox(anchor,
                chest.X + MathF.Cos(angle) * radius - 2f,
                chest.Y + MathF.Sin(angle) * 17f - index * 3f,
                4f, 4f, Violet * (.38f + index * .06f), flip);
        }
    }

    private void DrawWeaponTrail(Vector2 anchor, Vector2 hand, float angle,
        float length, Color color, bool flip)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, hand + direction * 24f - normal * 7f,
            hand + direction * length - normal * 12f, 5f, color, flip);
    }

    private void DrawImpactDust(Vector2 anchor, float progress, float scale)
    {
        float spread = (32f + progress * 85f) * scale;
        float alpha = MathF.Max(0f, 1f - progress) * .72f;
        DrawLine(anchor + new Vector2(-spread, -2f),
            anchor + new Vector2(-14f, -2f), 5f, Dust * alpha);
        DrawLine(anchor + new Vector2(14f, -2f),
            anchor + new Vector2(spread, -2f), 5f, Dust * alpha);
        for (int index = 0; index < 4; index++)
        {
            float side = index % 2 == 0 ? -1f : 1f;
            DrawBox(anchor, side * (22f + index * 13f) - 2f,
                -8f - progress * (11f + index * 3f), 5f, 5f,
                Dust * (alpha * .75f), false);
        }
    }

    private void DrawGroundBand(Vector2 anchor, float from, float to,
        Color color)
    {
        DrawLine(anchor + new Vector2(from, -3f),
            anchor + new Vector2(to, -3f), 4f, color);
        float direction = MathF.Sign(to - from);
        for (float x = from; direction > 0f ? x < to : x > to;
             x += direction * 36f)
        {
            DrawLine(anchor + new Vector2(x, -3f),
                anchor + new Vector2(x + direction * 10f, -10f),
                2f, color * .75f);
        }
    }

    private void DrawArcTicks(Vector2 center, float inner, float outer,
        Color color, bool flip)
    {
        float sign = flip ? -1f : 1f;
        for (int index = 0; index < 5; index++)
        {
            float angle = MathHelper.Lerp(-1.05f, .92f, index / 4f);
            Vector2 direction = new(
                MathF.Cos(angle) * sign,
                MathF.Sin(angle));
            DrawLine(center + direction * inner,
                center + direction * outer, 3f, color);
        }
    }

    private void DrawDiamond(Vector2 center, float radius, Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius * .48f);
        Vector2 right = center + new Vector2(radius, 0f);
        Vector2 bottom = center + new Vector2(0f, radius * .48f);
        Vector2 left = center + new Vector2(-radius, 0f);
        DrawLine(top, right, 2f, color);
        DrawLine(right, bottom, 2f, color);
        DrawLine(bottom, left, 2f, color);
        DrawLine(left, top, 2f, color);
    }

    private void DrawShadow(Vector2 anchor, float radius)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(
                (int)(anchor.X - radius),
                (int)anchor.Y - 4,
                (int)(radius * 2f),
                7),
            new Color(4, 5, 8, 150));
    }

    private void DrawBox(Vector2 anchor, float x, float y, float width,
        float height, Color color, bool flip)
    {
        float worldX = flip ? anchor.X - x - width : anchor.X + x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(worldX),
            (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLineLocal(Vector2 anchor, Vector2 from, Vector2 to,
        float thickness, Color color, bool flip)
    {
        if (flip)
        {
            from.X = -from.X;
            to.X = -to.X;
        }
        DrawLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .01f)
            return;
        _spriteBatch.Draw(_pixel, from, null, color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
