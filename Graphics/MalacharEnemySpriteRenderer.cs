using System;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Articulated, code-built sovereign silhouette for Map 6's main boss.
/// Anatomy and weapon motion stay readable without relying on aura coverage.
/// </summary>
public sealed class MalacharEnemySpriteRenderer
{
    public static readonly Vector2 VisualSize = new(210f, 198f);

    private static readonly Color Shadow = new(7, 6, 10, 170);
    private static readonly Color Outline = new(12, 10, 16);
    private static readonly Color Plate = new(43, 42, 52);
    private static readonly Color PlateEdge = new(108, 101, 119);
    private static readonly Color Robe = new(31, 25, 43);
    private static readonly Color RobeEdge = new(82, 62, 99);
    private static readonly Color Bronze = new(151, 111, 66);
    private static readonly Color Dominion = new(119, 69, 145);
    private static readonly Color CrownLight = new(224, 184, 104);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public MalacharEnemySpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(MalacharEnemy enemy, GameTime gameTime)
    {
        bool committed = enemy.CombatState is not (MalacharState.Ready or
            MalacharState.ReturnHome or MalacharState.Intro);
        bool flip = committed
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        float p = enemy.ActionProgress;
        bool dead = !enemy.IsAlive;

        float lean = 0f;
        float crouch = 0f;
        float swordAngle = .82f;
        float swordReach = 0f;
        float mantle = 0f;
        if (dead)
        {
            float d = enemy.DeathPresentationProgress;
            swordAngle = MathHelper.Lerp(.82f, 1.48f,
                Math.Clamp(d / .32f, 0f, 1f));
            crouch = MathHelper.Lerp(0f, 50f,
                Math.Clamp((d - .20f) / .62f, 0f, 1f));
            lean = MathHelper.Lerp(0f, 38f,
                Math.Clamp((d - .48f) / .52f, 0f, 1f));
        }
        else
        {
            Pose(enemy.CombatState, p, ref lean, ref crouch,
                ref swordAngle, ref swordReach, ref mantle);
        }

        float walk = !dead && MathF.Abs(enemy.VisualVelocityX) > 2f
            ? MathF.Sin(time * 6.2f) * 5f : 0f;
        float breath = dead ? 0f : MathF.Sin(time * 1.8f) * .65f;
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)anchor.X - 38, (int)anchor.Y - 5, 76, 8),
            Shadow);

        Vector2 hip = new(lean * .35f, -59f + crouch);
        Vector2 chest = new(lean, -116f + crouch + breath);
        DrawCloak(anchor, chest, hip, mantle, flip, dead,
            enemy.DeathPresentationProgress);

        DrawLimb(anchor, hip + new Vector2(-15f, 0f),
            new Vector2(-18f + walk, -34f + crouch),
            new Vector2(-24f - walk, -3f), 15f, Plate, flip);
        DrawLimb(anchor, hip + new Vector2(15f, 0f),
            new Vector2(18f - walk, -34f + crouch),
            new Vector2(24f + walk, -3f), 15f, Plate, flip);

        DrawBox(anchor, hip.X - 29f, hip.Y - 10f, 58f, 21f, Outline, flip);
        DrawBox(anchor, hip.X - 26f, hip.Y - 8f, 52f, 17f, Bronze, flip);
        DrawBox(anchor, chest.X - 35f, chest.Y - 17f, 70f, 65f,
            Outline, flip);
        DrawBox(anchor, chest.X - 32f, chest.Y - 14f, 64f, 59f,
            enemy.IsHitFlashing ? PlateEdge : Plate, flip);
        DrawLine(anchor, chest + new Vector2(-25f, 8f),
            chest + new Vector2(23f, 8f), 4f, Bronze, flip);
        DrawLine(anchor, chest + new Vector2(0f, -8f),
            chest + new Vector2(0f, 36f), 3f, RobeEdge, flip);

        DrawMantle(anchor, chest, mantle, flip);
        DrawHead(anchor, chest, flip, enemy.CombatPhase, time, dead,
            enemy.DeathPresentationProgress);

        Vector2 rearShoulder = chest + new Vector2(-28f, -4f);
        Vector2 frontShoulder = chest + new Vector2(28f, -2f);
        Vector2 rearHand = rearShoulder + Angle(swordAngle + .12f) * 30f;
        Vector2 frontHand = rearHand + Angle(swordAngle) * (24f + swordReach);
        DrawLimb(anchor, rearShoulder, rearHand, rearHand, 12f, Plate, flip);
        DrawLimb(anchor, frontShoulder, frontHand, frontHand, 12f, Plate, flip);
        DrawLine(anchor, rearHand, frontHand, 8f, Bronze, flip);
        DrawSovereignBlade(anchor, frontHand, swordAngle, flip,
            enemy.CombatPhase, dead);

        if (enemy.CombatState == MalacharState.ReversalStance)
            DrawReversalGuard(anchor, chest, flip, p);
        if (enemy.CombatState is MalacharState.StepCompress or
            MalacharState.StepMove or MalacharState.EdictStep)
            DrawStepFragments(anchor, time, flip);
        if (enemy.CombatState is MalacharState.PhaseTransition or
            MalacharState.DominionPlant or MalacharState.EdictRaise)
            DrawCrestPulse(anchor, chest, time, enemy.CombatPhase);
    }

    private static void Pose(
        MalacharState state,
        float p,
        ref float lean,
        ref float crouch,
        ref float swordAngle,
        ref float swordReach,
        ref float mantle)
    {
        switch (state)
        {
            case MalacharState.Intro:
                swordAngle = MathHelper.Lerp(1.42f, .82f, p);
                mantle = 7f * MathF.Sin(p * MathHelper.Pi);
                break;
            case MalacharState.CutOneWindup:
            case MalacharState.CutTwoTell:
            case MalacharState.CutThreeDelay:
                swordAngle = MathHelper.Lerp(.82f, -1.88f, p);
                lean = -9f;
                crouch = 7f;
                break;
            case MalacharState.CutOneActive:
            case MalacharState.CutTwoActive:
            case MalacharState.CutThreeActive:
            case MalacharState.DominionSlash:
                swordAngle = MathHelper.Lerp(-1.88f, .28f, p);
                lean = 18f;
                swordReach = 12f;
                mantle = 13f;
                break;
            case MalacharState.AdvanceWindup:
                swordAngle = -.82f;
                crouch = 10f;
                lean = -7f;
                break;
            case MalacharState.AdvanceStep:
            case MalacharState.AdvanceActive:
            case MalacharState.PursuitStrike:
                swordAngle = .08f;
                swordReach = 20f;
                lean = 21f;
                mantle = 16f;
                break;
            case MalacharState.ReversalStance:
            case MalacharState.ReversalPause:
                swordAngle = -1.05f;
                crouch = 5f;
                break;
            case MalacharState.ReversalActive:
                swordAngle = MathHelper.Lerp(-1.05f, .42f, p);
                lean = 16f;
                break;
            case MalacharState.RisingWindup:
                swordAngle = 1.36f;
                crouch = 13f;
                break;
            case MalacharState.RisingActive:
            case MalacharState.RisingEdge:
                swordAngle = MathHelper.Lerp(1.36f, -1.30f, p);
                lean = 12f;
                mantle = 12f;
                break;
            case MalacharState.StepCompress:
            case MalacharState.PursuitPause:
            case MalacharState.RushPause:
                crouch = 12f;
                swordAngle = -.54f;
                mantle = 11f;
                break;
            case MalacharState.StepMove:
            case MalacharState.PursuitStep:
            case MalacharState.RushStep:
            case MalacharState.EdictStep:
                lean = 22f;
                crouch = 5f;
                swordAngle = .18f;
                mantle = 20f;
                break;
            case MalacharState.ArcWindup:
            case MalacharState.ArcRelease:
                swordAngle = MathHelper.Lerp(.82f, -.42f, p);
                mantle = 8f;
                break;
            case MalacharState.PillarGesture:
            case MalacharState.PillarCommit:
            case MalacharState.DominionPlant:
            case MalacharState.DominionSequence:
            case MalacharState.EdictRaise:
            case MalacharState.EdictPressure:
            case MalacharState.EdictMark:
            case MalacharState.EdictRise:
                swordAngle = -1.48f;
                mantle = 12f;
                break;
            case MalacharState.BreakRaise:
            case MalacharState.BreakPause:
                swordAngle = -1.58f;
                crouch = 9f;
                break;
            case MalacharState.BreakActive:
            case MalacharState.EdictDescend:
                swordAngle = MathHelper.Lerp(-1.58f, 1.18f, p);
                lean = 15f;
                crouch = 18f;
                mantle = 17f;
                break;
            case MalacharState.RushOne:
            case MalacharState.RushTwo:
            case MalacharState.RushThree:
            case MalacharState.RushFour:
                swordAngle = p < .5f ? -.75f : .35f;
                lean = 19f;
                swordReach = 13f;
                mantle = 18f;
                break;
            case MalacharState.PhaseTransition:
                swordAngle = -1.40f;
                mantle = 16f * MathF.Sin(p * MathHelper.Pi);
                break;
        }
    }

    private void DrawCloak(Vector2 anchor, Vector2 chest, Vector2 hip,
        float lift, bool flip, bool dead, float death)
    {
        float fall = dead ? 30f * death : 0f;
        Vector2 top = chest + new Vector2(-22f, 13f);
        Vector2 bottom = hip + new Vector2(-34f - lift, 53f + fall);
        DrawLine(anchor, top, bottom, 34f, Outline, flip);
        DrawLine(anchor, top + new Vector2(2f, 2f),
            bottom + new Vector2(3f, -3f), 28f, Robe, flip);
        DrawLine(anchor, bottom + new Vector2(-7f, 0f),
            bottom + new Vector2(2f, -25f), 3f, RobeEdge, flip);
    }

    private void DrawMantle(Vector2 anchor, Vector2 chest, float lift, bool flip)
    {
        DrawBox(anchor, chest.X - 52f, chest.Y - 22f - lift * .2f,
            104f, 21f, Outline, flip);
        DrawBox(anchor, chest.X - 48f, chest.Y - 19f - lift * .2f,
            96f, 15f, RobeEdge, flip);
        DrawLine(anchor, chest + new Vector2(-47f, -12f),
            chest + new Vector2(-61f, 5f + lift), 7f, Bronze, flip);
        DrawLine(anchor, chest + new Vector2(47f, -12f),
            chest + new Vector2(61f, 5f + lift), 7f, Bronze, flip);
    }

    private void DrawHead(Vector2 anchor, Vector2 chest, bool flip, int phase,
        float time, bool dead, float death)
    {
        Vector2 head = chest + new Vector2(0f, -45f + (dead ? 11f * death : 0f));
        DrawBox(anchor, head.X - 17f, head.Y - 21f, 34f, 42f, Outline, flip);
        DrawBox(anchor, head.X - 14f, head.Y - 18f, 28f, 36f, Plate, flip);
        DrawBox(anchor, head.X - 4f, head.Y - 14f, 8f, 24f, Robe, flip);
        Color eye = dead ? new Color(57, 46, 53) : phase == 3
            ? CrownLight : new Color(164, 91, 137);
        DrawBox(anchor, head.X - 11f, head.Y - 3f, 7f, 2f, eye, flip);
        DrawBox(anchor, head.X + 4f, head.Y - 3f, 7f, 2f, eye, flip);
        for (int i = -1; i <= 1; i++)
        {
            float height = i == 0 ? 28f : 20f;
            DrawLine(anchor, head + new Vector2(i * 11f, -17f),
                head + new Vector2(i * 14f, -17f - height),
                i == 0 ? 6f : 5f, Bronze, flip);
        }
        if (!dead && phase >= 2)
        {
            float pulse = .5f + .5f * MathF.Sin(time * 4f);
            DrawBox(anchor, head.X - 2f, head.Y - 47f, 4f, 6f,
                new Color(CrownLight, (int)MathHelper.Lerp(90f, 210f, pulse)),
                flip);
        }
    }

    private void DrawSovereignBlade(Vector2 anchor, Vector2 hand,
        float angle, bool flip, int phase, bool dead)
    {
        Vector2 guard = hand + Angle(angle) * 12f;
        Vector2 tip = guard + Angle(angle) * 88f;
        DrawLine(anchor, hand, guard, 8f, Bronze, flip);
        DrawLine(anchor, guard + Angle(angle + 1.57f) * 15f,
            guard + Angle(angle - 1.57f) * 15f, 6f, Bronze, flip);
        DrawLine(anchor, guard, tip, 13f, Outline, flip);
        DrawLine(anchor, guard + Angle(angle) * 3f,
            tip - Angle(angle) * 4f, 8f,
            dead ? new Color(55, 51, 60) : PlateEdge, flip);
        DrawLine(anchor, guard + Angle(angle) * 9f,
            tip - Angle(angle) * 9f, 2f,
            dead ? new Color(64, 48, 63) :
                phase == 3 ? CrownLight : Dominion, flip);
    }

    private void DrawReversalGuard(Vector2 anchor, Vector2 chest,
        bool flip, float progress)
    {
        int alpha = (int)MathHelper.Lerp(95f, 210f,
            .5f + .5f * MathF.Sin(progress * MathHelper.TwoPi));
        DrawLine(anchor, chest + new Vector2(40f, -37f),
            chest + new Vector2(58f, 29f), 4f,
            new Color(CrownLight, alpha), flip);
        DrawLine(anchor, chest + new Vector2(58f, 29f),
            chest + new Vector2(34f, 15f), 3f,
            new Color(Dominion, alpha), flip);
    }

    private void DrawStepFragments(Vector2 anchor, float time, bool flip)
    {
        for (int i = 0; i < 4; i++)
        {
            float drift = (time * 34f + i * 19f) % 62f;
            DrawBox(anchor, -46f - drift, -30f - i * 17f, 13f, 3f,
                new Color(Dominion, 135 - i * 16), flip);
        }
    }

    private void DrawCrestPulse(Vector2 anchor, Vector2 chest,
        float time, int phase)
    {
        float radius = 42f + MathF.Sin(time * 4.2f) * 4f;
        Color color = phase == 3 ? CrownLight : Dominion;
        DrawLine(anchor, chest + new Vector2(-radius, -18f),
            chest + new Vector2(0f, -49f), 3f, new Color(color, 145), false);
        DrawLine(anchor, chest + new Vector2(0f, -49f),
            chest + new Vector2(radius, -18f), 3f, new Color(color, 145), false);
    }

    private void DrawLimb(Vector2 anchor, Vector2 root, Vector2 joint,
        Vector2 end, float thickness, Color color, bool flip)
    {
        DrawLine(anchor, root, joint, thickness, Outline, flip);
        DrawLine(anchor, root, joint, MathF.Max(3f, thickness - 5f), color, flip);
        if (Vector2.DistanceSquared(joint, end) > 1f)
        {
            DrawLine(anchor, joint, end, thickness - 1f, Outline, flip);
            DrawLine(anchor, joint, end, MathF.Max(3f, thickness - 6f), color, flip);
        }
    }

    private void DrawLine(Vector2 anchor, Vector2 from, Vector2 to,
        float thickness, Color color, bool flip)
    {
        Vector2 a = anchor + new Vector2(flip ? -from.X : from.X, from.Y);
        Vector2 b = anchor + new Vector2(flip ? -to.X : to.X, to.Y);
        Vector2 delta = b - a;
        if (delta.LengthSquared() <= .01f)
            return;
        _spriteBatch.Draw(_pixel, a, null, color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0f, .5f),
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
    }

    private void DrawBox(Vector2 anchor, float x, float y,
        float width, float height, Color color, bool flip)
    {
        float left = flip ? -x - width : x;
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)MathF.Round(anchor.X + left),
                (int)MathF.Round(anchor.Y + y),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))), color);
    }

    private static Vector2 Angle(float radians) =>
        new(MathF.Cos(radians), MathF.Sin(radians));
}
