using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Code-only presentation for the two frontier enemies polished in Map 4
/// pass one. Poses are authored facing right and mirrored around a stable
/// foot anchor.
/// </summary>
public sealed class DemonFrontierSpriteRenderer
{
    public static readonly Vector2 ImpVisualSize = new(78f, 62f);
    public static readonly Vector2 ArcherVisualSize = new(112f, 102f);

    private static readonly Color Outline = new(29, 18, 20);
    private static readonly Color ImpSkin = new(116, 61, 57);
    private static readonly Color ImpSkinDark = new(72, 45, 47);
    private static readonly Color AshSkin = new(111, 101, 96);
    private static readonly Color AshSkinDark = new(68, 61, 61);
    private static readonly Color Horn = new(151, 133, 108);
    private static readonly Color HornDark = new(83, 69, 63);
    private static readonly Color Leather = new(76, 48, 38);
    private static readonly Color Cloth = new(104, 39, 34);
    private static readonly Color ClothDark = new(58, 29, 31);
    private static readonly Color Metal = new(104, 95, 84);
    private static readonly Color MetalDark = new(54, 51, 51);
    private static readonly Color Bone = new(172, 156, 126);
    private static readonly Color Blackwood = new(45, 31, 29);
    private static readonly Color Ember = new(229, 103, 46);
    private static readonly Color EmberCore = new(247, 177, 76);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonLandsEnemy, AnimationClock>
        _clocks = new();

