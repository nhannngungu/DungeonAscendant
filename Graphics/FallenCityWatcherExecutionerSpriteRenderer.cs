using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only Fallen City support/heavy pair. Both rigs use one ground anchor
/// and mirror their complete pose so weapon motion and facing remain aligned.
/// </summary>
public sealed class FallenCityWatcherExecutionerSpriteRenderer
{
    public static readonly Vector2 WatcherVisualSize = new(84f, 76f);
    public static readonly Vector2 ExecutionerVisualSize = new(132f, 96f);

    private static readonly Color Outline = new(25, 23, 22);
    private static readonly Color Skin = new(124, 104, 84);
    private static readonly Color DeadSkin = new(96, 87, 75);
    private static readonly Color WatchLeather = new(75, 54, 41);
    private static readonly Color WatchCloth = new(83, 67, 53);
    private static readonly Color SashRed = new(126, 58, 43);
    private static readonly Color SashGold = new(158, 119, 55);
    private static readonly Color Iron = new(106, 104, 97);
    private static readonly Color IronLight = new(148, 142, 126);
    private static readonly Color Rust = new(112, 68, 44);
    private static readonly Color ApronDark = new(56, 46, 40);
    private static readonly Color Apron = new(83, 65, 53);
    private static readonly Color Hood = new(45, 41, 39);
    private static readonly Color Stain = new(92, 43, 39);
    private static readonly Color Boot = new(43, 36, 32);
    private static readonly Color Wood = new(78, 54, 38);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<FallenCityEnemy, VisualClock>
        _clocks = new();

    public FallenCityWatcherExecutionerSpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(FallenCityEnemy enemy, GameTime gameTime)
    {
        VisualClock clock = _clocks.GetValue(
            enemy,
            static _ => new VisualClock());
        clock.Advance(gameTime);

        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.CombatState != FallenCityCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        float shadow = enemy.Type == EnemyType.StreetExecutioner ? 66f : 39f;
        DrawBox(anchor, -shadow / 2f, -4f, shadow, 6f,
            new Color(10, 9, 9, 145), flip);

        if (!enemy.IsAlive)
        {
            if (enemy.Type == EnemyType.BellWatcher)
                DrawWatcherDeath(anchor, flip, enemy.DeathPresentationProgress);
            else
                DrawExecutionerDeath(anchor, flip,
                    enemy.DeathPresentationProgress);
            return;
        }

        if (enemy.Type == EnemyType.BellWatcher)
            DrawWatcher(enemy, anchor, flip, clock.Elapsed);
        else
            DrawExecutioner(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawWatcher(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 5f;
        float step = moving ? MathF.Sin(elapsed * 10f) * 4f : 0f;
        float scan = enemy.CombatState == FallenCityCombatState.Ready
            ? MathF.Sin(elapsed * 1.8f) * 3f
            : 0f;
        float alarmRaise = enemy.CombatState ==
            FallenCityCombatState.WatcherAlarmWindup
                ? MathHelper.SmoothStep(0f, 1f, enemy.ActionProgress)
                : enemy.CombatState == FallenCityCombatState.WatcherAlarmSignal
                    ? 1f
                    : 0f;
        float flinch = enemy.IsAlarmInterrupted ? -6f :
            enemy.IsHitFlashing ? -3f : 0f;
        float lean = enemy.CombatState is
            FallenCityCombatState.WatcherRetreat or
            FallenCityCombatState.WatcherAlarmApproach ? -3f : 0f;
        Vector2 hip = new(lean + flinch, -31f);
        Vector2 chest = new(lean + flinch, -51f);
        Vector2 head = chest + new Vector2(scan, -15f);

        DrawLine(anchor, new Vector2(-8f - step, 0f),
            hip + new Vector2(-6f, 0f), 7f, WatchCloth, flip);
        DrawLine(anchor, new Vector2(9f + step, 0f),
            hip + new Vector2(6f, 0f), 7f, WatchLeather, flip);
        DrawLine(anchor, new Vector2(-13f - step, 0f),
            new Vector2(-3f - step, 0f), 5f, Boot, flip);
        DrawLine(anchor, new Vector2(5f + step, 0f),
            new Vector2(15f + step, 0f), 5f, Boot, flip);

        DrawBox(anchor, chest.X - 13f, chest.Y - 7f, 27f, 29f,
            Outline, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 5f, 23f, 25f,
            enemy.IsHitFlashing ? new Color(120, 102, 81) : WatchCloth,
            flip);
        DrawLine(anchor, chest + new Vector2(-10f, -3f),
            chest + new Vector2(10f, 18f), 5f, SashRed, flip);
        DrawLine(anchor, chest + new Vector2(-8f, -3f),
            chest + new Vector2(11f, 16f), 2f, SashGold, flip);
        DrawBox(anchor, hip.X - 20f, hip.Y - 5f, 11f, 16f,
            Outline, flip);
        DrawBox(anchor, hip.X - 18f, hip.Y - 3f, 8f, 13f,
            WatchLeather, flip);

        DrawBox(anchor, head.X - 9f, head.Y - 7f, 18f, 16f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 5f, 15f, 13f,
            DeadSkin, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 11f, 22f, 8f,
            Outline, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 10f, 18f, 6f,
            Iron, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 1f, 3f, 3f,
            new Color(43, 37, 33), flip);

        Vector2 rearShoulder = chest + new Vector2(-11f, 0f);
        Vector2 rearHand = chest + new Vector2(-13f, 18f);
        Vector2 frontShoulder = chest + new Vector2(11f, 0f);
        Vector2 frontHand = chest + new Vector2(
            15f + alarmRaise * 3f,
            17f - alarmRaise * 29f);
        DrawLine(anchor, rearShoulder, rearHand, 6f, WatchLeather, flip);
        DrawLine(anchor, frontShoulder, frontHand, 6f, WatchLeather, flip);
        DrawBox(anchor, rearHand.X - 3f, rearHand.Y - 3f, 6f, 6f,
            Skin, flip);
        DrawBox(anchor, frontHand.X - 3f, frontHand.Y - 3f, 6f, 6f,
            Skin, flip);

        if (enemy.BellWatcherAction == BellWatcherActionKind.PanicSlash)
        {
            float angle = enemy.CombatState ==
                FallenCityCombatState.WatcherPanicSlashWindup
                    ? MathHelper.Lerp(-1.7f, -.65f, enemy.ActionProgress)
                    : enemy.CombatState ==
                        FallenCityCombatState.WatcherPanicSlashActive
                        ? MathHelper.Lerp(-.6f, .45f, enemy.ActionProgress)
                        : .65f;
            Vector2 blade = new(MathF.Cos(angle), MathF.Sin(angle));
            DrawLine(anchor, rearHand - blade * 5f,
                rearHand + blade * 22f, 4f, Outline, flip);
            DrawLine(anchor, rearHand,
                rearHand + blade * 21f, 2f, IronLight, flip);
        }
        else
        {
            DrawAlarmHorn(anchor, frontHand, alarmRaise, flip);
        }

        if (enemy.IsAlarmSignalActive)
        {
            Vector2 mouth = head + new Vector2(10f, 2f);
            for (int index = 0; index < 3; index++)
            {
                float reach = 13f + index * 8f;
                DrawLine(anchor,
                    mouth + new Vector2(reach, -5f - index * 2f),
                    mouth + new Vector2(reach + 5f, 0f),
                    2f,
                    new Color(220, 151, 68, 205 - index * 35),
                    flip);
                DrawLine(anchor,
                    mouth + new Vector2(reach + 5f, 0f),
                    mouth + new Vector2(reach, 5f + index * 2f),
                    2f,
                    new Color(220, 151, 68, 205 - index * 35),
                    flip);
            }
        }
        else if (enemy.IsAlarmInterrupted)
        {
            DrawBox(anchor, frontHand.X + 8f, frontHand.Y + 5f, 4f, 3f,
                new Color(144, 103, 56, 150), flip);
            DrawBox(anchor, frontHand.X + 14f, frontHand.Y + 9f, 3f, 3f,
                new Color(113, 83, 52, 115), flip);
        }

        if (moving && (int)(elapsed * 10f) % 5 == 0)
            DrawBox(anchor, -13f, -4f, 3f, 2f,
                new Color(139, 119, 89, 100), flip);
    }

    private void DrawAlarmHorn(
        Vector2 anchor,
        Vector2 hand,
        float raised,
        bool flip)
    {
        Vector2 direction = Vector2.Normalize(new Vector2(
            MathHelper.Lerp(.65f, 1f, raised),
            MathHelper.Lerp(.75f, -.12f, raised)));
        Vector2 bell = hand + direction * 20f;
        DrawLine(anchor, hand - direction * 4f, bell, 6f,
            Outline, flip);
        DrawLine(anchor, hand - direction * 3f, bell, 3f,
            SashGold, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, bell - normal * 8f, bell + normal * 8f,
            6f, Outline, flip);
        DrawLine(anchor, bell - normal * 6f, bell + normal * 6f,
            3f, new Color(177, 127, 56), flip);
    }

    private void DrawExecutioner(
        FallenCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool moving = MathF.Abs(enemy.VisualVelocityX) > 4f;
        float step = moving ? MathF.Sin(elapsed * 5.2f) * 4f : 0f;
        float breathe = MathF.Sin(elapsed * 2.3f) * 1.5f;
        float stagger = enemy.IsStaggered ? -7f :
            enemy.IsHitFlashing ? -3f : 0f;
        Vector2 hip = new(stagger, -37f);
        Vector2 chest = new(stagger, -65f + breathe);
        Vector2 head = chest + new Vector2(1f, -18f);

        DrawLine(anchor, new Vector2(-14f - step, 0f),
            hip + new Vector2(-10f, 0f), 13f, ApronDark, flip);
        DrawLine(anchor, new Vector2(15f + step, 0f),
            hip + new Vector2(10f, 0f), 13f, Apron, flip);
        DrawLine(anchor, new Vector2(-22f - step, 0f),
            new Vector2(-6f - step, 0f), 9f, Boot, flip);
        DrawLine(anchor, new Vector2(7f + step, 0f),
            new Vector2(24f + step, 0f), 9f, Boot, flip);

        DrawBox(anchor, chest.X - 24f, chest.Y - 10f, 48f, 39f,
            Outline, flip);
        DrawBox(anchor, chest.X - 22f, chest.Y - 8f, 44f, 35f,
            enemy.IsHitFlashing ? new Color(108, 88, 69) : ApronDark,
            flip);
        DrawBox(anchor, chest.X - 18f, chest.Y + 4f, 37f, 37f,
            Apron, flip);
        DrawBox(anchor, chest.X - 12f, chest.Y + 15f, 11f, 7f,
            Stain, flip);
        DrawBox(anchor, chest.X + 6f, chest.Y + 26f, 9f, 6f,
            new Color(75, 45, 40), flip);

        DrawBox(anchor, head.X - 14f, head.Y - 12f, 28f, 24f,
            Outline, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 10f, 24f, 21f,
            Hood, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 4f, 15f, 12f,
            DeadSkin, flip);
        DrawLine(anchor, head + new Vector2(-8f, -2f),
            head + new Vector2(9f, 1f), 5f, Hood, flip);
        DrawBox(anchor, head.X + 3f, head.Y, 3f, 2f,
            new Color(53, 43, 38), flip);

        DrawExecutionerChains(enemy, anchor, hip, step, flip);
        DrawExecutionerWeapon(enemy, anchor, chest, flip);

        if (moving && (int)(elapsed * 7f) % 4 == 0)
        {
            DrawBox(anchor, -27f, -4f, 5f, 3f,
                new Color(140, 120, 91, 115), flip);
            DrawBox(anchor, -35f, -3f, 3f, 2f,
                new Color(111, 99, 80, 90), flip);
        }

        if (enemy.ExecutionerImpactProgress > 0f)
            DrawExecutionerImpact(anchor, flip,
                enemy.ExecutionerImpactProgress);
    }

    private void DrawExecutionerChains(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 hip,
        float step,
        bool flip)
    {
        float sway = step * .25f;
        DrawLine(anchor, hip + new Vector2(-20f, 4f),
            hip + new Vector2(21f, 7f), 4f, Outline, flip);
        for (int index = 0; index < 7; index++)
        {
            float x = hip.X - 18f + index * 6f;
            DrawBox(anchor, x, hip.Y + 4f + (index % 2) * 2f,
                4f, 4f, index % 2 == 0 ? Rust : Iron, flip);
        }
        DrawLine(anchor, hip + new Vector2(15f, 7f),
            hip + new Vector2(24f + sway, 24f), 3f, Rust, flip);
    }

    private void DrawExecutionerWeapon(
        FallenCityEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip)
    {
        float angle = .72f;
        if (enemy.CombatState == FallenCityCombatState.ExecutionerWindup)
        {
            angle = enemy.StreetExecutionerAttack switch
            {
                StreetExecutionerAttackKind.ExecutionSlam or
                StreetExecutionerAttackKind.EnragedExecution =>
                    MathHelper.Lerp(.72f, -1.54f, enemy.ActionProgress),
                StreetExecutionerAttackKind.DraggingSweep =>
                    MathHelper.Lerp(.72f, .34f, enemy.ActionProgress),
                _ => MathHelper.Lerp(.72f, -2.18f, enemy.ActionProgress)
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.ExecutionerActive)
        {
            angle = enemy.StreetExecutionerAttack switch
            {
                StreetExecutionerAttackKind.ExecutionSlam or
                StreetExecutionerAttackKind.EnragedExecution =>
                    MathHelper.Lerp(-1.54f, 1.24f, enemy.ActionProgress),
                StreetExecutionerAttackKind.DraggingSweep =>
                    MathHelper.Lerp(.32f, -.20f, enemy.ActionProgress),
                _ => MathHelper.Lerp(-2.18f, .12f, enemy.ActionProgress)
            };
        }
        else if (enemy.CombatState == FallenCityCombatState.ExecutionerRecovery)
        {
            angle = enemy.StreetExecutionerAttack ==
                StreetExecutionerAttackKind.DraggingSweep ? -.12f : 1.05f;
        }

        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 shoulder = chest + new Vector2(17f, -2f);
        Vector2 grip = shoulder + direction * 23f;
        Vector2 haftEnd = grip + direction * 47f;
        Vector2 rearHand = grip - direction * 16f;
        DrawLine(anchor, chest + new Vector2(-15f, 0f), rearHand,
            11f, Apron, flip);
        DrawLine(anchor, shoulder, grip, 11f, ApronDark, flip);
        DrawLine(anchor, rearHand - direction * 5f, haftEnd,
            8f, Outline, flip);
        DrawLine(anchor, rearHand - direction * 4f, haftEnd,
            4f, Wood, flip);

        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 bladeRoot = haftEnd - direction * 5f;
        DrawLine(anchor, bladeRoot - normal * 15f,
            bladeRoot + normal * 16f, 17f, Outline, flip);
        DrawLine(anchor, bladeRoot - normal * 13f,
            bladeRoot + normal * 14f, 12f, Iron, flip);
        DrawLine(anchor, bladeRoot + normal * 14f,
            bladeRoot + normal * 19f + direction * 3f,
            7f, IronLight, flip);
    }

    private void DrawExecutionerImpact(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float spread = 1f - progress;
        for (int index = 0; index < 6; index++)
        {
            float direction = index < 3 ? -1f : 1f;
            float distance = 13f + (index % 3) * 11f + spread * 13f;
            float y = -5f - (index % 2) * 5f * progress;
            DrawBox(anchor, direction * distance, y,
                index % 2 == 0 ? 5f : 3f,
                index % 2 == 0 ? 4f : 3f,
                index % 2 == 0
                    ? new Color(135, 122, 101, 185)
                    : new Color(103, 96, 82, 150),
                flip);
        }
        DrawLine(anchor, new Vector2(-40f, -2f),
            new Vector2(47f, -2f), 3f,
            new Color(119, 106, 87, 115), flip);
    }

    private void DrawWatcherDeath(Vector2 anchor, bool flip, float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f, progress);
        Vector2 bodyStart = new(-5f + fall * 18f, -55f + fall * 49f);
        Vector2 bodyEnd = new(-8f, -27f + fall * 23f);
        DrawLine(anchor, bodyEnd, bodyStart, 24f, Outline, flip);
        DrawLine(anchor, bodyEnd, bodyStart, 19f, WatchCloth, flip);
        DrawBox(anchor, bodyStart.X + 8f, bodyStart.Y - 9f,
            17f, 15f, DeadSkin, flip);
        DrawLine(anchor, new Vector2(-13f, -1f), bodyEnd,
            7f, WatchLeather, flip);
        DrawLine(anchor, new Vector2(12f, -1f), bodyEnd,
            7f, WatchCloth, flip);
        DrawBox(anchor, 22f + fall * 9f, -5f, 12f, 6f,
            SashGold, flip);
    }

    private void DrawExecutionerDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.2f));
        Vector2 hip = new(-8f + fall * 11f, -38f + fall * 32f);
        Vector2 shoulder = new(0f + fall * 30f, -69f + fall * 57f);
        DrawLine(anchor, hip, shoulder, 43f, Outline, flip);
        DrawLine(anchor, hip, shoulder, 36f, ApronDark, flip);
        DrawBox(anchor, shoulder.X + 13f, shoulder.Y - 11f,
            28f, 23f, Hood, flip);
        DrawLine(anchor, new Vector2(-20f, -2f), hip,
            13f, Apron, flip);
        DrawLine(anchor, new Vector2(20f, -2f), hip,
            13f, ApronDark, flip);
        DrawLine(anchor, new Vector2(-36f, -3f),
            new Vector2(50f + fall * 12f, -2f), 7f, Wood, flip);
        DrawBox(anchor, 47f + fall * 12f, -13f, 22f, 20f,
            Iron, flip);
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
            Math.Max(1, (int)MathF.Ceiling(width)),
            Math.Max(1, (int)MathF.Ceiling(height))), color);
    }

    private void DrawLine(
        Vector2 anchor,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        bool flip)
    {
        if (flip)
        {
            start.X = -start.X;
            end.X = -end.X;
        }
        start += anchor;
        end += anchor;
        Vector2 delta = end - start;
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

    private sealed class VisualClock
    {
        public float Elapsed { get; private set; }

        public void Advance(GameTime gameTime)
        {
            Elapsed += Math.Min(
                .05f,
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}
