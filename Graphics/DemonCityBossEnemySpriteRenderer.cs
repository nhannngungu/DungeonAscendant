using System;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>Articulated code-built silhouettes for Map 6 enemies 59 and 60.</summary>
public sealed class DemonCityBossEnemySpriteRenderer
{
    public static readonly Vector2 ButcherVisualSize = new(224f, 174f);
    public static readonly Vector2 MagistrateVisualSize = new(176f, 176f);

    private static readonly Color Outline = new(12, 10, 13);
    private static readonly Color Iron = new(45, 43, 47);
    private static readonly Color IronEdge = new(112, 105, 101);
    private static readonly Color Leather = new(51, 34, 31);
    private static readonly Color Apron = new(76, 54, 49);
    private static readonly Color DriedBlood = new(91, 27, 31);
    private static readonly Color Bronze = new(133, 98, 61);
    private static readonly Color Robe = new(36, 28, 48);
    private static readonly Color RobeEdge = new(91, 71, 109);
    private static readonly Color Decree = new(126, 89, 164);
    private static readonly Color Verdict = new(217, 178, 103);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public DemonCityBossEnemySpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonCityBossEnemy enemy, GameTime gameTime)
    {
        bool flip = enemy.CombatState == DemonCityBossState.Ready
            ? enemy.Facing == EnemyFacingDirection.Left
            : enemy.AttackFacesLeft;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        DrawBox(anchor, -enemy.Size.X * .38f, -4f,
            enemy.Size.X * .76f, 7f, new Color(5, 4, 7, 165), flip);
        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        if (enemy.Type == EnemyType.CathedralButcher)
            DrawButcher(enemy, anchor, flip, time);
        else
            DrawMagistrate(enemy, anchor, flip, time);
    }

    private void DrawButcher(
        DemonCityBossEnemy enemy,
        Vector2 anchor,
        bool flip,
        float time)
    {
        float p = enemy.ActionProgress;
        float lean = 0f;
        float crouch = 0f;
        float bladeAngle = .72f;
        float bladeReach = 0f;
        bool dead = !enemy.IsAlive;

        if (dead)
        {
            float d = enemy.DeathPresentationProgress;
            lean = MathHelper.Lerp(0f, 34f, Math.Clamp((d - .28f) / .72f, 0f, 1f));
            crouch = MathHelper.Lerp(0f, 48f, Math.Clamp((d - .14f) / .58f, 0f, 1f));
            bladeAngle = MathHelper.Lerp(.72f, 1.50f, Math.Clamp(d / .34f, 0f, 1f));
            bladeReach = 20f * Math.Clamp(d / .3f, 0f, 1f);
        }
        else
        {
            switch (enemy.CombatState)
            {
                case DemonCityBossState.Intro:
                    bladeAngle = MathHelper.Lerp(1.42f, .72f, p);
                    break;
                case DemonCityBossState.CleaveWindup:
                    bladeAngle = MathHelper.Lerp(.72f, -1.92f, p);
                    lean = -10f * p;
                    crouch = 8f * p;
                    break;
                case DemonCityBossState.CleaveActive:
                case DemonCityBossState.FollowupActive:
                    bladeAngle = MathHelper.Lerp(-1.92f, .40f, p);
                    lean = 18f;
                    bladeReach = 9f;
                    break;
                case DemonCityBossState.FollowupTell:
                    bladeAngle = -1.28f;
                    lean = -7f;
                    break;
                case DemonCityBossState.ChopRaise:
                case DemonCityBossState.ChopHold:
                    bladeAngle = MathHelper.Lerp(.72f, -1.54f, p);
                    crouch = 7f;
                    break;
                case DemonCityBossState.ChopImpact:
                    bladeAngle = MathHelper.Lerp(-1.54f, 1.22f, p);
                    lean = 12f;
                    crouch = 12f;
                    break;
                case DemonCityBossState.DragLower:
                case DemonCityBossState.DragAdvance:
                    bladeAngle = 1.30f;
                    lean = 9f;
                    crouch = 16f;
                    bladeReach = 12f;
                    break;
                case DemonCityBossState.DragRise:
                    bladeAngle = MathHelper.Lerp(1.30f, -.65f, p);
                    lean = 14f;
                    break;
                case DemonCityBossState.StompWindup:
                    bladeAngle = .96f;
                    lean = -5f;
                    crouch = -9f * p;
                    break;
                case DemonCityBossState.StompImpact:
                    crouch = 16f;
                    bladeAngle = .86f;
                    break;
                case DemonCityBossState.PhaseTransition:
                    bladeAngle = -1.18f;
                    crouch = 5f + MathF.Sin(time * 18f) * 2f;
                    break;
            }
        }

        float breath = dead ? 0f : MathF.Sin(time * 2.1f) * .8f;
        if (!dead && enemy.CombatState == DemonCityBossState.Ready &&
            MathF.Abs(enemy.VisualVelocityX) > 2f)
        {
            bladeAngle = .88f + MathF.Sin(time * 5.2f - .7f) * .08f;
            bladeReach = 5f;
        }
        float step = !dead && MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(time * 6f) * 5f : 0f;
        Vector2 hip = new(lean * .42f, -51f + crouch);
        Vector2 chest = new(lean, -98f + crouch + breath);
        DrawLimb(anchor, hip + new Vector2(-13f, 0f),
            new Vector2(-17f + step, -26f + crouch),
            new Vector2(-23f - step, -2f), 16f, Leather, flip);
        DrawLimb(anchor, hip + new Vector2(13f, 0f),
            new Vector2(16f - step, -27f + crouch),
            new Vector2(24f + step, -2f), 17f, Iron, flip);
        DrawBox(anchor, hip.X - 29f, hip.Y - 10f, 58f, 20f, Outline, flip);
        DrawBox(anchor, hip.X - 26f, hip.Y - 8f, 52f, 16f, Leather, flip);

        DrawBox(anchor, chest.X - 39f, chest.Y - 17f, 78f, 56f, Outline, flip);
        DrawBox(anchor, chest.X - 36f, chest.Y - 14f, 72f, 50f,
            enemy.IsHitFlashing ? IronEdge : Leather, flip);
        DrawBox(anchor, chest.X - 23f, chest.Y + 2f, 48f, 63f, Apron, flip);
        DrawLine(anchor, chest + new Vector2(-20f, 10f),
            chest + new Vector2(18f, 54f), 4f, DriedBlood, flip);
        DrawBox(anchor, chest.X + 25f, chest.Y - 24f, 32f, 30f, Outline, flip);
        DrawBox(anchor, chest.X + 28f, chest.Y - 21f, 27f, 24f, Iron, flip);
        DrawLine(anchor, chest + new Vector2(30f, -16f),
            chest + new Vector2(51f, -8f), 3f, Bronze, flip);

        Vector2 head = chest + new Vector2(-3f, -40f);
        DrawBox(anchor, head.X - 17f, head.Y - 16f, 34f, 34f, Outline, flip);
        DrawBox(anchor, head.X - 14f, head.Y - 14f, 28f, 29f, Iron, flip);
        DrawLine(anchor, head + new Vector2(-11f, -7f),
            head + new Vector2(10f, -5f), 3f, IronEdge, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 5f, 5f, 2f, DriedBlood, flip);
        DrawBox(anchor, head.X + 5f, head.Y - 4f, 5f, 2f, DriedBlood, flip);
        DrawLine(anchor, head + new Vector2(-8f, 2f),
            head + new Vector2(9f, 10f), 2f, Bronze, flip);

        Vector2 shoulder = chest + new Vector2(-29f, -4f);
        if (dead && enemy.DeathPresentationProgress > .12f)
        {
            Vector2 rearHand = shoulder + new Vector2(-5f, 34f);
            Vector2 frontHand = chest + new Vector2(17f, 36f);
            DrawLine(anchor, shoulder, rearHand, 17f, Leather, flip);
            DrawLine(anchor, chest + new Vector2(24f, 0f), frontHand,
                15f, Leather, flip);
            float drop = Math.Clamp(
                (enemy.DeathPresentationProgress - .12f) / .22f, 0f, 1f);
            Vector2 droppedGrip = new(10f, MathHelper.Lerp(-42f, -17f, drop));
            DrawCleaver(anchor, droppedGrip, .04f, flip, dropped: true);
        }
        else
        {
            Vector2 handRear = shoulder + Angle(bladeAngle + .18f) * 30f;
            Vector2 handFront = handRear +
                Angle(bladeAngle) * (22f + bladeReach);
            DrawLine(anchor, shoulder, handRear, 17f, Leather, flip);
            DrawLine(anchor, chest + new Vector2(24f, 0f), handFront,
                15f, Leather, flip);
            DrawLine(anchor, handRear, handFront, 10f, IronEdge, flip);
            DrawCleaver(anchor, handFront, bladeAngle, flip, dropped: false);
        }

        if (!dead && enemy.CombatState is DemonCityBossState.ChopImpact or
            DemonCityBossState.StompImpact)
        {
            float radius = enemy.CombatState == DemonCityBossState.StompImpact
                ? 108f : 72f;
            Color impact = enemy.CombatState == DemonCityBossState.StompImpact
                ? new Color(135, 88, 62, 185)
                : new Color(116, 48, 43, 180);
            DrawLine(anchor, new Vector2(-radius, -3f),
                new Vector2(radius, -3f), 4f, impact, flip);
            DrawLine(anchor, new Vector2(-radius * .55f, -9f),
                new Vector2(-radius * .25f, -2f), 3f, Bronze, flip);
            DrawLine(anchor, new Vector2(radius * .25f, -2f),
                new Vector2(radius * .55f, -9f), 3f, Bronze, flip);
        }
    }

    private void DrawCleaver(
        Vector2 anchor, Vector2 hand, float angle, bool flip, bool dropped)
    {
        Vector2 shaftEnd = hand + Angle(angle) * 45f;
        DrawLine(anchor, hand, shaftEnd, 9f, Leather, flip);
        Vector2 bladeCenter = shaftEnd + Angle(angle) * 36f;
        float drawAngle = flip ? MathHelper.Pi - angle : angle;
        Rectangle blade = new(
            (int)(anchor.X + (flip ? -bladeCenter.X : bladeCenter.X)),
            (int)(anchor.Y + bladeCenter.Y), 74, 34);
        _spriteBatch.Draw(_pixel, blade, null,
            dropped ? new Color(54, 42, 43) : Iron,
            drawAngle, new Vector2(.5f, .5f), SpriteEffects.None, 0f);
        DrawLine(anchor, shaftEnd + Angle(angle + 1.57f) * 13f,
            shaftEnd + Angle(angle - 1.57f) * 13f, 4f, Bronze, flip);
        DrawLine(anchor, bladeCenter - Angle(angle) * 25f,
            bladeCenter + Angle(angle) * 27f, 3f, DriedBlood, flip);
    }

    private void DrawMagistrate(
        DemonCityBossEnemy enemy,
        Vector2 anchor,
        bool flip,
        float time)
    {
        float p = enemy.ActionProgress;
        float lean = 0f;
        float crouch = 0f;
        float scepterAngle = .28f;
        float mantleLift = 0f;
        bool dead = !enemy.IsAlive;
        if (dead)
        {
            lean = 28f * enemy.DeathPresentationProgress;
            crouch = 38f * Math.Clamp(enemy.DeathPresentationProgress / .76f, 0f, 1f);
            scepterAngle = 1.34f;
        }
        else switch (enemy.CombatState)
        {
            case DemonCityBossState.Intro:
                scepterAngle = MathHelper.Lerp(1.20f, .28f, p);
                mantleLift = 7f * MathF.Sin(p * MathHelper.Pi);
                break;
            case DemonCityBossState.BoltCharge:
            case DemonCityBossState.DecreeGesture:
            case DemonCityBossState.WritGesture:
                scepterAngle = MathHelper.Lerp(.28f, -1.12f, p);
                mantleLift = 9f;
                break;
            case DemonCityBossState.ScepterWindup:
                scepterAngle = -1.62f;
                lean = -7f;
                break;
            case DemonCityBossState.ScepterActive:
                scepterAngle = MathHelper.Lerp(-1.62f, .22f, p);
                lean = 15f;
                break;
            case DemonCityBossState.StepPlant:
            case DemonCityBossState.StepDistort:
                scepterAngle = 1.48f;
                crouch = 7f;
                mantleLift = 13f;
                break;
            case DemonCityBossState.VerdictPlant:
            case DemonCityBossState.VerdictSequence:
                scepterAngle = 1.55f;
                crouch = 5f;
                mantleLift = 15f;
                break;
            case DemonCityBossState.PhaseTransition:
                scepterAngle = -.98f;
                mantleLift = 12f + MathF.Sin(time * 15f) * 3f;
                break;
        }

        Vector2 hip = new(lean * .25f, -48f + crouch);
        Vector2 chest = new(lean, -94f + crouch);
        DrawBox(anchor, hip.X - 31f, hip.Y - 5f, 62f, 55f, Outline, flip);
        DrawBox(anchor, hip.X - 28f, hip.Y - 4f, 56f, 51f, Robe, flip);
        DrawLine(anchor, hip + new Vector2(-22f, 38f),
            hip + new Vector2(-5f, -2f), 3f, RobeEdge, flip);
        DrawLine(anchor, hip + new Vector2(20f, 38f),
            hip + new Vector2(5f, -2f), 3f, RobeEdge, flip);

        DrawBox(anchor, chest.X - 25f, chest.Y - 12f, 50f, 49f, Outline, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 9f, 44f, 43f,
            enemy.IsHitFlashing ? Verdict : Iron, flip);
        DrawLine(anchor, chest + new Vector2(0f, -6f),
            chest + new Vector2(0f, 29f), 4f, Bronze, flip);
        DrawBox(anchor, chest.X - 41f, chest.Y - 19f - mantleLift,
            82f, 18f + mantleLift, Outline, flip);
        DrawBox(anchor, chest.X - 37f, chest.Y - 16f - mantleLift,
            74f, 13f + mantleLift, RobeEdge, flip);

        Vector2 head = chest + new Vector2(0f, -43f);
        DrawBox(anchor, head.X - 14f, head.Y - 25f, 28f, 45f, Outline, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 22f, 22f, 39f, Iron, flip);
        Color maskGlow = dead
            ? Color.Lerp(Verdict, Iron, enemy.DeathPresentationProgress)
            : enemy.BossCombatPhase switch
            {
                3 => new Color(238, 195, 118),
                2 => new Color(207, 154, 102),
                _ => new Color(161, 123, 88)
            };
        DrawLine(anchor, head + new Vector2(-8f, -12f),
            head + new Vector2(8f, -12f), 3f, maskGlow, flip);
        DrawLine(anchor, head + new Vector2(-7f, -2f),
            head + new Vector2(7f, 8f), 2f, Bronze, flip);

        Vector2 shoulder = chest + new Vector2(-20f, -2f);
        if (dead && enemy.DeathPresentationProgress > .16f)
        {
            Vector2 elbow = shoulder + new Vector2(-7f, 25f);
            Vector2 hand = elbow + new Vector2(10f, 21f);
            DrawLimb(anchor, shoulder, elbow, hand, 10f, RobeEdge, flip);
            float drop = Math.Clamp(
                (enemy.DeathPresentationProgress - .16f) / .24f, 0f, 1f);
            DrawScepter(anchor,
                new Vector2(8f, MathHelper.Lerp(-44f, -16f, drop)),
                .03f, flip, powered: false);
        }
        else
        {
            Vector2 elbow = shoulder + Angle(scepterAngle + .18f) * 23f;
            Vector2 hand = elbow + Angle(scepterAngle) * 22f;
            DrawLimb(anchor, shoulder, elbow, hand, 10f, RobeEdge, flip);
            DrawScepter(anchor, hand, scepterAngle, flip, powered: true);
        }

        int plates = enemy.BossCombatPhase;
        float plateOpacity = dead
            ? Math.Clamp(1f - enemy.DeathPresentationProgress / .46f, 0f, 1f)
            : 1f;
        for (int index = 0; index < plates && plateOpacity > 0f; index++)
        {
            float phase = time * (1.15f + enemy.BossCombatPhase * .16f) +
                index * 2.1f;
            Vector2 plate = enemy.CombatState == DemonCityBossState.Intro
                ? chest + new Vector2(34f, -18f + index * 21f)
                : chest + new Vector2(
                    MathF.Cos(phase) * (37f + index * 3f),
                    -5f + MathF.Sin(phase) * 14f);
            DrawBox(anchor, plate.X - 7f, plate.Y - 10f, 14f, 20f,
                new Color(Decree, (int)(210f * plateOpacity)), flip);
            DrawLine(anchor, plate + new Vector2(-4f, -4f),
                plate + new Vector2(4f, 4f), 2f,
                new Color(Verdict, (int)(255f * plateOpacity)), flip);
        }

        if (enemy.CombatState is DemonCityBossState.StepPlant or
            DemonCityBossState.StepDistort)
        {
            DrawBox(enemy.StepDestination, -19f, -4f, 38f, 5f,
                new Color(Decree, 175), flip: false);
        }
    }

    private void DrawScepter(
        Vector2 anchor, Vector2 hand, float angle, bool flip, bool powered)
    {
        Vector2 end = hand + Angle(angle) * 66f;
        DrawLine(anchor, hand, end, 7f, Bronze, flip);
        Vector2 cross = Angle(angle + MathHelper.PiOver2) * 15f;
        DrawLine(anchor, end - cross, end + cross, 12f, Iron, flip);
        DrawLine(anchor, end - cross * .75f, end + cross * .75f,
            4f, powered ? Verdict : IronEdge, flip);
    }

    private void DrawLimb(
        Vector2 anchor, Vector2 start, Vector2 joint, Vector2 end,
        float width, Color color, bool flip)
    {
        DrawLine(anchor, start, joint, width + 3f, Outline, flip);
        DrawLine(anchor, joint, end, width + 3f, Outline, flip);
        DrawLine(anchor, start, joint, width, color, flip);
        DrawLine(anchor, joint, end, width, color, flip);
    }

    private void DrawLine(
        Vector2 anchor, Vector2 start, Vector2 end,
        float width, Color color, bool flip)
    {
        start = World(anchor, start, flip);
        end = World(anchor, end, flip);
        Vector2 delta = end - start;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), width), SpriteEffects.None, 0f);
    }

    private void DrawBox(
        Vector2 anchor, float x, float y, float width, float height,
        Color color, bool flip)
    {
        if (flip)
            x = -x - width;
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)(anchor.X + x), (int)(anchor.Y + y),
                Math.Max(1, (int)width), Math.Max(1, (int)height)), color);
    }

    private static Vector2 World(Vector2 anchor, Vector2 local, bool flip) =>
        anchor + new Vector2(flip ? -local.X : local.X, local.Y);

    private static Vector2 Angle(float angle) =>
        new(MathF.Cos(angle), MathF.Sin(angle));
}
