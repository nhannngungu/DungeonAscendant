using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only renderer for the Spine Hound and Rift Stalker. Both use stable
/// foot anchors; all action poses are authored facing right and mirrored as
/// a whole so combat facing and hit motion remain aligned.
/// </summary>
public sealed class DemonPredatorSpriteRenderer
{
    public static readonly Vector2 SpineHoundVisualSize = new(122f, 65f);
    public static readonly Vector2 RiftStalkerVisualSize = new(94f, 106f);

    private static readonly Color Outline = new(25, 17, 22);
    private static readonly Color HoundHide = new(81, 42, 39);
    private static readonly Color HoundHideDark = new(47, 35, 37);
    private static readonly Color HoundPlate = new(101, 69, 58);
    private static readonly Color Bone = new(164, 143, 113);
    private static readonly Color BoneDark = new(86, 72, 67);
    private static readonly Color Ember = new(222, 82, 38);
    private static readonly Color EmberCore = new(243, 145, 57);
    private static readonly Color RiftSkin = new(49, 34, 61);
    private static readonly Color RiftSkinLight = new(73, 49, 86);
    private static readonly Color RiftDark = new(25, 19, 34);
    private static readonly Color RiftViolet = new(135, 66, 168);
    private static readonly Color RiftRed = new(151, 47, 83);
    private static readonly Color Claw = new(132, 118, 117);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonLandsEnemy, AnimationClock>
        _clocks = new();

