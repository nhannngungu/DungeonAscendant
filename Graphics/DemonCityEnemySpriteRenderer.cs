using System;
using System.Runtime.CompilerServices;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonAscendant.Graphics;

/// <summary>
/// Articulated code-only silhouettes for Demon City's standard inhabitants.
/// Poses are authored facing right and mirrored as a whole.
/// </summary>
public sealed class DemonCityEnemySpriteRenderer
{
    public static readonly Vector2 EnforcerVisualSize = new(108f, 94f);
    public static readonly Vector2 KeeperVisualSize = new(112f, 106f);
    public static readonly Vector2 StalkerVisualSize = new(126f, 94f);
    public static readonly Vector2 CollectorVisualSize = new(146f, 116f);

    private static readonly Color Outline = new(20, 18, 22);
    private static readonly Color DemonSkin = new(116, 70, 67);
    private static readonly Color DemonSkinLight = new(154, 91, 82);
    private static readonly Color LeatherDark = new(36, 31, 34);
    private static readonly Color Leather = new(58, 48, 49);
    private static readonly Color IronDark = new(48, 48, 53);
    private static readonly Color Iron = new(86, 84, 88);
    private static readonly Color IronLight = new(132, 127, 126);
    private static readonly Color Crimson = new(119, 39, 43);
    private static readonly Color Boot = new(31, 27, 29);
    private static readonly Color RobeDark = new(38, 31, 45);
    private static readonly Color Robe = new(61, 47, 70);
    private static readonly Color RobeEdge = new(88, 54, 69);
    private static readonly Color BronzeDark = new(72, 53, 43);
    private static readonly Color Bronze = new(124, 91, 60);
    private static readonly Color SoulDim = new(91, 87, 135);
    private static readonly Color SoulBright = new(174, 155, 211);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly ConditionalWeakTable<DemonCityEnemy, AnimationClock>
        _clocks = new();

    public DemonCityEnemySpriteRenderer(
        SpriteBatch spriteBatch,
        Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Draw(DemonCityEnemy enemy, GameTime gameTime)
    {
        AnimationClock clock = _clocks.GetValue(
            enemy, static _ => new AnimationClock());
        VisualState state = SelectState(enemy);
        clock.Update(gameTime, state);
        Vector2 anchor = new(enemy.Position.X, enemy.Bounds.Bottom);
        bool committed = enemy.CombatState != DemonCityCombatState.Ready;
        bool flip = committed
            ? enemy.AttackFacesLeft
            : enemy.Facing == EnemyFacingDirection.Left;

        float shadowWidth = enemy.Type switch
        {
            EnemyType.DemonCityEnforcer => 50f,
            EnemyType.ChainCollector => 46f,
            _ => 42f
        };
        DrawBox(anchor, -shadowWidth / 2f, -3f, shadowWidth, 5f,
            new Color(9, 8, 11, 145), flip);

        if (!enemy.IsAlive)
        {
            switch (enemy.Type)
            {
                case EnemyType.DemonCityEnforcer:
                    DrawEnforcerDeath(anchor, flip, enemy.DeathPresentationProgress);
                    break;
                case EnemyType.SoulLanternKeeper:
                    DrawKeeperDeath(anchor, flip, enemy.DeathPresentationProgress);
                    break;
                case EnemyType.AshbladeStalker:
                    DrawStalkerDeath(anchor, flip, enemy.DeathPresentationProgress);
                    break;
                case EnemyType.ChainCollector:
                    DrawCollectorDeath(anchor, flip, enemy.DeathPresentationProgress);
                    break;
            }
            return;
        }

        if (enemy.Type == EnemyType.DemonCityEnforcer)
            DrawEnforcer(enemy, anchor, flip, state,
                CreatePose(enemy, state, clock.Elapsed));
        else if (enemy.Type == EnemyType.SoulLanternKeeper)
            DrawKeeper(enemy, anchor, flip, state,
                CreatePose(enemy, state, clock.Elapsed), clock.Elapsed);
        else if (enemy.Type == EnemyType.AshbladeStalker)
            DrawStalker(enemy, anchor, flip, state, clock.Elapsed);
        else
            DrawCollector(enemy, anchor, flip, state, clock.Elapsed);
    }

    private void DrawEnforcer(
        DemonCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        VisualState state,
        Pose pose)
    {
        Color skin = enemy.IsHitFlashing
            ? new Color(210, 154, 132)
            : DemonSkin;
        Vector2 hip = new(pose.Lean, -37f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -60f + pose.Crouch + pose.Bob);
        Vector2 rearFoot = new(-10f - pose.Step, 0f);
        Vector2 frontFoot = new(11f + pose.Step, 0f);
        Vector2 rearKnee = new(-8f + pose.Step * .55f, -20f + pose.Crouch);
        Vector2 frontKnee = new(9f - pose.Step * .55f, -21f + pose.Crouch);

        DrawLimb(anchor, rearFoot, rearKnee, LeatherDark, 8f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-7f, 0f),
            Leather, 9f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, LeatherDark, 8f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(7f, 0f),
            Leather, 9f, flip);
        DrawLine(anchor, rearFoot + new Vector2(-7f, 0f),
            rearFoot + new Vector2(6f, 0f), 6f, Boot, flip);
        DrawLine(anchor, frontFoot + new Vector2(-5f, 0f),
            frontFoot + new Vector2(9f, 0f), 6f, Boot, flip);
        DrawBox(anchor, rearKnee.X - 6f, rearKnee.Y - 5f, 12f, 10f,
            IronDark, flip);
        DrawBox(anchor, frontKnee.X - 6f, frontKnee.Y - 5f, 12f, 10f,
            Iron, flip);

        // Split coat tails preserve a readable pelvis and independently moving legs.
        DrawLine(anchor, hip + new Vector2(-8f, 0f),
            hip + new Vector2(-12f - pose.CoatTrail, 25f),
            12f, LeatherDark, flip);
        DrawLine(anchor, hip + new Vector2(7f, 0f),
            hip + new Vector2(10f - pose.CoatTrail * .25f, 24f),
            11f, Leather, flip);
        DrawBox(anchor, hip.X - 16f, hip.Y - 5f, 32f, 8f,
            Outline, flip);
        DrawBox(anchor, hip.X - 14f, hip.Y - 4f, 28f, 6f,
            new Color(83, 57, 45), flip);
        DrawBox(anchor, hip.X - 2f, hip.Y - 5f, 5f, 7f,
            Bronze, flip);
        DrawLine(anchor, hip + new Vector2(-13f, 2f),
            hip + new Vector2(-18f, 17f), 2.5f, BronzeDark, flip);
        DrawBox(anchor, hip.X - 21f, hip.Y + 14f, 6f, 6f,
            Bronze, flip);

        DrawBox(anchor, chest.X - 17f, chest.Y - 8f, 34f, 29f,
            Outline, flip);
        DrawBox(anchor, chest.X - 15f, chest.Y - 6f, 30f, 26f,
            LeatherDark, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 3f, 22f, 19f,
            IronDark, flip);
        DrawLine(anchor, chest + new Vector2(-9f, -2f),
            chest + new Vector2(9f, 14f), 3f, Iron, flip);
        DrawBox(anchor, chest.X + 2f, chest.Y + 3f, 6f, 8f,
            Crimson, flip);

        Vector2 neck = chest + new Vector2(pose.HeadShift, -12f);
        Vector2 head = neck + new Vector2(1f, -10f);
        DrawLine(anchor, chest + new Vector2(0f, -6f), neck,
            8f, skin, flip);
        DrawDemonHead(anchor, head, skin, flip, masked: true);

        Vector2 shieldShoulder = chest + new Vector2(13f, -3f);
        Vector2 shieldElbow = shieldShoulder + new Vector2(
            7f + pose.ShieldPush * .45f, 10f);
        Vector2 shieldHand = shieldElbow + new Vector2(
            6f + pose.ShieldPush * .55f, 10f - pose.ShieldRaise);
        DrawLimb(anchor, shieldShoulder, shieldElbow, skin, 8f, flip);
        DrawLimb(anchor, shieldElbow, shieldHand, IronDark, 7f, flip);
        DrawForearmShield(anchor, shieldHand, pose.ShieldRaise, flip);

        Vector2 weaponShoulder = chest + new Vector2(-14f, -2f);
        Vector2 weaponDirection = new(
            MathF.Cos(pose.WeaponAngle),
            MathF.Sin(pose.WeaponAngle));
        Vector2 weaponElbow = weaponShoulder + weaponDirection * 13f;
        Vector2 weaponHand = weaponElbow + weaponDirection * 13f;
        DrawBox(anchor, weaponShoulder.X - 9f, weaponShoulder.Y - 7f,
            17f, 11f, Outline, flip);
        DrawBox(anchor, weaponShoulder.X - 8f, weaponShoulder.Y - 6f,
            15f, 8f, Iron, flip);
        DrawLimb(anchor, weaponShoulder, weaponElbow, skin, 9f, flip);
        DrawLimb(anchor, weaponElbow, weaponHand, Leather, 8f, flip);
        DrawCleaver(anchor, weaponHand, weaponDirection, flip);

        if (state == VisualState.Hurt)
            DrawBox(anchor, chest.X + 22f, chest.Y - 5f, 4f, 3f,
                new Color(151, 48, 50, 170), flip);
    }