    public DemonFrontierSpriteRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonLandsEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy,
            static _ => new AnimationClock());
        FrontierVisualState state = SelectState(enemy);
        clock.Update(gameTime, state);

        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool actionLocked = enemy.FrontierCombatState !=
            DemonFrontierCombatState.Ready;
        bool flip = actionLocked
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;
        float shadowWidth = enemy.Type == EnemyType.ImpRaider ? 38f : 49f;
        DrawBox(anchor, -shadowWidth * .5f, -3f, shadowWidth, 5f,
            new Color(8, 7, 8, 145), flip);

        if (!enemy.IsAlive)
        {
            DrawDeath(enemy, anchor, flip);
            return;
        }

        if (enemy.Type == EnemyType.ImpRaider)
            DrawImp(enemy, anchor, flip, state, clock.Elapsed);
        else
            DrawArcher(enemy, anchor, flip, state, clock.Elapsed);
    }

    private void DrawImp(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        FrontierVisualState state,
        float elapsed)
    {
        float cycle = MathF.Sin(elapsed * 12f);
        float idleHop = state == FrontierVisualState.Idle
            ? -MathF.Max(0f, MathF.Sin(elapsed * 3.1f) - .82f) * 13f
            : 0f;
        float bob = state == FrontierVisualState.Run ? MathF.Abs(cycle) * 3f :
            MathF.Sin(elapsed * 5f) * .8f + idleHop;
        float step = state == FrontierVisualState.Run ? cycle * 8f :
            MathF.Sin(elapsed * 2.6f) * 1.5f;
        float crouch = state is FrontierVisualState.PounceCrouch or
            FrontierVisualState.LowStab ? 8f : 0f;
        float lean = state switch
        {
            FrontierVisualState.Pounce => 14f,
            FrontierVisualState.Slash => 8f,
            FrontierVisualState.LowStab => 12f,
            FrontierVisualState.Recoil => -8f,
            FrontierVisualState.Stagger => -10f,
            _ => state == FrontierVisualState.Run ? 6f : 0f
        };
        if (enemy.IsHitFlashing)
            lean -= 5f;

        Color skin = enemy.IsHitFlashing
            ? new Color(221, 157, 129)
            : ImpSkin;
        Vector2 hip = new(lean, -25f + crouch + bob);
        Vector2 chest = hip + new Vector2(4f, -14f);
        Vector2 head = chest + new Vector2(8f, -11f);
        Vector2 rearFoot = new(-10f - step, 0f);
        Vector2 frontFoot = new(12f + step, 0f);
        Vector2 rearKnee = new(-6f + step, -13f + crouch);
        Vector2 frontKnee = new(8f - step, -14f + crouch);

        DrawLimb(anchor, rearFoot, rearKnee, ImpSkinDark, flip, 5f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 1f), skin, flip, 6f);
        DrawLimb(anchor, frontFoot, frontKnee, ImpSkinDark, flip, 5f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 1f), skin, flip, 6f);
        DrawClawFoot(anchor, rearFoot, flip);
        DrawClawFoot(anchor, frontFoot, flip);

        // Wiry torso, crossed straps, and mismatched scavenged plates.
        DrawBox(anchor, chest.X - 9f, chest.Y - 5f, 19f, 23f,
            Outline, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y - 3f, 15f, 20f,
            skin, flip);
        DrawLineLocal(anchor, chest + new Vector2(-7f, -2f),
            chest + new Vector2(7f, 16f), 4f, Leather, flip);
        DrawBox(anchor, chest.X - 13f, chest.Y - 5f, 10f, 8f,
            MetalDark, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 4f, 7f, 5f,
            Metal, flip);
        DrawBox(anchor, hip.X - 10f, hip.Y - 1f, 23f, 5f,
            Leather, flip);
        DrawBox(anchor, hip.X - 8f, hip.Y + 3f, 8f, 14f,
            ClothDark, flip);
        DrawBox(anchor, hip.X + 2f, hip.Y + 3f, 9f, 11f,
            Cloth, flip);

        DrawImpHead(anchor, head, skin, flip);

        Vector2 shoulder = chest + new Vector2(9f, 0f);
        float weaponAngle = state switch
        {
            FrontierVisualState.Slash => MathHelper.Lerp(-1.9f, .45f,
                enemy.ActionProgress),
            FrontierVisualState.Pounce => -.18f,
            FrontierVisualState.PounceCrouch => -1.45f,
            FrontierVisualState.LowStab => .08f,
            FrontierVisualState.Recoil => -1.7f,
            _ => -.72f + cycle * .08f
        };
        Vector2 bladeDirection = new(
            MathF.Cos(weaponAngle), MathF.Sin(weaponAngle));
        Vector2 weaponHand = shoulder + bladeDirection * 15f;
        DrawLimb(anchor, shoulder, weaponHand, skin, flip, 5f);
        DrawJaggedBlade(anchor, weaponHand, bladeDirection, flip);

        Vector2 rearShoulder = chest + new Vector2(-7f, 1f);
        Vector2 clawHand = rearShoulder + new Vector2(
            state == FrontierVisualState.LowStab ? 17f : -5f,
            state == FrontierVisualState.LowStab ? 13f : 15f);
        DrawLimb(anchor, rearShoulder, clawHand, ImpSkinDark, flip, 5f);
        DrawHandClaws(anchor, clawHand, flip);

        if (state is FrontierVisualState.Run or FrontierVisualState.Pounce)
        {
            DrawBox(anchor, -20f - MathF.Abs(step), -5f, 4f, 3f,
                new Color(138, 104, 76, 120), flip);
            DrawBox(anchor, -28f - MathF.Abs(step), -8f, 3f, 2f,
                new Color(182, 81, 42, 95), flip);
        }
    }

    private void DrawImpHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip)
    {
        DrawBox(anchor, head.X - 9f, head.Y - 7f, 20f, 17f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 5f, 17f, 14f,
            skin, flip);
        DrawLineLocal(anchor, head + new Vector2(-5f, -5f),
            head + new Vector2(-11f, -15f), 5f, HornDark, flip);
        DrawLineLocal(anchor, head + new Vector2(-11f, -15f),
            head + new Vector2(-7f, -20f), 3f, Horn, flip);
        DrawLineLocal(anchor, head + new Vector2(5f, -5f),
            head + new Vector2(8f, -16f), 5f, HornDark, flip);
        DrawLineLocal(anchor, head + new Vector2(8f, -16f),
            head + new Vector2(13f, -18f), 3f, Horn, flip);
        DrawBox(anchor, head.X + 4f, head.Y - 1f, 3f, 2f,
            new Color(235, 119, 59, 210), flip);
        DrawBox(anchor, head.X + 7f, head.Y + 5f, 5f, 3f,
            ImpSkinDark, flip);
    }

    private void DrawArcher(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        bool flip,
        FrontierVisualState state,
        float elapsed)
    {
        float sway = MathF.Sin(elapsed * 2.4f) * 1.3f;
        float stride = state == FrontierVisualState.Run
            ? MathF.Sin(elapsed * 10f) * 7f : 0f;
        float hop = state == FrontierVisualState.Retreat
            ? -MathF.Sin(enemy.ActionProgress * MathF.PI) * 9f : 0f;
        float recoil = state == FrontierVisualState.Release ? -4f :
            state == FrontierVisualState.Stagger ? -9f : 0f;
        Color skin = enemy.IsHitFlashing
            ? new Color(210, 183, 154)
            : AshSkin;

        Vector2 hip = new(recoil, -37f + hop);
        Vector2 chest = hip + new Vector2(sway, -25f);
        Vector2 head = chest + new Vector2(1f, -19f);
        Vector2 rearFoot = new(-10f - stride, hop);
        Vector2 frontFoot = new(12f + stride, hop);
        Vector2 rearKnee = new(-7f + stride, -19f + hop);
        Vector2 frontKnee = new(8f - stride, -20f + hop);

        DrawTornCloth(anchor, hip, sway, flip);
        DrawLimb(anchor, rearFoot, rearKnee, AshSkinDark, flip, 6f);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-6f, 0f), skin, flip, 7f);
        DrawLimb(anchor, frontFoot, frontKnee, AshSkinDark, flip, 6f);
        DrawLimb(anchor, frontKnee, hip + new Vector2(6f, 0f), skin, flip, 7f);
        DrawClawFoot(anchor, rearFoot, flip);
        DrawClawFoot(anchor, frontFoot, flip);

        DrawBox(anchor, chest.X - 11f, chest.Y - 6f, 23f, 31f,
            Outline, flip);
        DrawBox(anchor, chest.X - 9f, chest.Y - 4f, 19f, 28f,
            AshSkinDark, flip);
        DrawBox(anchor, chest.X - 8f, chest.Y, 17f, 7f,
            MetalDark, flip);
        DrawBox(anchor, chest.X - 7f, chest.Y + 10f, 15f, 5f,
            Metal, flip);
        DrawLineLocal(anchor, chest + new Vector2(-9f, -3f),
            chest + new Vector2(8f, 22f), 3f, Cloth, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 7f, 11f, 7f,
            MetalDark, flip);
        DrawArcherHead(anchor, head, skin, flip);
        DrawBowAndArms(enemy, anchor, chest, flip, state);

        if (state == FrontierVisualState.Retreat)
        {
            DrawBox(anchor, 21f, -5f, 4f, 3f,
                new Color(139, 107, 78, 130), flip);
            DrawBox(anchor, 31f, -8f, 3f, 2f,
                new Color(195, 77, 39, 105), flip);
        }
    }

    private void DrawTornCloth(
        Vector2 anchor,
        Vector2 hip,
        float sway,
        bool flip)
    {
        DrawBox(anchor, hip.X - 13f, hip.Y - 2f, 27f, 6f,
            Leather, flip);
        DrawBox(anchor, hip.X - 15f - sway, hip.Y + 3f, 11f, 24f,
            ClothDark, flip);
        DrawBox(anchor, hip.X - 3f, hip.Y + 3f, 9f, 19f,
            Cloth, flip);
        DrawBox(anchor, hip.X + 8f + sway, hip.Y + 3f, 7f, 27f,
            ClothDark, flip);
    }

    private void DrawArcherHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip)
    {
        DrawBox(anchor, head.X - 9f, head.Y - 10f, 19f, 23f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 8f, 15f, 20f,
            skin, flip);
        DrawLineLocal(anchor, head + new Vector2(-5f, -8f),
            head + new Vector2(-15f, -28f), 6f, HornDark, flip);
        DrawLineLocal(anchor, head + new Vector2(-15f, -28f),
            head + new Vector2(-12f, -38f), 4f, Horn, flip);
        DrawLineLocal(anchor, head + new Vector2(5f, -8f),
            head + new Vector2(14f, -29f), 6f, HornDark, flip);
        DrawLineLocal(anchor, head + new Vector2(14f, -29f),
            head + new Vector2(11f, -39f), 4f, Horn, flip);
        DrawBox(anchor, head.X + 3f, head.Y - 2f, 3f, 2f,
            new Color(224, 105, 53, 190), flip);
    }

    private void DrawBowAndArms(
        DemonLandsEnemy enemy,
        Vector2 anchor,
        Vector2 chest,
        bool flip,
        FrontierVisualState state)
    {
        float draw = state == FrontierVisualState.Draw
            ? MathHelper.Lerp(2f, 20f, enemy.ActionProgress)
            : state == FrontierVisualState.Release ? 0f : 6f;
        float aimY = enemy.HellArcherAttack == HellArcherAttackKind.HighArcShot
            ? -12f : 3f;
        Vector2 bowGrip = chest + new Vector2(22f, aimY);
        Vector2 bowTop = bowGrip + new Vector2(5f, -27f);
        Vector2 bowBottom = bowGrip + new Vector2(5f, 27f);
        Vector2 stringHand = bowGrip + new Vector2(-draw, 0f);
        Vector2 rearShoulder = chest + new Vector2(-8f, 0f);
        Vector2 frontShoulder = chest + new Vector2(9f, 0f);

        DrawLimb(anchor, rearShoulder, stringHand, AshSkin, flip, 5f);
        DrawLimb(anchor, frontShoulder, bowGrip, AshSkinDark, flip, 5f);
        DrawLineLocal(anchor, bowTop, bowGrip, 7f, Outline, flip);
        DrawLineLocal(anchor, bowGrip, bowBottom, 7f, Outline, flip);
        DrawLineLocal(anchor, bowTop, bowGrip, 4f, Bone, flip);
        DrawLineLocal(anchor, bowGrip, bowBottom, 4f, Blackwood, flip);
        DrawLineLocal(anchor, bowTop, stringHand, 1.5f,
            new Color(198, 73, 42, 210), flip);
        DrawLineLocal(anchor, stringHand, bowBottom, 1.5f,
            new Color(198, 73, 42, 210), flip);
        DrawBox(anchor, bowGrip.X + 1f, bowGrip.Y - 3f, 5f, 6f,
            new Color(185, 71, 38, 160), flip);

        if (state == FrontierVisualState.Draw)
        {
            Vector2 arrowTail = stringHand + new Vector2(2f, 0f);
            Vector2 arrowTip = bowGrip + new Vector2(23f, 0f);
            DrawLineLocal(anchor, arrowTail, arrowTip, 2f, Blackwood, flip);
            DrawBox(anchor, arrowTip.X - 1f, arrowTip.Y - 3f, 6f, 6f,
                Ember, flip);
            DrawBox(anchor, arrowTip.X + 1f, arrowTip.Y - 1f, 3f, 3f,
                EmberCore, flip);
        }
    }

    private void DrawJaggedBlade(
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip)
    {
        Vector2 guard = hand + direction * 5f;
        Vector2 tip = guard + direction * 24f;
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLineLocal(anchor, hand - direction * 5f, guard, 5f,
            Leather, flip);
        DrawLineLocal(anchor, guard - normal * 6f, guard + normal * 6f,
            4f, MetalDark, flip);
        DrawLineLocal(anchor, guard, tip, 7f, Outline, flip);
        DrawLineLocal(anchor, guard + normal, tip + normal * 2f, 4f,
            Metal, flip);
        DrawLineLocal(anchor, tip - direction * 7f,
            tip - direction * 3f - normal * 4f, 3f, MetalDark, flip);
    }

    private void DrawClawFoot(Vector2 anchor, Vector2 foot, bool flip)
    {
        DrawLineLocal(anchor, foot + new Vector2(-5f, 0f),
            foot + new Vector2(7f, 0f), 5f, Outline, flip);
        DrawLineLocal(anchor, foot + new Vector2(5f, -1f),
            foot + new Vector2(11f, 1f), 2f, Horn, flip);
    }

    private void DrawHandClaws(Vector2 anchor, Vector2 hand, bool flip)
    {
        DrawLineLocal(anchor, hand, hand + new Vector2(7f, 2f), 2f,
            Horn, flip);
        DrawLineLocal(anchor, hand + new Vector2(0f, 2f),
            hand + new Vector2(6f, 6f), 2f, Horn, flip);
    }

    private void DrawDeath(DemonLandsEnemy enemy, Vector2 anchor, bool flip)
    {
        float progress = enemy.DeathPresentationProgress;
        float drop = MathHelper.SmoothStep(0f, 28f, progress);
        float settle = MathHelper.SmoothStep(0f, 1f, progress);
        Color skin = enemy.Type == EnemyType.ImpRaider
            ? ImpSkinDark : AshSkinDark;
        float length = enemy.Type == EnemyType.ImpRaider ? 45f : 68f;
        DrawLineLocal(anchor, new Vector2(-length * .42f, -9f + drop * .18f),
            new Vector2(length * .43f, -6f),
            enemy.Type == EnemyType.ImpRaider ? 16f : 20f,
            Outline, flip);
        DrawLineLocal(anchor, new Vector2(-length * .40f, -9f + drop * .18f),
            new Vector2(length * .40f, -6f),
            enemy.Type == EnemyType.ImpRaider ? 12f : 16f,
            skin, flip);
        DrawBox(anchor, length * .31f, -13f + settle * 5f,
            17f, 13f, skin, flip);
        DrawBox(anchor, -length * .5f - settle * 8f, -5f, 4f, 3f,
            new Color(143, 101, 71, 120), flip);
        DrawBox(anchor, length * .42f + settle * 9f, -4f, 3f, 2f,
            new Color(193, 76, 38, 100), flip);
    }

    private static FrontierVisualState SelectState(DemonLandsEnemy enemy)
    {
        if (!enemy.IsAlive)
            return FrontierVisualState.Death;
        if (enemy.IsStaggered)
            return FrontierVisualState.Stagger;
        return enemy.FrontierCombatState switch
        {
            DemonFrontierCombatState.ImpPounceCrouch =>
                FrontierVisualState.PounceCrouch,
            DemonFrontierCombatState.ImpPounceActive =>
                FrontierVisualState.Pounce,
            DemonFrontierCombatState.ImpSlashActive =>
                FrontierVisualState.Slash,
            DemonFrontierCombatState.ImpFrenzyWindup or
            DemonFrontierCombatState.ImpFrenzyActive =>
                FrontierVisualState.LowStab,
            DemonFrontierCombatState.ImpSlashRecovery or
            DemonFrontierCombatState.ImpPounceRecovery or
            DemonFrontierCombatState.ImpFrenzyRecovery =>
                FrontierVisualState.Recoil,
            DemonFrontierCombatState.ArcherDraw => FrontierVisualState.Draw,
            DemonFrontierCombatState.ArcherRelease => FrontierVisualState.Release,
            DemonFrontierCombatState.ArcherRetreat => FrontierVisualState.Retreat,
            DemonFrontierCombatState.ArcherReposition => FrontierVisualState.Run,
            _ => MathF.Abs(enemy.VisualVelocityX) > 8f
                ? FrontierVisualState.Run
                : FrontierVisualState.Idle
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
        public FrontierVisualState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, FrontierVisualState state)
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

    private enum FrontierVisualState
    {
        Idle,
        Run,
        PounceCrouch,
        Pounce,
        Slash,
        LowStab,
        Draw,
        Release,
        Retreat,
        Recoil,
        Stagger,
        Death
    }
}
