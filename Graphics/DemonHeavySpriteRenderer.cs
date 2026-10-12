using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only presentation for the Bone Harvester and Hellbrute. Their poses
/// are authored facing right and mirrored around stable ground anchors.
/// </summary>
public sealed class DemonHeavySpriteRenderer
{
    public static readonly Vector2 BoneHarvesterVisualSize = new(138f, 112f);
    public static readonly Vector2 HellbruteVisualSize = new(154f, 122f);

    private static readonly Color Outline = new(26, 20, 21);
    private static readonly Color LeatherSkin = new(63, 43, 40);
    private static readonly Color LeatherSkinLight = new(89, 55, 47);
    private static readonly Color Bone = new(184, 169, 137);
    private static readonly Color BoneShade = new(113, 98, 81);
    private static readonly Color BoneDark = new(67, 59, 55);
    private static readonly Color HarvesterCloth = new(72, 35, 34);
    private static readonly Color RedGlow = new(201, 55, 43);
    private static readonly Color Volcanic = new(57, 48, 49);
    private static readonly Color VolcanicLight = new(82, 62, 57);
    private static readonly Color VolcanicDark = new(37, 32, 35);
    private static readonly Color Magma = new(224, 74, 32);
    private static readonly Color MagmaCore = new(249, 147, 49);
    private static readonly Color Metal = new(91, 85, 79);
    private static readonly Color Dust = new(151, 116, 80);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonLandsEnemy, AnimationClock>
        _clocks = new();