    private void DrawKeeper(
        DemonCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        VisualState state,
        Pose pose,
        float elapsed)
    {
        Color skin = enemy.IsHitFlashing
            ? new Color(194, 151, 163)
            : new Color(104, 72, 91);
        Vector2 hip = new(pose.Lean, -39f + pose.Crouch);
        Vector2 chest = new(
            pose.Lean + pose.UpperLean,
            -65f + pose.Crouch + pose.Bob);
        Vector2 rearFoot = new(-7f - pose.Step, 0f);
        Vector2 frontFoot = new(8f + pose.Step, 0f);
        Vector2 rearKnee = new(-6f + pose.Step * .5f, -21f + pose.Crouch);
        Vector2 frontKnee = new(7f - pose.Step * .5f, -22f + pose.Crouch);

        DrawLimb(anchor, rearFoot, rearKnee, RobeDark, 6f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f),
            Robe, 7f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, RobeDark, 6f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 0f),
            Robe, 7f, flip);
        DrawLine(anchor, rearFoot + new Vector2(-5f, 0f),
            rearFoot + new Vector2(5f, 0f), 5f, Boot, flip);
        DrawLine(anchor, frontFoot + new Vector2(-4f, 0f),
            frontFoot + new Vector2(7f, 0f), 5f, Boot, flip);

        // Layered robe panels are separate, asymmetric masses rather than one block.
        DrawLine(anchor, chest + new Vector2(-11f, 11f),
            hip + new Vector2(-16f - pose.CoatTrail, 35f),
            18f, RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(8f, 12f),
            hip + new Vector2(13f, 36f), 16f, Robe, flip);
        DrawLine(anchor, hip + new Vector2(-3f, 0f),
            hip + new Vector2(-2f, 35f), 5f, RobeEdge, flip);
        DrawBox(anchor, hip.X - 13f, hip.Y - 4f, 26f, 7f,
            BronzeDark, flip);
        DrawBox(anchor, hip.X - 2f, hip.Y - 5f, 5f, 8f,
            Bronze, flip);

        DrawBox(anchor, chest.X - 13f, chest.Y - 7f, 26f, 29f,
            Outline, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 5f, 22f, 26f,
            RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(-8f, -3f),
            chest + new Vector2(7f, 17f), 4f, RobeEdge, flip);
        DrawBox(anchor, chest.X + 7f, chest.Y - 8f, 10f, 6f,
            BronzeDark, flip);

        Vector2 neck = chest + new Vector2(pose.HeadShift, -11f);
        Vector2 head = neck + new Vector2(0f, -11f);
        DrawLine(anchor, chest + new Vector2(0f, -6f), neck,
            6f, skin, flip);
        DrawKeeperHead(anchor, head, skin, flip);

        Vector2 lanternShoulder = chest + new Vector2(12f, -2f);
        Vector2 lanternElbow = lanternShoulder + new Vector2(
            5f + pose.LanternLift * .25f,
            13f - pose.LanternLift * .45f);
        Vector2 lanternHand = lanternElbow + new Vector2(
            5f + pose.LanternLift * .30f,
            14f - pose.LanternLift * .55f);
        DrawLimb(anchor, lanternShoulder, lanternElbow, skin, 6f, flip);
        DrawLimb(anchor, lanternElbow, lanternHand, RobeEdge, 6f, flip);

        float sway = MathF.Sin(elapsed * 2.2f) *
            (state == VisualState.Walk ? 3f : 1.4f);
        Vector2 lanternTop = lanternHand + new Vector2(
            pose.LanternForward,
            5f + sway);
        DrawLantern(
            anchor,
            lanternTop,
            flip,
            enemy.LanternChargeRatio,
            state == VisualState.BurstActive,
            elapsed);

        Vector2 gestureShoulder = chest + new Vector2(-12f, -1f);
        Vector2 gestureElbow = gestureShoulder + new Vector2(
            -7f,
            12f - pose.GestureRaise * .35f);
        Vector2 gestureHand = gestureElbow + new Vector2(
            8f + pose.GestureOpen,
            7f - pose.GestureRaise * .65f);
        DrawLimb(anchor, gestureShoulder, gestureElbow, Robe, 6f, flip);
        DrawLimb(anchor, gestureElbow, gestureHand, skin, 5f, flip);
        DrawLine(anchor, gestureHand, gestureHand + new Vector2(7f, -3f),
            2f, DemonSkinLight, flip);
        DrawLine(anchor, gestureHand, gestureHand + new Vector2(6f, 2f),
            2f, DemonSkinLight, flip);

        if (state == VisualState.BurstActive)
        {
            float radius = MathHelper.Lerp(24f, 67f, enemy.ActionProgress);
            Color pulse = new(141, 117, 180,
                (int)(180f * (1f - enemy.ActionProgress)));
            DrawDiamond(anchor, new Vector2(0f, -39f), radius,
                radius * .60f, 3f, pulse, flip);
        }
    }

    private void DrawStalker(
        DemonCityEnemy enemy,
        Vector2 anchor,
        bool flip,
        VisualState state,
        float elapsed)
    {
        float p = enemy.CombatState == DemonCityCombatState.Ready
            ? 0f : enemy.ActionProgress;
        float wave = MathF.Sin(elapsed * MathHelper.TwoPi * 1.2f);
        float crouch = 4f;
        float lean = 5f + wave * .5f;
        float step = MathF.Abs(enemy.VisualVelocityX) > 1f ? wave * 7f : wave * 1.2f;
        float frontAngle = .72f;
        float rearAngle = 2.45f;
        if (enemy.CombatState == DemonCityCombatState.StalkerHunterStep)
        { lean = 10f; crouch = 7f; step = 9f; }
        else if (enemy.CombatState == DemonCityCombatState.StalkerAshCutWindup)
        { crouch = 7f; lean = -2f; frontAngle = MathHelper.Lerp(.7f, -1.8f, p); }
        else if (enemy.CombatState == DemonCityCombatState.StalkerAshCutActive)
        { lean = 12f; frontAngle = MathHelper.Lerp(-1.8f, .85f, p); step = 5f; }
        else if (enemy.CombatState == DemonCityCombatState.StalkerTwinRipWindup)
        { crouch = 7f; frontAngle = MathHelper.Lerp(.7f, -1.35f, p); rearAngle = MathHelper.Lerp(2.45f, -2.7f, p); }
        else if (enemy.CombatState == DemonCityCombatState.StalkerTwinRipFirst)
        { lean = 11f; frontAngle = MathHelper.Lerp(-1.35f, .7f, p); rearAngle = -2.7f; }
        else if (enemy.CombatState == DemonCityCombatState.StalkerTwinRipTurn)
        { lean = 2f; frontAngle = .85f; rearAngle = MathHelper.Lerp(-2.7f, -1.2f, p); }
        else if (enemy.CombatState == DemonCityCombatState.StalkerTwinRipSecond)
        { lean = 10f; frontAngle = .95f; rearAngle = MathHelper.Lerp(-1.2f, 1.15f, p); }
        else if (enemy.CombatState == DemonCityCombatState.StalkerCrossingWindup)
        { crouch = MathHelper.Lerp(5f, 12f, p); lean = -1f; frontAngle = MathHelper.Lerp(.7f, -1.25f, p); rearAngle = MathHelper.Lerp(2.45f, -2.0f, p); }
        else if (enemy.CombatState == DemonCityCombatState.StalkerCrossingActive)
        { crouch = 8f; lean = 14f; step = 8f; frontAngle = MathHelper.Lerp(-1.25f, .35f, p); rearAngle = MathHelper.Lerp(-2f, .75f, p); }
        else if (state == VisualState.Recovery)
        { crouch = MathHelper.Lerp(9f, 4f, p); lean = MathHelper.Lerp(12f, 5f, p); frontAngle = 1.2f; rearAngle = 2.0f; }
        else if (state is VisualState.Hurt or VisualState.Stagger)
        { crouch = state == VisualState.Stagger ? 11f : 7f; lean = -8f; frontAngle = 1.8f; rearAngle = 2.8f; }

        Color skin = enemy.IsHitFlashing ? new Color(218, 151, 126) : new Color(128, 73, 64);
        Vector2 hip = new(lean - 2f, -35f + crouch);
        Vector2 chest = new(lean + 2f, -58f + crouch + MathF.Max(0f, wave) * .6f);
        Vector2 rearFoot = new(-11f - step, 0f);
        Vector2 frontFoot = new(12f + step, 0f);
        Vector2 rearKnee = new(-8f + step * .45f, -19f + crouch);
        Vector2 frontKnee = new(8f - step * .45f, -20f + crouch);
        DrawLimb(anchor, rearFoot, rearKnee, LeatherDark, 6f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f), new Color(74, 70, 68), 7f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, Boot, 6f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(6f, 0f), new Color(91, 82, 76), 7f, flip);
        DrawLine(anchor, rearFoot + new Vector2(-5f, 0f), rearFoot + new Vector2(7f, 0f), 5f, Boot, flip);
        DrawLine(anchor, frontFoot + new Vector2(-4f, 0f), frontFoot + new Vector2(9f, 0f), 5f, Boot, flip);
        DrawLine(anchor, hip + new Vector2(-7f, 1f), hip + new Vector2(-13f - wave, 24f), 9f, new Color(73, 72, 72), flip);
        DrawLine(anchor, hip + new Vector2(6f, 1f), hip + new Vector2(12f, 20f), 7f, Crimson, flip);
        DrawBox(anchor, hip.X - 14f, hip.Y - 4f, 28f, 7f, LeatherDark, flip);
        DrawBox(anchor, hip.X - 18f, hip.Y + 2f, 7f, 9f, new Color(77, 55, 43), flip);
        DrawBox(anchor, chest.X - 13f, chest.Y - 7f, 26f, 28f, Outline, flip);
        DrawBox(anchor, chest.X - 11f, chest.Y - 5f, 22f, 25f, LeatherDark, flip);
        DrawLine(anchor, chest + new Vector2(-9f, -2f), chest + new Vector2(7f, 18f), 4f, Crimson, flip);
        DrawBox(anchor, chest.X - 17f, chest.Y - 5f, 9f, 9f, IronDark, flip);
        Vector2 head = chest + new Vector2(3f, -20f);
        DrawLine(anchor, chest + new Vector2(1f, -5f), head + new Vector2(-1f, 7f), 6f, skin, flip);
        DrawBox(anchor, head.X - 8f, head.Y - 8f, 17f, 17f, Outline, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 7f, 14f, 15f, new Color(43, 37, 39), flip);
        DrawLine(anchor, head + new Vector2(-4f, -6f), head + new Vector2(-10f, -13f), 3f, IronDark, flip);
        DrawLine(anchor, head + new Vector2(4f, -6f), head + new Vector2(8f, -14f), 3f, IronDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 3f, 2f, new Color(220, 82, 53), flip);

        DrawStalkerArmAndBlade(anchor, chest + new Vector2(12f, -1f), frontAngle, skin, Crimson, flip, false);
        DrawStalkerArmAndBlade(anchor, chest + new Vector2(-12f, 1f), rearAngle, skin, Leather, flip, true);
        if (enemy.CombatState == DemonCityCombatState.StalkerCrossingActive)
        {
            DrawLine(anchor, new Vector2(13f, -66f), new Vector2(72f, -19f), 2f,
                new Color(151, 111, 92, 105), flip);
            DrawLine(anchor, new Vector2(15f, -20f), new Vector2(72f, -64f), 2f,
                new Color(151, 111, 92, 105), flip);
        }
    }

    private void DrawStalkerArmAndBlade(Vector2 anchor, Vector2 shoulder,
        float angle, Color skin, Color sleeve, bool flip, bool rear)
    {
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 elbow = shoulder + direction * 12f + new Vector2(0f, rear ? 3f : 0f);
        Vector2 hand = elbow + direction * 12f;
        DrawLimb(anchor, shoulder, elbow, sleeve, rear ? 6f : 7f, flip);
        DrawLimb(anchor, elbow, hand, skin, 6f, flip);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(anchor, hand - normal * 5f, hand + normal * 5f, 3f, Crimson, flip);
        Vector2 bladeMid = hand + direction * 16f + normal * 5f;
        Vector2 bladeTip = hand + direction * 31f + normal * 1f;
        DrawLine(anchor, hand + direction * 3f, bladeMid, 6f, Outline, flip);
        DrawLine(anchor, bladeMid, bladeTip, 6f, Outline, flip);
        DrawLine(anchor, hand + direction * 4f, bladeMid, 3f, Iron, flip);
        DrawLine(anchor, bladeMid, bladeTip, 3f, IronLight, flip);
    }

    private void DrawCollector(DemonCityEnemy enemy, Vector2 anchor, bool flip,
        VisualState state, float elapsed)
    {
        float p = enemy.CombatState == DemonCityCombatState.Ready ? 0f : enemy.ActionProgress;
        float wave = MathF.Sin(elapsed * MathHelper.TwoPi * .62f);
        float crouch = state == VisualState.Stagger ? 10f : 0f;
        float lean = state is VisualState.Hurt or VisualState.Stagger ? -7f : 1f;
        float step = MathF.Abs(enemy.VisualVelocityX) > 1f ? wave * 4f : 0f;
        if (enemy.CombatState == DemonCityCombatState.CollectorRetreat) { lean = -5f; step = -5f; }
        if (enemy.HookConnected && enemy.CombatState == DemonCityCombatState.CollectorHookResolve) { lean = -7f; crouch = 3f; }
        if (enemy.CombatState is DemonCityCombatState.CollectorSweepGather or DemonCityCombatState.CollectorHookGather) { lean = -4f; crouch = 3f; }
        if (enemy.CombatState == DemonCityCombatState.CollectorSweepActive) { lean = 8f; step = 4f; }
        if (enemy.CombatState == DemonCityCombatState.CollectorDropActive) { lean = 7f; crouch = 5f; }
        Vector2 hip = new(lean, -42f + crouch);
        Vector2 chest = new(lean - 2f, -73f + crouch + MathF.Max(0f, wave) * .5f);
        Color skin = enemy.IsHitFlashing ? new Color(201, 157, 148) : new Color(94, 65, 70);
        Vector2 rearFoot = new(-8f - step, 0f), frontFoot = new(9f + step, 0f);
        Vector2 rearKnee = new(-6f + step * .4f, -23f + crouch), frontKnee = new(7f - step * .4f, -24f + crouch);
        DrawLimb(anchor, rearFoot, rearKnee, Boot, 6f, flip);
        DrawLimb(anchor, rearKnee, hip + new Vector2(-5f, 0f), RobeDark, 7f, flip);
        DrawLimb(anchor, frontFoot, frontKnee, Boot, 6f, flip);
        DrawLimb(anchor, frontKnee, hip + new Vector2(5f, 0f), Robe, 7f, flip);
        DrawLine(anchor, chest + new Vector2(-12f, 11f), hip + new Vector2(-15f - wave, 39f), 17f, RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(9f, 12f), hip + new Vector2(13f, 38f), 15f, new Color(55, 43, 57), flip);
        DrawBox(anchor, hip.X - 14f, hip.Y - 4f, 28f, 7f, BronzeDark, flip);
        DrawBox(anchor, chest.X - 14f, chest.Y - 8f, 28f, 35f, Outline, flip);
        DrawBox(anchor, chest.X - 12f, chest.Y - 6f, 24f, 31f, RobeDark, flip);
        DrawLine(anchor, chest + new Vector2(-9f, -3f), chest + new Vector2(8f, 20f), 4f, Leather, flip);
        DrawBox(anchor, chest.X + 7f, chest.Y - 8f, 11f, 7f, IronDark, flip);
        Vector2 head = chest + new Vector2(-1f, -22f);
        DrawBox(anchor, head.X - 11f, head.Y - 9f, 22f, 20f, Outline, flip);
        DrawBox(anchor, head.X - 9f, head.Y - 8f, 18f, 18f, RobeDark, flip);
        DrawBox(anchor, head.X - 6f, head.Y - 3f, 13f, 11f, IronDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y, 3f, 2f, SoulDim, flip);

        float cageSway = wave * 2f;
        DrawSmallCage(anchor, hip + new Vector2(-18f, 7f + cageSway), flip, false);
        DrawSmallCage(anchor, chest + new Vector2(-17f, 9f - cageSway * .5f), flip, true);

        Vector2 shoulder = chest + new Vector2(13f, 0f);
        Vector2 elbow = shoulder + new Vector2(8f, 13f);
        Vector2 hand = elbow + new Vector2(7f, 11f);
        if (enemy.CombatState is DemonCityCombatState.CollectorHookAim or DemonCityCombatState.CollectorHookExtend or DemonCityCombatState.CollectorHookResolve)
        { elbow = shoulder + new Vector2(9f, -5f); hand = elbow + new Vector2(12f, 1f); }
        else if (enemy.CombatState == DemonCityCombatState.CollectorSweepGather)
        { elbow = shoulder + new Vector2(-10f, -3f); hand = elbow + new Vector2(-12f, 6f); }
        else if (enemy.CombatState == DemonCityCombatState.CollectorSweepActive)
        { elbow = shoulder + new Vector2(12f, 2f); hand = elbow + new Vector2(14f, 5f); }
        else if (enemy.CombatState is DemonCityCombatState.CollectorDropWindup or DemonCityCombatState.CollectorDropActive)
        { elbow = shoulder + new Vector2(3f, -15f); hand = elbow + new Vector2(10f, -8f + p * 22f); }
        DrawLimb(anchor, shoulder, elbow, Robe, 7f, flip);
        DrawLimb(anchor, elbow, hand, skin, 6f, flip);
        Vector2 spareShoulder = chest + new Vector2(-12f, 2f);
        Vector2 spareHand = spareShoulder + new Vector2(-4f, 24f);
        DrawLimb(anchor, spareShoulder, spareShoulder + new Vector2(-7f, 12f), RobeDark, 6f, flip);
        DrawLimb(anchor, spareShoulder + new Vector2(-7f, 12f), spareHand, skin, 5f, flip);
        DrawChainForCollector(enemy, anchor, hand, spareHand, flip, elapsed);
    }

    private void DrawChainForCollector(DemonCityEnemy enemy, Vector2 anchor,
        Vector2 hand, Vector2 spareHand, bool flip, float elapsed)
    {
        float p = enemy.ActionProgress;
        Vector2 end, control;
        if (enemy.CombatState is DemonCityCombatState.CollectorHookExtend or
            DemonCityCombatState.CollectorHookResolve or DemonCityCombatState.CollectorHookRetract)
        {
            Vector2 target = enemy.HookTargetOffset;
            if (flip) target.X = -target.X;
            end = new Vector2(target.X, target.Y) * enemy.HookExtensionRatio;
            end += hand * (1f - enemy.HookExtensionRatio);
            control = Vector2.Lerp(hand, end, .50f) + new Vector2(0f, 12f * (1f - enemy.HookExtensionRatio));
        }
        else if (enemy.CombatState == DemonCityCombatState.CollectorSweepActive)
        {
            float angle = MathHelper.Lerp(-2.5f, .28f, p);
            end = hand + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 132f;
            control = hand + new Vector2(48f, 38f - p * 58f);
        }
        else if (enemy.CombatState == DemonCityCombatState.CollectorSweepGather)
        { end = hand + new Vector2(-42f, 28f); control = hand + new Vector2(-15f, 45f); }
        else if (enemy.CombatState is DemonCityCombatState.CollectorDropWindup or DemonCityCombatState.CollectorDropActive)
        { end = hand + new Vector2(25f, enemy.CombatState == DemonCityCombatState.CollectorDropActive ? 78f : 28f); control = hand + new Vector2(42f, 12f); }
        else
        { float sway = MathF.Sin(elapsed * 1.7f) * 5f; end = hand + new Vector2(18f + sway, 50f); control = hand + new Vector2(-4f, 34f); }
        DrawChainCurve(anchor, hand, control, end, flip);
        DrawHook(anchor, end, flip);
        DrawChainCurve(anchor, spareHand, spareHand + new Vector2(-15f, 16f), spareHand + new Vector2(2f, 29f), flip, 5);
    }

    private void DrawChainCurve(Vector2 anchor, Vector2 start, Vector2 control,
        Vector2 end, bool flip, int segments = 12)
    {
        Vector2 prior = start;
        for (int index = 1; index <= segments; index++)
        {
            float t = index / (float)segments, inv = 1f - t;
            Vector2 point = inv * inv * start + 2f * inv * t * control + t * t * end;
            DrawLine(anchor, prior, point, 4f, Outline, flip);
            DrawLine(anchor, prior, point, 2f, IronLight, flip);
            if ((index & 1) == 0)
                DrawBox(anchor, point.X - 2f, point.Y - 2f, 4f, 4f, Iron, flip);
            prior = point;
        }
    }

    private void DrawHook(Vector2 anchor, Vector2 end, bool flip)
    {
        DrawLine(anchor, end, end + new Vector2(11f, 7f), 5f, Outline, flip);
        DrawLine(anchor, end, end + new Vector2(10f, 6f), 3f, IronLight, flip);
        DrawLine(anchor, end + new Vector2(10f, 6f), end + new Vector2(5f, 15f), 5f, Outline, flip);
        DrawLine(anchor, end + new Vector2(10f, 6f), end + new Vector2(6f, 14f), 3f, Iron, flip);
    }

    private void DrawSmallCage(Vector2 anchor, Vector2 top, bool flip, bool dim)
    {
        DrawLine(anchor, top, top + new Vector2(0f, 5f), 2f, Bronze, flip);
        DrawBox(anchor, top.X - 6f, top.Y + 4f, 12f, 16f, Outline, flip);
        DrawBox(anchor, top.X - 4f, top.Y + 6f, 8f, 12f, BronzeDark, flip);
        DrawLine(anchor, top + new Vector2(0f, 6f), top + new Vector2(0f, 18f), 2f, Bronze, flip);
        DrawBox(anchor, top.X - 2f, top.Y + 10f, 4f, 4f, dim ? SoulDim : SoulBright, flip);
    }

    private void DrawDemonHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip,
        bool masked)
    {
        DrawBox(anchor, head.X - 9f, head.Y - 8f, 18f, 18f,
            Outline, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 7f, 15f, 16f,
            skin, flip);
        DrawLine(anchor, head + new Vector2(-5f, -6f),
            head + new Vector2(-11f, -14f), 4f, IronDark, flip);
        DrawLine(anchor, head + new Vector2(4f, -6f),
            head + new Vector2(8f, -15f), 4f, IronDark, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 2f, 4f, 3f,
            new Color(226, 87, 63), flip);
        if (masked)
        {
            DrawBox(anchor, head.X - 8f, head.Y + 2f, 16f, 8f,
                IronDark, flip);
            DrawLine(anchor, head + new Vector2(-6f, 4f),
                head + new Vector2(6f, 4f), 2f, Iron, flip);
        }
    }

    private void DrawKeeperHead(
        Vector2 anchor,
        Vector2 head,
        Color skin,
        bool flip)
    {
        DrawBox(anchor, head.X - 13f, head.Y - 11f, 25f, 22f,
            Outline, flip);
        DrawBox(anchor, head.X - 11f, head.Y - 10f, 22f, 20f,
            RobeDark, flip);
        DrawBox(anchor, head.X - 7f, head.Y - 5f, 14f, 14f,
            skin, flip);
        DrawBox(anchor, head.X + 2f, head.Y - 1f, 3f, 3f,
            SoulBright, flip);
        DrawLine(anchor, head + new Vector2(-8f, -9f),
            head + new Vector2(0f, -17f), 3f, BronzeDark, flip);
        DrawLine(anchor, head + new Vector2(0f, -17f),
            head + new Vector2(8f, -9f), 3f, Bronze, flip);
    }

    private void DrawForearmShield(
        Vector2 anchor,
        Vector2 hand,
        float raise,
        bool flip)
    {
        float y = hand.Y - 12f - raise * .25f;
        DrawBox(anchor, hand.X - 5f, y, 15f, 25f, Outline, flip);
        DrawBox(anchor, hand.X - 3f, y + 2f, 11f, 21f,
            IronDark, flip);
        DrawLine(anchor, new Vector2(hand.X + 2f, y + 3f),
            new Vector2(hand.X + 2f, y + 21f), 3f, Iron, flip);
        DrawBox(anchor, hand.X, y + 8f, 5f, 6f, Crimson, flip);
    }

    private void DrawCleaver(
        Vector2 anchor,
        Vector2 hand,
        Vector2 direction,
        bool flip)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 bladeBase = hand + direction * 8f;
        Vector2 tip = bladeBase + direction * 31f;
        DrawLine(anchor, hand - direction * 8f, bladeBase,
            5f, new Color(76, 49, 39), flip);
        DrawLine(anchor, bladeBase - normal * 7f,
            bladeBase + normal * 7f, 5f, IronDark, flip);
        DrawLine(anchor, bladeBase, tip, 13f, Outline, flip);
        DrawLine(anchor, bladeBase + normal, tip + normal * 2f,
            9f, Iron, flip);
        DrawLine(anchor, tip - direction * 7f + normal * 5f,
            tip + direction * 2f, 3f, IronLight, flip);
    }

    private void DrawLantern(
        Vector2 anchor,
        Vector2 top,
        bool flip,
        float charge,
        bool bursting,
        float elapsed)
    {
        DrawLine(anchor, top + new Vector2(-5f, 0f),
            top + new Vector2(-5f, 8f), 3f, Bronze, flip);
        DrawLine(anchor, top + new Vector2(-5f, 0f),
            top + new Vector2(5f, 0f), 3f, Bronze, flip);
        DrawLine(anchor, top + new Vector2(5f, 0f),
            top + new Vector2(5f, 8f), 3f, Bronze, flip);
        Vector2 cage = top + new Vector2(0f, 18f);
        DrawBox(anchor, cage.X - 13f, cage.Y - 10f, 26f, 29f,
            Outline, flip);
        DrawBox(anchor, cage.X - 11f, cage.Y - 8f, 22f, 25f,
            BronzeDark, flip);
        DrawBox(anchor, cage.X - 8f, cage.Y - 5f, 16f, 19f,
            new Color(35, 31, 48), flip);
        float pulse = .5f + .5f * MathF.Sin(elapsed * 4.5f);
        int core = 7 + (int)MathF.Round(charge * 5f);
        Color soul = Color.Lerp(SoulDim, SoulBright,
            Math.Clamp(charge * .85f + pulse * .15f, 0f, 1f));
        DrawBox(anchor, cage.X - core / 2f, cage.Y + 1f - core / 2f,
            core, core, soul, flip);
        for (int bar = -1; bar <= 1; bar++)
        {
            DrawLine(anchor,
                cage + new Vector2(bar * 7f, -7f),
                cage + new Vector2(bar * 7f, 16f),
                2f, Bronze, flip);
        }
        DrawLine(anchor, cage + new Vector2(-11f, -7f),
            cage + new Vector2(11f, -7f), 3f, Bronze, flip);
        DrawLine(anchor, cage + new Vector2(-11f, 16f),
            cage + new Vector2(11f, 16f), 3f, Bronze, flip);
        if (charge > 0f || bursting)
        {
            float radius = 15f + charge * 8f;
            DrawDiamond(anchor, cage + new Vector2(0f, 4f),
                radius, radius * .70f, 2f,
                new Color(154, 135, 194,
                    (int)(80f + charge * 95f)), flip);
        }
    }

    private void DrawEnforcerDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float buckle = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.7f));
        float fall = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .22f) / .78f));
        DrawLine(anchor,
            new Vector2(-7f, -60f + buckle * 28f),
            new Vector2(24f + fall * 20f, -28f + buckle * 23f),
            18f, LeatherDark, flip);
        DrawBox(anchor, 18f + fall * 20f, -48f + buckle * 38f,
            17f, 16f, DemonSkin, flip);
        DrawLine(anchor, new Vector2(-12f, -4f),
            new Vector2(12f, -3f), 8f, Leather, flip);
        // The cleaver visibly leaves the hand as the knees buckle.
        DrawLine(anchor, new Vector2(-18f, -18f + buckle * 12f),
            new Vector2(-48f - fall * 10f, -3f), 11f, Iron, flip);
        DrawBox(anchor, 30f + fall * 13f, -9f, 16f, 22f,
            IronDark, flip);
    }

    private void DrawKeeperDeath(
        Vector2 anchor,
        bool flip,
        float progress)
    {
        float sink = MathHelper.SmoothStep(0f, 1f,
            Math.Min(1f, progress * 1.55f));
        float settle = MathHelper.SmoothStep(0f, 1f,
            Math.Max(0f, (progress - .30f) / .70f));
        DrawLine(anchor,
            new Vector2(-10f, -67f + sink * 48f),
            new Vector2(20f + settle * 18f, -33f + sink * 26f),
            20f, RobeDark, flip);
        DrawBox(anchor, 17f + settle * 18f, -55f + sink * 45f,
            18f, 17f, new Color(82, 59, 75), flip);
        // Lantern drops intact and its core goes dark.
        DrawLine(anchor, new Vector2(-8f, -24f + sink * 15f),
            new Vector2(-27f, -7f), 4f, BronzeDark, flip);
        DrawBox(anchor, -40f, -22f + settle * 10f, 24f, 25f,
            Outline, flip);
        DrawBox(anchor, -37f, -19f + settle * 10f, 18f, 18f,
            BronzeDark, flip);
        DrawBox(anchor, -32f, -14f + settle * 10f, 8f, 8f,
            new Color(55, 51, 68), flip);
    }

    private void DrawStalkerDeath(Vector2 anchor, bool flip, float progress)
    {
        float buckle = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 1.8f));
        float fall = MathHelper.SmoothStep(0f, 1f, Math.Max(0f, (progress - .22f) / .78f));
        DrawLine(anchor, new Vector2(-8f, -4f), new Vector2(8f, -3f), 7f, LeatherDark, flip);
        DrawLine(anchor, new Vector2(0f, -58f + buckle * 31f),
            new Vector2(24f + fall * 23f, -29f + buckle * 24f), 16f, LeatherDark, flip);
        DrawBox(anchor, 19f + fall * 23f, -49f + buckle * 39f, 16f, 15f,
            new Color(64, 48, 49), flip);
        DrawLine(anchor, new Vector2(-15f, -22f + buckle * 12f),
            new Vector2(-47f - fall * 8f, -4f), 5f, Iron, flip);
        DrawLine(anchor, new Vector2(12f, -26f + buckle * 15f),
            new Vector2(50f + fall * 10f, -5f), 5f, IronLight, flip);
    }

    private void DrawCollectorDeath(Vector2 anchor, bool flip, float progress)
    {
        float fold = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 1.5f));
        float settle = MathHelper.SmoothStep(0f, 1f, Math.Max(0f, (progress - .28f) / .72f));
        DrawLine(anchor, new Vector2(-4f, -75f + fold * 52f),
            new Vector2(23f + settle * 19f, -35f + fold * 29f), 20f, RobeDark, flip);
        DrawBox(anchor, 18f + settle * 19f, -69f + fold * 52f, 19f, 18f,
            IronDark, flip);
        Vector2 hand = new(-5f, -27f + fold * 18f);
        Vector2 hook = new(-65f - settle * 16f, -2f);
        DrawChainCurve(anchor, hand, new Vector2(-26f, -3f), hook, flip, 9);
        DrawHook(anchor, hook, flip);
        DrawSmallCage(anchor, new Vector2(10f, -18f + fold * 15f), flip, false);
        DrawSmallCage(anchor, new Vector2(26f, -13f + fold * 11f), flip, true);
    }

    private static Pose CreatePose(
        DemonCityEnemy enemy,
        VisualState state,
        float elapsed)
    {
        float p = enemy.CombatState == DemonCityCombatState.Ready
            ? 0f
            : enemy.ActionProgress;
        float wave = MathF.Sin(elapsed * MathHelper.TwoPi * .82f);
        var pose = new Pose
        {
            WeaponAngle = 1.12f,
            ShieldRaise = 2f,
            CoatTrail = 2f,
            LanternForward = 0f
        };
        switch (state)
        {
            case VisualState.Idle:
                pose.Bob = MathF.Max(0f, wave) * .8f;
                pose.UpperLean = enemy.Type == EnemyType.DemonCityEnforcer
                    ? 2f + wave * .5f
                    : wave * .45f;
                pose.HeadShift = wave * .35f;
                pose.CoatTrail = 2f + wave * .6f;
                break;
            case VisualState.Walk:
                pose.Step = wave * (enemy.Type == EnemyType.DemonCityEnforcer
                    ? 6f : 4f);
                pose.Bob = MathF.Abs(wave) * 1.6f;
                pose.UpperLean = enemy.Type == EnemyType.DemonCityEnforcer
                    ? 4f : 1.5f;
                pose.CoatTrail = 4f + MathF.Abs(wave) * 2f;
                pose.WeaponAngle = .9f + wave * .12f;
                break;
            case VisualState.PressureStep:
                pose.Step = 6f;
                pose.Crouch = 3f;
                pose.UpperLean = 6f;
                pose.WeaponAngle = .85f;
                pose.ShieldPush = 4f;
                pose.CoatTrail = 7f;
                break;
            case VisualState.CleaveWindup:
                pose.Crouch = p * 4f;
                pose.UpperLean = MathHelper.Lerp(1f, -7f, p);
                pose.WeaponAngle = MathHelper.Lerp(1.1f, -1.75f, p);
                pose.ShieldRaise = 7f;
                break;
            case VisualState.CleaveActive:
                pose.Crouch = 3f;
                pose.UpperLean = MathHelper.Lerp(-5f, 9f, p);
                pose.WeaponAngle = MathHelper.Lerp(-1.75f, .78f, p);
                pose.Step = 4f;
                break;
            case VisualState.BackhandWindup:
                pose.Crouch = 2f;
                pose.UpperLean = 5f;
                pose.WeaponAngle = MathHelper.Lerp(.85f, .18f, p);
                break;
            case VisualState.BackhandActive:
                pose.UpperLean = MathHelper.Lerp(7f, -1f, p);
                pose.WeaponAngle = MathHelper.Lerp(.18f, -2.65f, p);
                pose.Step = 3f;
                break;
            case VisualState.ShieldWindup:
                pose.Crouch = 5f;
                pose.UpperLean = -5f;
                pose.ShieldRaise = 10f;
                pose.ShieldPush = MathHelper.Lerp(0f, -9f, p);
                pose.WeaponAngle = 1.5f;
                break;
            case VisualState.ShieldActive:
                pose.Crouch = 3f;
                pose.UpperLean = 9f;
                pose.ShieldRaise = 11f;
                pose.ShieldPush = MathHelper.Lerp(-7f, 18f, p);
                pose.Step = 5f;
                pose.WeaponAngle = 1.65f;
                break;
            case VisualState.BoltCharge:
                pose.LanternLift = MathHelper.Lerp(0f, 25f, p);
                pose.LanternForward = MathHelper.Lerp(0f, 8f, p);
                pose.GestureRaise = MathHelper.Lerp(0f, 19f, p);
                pose.GestureOpen = MathHelper.Lerp(0f, 8f, p);
                pose.UpperLean = -2f;
                break;
            case VisualState.BoltRelease:
                pose.LanternLift = 25f;
                pose.LanternForward = 11f;
                pose.GestureRaise = 19f;
                pose.GestureOpen = 11f;
                pose.UpperLean = 3f * (1f - p);
                break;
            case VisualState.BurstWindup:
                pose.Crouch = 3f;
                pose.LanternLift = MathHelper.Lerp(0f, 15f, p);
                pose.LanternForward = MathHelper.Lerp(0f, -11f, p);
                pose.GestureRaise = 8f;
                pose.UpperLean = -5f;
                break;
            case VisualState.BurstActive:
                pose.Crouch = 2f;
                pose.LanternLift = 18f;
                pose.LanternForward = MathHelper.Lerp(-8f, 13f, p);
                pose.GestureRaise = 17f;
                pose.GestureOpen = 10f;
                pose.UpperLean = 4f;
                break;
            case VisualState.Retreat:
                pose.Step = -5f;
                pose.UpperLean = -5f;
                pose.CoatTrail = 7f;
                pose.LanternForward = -4f;
                break;
            case VisualState.Recovery:
                pose.Crouch = MathHelper.Lerp(4f, 0f, p);
                pose.UpperLean = MathHelper.Lerp(7f, 0f, p);
                pose.WeaponAngle = MathHelper.Lerp(.55f, 1.12f, p);
                pose.LanternLift = MathHelper.Lerp(18f, 0f, p);
                pose.GestureRaise = MathHelper.Lerp(12f, 0f, p);
                break;
            case VisualState.Hurt:
                pose.Crouch = 4f;
                pose.UpperLean = -9f;
                pose.HeadShift = -4f;
                pose.WeaponAngle = 1.7f;
                pose.LanternForward = -6f;
                break;
            case VisualState.Stagger:
                pose.Crouch = 8f;
                pose.Lean = -4f;
                pose.UpperLean = -12f;
                pose.HeadShift = -5f;
                pose.WeaponAngle = 2f;
                pose.ShieldRaise = -2f;
                pose.LanternForward = -9f;
                break;
        }
        return pose;
    }

    private static VisualState SelectState(DemonCityEnemy enemy)
    {
        if (enemy.IsStaggered)
            return VisualState.Stagger;
        if (enemy.IsHitFlashing)
            return VisualState.Hurt;
        return enemy.CombatState switch
        {
            DemonCityCombatState.EnforcerPressureStep => VisualState.PressureStep,
            DemonCityCombatState.EnforcerCleaveWindup => VisualState.CleaveWindup,
            DemonCityCombatState.EnforcerCleaveActive => VisualState.CleaveActive,
            DemonCityCombatState.EnforcerBackhandWindup => VisualState.BackhandWindup,
            DemonCityCombatState.EnforcerBackhandActive => VisualState.BackhandActive,
            DemonCityCombatState.EnforcerShieldWindup => VisualState.ShieldWindup,
            DemonCityCombatState.EnforcerShieldActive => VisualState.ShieldActive,
            DemonCityCombatState.KeeperRetreat => VisualState.Retreat,
            DemonCityCombatState.KeeperBoltCharge => VisualState.BoltCharge,
            DemonCityCombatState.KeeperBoltRelease => VisualState.BoltRelease,
            DemonCityCombatState.KeeperBurstWindup => VisualState.BurstWindup,
            DemonCityCombatState.KeeperBurstActive => VisualState.BurstActive,
            DemonCityCombatState.StalkerHunterStep => VisualState.HunterStep,
            DemonCityCombatState.StalkerAshCutWindup => VisualState.AshCutWindup,
            DemonCityCombatState.StalkerAshCutActive => VisualState.AshCutActive,
            DemonCityCombatState.StalkerTwinRipWindup => VisualState.TwinWindup,
            DemonCityCombatState.StalkerTwinRipFirst => VisualState.TwinFirst,
            DemonCityCombatState.StalkerTwinRipTurn => VisualState.TwinTurn,
            DemonCityCombatState.StalkerTwinRipSecond => VisualState.TwinSecond,
            DemonCityCombatState.StalkerCrossingWindup => VisualState.CrossingWindup,
            DemonCityCombatState.StalkerCrossingActive => VisualState.CrossingActive,
            DemonCityCombatState.CollectorSweepGather => VisualState.ChainGather,
            DemonCityCombatState.CollectorSweepActive => VisualState.ChainSweep,
            DemonCityCombatState.CollectorHookGather => VisualState.ChainGather,
            DemonCityCombatState.CollectorHookAim => VisualState.HookAim,
            DemonCityCombatState.CollectorHookExtend => VisualState.HookThrow,
            DemonCityCombatState.CollectorHookResolve => VisualState.HookResolve,
            DemonCityCombatState.CollectorHookRetract => VisualState.HookRetract,
            DemonCityCombatState.CollectorDropWindup => VisualState.ChainDropWindup,
            DemonCityCombatState.CollectorDropActive => VisualState.ChainDrop,
            DemonCityCombatState.StalkerDisengage or
                DemonCityCombatState.CollectorRetreat => VisualState.Retreat,
            DemonCityCombatState.EnforcerCleaveRecovery or
                DemonCityCombatState.EnforcerBackhandRecovery or
                DemonCityCombatState.EnforcerShieldRecovery or
                DemonCityCombatState.KeeperBoltRecovery or
                DemonCityCombatState.KeeperBurstRecovery or
                DemonCityCombatState.StalkerAshCutRecovery or
                DemonCityCombatState.StalkerTwinRipRecovery or
                DemonCityCombatState.StalkerCrossingRecovery or
                DemonCityCombatState.CollectorSweepRecovery or
                DemonCityCombatState.CollectorHookRecovery or
                DemonCityCombatState.CollectorDropRecovery => VisualState.Recovery,
            _ => MathF.Abs(enemy.VisualVelocityX) > 1f
                ? VisualState.Walk
                : VisualState.Idle
        };
    }

    private void DrawLimb(
        Vector2 anchor,
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness,
        bool flip)
    {
        DrawLine(anchor, from, to, thickness + 2f, Outline, flip);
        DrawLine(anchor, from, to, thickness, color, flip);
    }

    private void DrawDiamond(
        Vector2 anchor,
        Vector2 center,
        float radiusX,
        float radiusY,
        float thickness,
        Color color,
        bool flip)
    {
        Vector2 top = center + new Vector2(0f, -radiusY);
        Vector2 right = center + new Vector2(radiusX, 0f);
        Vector2 bottom = center + new Vector2(0f, radiusY);
        Vector2 left = center + new Vector2(-radiusX, 0f);
        DrawLine(anchor, top, right, thickness, color, flip);
        DrawLine(anchor, right, bottom, thickness, color, flip);
        DrawLine(anchor, bottom, left, thickness, color, flip);
        DrawLine(anchor, left, top, thickness, color, flip);
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
            Math.Max(1, (int)MathF.Round(width)),
            Math.Max(1, (int)MathF.Round(height))), color);
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
        public VisualState State { get; private set; }
        public float Elapsed { get; private set; }

        public void Update(GameTime gameTime, VisualState state)
        {
            if (State != state)
            {
                State = state;
                Elapsed = 0f;
                return;
            }
            Elapsed += MathF.Min(
                (float)gameTime.ElapsedGameTime.TotalSeconds, .10f);
        }
    }

    private enum VisualState
    {
        Idle,
        Walk,
        PressureStep,
        CleaveWindup,
        CleaveActive,
        BackhandWindup,
        BackhandActive,
        ShieldWindup,
        ShieldActive,
        BoltCharge,
        BoltRelease,
        BurstWindup,
        BurstActive,
        Retreat,
        Recovery,
        Hurt,
        Stagger,
        HunterStep,
        AshCutWindup,
        AshCutActive,
        TwinWindup,
        TwinFirst,
        TwinTurn,
        TwinSecond,
        CrossingWindup,
        CrossingActive,
        ChainGather,
        ChainSweep,
        HookAim,
        HookThrow,
        HookResolve,
        HookRetract,
        ChainDropWindup,
        ChainDrop
    }

    private struct Pose
    {
        public float Bob;
        public float Lean;
        public float UpperLean;
        public float Crouch;
        public float Step;
        public float HeadShift;
        public float WeaponAngle;
        public float ShieldRaise;
        public float ShieldPush;
        public float CoatTrail;
        public float LanternLift;
        public float LanternForward;
        public float GestureRaise;
        public float GestureOpen;
    }
}
