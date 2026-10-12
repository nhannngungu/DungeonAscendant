using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only silhouettes and combat tells for the Corruption Priest and
/// Rift Warden. Poses are authored facing right and mirrored at the feet.
/// </summary>
public sealed class DemonRitualSpriteRenderer
{
    public static readonly Vector2 PriestVisualSize = new(112f, 108f);
    public static readonly Vector2 WardenVisualSize = new(156f, 124f);

    private static readonly Color Outline = new(24, 17, 29);
    private static readonly Color AshSkin = new(103, 91, 96);
    private static readonly Color Robe = new(67, 31, 66);
    private static readonly Color RobeDark = new(39, 24, 48);
    private static readonly Color Bone = new(174, 157, 128);
    private static readonly Color Charm = new(180, 72, 76);
    private static readonly Color Violet = new(137, 66, 181);
    private static readonly Color VioletCore = new(211, 135, 239);
    private static readonly Color Armor = new(56, 49, 70);
    private static readonly Color ArmorLight = new(91, 78, 106);
    private static readonly Color BlackMetal = new(35, 32, 43);
    private static readonly Color Glaive = new(151, 139, 154);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonLandsEnemy, Clock> _clocks = new();

    public DemonRitualSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonLandsEnemy enemy, GameTime gameTime)
    {
        Clock clock = _clocks.GetValue(enemy, static _ => new Clock());
        clock.Elapsed += MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .05f);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool locked = enemy.FrontierCombatState != DemonFrontierCombatState.Ready;
        bool flip = locked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        if (enemy.Type == EnemyType.CorruptionPriest)
            DrawPriestEffects(enemy, anchor, flip, clock.Elapsed);
        else
            DrawWardenEffects(enemy, anchor, flip, clock.Elapsed);

        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.RiftWarden && enemy.IsWardenHidden)
            return;

        DrawBox(anchor, enemy.Type == EnemyType.RiftWarden ? -44f : -24f,
            -3f, enemy.Type == EnemyType.RiftWarden ? 88f : 48f, 6f,
            new Color(7, 6, 10, 155), flip);
        if (enemy.Type == EnemyType.CorruptionPriest)
            DrawPriest(enemy, anchor, flip, clock.Elapsed);
        else
            DrawWarden(enemy, anchor, flip, clock.Elapsed);
    }

    private void DrawPriest(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        bool casting = enemy.FrontierCombatState is
            DemonFrontierCombatState.PriestChantWindup or
            DemonFrontierCombatState.PriestChantActive or
            DemonFrontierCombatState.PriestAnchorWindup or
            DemonFrontierCombatState.PriestAnchorActive or
            DemonFrontierCombatState.PriestGroundWindup or
            DemonFrontierCombatState.PriestGroundActive or
            DemonFrontierCombatState.PriestFinalRiteWindup or
            DemonFrontierCombatState.PriestFinalRiteActive;
        bool striking = enemy.FrontierCombatState is
            DemonFrontierCombatState.PriestStaffWindup or
            DemonFrontierCombatState.PriestStaffActive;
        float stride = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(elapsed * 7f) * 5f
            : MathF.Sin(elapsed * 2.2f) * 1.2f;
        float recoil = enemy.IsStaggered ? -9f : enemy.IsHitFlashing ? -5f : 0f;
        float rise = casting ? -3f : MathF.Sin(elapsed * 2.5f) * .8f;
        Vector2 hip = new(recoil, -34f + rise);
        Vector2 chest = hip + new Vector2(1f, -29f);
        Vector2 head = chest + new Vector2(1f, -19f);

        DrawLine(anchor, new Vector2(-9f - stride, -1f), hip + new Vector2(-6f, 3f),
            8f, RobeDark, flip);
        DrawLine(anchor, new Vector2(10f + stride, -1f), hip + new Vector2(6f, 3f),
            8f, Robe, flip);
        DrawBox(anchor, hip.X - 19f, hip.Y - 2f, 38f, 37f, Outline, flip);
        DrawBox(anchor, hip.X - 16f, hip.Y, 32f, 33f, RobeDark, flip);
        DrawBox(anchor, hip.X - 6f, hip.Y, 12f, 33f, Robe, flip);
        DrawBox(anchor, chest.X - 15f, chest.Y - 8f, 31f, 35f, Outline, flip);
        DrawBox(anchor, chest.X - 12f, chest.Y - 5f, 25f, 31f,
            enemy.IsHitFlashing ? new Color(162, 113, 144) : Robe, flip);
        DrawLine(anchor, chest + new Vector2(-11f, -3f),
            chest + new Vector2(10f, 22f), 3f, Charm, flip);

        // Bone cords, seals and charms keep the caster silhouette asymmetrical.
        float sway = MathF.Sin(elapsed * 3.1f) * 2f;
        DrawLine(anchor, chest + new Vector2(-12f, 6f),
            hip + new Vector2(-19f + sway, 19f), 2f, Bone, flip);
        DrawBox(anchor, hip.X - 23f + sway, hip.Y + 16f, 8f, 12f, Charm, flip);
        DrawLine(anchor, chest + new Vector2(11f, 8f),
            hip + new Vector2(18f - sway, 15f), 2f, Bone, flip);
        DrawBox(anchor, hip.X + 15f - sway, hip.Y + 13f, 7f, 9f, Bone, flip);

        DrawBox(anchor, head.X - 10f, head.Y - 10f, 20f, 22f, Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 7f, 14f, 18f, AshSkin, flip);
        DrawLine(anchor, head + new Vector2(-5f, -8f),
            head + new Vector2(-15f, -25f), 5f, Bone, flip);
        DrawLine(anchor, head + new Vector2(5f, -8f),
            head + new Vector2(14f, -26f), 5f, Bone, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 2f, 3f, 2f,
            new Color(203, 96, 206, 205), flip);

        Vector2 rearShoulder = chest + new Vector2(-11f, 0f);
        Vector2 rearHand = chest + new Vector2(casting ? -28f : -17f,
            casting ? -15f : 16f);
        DrawLine(anchor, rearShoulder, rearHand, 6f, AshSkin, flip);

        Vector2 shoulder = chest + new Vector2(12f, 0f);
        float staffAngle = striking
            ? MathHelper.Lerp(-1.3f, .22f, enemy.ActionProgress)
            : casting ? -1.48f : -1.32f;
        Vector2 direction = new(MathF.Cos(staffAngle), MathF.Sin(staffAngle));
        Vector2 hand = shoulder + direction * 14f;
        Vector2 staffStart = hand - direction * 38f;
        Vector2 staffEnd = hand + direction * 55f;
        DrawLine(anchor, shoulder, hand, 6f, AshSkin, flip);
        DrawLine(anchor, staffStart, staffEnd, 7f, Outline, flip);
        DrawLine(anchor, staffStart, staffEnd, 4f, new Color(72, 51, 47), flip);
        DrawBox(anchor, staffEnd.X - 6f, staffEnd.Y - 6f, 12f, 12f, Bone, flip);
        if (casting)
        {
            float pulse = .45f + .35f * MathF.Sin(elapsed * 8f);
            DrawBox(anchor, staffEnd.X - 10f, staffEnd.Y - 10f, 20f, 20f,
                new Color((byte)137, (byte)66, (byte)181,
                    (byte)(70 + 90 * pulse)), flip);
            DrawBox(anchor, rearHand.X - 4f, rearHand.Y - 4f, 8f, 8f,
                VioletCore, flip);
        }
    }

    private void DrawPriestEffects(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        if (enemy.HasTaintedGround)
        {
            Rectangle zone = enemy.TaintedGroundBounds;
            _spriteBatch.Draw(_pixel, zone, new Color(99, 38, 114, 42));
            DrawOutline(zone, 2, new Color(173, 78, 188, 145));
            for (int i = 0; i < 5; i++)
            {
                int x = zone.Left + 13 + i * 28;
                int height = 3 + (i % 3) * 2;
                _spriteBatch.Draw(_pixel,
                    new Rectangle(x, zone.Bottom - height - 2, 10, height),
                    new Color(205, 92, 186, 105));
            }
        }
        if (enemy.HasAnchorTether)
        {
            Vector2 from = enemy.Position + new Vector2(0f, -56f);
            Vector2 to = enemy.BlessedAnchorPosition;
            DrawWorldLine(from, to, 5f, new Color(99, 42, 133, 90));
            DrawWorldLine(from, to, 2f, new Color(203, 112, 220, 190));
        }
    }

    private void DrawWarden(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        float march = MathF.Abs(enemy.VisualVelocityX) > 3f
            ? MathF.Sin(elapsed * 5.2f) * 7f : 0f;
        float recoil = enemy.IsStaggered ? -7f : enemy.IsHitFlashing ? -4f : 0f;
        Vector2 hip = new(recoil, -39f);
        Vector2 chest = hip + new Vector2(0f, -34f);
        Vector2 head = chest + new Vector2(0f, -22f);

        DrawLine(anchor, new Vector2(-15f - march, -1f), hip + new Vector2(-10f, 3f),
            13f, BlackMetal, flip);
        DrawLine(anchor, new Vector2(17f + march, -1f), hip + new Vector2(10f, 3f),
            13f, Armor, flip);
        DrawBox(anchor, hip.X - 25f, hip.Y - 5f, 50f, 43f, Outline, flip);
        DrawBox(anchor, hip.X - 21f, hip.Y - 1f, 42f, 37f, Armor, flip);
        DrawBox(anchor, chest.X - 28f, chest.Y - 12f, 57f, 45f, Outline, flip);
        DrawBox(anchor, chest.X - 24f, chest.Y - 8f, 49f, 38f,
            enemy.IsHitFlashing ? new Color(142, 116, 153) : Armor, flip);
        DrawBox(anchor, chest.X - 31f, chest.Y - 8f, 13f, 17f, ArmorLight, flip);
        DrawBox(anchor, chest.X + 19f, chest.Y - 8f, 15f, 18f, ArmorLight, flip);

        // A contained rift core and restrained cracks, readable without bloom.
        DrawBox(anchor, chest.X - 8f, chest.Y + 1f, 17f, 18f, Outline, flip);
        DrawBox(anchor, chest.X - 5f, chest.Y + 4f, 11f, 12f, Violet, flip);
        DrawBox(anchor, chest.X - 2f, chest.Y + 6f, 5f, 8f, VioletCore, flip);
        DrawLine(anchor, chest + new Vector2(-4f, 17f),
            hip + new Vector2(-15f, 7f), 2f, Violet, flip);
        DrawLine(anchor, chest + new Vector2(5f, 17f),
            hip + new Vector2(17f, 5f), 2f, Violet, flip);

        DrawBox(anchor, head.X - 15f, head.Y - 12f, 30f, 27f, Outline, flip);
        DrawBox(anchor, head.X - 12f, head.Y - 9f, 24f, 22f, BlackMetal, flip);
        DrawLine(anchor, head + new Vector2(-9f, -8f),
            head + new Vector2(-25f, -27f), 8f, Armor, flip);
        DrawLine(anchor, head + new Vector2(9f, -8f),
            head + new Vector2(25f, -28f), 8f, Armor, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 2f, 5f, 3f, VioletCore, flip);

        float angle = enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.WardenThrustWindup => -2.32f,
            DemonFrontierCombatState.WardenThrustActive => -.04f,
            DemonFrontierCombatState.WardenGateAttackWindup => -2.05f,
            DemonFrontierCombatState.WardenGateAttackActive => -.08f,
            DemonFrontierCombatState.WardenSweepWindup => -1.72f,
            DemonFrontierCombatState.WardenSweepActive =>
                MathHelper.Lerp(-1.72f, .75f, enemy.ActionProgress),
            _ => -.88f
        };
        Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 hand = chest + new Vector2(12f, 5f);
        Vector2 shaftStart = hand - dir * 38f;
        Vector2 shaftEnd = hand + dir * 73f;
        DrawLine(anchor, chest + new Vector2(-18f, 0f), hand - dir * 12f,
            11f, ArmorLight, flip);
        DrawLine(anchor, chest + new Vector2(19f, 1f), hand + dir * 7f,
            11f, Armor, flip);
        DrawLine(anchor, shaftStart, shaftEnd, 9f, Outline, flip);
        DrawLine(anchor, shaftStart, shaftEnd, 5f, BlackMetal, flip);
        Vector2 normal = new(-dir.Y, dir.X);
        DrawLine(anchor, shaftEnd, shaftEnd - dir * 17f + normal * 31f,
            12f, Outline, flip);
        DrawLine(anchor, shaftEnd, shaftEnd - dir * 15f + normal * 29f,
            7f, Glaive, flip);
    }

    private void DrawWardenEffects(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        float elapsed)
    {
        if (enemy.HasRiftScar)
        {
            Rectangle scar = enemy.RiftScarBounds;
            _spriteBatch.Draw(_pixel, scar, new Color(91, 43, 127, 48));
            DrawOutline(scar, 2, new Color(164, 80, 209, 150));
        }
        if (enemy.HasRiftGate)
            DrawPortal(enemy.RiftGatePosition, elapsed, 1f);
        if (enemy.FrontierCombatState is DemonFrontierCombatState.WardenStepEntry or
            DemonFrontierCombatState.WardenStepHidden)
            DrawPortal(enemy.RiftEntryPosition, elapsed, .82f);
        if (enemy.HasRiftExitPosition && enemy.FrontierCombatState is
            DemonFrontierCombatState.WardenStepExit or
            DemonFrontierCombatState.WardenStepRecovery)
            DrawPortal(enemy.RiftExitPosition, elapsed, .72f);
    }

    private void DrawPortal(Vector2 position, float elapsed, float scale)
    {
        int pulse = (int)MathF.Round(MathF.Sin(elapsed * 7f) * 3f);
        Rectangle outer = new(
            (int)(position.X - (23 + pulse) * scale),
            (int)(position.Y - 39 * scale),
            (int)((46 + pulse * 2) * scale),
            (int)(78 * scale));
        _spriteBatch.Draw(_pixel, outer, new Color(67, 27, 92, 88));
        DrawOutline(outer, 4, new Color(158, 72, 205, 190));
        Rectangle slit = new(outer.Center.X - 4, outer.Top + 10,
            8, Math.Max(8, outer.Height - 20));
        _spriteBatch.Draw(_pixel, slit, new Color(218, 143, 241, 185));
    }

    private void DrawDeath(DemonLandsEnemy enemy, Vector2 anchor, bool flip)
    {
        float collapse = enemy.DeathPresentationProgress;
        float width = enemy.Type == EnemyType.RiftWarden ? 75f : 55f;
        float height = MathF.Max(8f,
            (enemy.Type == EnemyType.RiftWarden ? 76f : 66f) * (1f - collapse * .82f));
        DrawBox(anchor, -width * .5f, -height, width, height,
            enemy.Type == EnemyType.RiftWarden ? Armor : RobeDark, flip);
        DrawBox(anchor, -6f, -height * .58f, 12f, 10f,
            new Color((byte)137, (byte)66, (byte)181,
                (byte)(190 * (1f - collapse))), flip);
    }

    private void DrawBox(Vector2 anchor, float x, float y, float width,
        float height, Color color, bool flip)
    {
        if (flip)
            x = -x - width;
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)MathF.Round(anchor.X + x), (int)MathF.Round(anchor.Y + y),
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
    }

    private void DrawLine(Vector2 anchor, Vector2 start, Vector2 end,
        float thickness, Color color, bool flip)
    {
        if (flip)
        {
            start.X = -start.X;
            end.X = -end.X;
        }
        DrawWorldLine(anchor + start, anchor + end, thickness, color);
    }

    private void DrawWorldLine(Vector2 start, Vector2 end,
        float thickness, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < .01f)
            return;
        _spriteBatch.Draw(_pixel, start, null, color,
            MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private void DrawOutline(Rectangle rectangle, int thickness, Color color)
    {
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Bottom - thickness,
                rectangle.Width, thickness), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        _spriteBatch.Draw(_pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Y,
                thickness, rectangle.Height), color);
    }

    private sealed class Clock
    {
        public float Elapsed;
    }
}