    public DemonPredatorSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonLandsEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy,
            static _ => new AnimationClock());
        PredatorVisualState state = SelectState(enemy);
        clock.Update(gameTime, state);

        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.FrontierCombatState !=
            DemonFrontierCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        if (enemy.Type == EnemyType.RiftStalker)
        {
            DrawRiftPresentation(enemy, anchor, clock.Elapsed);
            if (enemy.IsRiftHidden)
                return;
        }

        float shadowWidth = enemy.Type == EnemyType.SpineHound ? 72f : 36f;
        DrawBox(anchor, -shadowWidth * .5f, -3f, shadowWidth, 5f,
            new Color(7, 6, 8, 145), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.SpineHound)
                DrawHoundDeath(enemy, anchor, flip);
            else
                DrawStalkerDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.SpineHound)
            DrawHound(enemy, anchor, flip, state, clock.Elapsed);
        else
            DrawStalker(enemy, anchor, flip, state, clock.Elapsed);
    }

    private void DrawHound(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        PredatorVisualState state,
        float elapsed)
    {
        float cycle = MathF.Sin(elapsed *
            (state == PredatorVisualState.Run ? 13f : 5f));
        float stride = state switch
        {
            PredatorVisualState.Run => cycle * 12f,
            PredatorVisualState.Stalk => cycle * 5f,
            PredatorVisualState.Rush => 13f,
            _ => 0f
        };
        float crouch = state is PredatorVisualState.Threaten or
            PredatorVisualState.LeapCrouch ? 8f : 0f;
        float lift = state == PredatorVisualState.Leap
            ? -MathF.Sin(enemy.ActionProgress * MathF.PI) * 7f
            : 0f;
        float lean = state switch
        {
            PredatorVisualState.Leap => 14f,
            PredatorVisualState.Rush => 17f,
            PredatorVisualState.Recovery => -7f,
            PredatorVisualState.Stagger => -10f,
            _ => 0f
        };
        float spineFlare = state == PredatorVisualState.SpineFlare
            ? MathHelper.Lerp(0f, 9f, enemy.ActionProgress)
            : state == PredatorVisualState.SpineBurst ? 10f :
                MathF.Max(0f, MathF.Sin(elapsed * 7f)) * 1.5f;
        Color hide = enemy.IsHitFlashing
            ? new Color(207, 146, 120)
            : HoundHide;

        Vector2 body = new(lean - 5f, -27f + crouch + lift);
        Vector2 shoulder = body + new Vector2(22f, -2f);
        Vector2 haunch = body + new Vector2(-23f, 2f);
        Vector2 neck = shoulder + new Vector2(14f, -5f);
        Vector2 skull = neck + new Vector2(16f, 0f);

        // Heavy tail is segmented and drops below the dorsal line, keeping
        // the silhouette long without reading as a natural wolf tail.
        Vector2 tailBase = haunch + new Vector2(-14f, 0f);
        Vector2 tailMid = tailBase + new Vector2(-22f, 8f - stride * .12f);
        Vector2 tailTip = tailMid + new Vector2(-24f, -2f + cycle * 3f);
        DrawLimb(anchor, tailBase, tailMid, HoundHideDark, flip, 11f);
        DrawLimb(anchor, tailMid, tailTip, HoundHideDark, flip, 8f);
        for (int index = 0; index < 3; index++)
        {
            Vector2 spike = Vector2.Lerp(tailBase, tailTip, .28f + index * .23f);
            DrawLineLocal(anchor, spike,
                spike + new Vector2(-2f, -7f + index), 4f,
                BoneDark, flip);
        }

        DrawHoundLeg(anchor, haunch + new Vector2(-7f, 5f),
            new Vector2(-25f - stride, -12f + crouch + lift),
            new Vector2(-29f - stride, 0f + lift), hide, flip);
        DrawHoundLeg(anchor, haunch + new Vector2(6f, 6f),
            new Vector2(-6f + stride, -11f + crouch + lift),
            new Vector2(-2f + stride, 0f + lift), HoundHideDark, flip);
        DrawHoundLeg(anchor, shoulder + new Vector2(-4f, 7f),
            new Vector2(14f - stride, -11f + crouch + lift),
            new Vector2(12f - stride, 0f + lift), HoundHideDark, flip);
        DrawHoundLeg(anchor, shoulder + new Vector2(8f, 6f),
            new Vector2(31f + stride, -12f + crouch + lift),
            new Vector2(36f + stride, 0f + lift), hide, flip);

        DrawBox(anchor, body.X - 31f, body.Y - 12f, 61f, 27f,
            Outline, flip);
        DrawBox(anchor, body.X - 29f, body.Y - 10f, 57f, 23f,
            hide, flip);
        DrawBox(anchor, body.X - 24f, body.Y - 8f, 18f, 18f,
            HoundPlate, flip);
        DrawBox(anchor, body.X - 2f, body.Y - 9f, 19f, 19f,
            HoundHideDark, flip);
        DrawBox(anchor, body.X + 18f, body.Y - 7f, 12f, 16f,
            HoundPlate, flip);

        // Restrained ember cracks sit between armor-like hide plates.
        DrawLineLocal(anchor, body + new Vector2(-5f, -5f),
            body + new Vector2(0f, 7f), 2f, Ember, flip);
        DrawLineLocal(anchor, body + new Vector2(16f, -5f),
            body + new Vector2(19f, 6f), 1.5f, EmberCore, flip);

        DrawHoundSpines(anchor, body, spineFlare, flip);
        DrawLimb(anchor, shoulder, neck, HoundHideDark, flip, 14f);
        DrawElongatedSkull(anchor, skull, hide, flip, state);

        if (state is PredatorVisualState.Run or PredatorVisualState.Rush or
            PredatorVisualState.Leap)
        {
            DrawBox(anchor, -39f - MathF.Abs(stride), -5f, 5f, 3f,
                new Color(142, 105, 75, 125), flip);
            DrawBox(anchor, -50f - MathF.Abs(stride), -8f, 3f, 2f,
                new Color(196, 73, 36, 95), flip);
        }

        if (state == PredatorVisualState.SpineBurst)
            DrawSpineBurstParticles(anchor, flip, enemy.ActionProgress);
    }

    private void DrawHoundLeg(
        Vector2 anchor,
        Vector2 hip,
        Vector2 knee,
        Vector2 foot,
        Color hide,
        bool flip)
    {
        DrawLimb(anchor, hip, knee, hide, flip, 7f);
        DrawLimb(anchor, knee, foot, HoundHideDark, flip, 6f);
        DrawLineLocal(anchor, foot + new Vector2(-4f, 0f),
            foot + new Vector2(9f, 0f), 5f, Outline, flip);
        DrawLineLocal(anchor, foot + new Vector2(6f, -1f),
            foot + new Vector2(13f, 1f), 2f, Claw, flip);
    }

    private void DrawHoundSpines(
        Vector2 anchor,
        Vector2 body,
        float flare,
        bool flip)
    {
        for (int index = 0; index < 7; index++)
        {
            float x = body.X - 25f + index * 8f;
            float baseY = body.Y - 9f;
            float height = 10f + (index % 3) * 4f + flare;
            DrawLineLocal(anchor, new Vector2(x, baseY + 2f),
                new Vector2(x - 3f, baseY - height), 7f,
                Outline, flip);
            DrawLineLocal(anchor, new Vector2(x, baseY),
                new Vector2(x - 3f, baseY - height + 2f), 4f,
                index % 2 == 0 ? Bone : BoneDark, flip);
        }
    }

    private void DrawElongatedSkull(
        Vector2 anchor,
        Vector2 skull,
        Color hide,
        bool flip,
        PredatorVisualState state)
    {
        float jawDrop = state is PredatorVisualState.Threaten or
            PredatorVisualState.Rush ? 5f : 1f;
        DrawBox(anchor, skull.X - 11f, skull.Y - 10f, 24f, 19f,
            Outline, flip);
        DrawBox(anchor, skull.X - 9f, skull.Y - 8f, 22f, 15f,
            BoneDark, flip);
        DrawBox(anchor, skull.X + 8f, skull.Y - 5f, 23f, 11f,
            Outline, flip);
        DrawBox(anchor, skull.X + 9f, skull.Y - 4f, 20f, 8f,
            Bone, flip);
        DrawBox(anchor, skull.X + 13f, skull.Y + 3f + jawDrop, 17f, 5f,
            HoundHideDark, flip);
        DrawBox(anchor, skull.X + 1f, skull.Y - 4f, 4f, 3f,
            new Color(229, 91, 44, 205), flip);
        DrawLineLocal(anchor, skull + new Vector2(15f, 4f + jawDrop),
            skull + new Vector2(17f, 9f + jawDrop), 2f, Claw, flip);
        DrawLineLocal(anchor, skull + new Vector2(24f, 3f + jawDrop),
            skull + new Vector2(25f, 8f + jawDrop), 2f, Claw, flip);
    }

    private void DrawSpineBurstParticles(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float spread = 19f + progress * 14f;
        for (int index = 0; index < 5; index++)
        {
            float x = -28f + index * 14f;
            float y = -49f - (index % 2) * 7f - progress * 8f;
            DrawBox(anchor, x + MathF.Sign(x == 0f ? 1f : x) * spread * .15f,
                y, 3f, 3f, index % 2 == 0 ? Bone : Ember, flip);
        }
    }

    private void DrawStalker(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        PredatorVisualState state,
        float elapsed)
    {
        float stillness = state == PredatorVisualState.Idle
            ? MathF.Sin(elapsed * .9f) * .25f
            : MathF.Sin(elapsed * 4f);
        float stride = state == PredatorVisualState.Stalk
            ? MathF.Sin(elapsed * 5.5f) * 4f
            : state == PredatorVisualState.Backstep
                ? -enemy.ActionProgress * 8f
                : 0f;
        float crouch = state is PredatorVisualState.RiftEntry or
            PredatorVisualState.AmbushTell ? 9f : 0f;
        float lean = state switch
        {
            PredatorVisualState.Slash => 9f,
            PredatorVisualState.Backstep => -9f,
            PredatorVisualState.Stagger => -11f,
            _ => 0f
        };
        float exitRise = state == PredatorVisualState.RiftExit
            ? MathHelper.Lerp(9f, 0f, enemy.ActionProgress)
            : 0f;
        Color skin = enemy.IsHitFlashing
            ? new Color(188, 133, 170)
            : RiftSkin;

        Vector2 hip = new(lean, -37f + crouch + exitRise);
        Vector2 chest = hip + new Vector2(stillness, -28f);
        Vector2 head = chest + new Vector2(1f, -18f);
        Vector2 rearFoot = new(-8f - stride, 0f);
        Vector2 frontFoot = new(10f + stride, 0f);
        Vector2 rearKnee = new(-7f + stride, -20f + crouch + exitRise);
        Vector2 frontKnee = new(8f - stride, -21f + crouch + exitRise);

        DrawLimb(anchor, rearFoot, rearKnee, RiftDark, flip, 6f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f), skin, flip, 7f);
        DrawLimb(anchor, frontFoot, frontKnee, RiftDark, flip, 6f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 0f), skin, flip, 7f);
        DrawStalkerClaws(anchor, rearFoot, flip, feet: true);
        DrawStalkerClaws(anchor, frontFoot, flip, feet: true);

        // Narrow, solid torso and grounded legs keep it physical rather than
        // spectral. Minimal cloth never obscures the long-limbed silhouette.
        DrawBox(anchor, chest.X - 9f, chest.Y - 7f, 19f, 34f,
            Outline, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y - 5f, 15f, 30f,
            skin, flip);
        DrawBox(anchor, chest.X - 10f, chest.Y + 20f, 21f, 5f,
            RiftDark, flip);
        DrawBox(anchor, hip.X - 10f, hip.Y + 1f, 8f, 17f,
            new Color(61, 37, 61), flip);
        DrawBox(anchor, hip.X + 3f, hip.Y + 1f, 9f, 22f,
            new Color(43, 29, 48), flip);

        DrawLineLocal(anchor, chest + new Vector2(-5f, -2f),
            chest + new Vector2(3f, 9f), 2f, RiftViolet, flip);
        DrawLineLocal(anchor, chest + new Vector2(2f, 10f),
            chest + new Vector2(-2f, 21f), 2f, RiftRed, flip);
        DrawWarpedHead(anchor, head, skin, flip);
        DrawStalkerArms(enemy, anchor, chest, flip, state);

        if (state is PredatorVisualState.RiftEntry or
            PredatorVisualState.RiftExit or PredatorVisualState.AmbushTell)
        {
            DrawDistortionMarks(anchor, chest, flip, elapsed,
                state == PredatorVisualState.RiftExit ? 180 : 125);
        }
    }

    private void DrawWarpedHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip)
    {
        DrawBox(anchor, head.X - 8f, head.Y - 10f, 17f, 22f,
            Outline, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 8f, 13f, 19f,
            skin, flip);
        DrawLineLocal(anchor, head + new Vector2(-4f, -8f),
            head + new Vector2(-9f, -19f), 4f, RiftDark, flip);
        DrawLineLocal(anchor, head + new Vector2(4f, -8f),
            head + new Vector2(9f, -20f), 4f, RiftDark, flip);
        DrawLineLocal(anchor, head + new Vector2(-9f, -19f),
            head + new Vector2(-6f, -24f), 2f, BoneDark, flip);
        DrawLineLocal(anchor, head + new Vector2(9f, -20f),
            head + new Vector2(6f, -25f), 2f, BoneDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 2f, 3f, 2f,
            new Color(191, 81, 188, 205), flip);
    }

    private void DrawStalkerArms(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        PredatorVisualState state)
    {
        Vector2 rearShoulder = chest + new Vector2(-7f, -1f);
        Vector2 frontShoulder = chest + new Vector2(8f, -1f);
        Vector2 rearElbow = rearShoulder + new Vector2(-10f, 22f);
        Vector2 rearHand = rearElbow + new Vector2(5f, 20f);
        float slashProgress = state == PredatorVisualState.Slash
            ? enemy.ActionProgress : 0f;
        bool secondClaw = enemy.FrontierCombatState ==
            DemonFrontierCombatState.StalkerTwinSecond;
        float sweep = state == PredatorVisualState.Slash
            ? secondClaw
                ? MathHelper.Lerp(.65f, -.75f, slashProgress)
                : MathHelper.Lerp(-1.55f, .40f, slashProgress)
            : state == PredatorVisualState.AmbushTell
                ? -1.28f
                : -.15f;
        Vector2 direction = new(MathF.Cos(sweep), MathF.Sin(sweep));
        Vector2 frontElbow = frontShoulder + direction * 22f;
        Vector2 frontHand = frontElbow + direction * 22f;

        DrawLimb(anchor, rearShoulder, rearElbow, RiftSkinLight, flip, 6f);
        DrawLimb(anchor, rearElbow, rearHand, RiftSkin, flip, 5f);
        DrawLimb(anchor, frontShoulder, frontElbow, RiftSkin, flip, 6f);
        DrawLimb(anchor, frontElbow, frontHand, RiftSkinLight, flip, 5f);
        DrawStalkerClaws(anchor, rearHand, flip, feet: false);
        DrawStalkerClaws(anchor, frontHand, flip, feet: false);
    }

    private void DrawStalkerClaws(
        Vector2 anchor,
        Vector2 point,
        bool flip,
        bool feet)
    {
        float length = feet ? 8f : 10f;
        DrawLineLocal(anchor, point, point + new Vector2(length, 1f),
            2f, Claw, flip);
        DrawLineLocal(anchor, point + new Vector2(0f, 2f),
            point + new Vector2(length - 1f, 5f), 2f, Claw, flip);
        if (!feet)
        {
            DrawLineLocal(anchor, point + new Vector2(-1f, 4f),
                point + new Vector2(length - 3f, 8f), 2f, Claw, flip);
        }
    }

    private void DrawRiftPresentation(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        float elapsed)
    {
        DemonFrontierCombatState state = enemy.FrontierCombatState;
        if (state is DemonFrontierCombatState.StalkerRiftEntry or
            DemonFrontierCombatState.StalkerRiftHidden or
            DemonFrontierCombatState.StalkerVoidEntry or
            DemonFrontierCombatState.StalkerVoidHidden)
        {
            Vector2 entryAnchor = new(
                enemy.RiftEntryPosition.X,
                enemy.RiftEntryPosition.Y + enemy.Size.Y * .5f);
            float close = state is DemonFrontierCombatState.StalkerRiftHidden or
                DemonFrontierCombatState.StalkerVoidHidden
                ? 1f - enemy.ActionProgress
                : .55f + enemy.ActionProgress * .45f;
            DrawRift(entryAnchor + new Vector2(0f, -35f),
                16f + close * 13f, elapsed, 175);
        }
        if (state is DemonFrontierCombatState.StalkerRiftExit or
            DemonFrontierCombatState.StalkerAmbushWindup or
            DemonFrontierCombatState.StalkerVoidExit)
        {
            DrawRift(anchor + new Vector2(0f, -35f),
                27f - enemy.ActionProgress * 5f, elapsed, 195);
        }
    }

    private void DrawRift(Vector2 center, float radius, float elapsed, int alpha)
    {
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int index = 1; index <= 10; index++)
        {
            float angle = MathHelper.TwoPi * index / 10f + elapsed * .7f;
            float warpedRadius = radius + MathF.Sin(angle * 3f + elapsed * 5f) * 3f;
            Vector2 point = center + new Vector2(
                MathF.Cos(angle) * warpedRadius * .58f,
                MathF.Sin(angle) * warpedRadius);
            DrawWorldLine(previous, point, 4f,
                new Color(56, 27, 69, alpha / 2));
            DrawWorldLine(previous, point, 2f,
                index % 2 == 0
                    ? new Color(144, 68, 174, alpha)
                    : new Color(154, 48, 86, alpha));
            previous = point;
        }
        DrawBox(center, -2f, -radius - 8f, 4f, 4f,
            new Color(157, 76, 180, alpha), flip: false);
        DrawBox(center, radius * .45f, 7f, 3f, 3f,
            new Color(176, 53, 91, alpha), flip: false);
    }

    private void DrawDistortionMarks(
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        float elapsed,
        int alpha)
    {
        float drift = MathF.Sin(elapsed * 8f) * 3f;
        DrawLineLocal(anchor, chest + new Vector2(-25f - drift, -8f),
            chest + new Vector2(-17f - drift, 18f), 2f,
            new Color(132, 59, 158, alpha), flip);
        DrawLineLocal(anchor, chest + new Vector2(20f + drift, -14f),
            chest + new Vector2(27f + drift, 14f), 2f,
            new Color(145, 45, 80, alpha), flip);
        DrawBox(anchor, chest.X - 29f, chest.Y + 3f + drift, 3f, 3f,
            new Color(164, 80, 181, alpha), flip);
    }

    private void DrawHoundDeath(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        float collapse = MathHelper.SmoothStep(0f, 19f, progress);
        DrawLineLocal(anchor, new Vector2(-43f, -20f + collapse),
            new Vector2(38f, -13f + collapse * .55f), 25f,
            Outline, flip);
        DrawLineLocal(anchor, new Vector2(-41f, -20f + collapse),
            new Vector2(36f, -13f + collapse * .55f), 19f,
            HoundHideDark, flip);
        for (int index = 0; index < 5; index++)
        {
            float x = -22f + index * 10f;
            DrawLineLocal(anchor, new Vector2(x, -20f + collapse),
                new Vector2(x - 2f, -30f + collapse), 3f,
                BoneDark, flip);
        }
        DrawBox(anchor, -50f - progress * 7f, -5f, 4f, 3f,
            new Color(142, 103, 72, 115), flip);
    }

    private void DrawStalkerDeath(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        float collapse = MathHelper.SmoothStep(0f, 56f, progress);
        float inward = MathHelper.SmoothStep(1f, .35f, progress);
        Vector2 center = new(0f, -43f + collapse * .45f);
        DrawLineLocal(anchor, center + new Vector2(-7f * inward, -28f * inward),
            center + new Vector2(5f * inward, 29f * inward), 15f,
            Outline, flip);
        DrawLineLocal(anchor, center + new Vector2(-6f * inward, -27f * inward),
            center + new Vector2(4f * inward, 27f * inward), 10f,
            RiftSkin, flip);
        float radius = 22f * inward;
        for (int index = 0; index < 6; index++)
        {
            float angle = MathHelper.TwoPi * index / 6f;
            Vector2 particle = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius);
            DrawBox(anchor, particle.X, particle.Y, 3f, 3f,
                index % 2 == 0 ? RiftViolet : RiftRed, flip);
        }
    }

    private static PredatorVisualState SelectState(DemonLandsEnemy enemy)
    {
        if (!enemy.IsAlive)
            return PredatorVisualState.Death;
        if (enemy.IsStaggered)
            return PredatorVisualState.Stagger;
        return enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.HoundThreaten => PredatorVisualState.Threaten,
            DemonFrontierCombatState.HoundLeapCrouch => PredatorVisualState.LeapCrouch,
            DemonFrontierCombatState.HoundLeapActive => PredatorVisualState.Leap,
            DemonFrontierCombatState.HoundRushWindup => PredatorVisualState.Threaten,
            DemonFrontierCombatState.HoundRushActive => PredatorVisualState.Rush,
            DemonFrontierCombatState.HoundSpineFlare => PredatorVisualState.SpineFlare,
            DemonFrontierCombatState.HoundSpineBurstActive => PredatorVisualState.SpineBurst,
            DemonFrontierCombatState.HoundLeapRecovery or
            DemonFrontierCombatState.HoundRushRecovery or
            DemonFrontierCombatState.HoundSpineBurstRecovery =>
                PredatorVisualState.Recovery,
            DemonFrontierCombatState.HoundReposition => PredatorVisualState.Stalk,
            DemonFrontierCombatState.StalkerRiftEntry or
            DemonFrontierCombatState.StalkerVoidEntry => PredatorVisualState.RiftEntry,
            DemonFrontierCombatState.StalkerRiftExit or
            DemonFrontierCombatState.StalkerVoidExit => PredatorVisualState.RiftExit,
            DemonFrontierCombatState.StalkerAmbushWindup or
            DemonFrontierCombatState.StalkerSlashWindup or
            DemonFrontierCombatState.StalkerTwinWindup or
            DemonFrontierCombatState.StalkerTwinGap => PredatorVisualState.AmbushTell,
            DemonFrontierCombatState.StalkerSlashActive or
            DemonFrontierCombatState.StalkerAmbushActive or
            DemonFrontierCombatState.StalkerTwinFirst or
            DemonFrontierCombatState.StalkerTwinSecond => PredatorVisualState.Slash,
            DemonFrontierCombatState.StalkerSlashRecovery or
            DemonFrontierCombatState.StalkerAmbushRecovery or
            DemonFrontierCombatState.StalkerTwinRecovery or
            DemonFrontierCombatState.StalkerVoidRecovery =>
                PredatorVisualState.Recovery,
            DemonFrontierCombatState.StalkerBackstep => PredatorVisualState.Backstep,
            _ => MathF.Abs(enemy.VisualVelocityX) > 8f
                ? enemy.Type == EnemyType.SpineHound
                    ? PredatorVisualState.Run
                    : PredatorVisualState.Stalk
                : PredatorVisualState.Idle
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
        float localX = flip ? -x - width : x;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(anchor.X + localX),
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
        DrawWorldLine(anchor + from, anchor + to, thickness, color);
    }

    private void DrawWorldLine(
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

    private sealed class AnimationClock
    {
        public PredatorVisualState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, PredatorVisualState state)
        {
            float delta = MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds,
                .05f);
            if (State != state)
            {
                State = state;
                Elapsed = 0f;
            }
            else
            {
                Elapsed += delta;
            }
        }
    }

    private enum PredatorVisualState
    {
        Idle,
        Stalk,
        Run,
        Threaten,
        LeapCrouch,
        Leap,
        Rush,
        SpineFlare,
        SpineBurst,
        RiftEntry,
        RiftExit,
        AmbushTell,
        Slash,
        Backstep,
        Recovery,
        Stagger,
        Death
    }
}