    public DemonHeavySpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonLandsEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy,
            static _ => new AnimationClock());
        HeavyVisualState state = SelectState(enemy);
        clock.Update(gameTime, state);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.FrontierCombatState !=
            DemonFrontierCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        float shadowWidth = enemy.Type == EnemyType.Hellbrute ? 91f : 50f;
        DrawBox(anchor, -shadowWidth * .5f, -3f, shadowWidth, 6f,
            new Color(8, 7, 7, 150), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.BoneHarvester)
                DrawHarvesterDeath(enemy, anchor, flip);
            else
                DrawBruteDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.BoneHarvester)
            DrawHarvester(enemy, anchor, flip, state, clock.Elapsed);
        else
            DrawBrute(enemy, anchor, flip, state, clock.Elapsed);
    }

    private void DrawHarvester(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        HeavyVisualState state,
        float elapsed)
    {
        float breathe = MathF.Sin(elapsed * 2.1f) * .7f;
        float step = state == HeavyVisualState.Walk
            ? MathF.Sin(elapsed * 5.2f) * 5f : 0f;
        float recoil = state == HeavyVisualState.Recovery ? -6f :
            state == HeavyVisualState.Stagger ? -10f : 0f;
        float crouch = state == HeavyVisualState.HookWindup ? 5f : 0f;
        Color skin = enemy.IsHitFlashing
            ? new Color(211, 165, 136)
            : LeatherSkin;

        Vector2 hip = new(recoil, -39f + crouch);
        Vector2 chest = hip + new Vector2(breathe, -30f);
        Vector2 head = chest + new Vector2(1f, -19f);
        Vector2 rearFoot = new(-10f - step, 0f);
        Vector2 frontFoot = new(12f + step, 0f);
        Vector2 rearKnee = new(-8f + step, -21f + crouch);
        Vector2 frontKnee = new(9f - step, -22f + crouch);

        DrawLimb(anchor, rearFoot, rearKnee, BoneDark, flip, 7f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-7f, 0f), skin, flip, 8f);
        DrawLimb(anchor, frontFoot, frontKnee, BoneDark, flip, 7f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(7f, 0f), skin, flip, 8f);
        DrawBox(anchor, rearFoot.X - 7f, rearFoot.Y - 4f, 16f, 5f,
            BoneShade, flip);
        DrawBox(anchor, frontFoot.X - 5f, frontFoot.Y - 4f, 17f, 5f,
            Bone, flip);

        // Lean physical torso beneath layered, non-skeletal trophy armor.
        DrawBox(anchor, chest.X - 13f, chest.Y - 8f, 27f, 38f,
            Outline, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 6f, 23f, 34f,
            skin, flip);
        DrawBoneArmor(anchor, chest, flip);
        DrawBox(anchor, hip.X - 13f, hip.Y - 2f, 28f, 6f,
            BoneDark, flip);
        DrawBox(anchor, hip.X - 13f, hip.Y + 4f, 10f, 18f,
            HarvesterCloth, flip);
        DrawBox(anchor, hip.X + 4f, hip.Y + 4f, 11f, 22f,
            new Color(48, 31, 32), flip);
        DrawTrophies(anchor, hip, flip, elapsed);
        DrawHarvesterHead(anchor, head, skin, flip);
        DrawScytheAndArms(enemy, anchor, chest, flip, state);

        if (state is HeavyVisualState.SweepActive or
            HeavyVisualState.HookActive or HeavyVisualState.ReapActive)
        {
            DrawBoneDust(anchor, flip, enemy.ActionProgress,
                state == HeavyVisualState.ReapActive);
        }
    }

    private void DrawBoneArmor(Vector2 anchor, Vector2 chest, bool flip)
    {
        DrawBox(anchor, chest.X - 16f, chest.Y - 10f, 12f, 10f,
            Outline, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 8f, 9f, 7f,
            Bone, flip);
        DrawBox(anchor, chest.X + 5f, chest.Y - 10f, 14f, 10f,
            Outline, flip);
        DrawBox(anchor, chest.X + 7f, chest.Y - 8f, 10f, 7f,
            BoneShade, flip);
        for (int index = 0; index < 3; index++)
        {
            float y = chest.Y + index * 9f;
            DrawLineLocal(anchor, new Vector2(chest.X - 10f, y),
                new Vector2(chest.X + 10f, y + 2f), 6f,
                Outline, flip);
            DrawLineLocal(anchor, new Vector2(chest.X - 9f, y),
                new Vector2(chest.X + 9f, y + 2f), 3f,
                index % 2 == 0 ? Bone : BoneShade, flip);
        }
        DrawLineLocal(anchor, chest + new Vector2(-8f, -3f),
            chest + new Vector2(8f, 23f), 2f, RedGlow, flip);
    }

    private void DrawTrophies(
        Vector2 anchor,
        Vector2 hip,
        bool flip,
        float elapsed)
    {
        float sway = MathF.Sin(elapsed * 3f) * 2f;
        DrawLineLocal(anchor, hip + new Vector2(-12f, 1f),
            hip + new Vector2(-17f + sway, 22f), 3f, BoneDark, flip);
        DrawBox(anchor, hip.X - 23f + sway, hip.Y + 18f, 12f, 11f,
            Outline, flip);
        DrawBox(anchor, hip.X - 21f + sway, hip.Y + 19f, 9f, 8f,
            Bone, flip);
        DrawBox(anchor, hip.X - 19f + sway, hip.Y + 21f, 2f, 2f,
            BoneDark, flip);
        DrawLineLocal(anchor, hip + new Vector2(12f, 2f),
            hip + new Vector2(18f - sway, 18f), 3f, BoneDark, flip);
        DrawBox(anchor, hip.X + 15f - sway, hip.Y + 15f, 8f, 10f,
            BoneShade, flip);
    }

    private void DrawHarvesterHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip)
    {
        DrawBox(anchor, head.X - 8f, head.Y - 11f, 17f, 23f,
            Outline, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 9f, 13f, 20f,
            skin, flip);
        DrawLineLocal(anchor, head + new Vector2(-4f, -9f),
            head + new Vector2(-10f, -24f), 5f, BoneDark, flip);
        DrawLineLocal(anchor, head + new Vector2(4f, -9f),
            head + new Vector2(10f, -25f), 5f, BoneDark, flip);
        DrawLineLocal(anchor, head + new Vector2(-10f, -24f),
            head + new Vector2(-7f, -30f), 3f, Bone, flip);
        DrawLineLocal(anchor, head + new Vector2(10f, -25f),
            head + new Vector2(7f, -31f), 3f, Bone, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 3f, 3f, 2f,
            new Color(224, 64, 47, 215), flip);
    }

    private void DrawScytheAndArms(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        HeavyVisualState state)
    {
        float angle = state switch
        {
            HeavyVisualState.SweepWindup => MathHelper.Lerp(
                -.75f, -2.35f, enemy.ActionProgress),
            HeavyVisualState.SweepActive => MathHelper.Lerp(
                -2.35f, .28f, enemy.ActionProgress),
            HeavyVisualState.HookWindup => MathHelper.Lerp(
                -1.15f, -.08f, enemy.ActionProgress),
            HeavyVisualState.HookActive => -.02f,
            HeavyVisualState.ReapWindup => MathHelper.Lerp(
                -.65f, -1.58f, enemy.ActionProgress),
            HeavyVisualState.ReapActive => MathHelper.Lerp(
                -1.58f, .95f, enemy.ActionProgress),
            HeavyVisualState.Recovery => .75f,
            _ => -.82f
        };
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 rearShoulder = chest + new Vector2(-9f, 0f);
        Vector2 frontShoulder = chest + new Vector2(10f, 1f);
        Vector2 rearHand = chest + new Vector2(8f, 9f) + direction * 4f;
        Vector2 frontHand = rearHand + direction * 17f;
        Vector2 shaftStart = rearHand - direction * 31f;
        Vector2 shaftEnd = frontHand + direction * 63f;

        DrawLimb(anchor, rearShoulder, rearHand, LeatherSkinLight, flip, 7f);
        DrawLimb(anchor, frontShoulder, frontHand, LeatherSkin, flip, 7f);
        DrawLineLocal(anchor, shaftStart, shaftEnd, 7f, Outline, flip);
        DrawLineLocal(anchor, shaftStart, shaftEnd, 4f, BoneDark, flip);
        Vector2 bladeRoot = shaftEnd - direction * 5f;
        Vector2 bladeMid = shaftEnd + normal * 23f - direction * 6f;
        Vector2 bladeTip = shaftEnd + normal * 38f - direction * 21f;
        DrawLineLocal(anchor, bladeRoot, bladeMid, 10f, Outline, flip);
        DrawLineLocal(anchor, bladeMid, bladeTip, 8f, Outline, flip);
        DrawLineLocal(anchor, bladeRoot + normal, bladeMid, 6f, BoneShade, flip);
        DrawLineLocal(anchor, bladeMid, bladeTip, 4f, Bone, flip);
        DrawBox(anchor, bladeTip.X - 2f, bladeTip.Y - 2f, 4f, 4f,
            RedGlow, flip);
    }

    private void DrawBoneDust(
        Vector2 anchor,
        bool flip,
        float progress,
        bool groundImpact)
    {
        float yBase = groundImpact ? -5f : -20f;
        for (int index = 0; index < 5; index++)
        {
            float x = 24f + index * 13f + progress * 10f;
            float y = yBase - (index % 3) * 6f;
            DrawBox(anchor, x, y, index % 2 == 0 ? 4f : 3f, 3f,
                index % 2 == 0 ? BoneShade : new Color(137, 94, 73, 150),
                flip);
        }
    }

    private void DrawBrute(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        HeavyVisualState state,
        float elapsed)
    {
        float breath = MathF.Sin(elapsed * 2f) * 2f;
        float step = state == HeavyVisualState.Walk
            ? MathF.Sin(elapsed * 3.4f) * 6f : 0f;
        float crouch = state is HeavyVisualState.ChargeWindup or
            HeavyVisualState.ChargeActive ? 10f : 0f;
        float lean = state switch
        {
            HeavyVisualState.ChargeActive => 15f,
            HeavyVisualState.Recovery => -7f,
            HeavyVisualState.Stagger => -9f,
            _ => 0f
        };
        float glow = enemy.IsEnragedBrute ? 1f : .58f;
        Color stone = enemy.IsHitFlashing
            ? new Color(190, 142, 111)
            : Volcanic;

        Vector2 hip = new(lean, -41f + crouch);
        Vector2 chest = hip + new Vector2(breath * .25f, -35f);
        Vector2 head = chest + new Vector2(2f, -24f);
        Vector2 rearFoot = new(-23f - step, 0f);
        Vector2 frontFoot = new(25f + step, 0f);
        Vector2 rearKnee = new(-22f + step, -23f + crouch);
        Vector2 frontKnee = new(23f - step, -24f + crouch);

        DrawLimb(anchor, rearFoot, rearKnee, VolcanicDark, flip, 13f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-17f, 2f), stone, flip, 16f);
        DrawLimb(anchor, frontFoot, frontKnee, VolcanicDark, flip, 13f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(17f, 2f), stone, flip, 16f);
        DrawBox(anchor, rearFoot.X - 13f, -7f, 28f, 8f,
            VolcanicDark, flip);
        DrawBox(anchor, frontFoot.X - 11f, -7f, 30f, 8f,
            Volcanic, flip);

        // Massive unarmored torso reads as living siege mass.
        DrawBox(anchor, chest.X - 34f, chest.Y - 13f, 69f, 54f,
            Outline, flip);
        DrawBox(anchor, chest.X - 31f, chest.Y - 10f, 63f, 49f,
            stone, flip);
        DrawBox(anchor, chest.X - 27f, chest.Y - 7f, 25f, 20f,
            VolcanicLight, flip);
        DrawBox(anchor, chest.X + 5f, chest.Y - 8f, 25f, 22f,
            VolcanicDark, flip);
        DrawMagmaCracks(anchor, chest, flip, glow);
        DrawBruteHead(anchor, head, stone, flip, glow);
        DrawBruteArms(enemy, anchor, chest, flip, state, stone);
        DrawBruteChain(anchor, hip, flip, elapsed);

        if (state is HeavyVisualState.SlamActive or
            HeavyVisualState.SeismicActive)
        {
            DrawBruteImpact(enemy, anchor, flip,
                state == HeavyVisualState.SeismicActive);
        }
        if (state == HeavyVisualState.ChargeActive)
        {
            DrawBox(anchor, -49f, -7f, 7f, 4f,
                new Color(156, 113, 78, 150), flip);
            DrawBox(anchor, -64f, -10f, 4f, 3f,
                new Color(220, 82, 35, 120), flip);
        }
    }

    private void DrawMagmaCracks(
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        float glow)
    {
        Color outer = new(197, 58, 31, (int)(155 + glow * 70f));
        Color core = new(247, 135, 43, (int)(125 + glow * 100f));
        DrawLineLocal(anchor, chest + new Vector2(-15f, -8f),
            chest + new Vector2(-4f, 10f), 4f, outer, flip);
        DrawLineLocal(anchor, chest + new Vector2(-4f, 10f),
            chest + new Vector2(-11f, 30f), 3f, core, flip);
        DrawLineLocal(anchor, chest + new Vector2(10f, -5f),
            chest + new Vector2(2f, 13f), 4f, outer, flip);
        DrawLineLocal(anchor, chest + new Vector2(2f, 13f),
            chest + new Vector2(17f, 31f), 3f, core, flip);
    }

    private void DrawBruteHead(
        Vector2 anchor,
        Vector2 head,
        Color stone,
        bool flip,
        float glow)
    {
        DrawBox(anchor, head.X - 16f, head.Y - 13f, 33f, 27f,
            Outline, flip);
        DrawBox(anchor, head.X - 13f, head.Y - 10f, 27f, 23f,
            stone, flip);
        DrawLineLocal(anchor, head + new Vector2(-11f, -8f),
            head + new Vector2(-31f, -26f), 11f, Outline, flip);
        DrawLineLocal(anchor, head + new Vector2(-10f, -8f),
            head + new Vector2(-29f, -24f), 7f, BoneDark, flip);
        DrawLineLocal(anchor, head + new Vector2(11f, -8f),
            head + new Vector2(31f, -27f), 11f, Outline, flip);
        DrawLineLocal(anchor, head + new Vector2(10f, -8f),
            head + new Vector2(29f, -25f), 7f, BoneShade, flip);
        DrawBox(anchor, head.X + 4f, head.Y - 2f, 4f, 3f,
            new Color(244, 101, 36, (int)(160 + glow * 80f)), flip);
    }

    private void DrawBruteArms(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        HeavyVisualState state,
        Color stone)
    {
        Vector2 rearShoulder = chest + new Vector2(-29f, -5f);
        Vector2 frontShoulder = chest + new Vector2(30f, -4f);
        Vector2 rearElbow;
        Vector2 frontElbow;
        Vector2 rearFist;
        Vector2 frontFist;

        if (state is HeavyVisualState.SlamWindup or HeavyVisualState.SeismicWindup)
        {
            float raise = enemy.ActionProgress;
            rearElbow = rearShoulder + new Vector2(-13f, -12f - raise * 18f);
            frontElbow = frontShoulder + new Vector2(9f, -12f - raise * 18f);
            rearFist = rearElbow + new Vector2(16f, -18f - raise * 9f);
            frontFist = frontElbow + new Vector2(-16f, -18f - raise * 9f);
        }
        else if (state is HeavyVisualState.SlamActive or HeavyVisualState.SeismicActive)
        {
            rearElbow = rearShoulder + new Vector2(-9f, 25f);
            frontElbow = frontShoulder + new Vector2(7f, 25f);
            rearFist = new Vector2(-15f, -6f);
            frontFist = new Vector2(17f, -6f);
        }
        else if (state is HeavyVisualState.BackhandWindup or
            HeavyVisualState.BackhandActive)
        {
            float sweep = state == HeavyVisualState.BackhandActive
                ? enemy.ActionProgress : 0f;
            rearElbow = rearShoulder + new Vector2(-14f, 19f);
            rearFist = rearElbow + new Vector2(2f, 22f);
            frontElbow = frontShoulder + new Vector2(
                MathHelper.Lerp(-24f, 25f, sweep),
                MathHelper.Lerp(-19f, 10f, sweep));
            frontFist = frontElbow + new Vector2(
                MathHelper.Lerp(-23f, 27f, sweep), 8f);
        }
        else
        {
            rearElbow = rearShoulder + new Vector2(-13f, 22f);
            frontElbow = frontShoulder + new Vector2(14f, 22f);
            rearFist = rearElbow + new Vector2(2f, 24f);
            frontFist = frontElbow + new Vector2(-2f, 24f);
        }

        DrawLimb(anchor, rearShoulder, rearElbow, VolcanicDark, flip, 17f);
        DrawLimb(anchor, rearElbow, rearFist, stone, flip, 15f);
        DrawLimb(anchor, frontShoulder, frontElbow, stone, flip, 18f);
        DrawLimb(anchor, frontElbow, frontFist, VolcanicLight, flip, 16f);
        DrawBox(anchor, rearFist.X - 10f, rearFist.Y - 8f, 20f, 18f,
            VolcanicDark, flip);
        DrawBox(anchor, frontFist.X - 10f, frontFist.Y - 8f, 21f, 18f,
            Volcanic, flip);
        DrawLineLocal(anchor, frontShoulder + new Vector2(1f, 2f),
            frontElbow + new Vector2(1f, -1f), 3f, Magma, flip);
    }

    private void DrawBruteChain(
        Vector2 anchor,
        Vector2 hip,
        bool flip,
        float elapsed)
    {
        float sway = MathF.Sin(elapsed * 2.8f) * 2f;
        Vector2 previous = hip + new Vector2(-24f, 2f);
        for (int index = 0; index < 4; index++)
        {
            Vector2 next = previous + new Vector2(-5f + sway * .2f, 8f);
            DrawLineLocal(anchor, previous, next, 5f, Outline, flip);
            DrawLineLocal(anchor, previous, next, 2f, Metal, flip);
            previous = next;
        }
        DrawBox(anchor, previous.X - 6f, previous.Y, 13f, 10f,
            Metal, flip);
    }

    private void DrawBruteImpact(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        bool seismic)
    {
        float progress = enemy.ActionProgress;
        float center = seismic ? 42f + progress * 86f : 0f;
        float radius = seismic ? 28f : 46f;
        DrawLineLocal(anchor, new Vector2(center - radius, -3f),
            new Vector2(center + radius, -3f), 5f,
            new Color(113, 77, 59, 170), flip);
        for (int index = 0; index < 6; index++)
        {
            float x = center - radius + index * radius * .4f;
            float y = -7f - (index % 3) * 7f - progress * 4f;
            DrawBox(anchor, x, y, index % 2 == 0 ? 6f : 4f, 4f,
                index % 3 == 0 ? Magma : Dust, flip);
        }
    }

    private void DrawHarvesterDeath(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        float fall = MathHelper.SmoothStep(0f, 64f, progress);
        DrawLineLocal(anchor, new Vector2(-9f, -70f + fall),
            new Vector2(24f, -8f), 23f, Outline, flip);
        DrawLineLocal(anchor, new Vector2(-8f, -69f + fall),
            new Vector2(22f, -8f), 17f, LeatherSkin, flip);
        DrawLineLocal(anchor, new Vector2(-38f - progress * 9f, -8f),
            new Vector2(44f, -5f), 6f, BoneDark, flip);
        DrawBox(anchor, -32f - progress * 11f, -5f, 9f, 8f,
            Bone, flip);
        DrawBox(anchor, 36f + progress * 7f, -4f, 7f, 6f,
            BoneShade, flip);
    }

    private void DrawBruteDeath(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        float collapse = MathHelper.SmoothStep(0f, 69f, progress);
        DrawBox(anchor, -43f, -82f + collapse, 88f, 74f,
            Outline, flip);
        DrawBox(anchor, -39f, -78f + collapse, 80f, 68f,
            VolcanicDark, flip);
        DrawLineLocal(anchor, new Vector2(-24f, -70f + collapse),
            new Vector2(-8f, -21f), 5f, Magma, flip);
        DrawLineLocal(anchor, new Vector2(18f, -69f + collapse),
            new Vector2(6f, -19f), 4f, MagmaCore, flip);
        DrawBox(anchor, -55f - progress * 10f, -7f, 8f, 5f,
            Dust, flip);
        DrawBox(anchor, 49f + progress * 12f, -8f, 6f, 5f,
            new Color(205, 72, 33, 140), flip);
    }

    private static HeavyVisualState SelectState(DemonLandsEnemy enemy)
    {
        if (!enemy.IsAlive)
            return HeavyVisualState.Death;
        if (enemy.IsStaggered)
            return HeavyVisualState.Stagger;
        return enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.HarvesterSweepWindup => HeavyVisualState.SweepWindup,
            DemonFrontierCombatState.HarvesterSweepActive => HeavyVisualState.SweepActive,
            DemonFrontierCombatState.HarvesterHookWindup => HeavyVisualState.HookWindup,
            DemonFrontierCombatState.HarvesterHookActive => HeavyVisualState.HookActive,
            DemonFrontierCombatState.HarvesterReapWindup => HeavyVisualState.ReapWindup,
            DemonFrontierCombatState.HarvesterReapActive => HeavyVisualState.ReapActive,
            DemonFrontierCombatState.HarvesterSweepRecovery or
            DemonFrontierCombatState.HarvesterHookRecovery or
            DemonFrontierCombatState.HarvesterReapRecovery => HeavyVisualState.Recovery,
            DemonFrontierCombatState.BruteSlamWindup => HeavyVisualState.SlamWindup,
            DemonFrontierCombatState.BruteSlamActive => HeavyVisualState.SlamActive,
            DemonFrontierCombatState.BruteChargeWindup => HeavyVisualState.ChargeWindup,
            DemonFrontierCombatState.BruteChargeActive => HeavyVisualState.ChargeActive,
            DemonFrontierCombatState.BruteBackhandWindup => HeavyVisualState.BackhandWindup,
            DemonFrontierCombatState.BruteBackhandActive => HeavyVisualState.BackhandActive,
            DemonFrontierCombatState.BruteSeismicWindup => HeavyVisualState.SeismicWindup,
            DemonFrontierCombatState.BruteSeismicActive => HeavyVisualState.SeismicActive,
            DemonFrontierCombatState.BruteEnrage => HeavyVisualState.Enrage,
            DemonFrontierCombatState.BruteSlamRecovery or
            DemonFrontierCombatState.BruteChargeRecovery or
            DemonFrontierCombatState.BruteBackhandRecovery or
            DemonFrontierCombatState.BruteSeismicRecovery => HeavyVisualState.Recovery,
            _ => MathF.Abs(enemy.VisualVelocityX) > 4f
                ? HeavyVisualState.Walk
                : HeavyVisualState.Idle
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
        DrawLineLocal(anchor, from, to, thickness + 3f, Outline, flip);
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
        public HeavyVisualState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, HeavyVisualState state)
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

    private enum HeavyVisualState
    {
        Idle,
        Walk,
        SweepWindup,
        SweepActive,
        HookWindup,
        HookActive,
        ReapWindup,
        ReapActive,
        SlamWindup,
        SlamActive,
        ChargeWindup,
        ChargeActive,
        BackhandWindup,
        BackhandActive,
        SeismicWindup,
        SeismicActive,
        Enrage,
        Recovery,
        Stagger,
        Death
    }
}
