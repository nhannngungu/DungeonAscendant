using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// The definitive seven-weapon move table.  Values are intentionally centralized
/// so timing, stamina, hitboxes and resource hooks can be tuned without controller
/// conditionals.
/// </summary>
public static class WeaponTechniqueCatalog
{
    private static readonly ProjectileDefinition Arrow = new(
        ProjectileType.Arrow, 640f, new Vector2(18f, 6f), 2.8f);
    private static readonly ProjectileDefinition PiercingArrow = new(
        ProjectileType.Arrow, 780f, new Vector2(24f, 7f), 3.0f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 1);
    private static readonly ProjectileDefinition StormArrow = new(
        ProjectileType.Arrow, 630f, new Vector2(17f, 6f), 3.0f,
        ProjectileTrajectoryType.ArrowRain, gravity: 760f,
        impactRadius: 30f, splitCount: RangerTuning.ArrowStormRainArrowCount);
    private static readonly ProjectileDefinition AerialArrow = new(
        ProjectileType.Arrow, 665f, new Vector2(18f, 6f), 2.6f,
        ProjectileTrajectoryType.Straight);
    private static readonly ProjectileDefinition SkyfallVolleyArrow = new(
        ProjectileType.Arrow, 610f, new Vector2(18f, 6f), 2.1f,
        ProjectileTrajectoryType.Ballistic, gravity: 180f);
    private static readonly ProjectileDefinition PhantomArrow = new(
        ProjectileType.Arrow, 720f, new Vector2(19f, 6f), 2.8f);
    private static readonly ProjectileDefinition PhantomFinalArrow = new(
        ProjectileType.Arrow, 900f, new Vector2(30f, 9f), 3.0f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 1);
    private static readonly ProjectileDefinition ThreefoldArrow = new(
        ProjectileType.Arrow, 600f, new Vector2(17f, 6f), 2.8f,
        ProjectileTrajectoryType.Spread);
    private static readonly ProjectileDefinition SkyfallArrow = new(
        ProjectileType.Arrow, 650f, new Vector2(18f, 6f), 3.2f,
        ProjectileTrajectoryType.ArrowRain, gravity: 780f,
        impactRadius: 34f, splitCount: RangerTuning.SkyfallRainArrowCount);
    private static readonly ProjectileDefinition DragonPiercerArrow = new(
        ProjectileType.Arrow, 850f, new Vector2(26f, 8f), 3.2f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 2);
    private static readonly ProjectileDefinition FallingStarArrow = new(
        ProjectileType.Arrow, 690f, new Vector2(24f, 8f), 3.4f,
        ProjectileTrajectoryType.Arc, gravity: 940f, impactRadius: 48f);
    private static readonly ProjectileDefinition PredatorFanArrow = new(
        ProjectileType.Arrow, 620f, new Vector2(17f, 6f), 2.6f,
        ProjectileTrajectoryType.Spread);
    private static readonly ProjectileDefinition PredatorCenterArrow = new(
        ProjectileType.Arrow, 780f, new Vector2(24f, 8f), 3f,
        ProjectileTrajectoryType.Straight);
    private static readonly ProjectileDefinition HeavenArrow = new(
        ProjectileType.Arrow, 1400f, new Vector2(58f, 18f), 3.5f,
        ProjectileTrajectoryType.MassivePenetrating, remainingPierces: 99,
        penetratesTerrain: true);
    private static readonly ProjectileDefinition ArcaneBolt = new(
        ProjectileType.ArcaneBolt, 420f, new Vector2(18f, 18f), 3.1f);
    private static readonly ProjectileDefinition AncientRuneSpark = new(
        ProjectileType.ArcaneBolt, 570f, new Vector2(11f, 11f), 2.2f);
    private static readonly ProjectileDefinition AncientArcanePulse = new(
        ProjectileType.ArcaneBolt, 510f, new Vector2(18f, 18f), 2.5f);
    private static readonly ProjectileDefinition AncientTwinGlyph = new(
        ProjectileType.ArcaneBolt, 600f, new Vector2(13f, 13f), 2.4f);
    private static readonly ProjectileDefinition AncientSpiralLance = new(
        ProjectileType.ArcaneBolt, 670f, new Vector2(30f, 12f), 2.5f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 1);
    private static readonly ProjectileDefinition AncientElderBurst = new(
        ProjectileType.ArcaneBolt, 485f, new Vector2(30f, 30f), 2.8f,
        impactRadius: 44f);
    private static readonly ProjectileDefinition AncientBlinkLance = new(
        ProjectileType.ArcaneBolt, 760f, new Vector2(38f, 12f), 2.1f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 1,
        impactRadius: 38f);
    private static readonly ProjectileDefinition RuneBrandSigil = new(
        ProjectileType.ArcaneBolt, 560f, new Vector2(20f, 20f), 2.2f,
        impactRadius: 1f);
    private static readonly ProjectileDefinition RunicSpearExtension = new(
        ProjectileType.ArcaneBolt, 880f, new Vector2(54f, 12f), 2.4f,
        ProjectileTrajectoryType.Penetrating,
        remainingPierces: SpellbladeTuning.RunicSpearMaximumTargets - 1);

    public static readonly WeaponCombatProfile Knight = new(
        "knight", WeaponTechniqueInput.Skill1,
        T("knights-edge", "Knight's Edge", WeaponTechniqueInput.Skill1, 7,
            new[] {
                M(AttackKind.LightOne, 1f, 18, .11f, .11f, .18f, 18, 76, 48, AttackHitboxShape.StandardArc, 35),
                M(AttackKind.LightTwo, 1.08f, 22, .12f, .11f, .19f, 20, 78, 50, AttackHitboxShape.StandardArc, 30),
                M(AttackKind.LightThree, 1.35f, 38, .18f, .13f, .30f, 34, 82, 58, AttackHitboxShape.Chop, 18)
            }, manual: true, stageCost: 7),
        T("iron-bastion", "Iron Bastion", WeaponTechniqueInput.Skill2, 6,
            new[] { M(AttackKind.LightTwo, 0f, 0, 0f, .72f, .22f, 0, 58, 74, AttackHitboxShape.Thrust) },
            guard: true, effect: WeaponTechniqueEffect.IronBastion),
        T("shield-breaker", "Shield Breaker", WeaponTechniqueInput.Skill3, 18,
            new[] { M(AttackKind.LightThree, 1.05f, 58, .16f, .12f, .32f, 52, 62, 62, AttackHitboxShape.Thrust, 150) },
            effect: WeaponTechniqueEffect.DefenseWeaken),
        T("vanguard-slash", "Vanguard Slash", WeaponTechniqueInput.DashSkill, 24,
            new[] { M(AttackKind.LightOne, 1.35f, 36, .06f, .24f, .30f, 40, 102, 46, AttackHitboxShape.StandardArc, 360) },
            guard: true),
        T("guardians-reversal", "Guardian's Reversal", WeaponTechniqueInput.Skill1Skill2, 28,
            new[] { M(AttackKind.LightTwo, 1.4f, 52, .16f, .12f, .30f, 44, 86, 54, AttackHitboxShape.StandardArc, 30) },
            guard: true, effect: WeaponTechniqueEffect.GuardianReversal),
        T("royal-cleaver", "Royal Cleaver", WeaponTechniqueInput.Skill1Skill3, 32,
            new[] { M(AttackKind.Heavy, 2.05f, 62, .28f, .16f, .46f, 68, 102, 74, AttackHitboxShape.WideArc, 18) }),
        T("iron-juggernaut", "Iron Juggernaut", WeaponTechniqueInput.Skill2Skill3, 38,
            new[] {
                M(AttackKind.LightOne, .48f, 34, .08f, .16f, .05f, 38, 58, 68, AttackHitboxShape.Thrust, 275),
                M(AttackKind.LightTwo, .52f, 38, .04f, .16f, .06f, 42, 60, 70, AttackHitboxShape.Thrust, 290),
                M(AttackKind.Heavy, 1.15f, 82, .18f, .16f, .45f, 58, 92, 84, AttackHitboxShape.WideArc)
            }, guard: true, effect: WeaponTechniqueEffect.IronJuggernaut),
        T("oath-unbroken", "Oath of the Unbroken", WeaponTechniqueInput.Ultimate, 100,
            new[] {
                M(AttackKind.LightOne, 0f, 0, 0f, .85f, .12f, 0, 60, 80, AttackHitboxShape.Thrust),
                M(AttackKind.LightThree, .8f, 82, .18f, .15f, .20f, 42, 108, 96, AttackHitboxShape.WideArc),
                M(AttackKind.Heavy, 2.2f, 78, .30f, .16f, .60f, 78, 112, 78, AttackHitboxShape.WideArc, 22)
            }, full: true, guard: true, effect: WeaponTechniqueEffect.OathStance));

    public static readonly WeaponCombatProfile Duelist = new(
        "duelist", WeaponTechniqueInput.Skill1,
        T("twin-fang", "Twin Fang", WeaponTechniqueInput.Skill1, 4, gain: 5,
            manual: true, stageCost: 4,
            stages: new[] {
                M(AttackKind.LightOne, .62f, 7, .05f, .07f, .09f, 5, 60, 30, AttackHitboxShape.StandardArc, 42),
                M(AttackKind.LightTwo, .64f, 7, .045f, .07f, .085f, 5, 56, 29, AttackHitboxShape.ShortArc, 38),
                M(AttackKind.LightThree, .72f, 8, .045f, .08f, .08f, 6, 63, 42, AttackHitboxShape.StandardArc, 44),
                M(AttackKind.LightFour, .76f, 9, .05f, .09f, .09f, 8, 58, 34, AttackHitboxShape.WideArc, 36),
                M(AttackKind.LightFive, 1.18f, 18, .07f, .11f, .18f, 24, 72, 52, AttackHitboxShape.Cross, 54)
            }),
        T("phantom-step", "Phantom Step", WeaponTechniqueInput.Skill2, 18, gain: 10,
            effect: WeaponTechniqueEffect.DuelistPhantomStep,
            stages: new[] {
                M(AttackKind.LightTwo, .52f, 6, .08f, .07f, .04f, 5, 68, 32, AttackHitboxShape.Cross),
                M(AttackKind.LightFour, .68f, 8, .035f, .07f, .12f, 8, 68, 32, AttackHitboxShape.Cross)
            }),
        T("blade-tempest", "Blade Tempest", WeaponTechniqueInput.Skill3, 24, gain: 4,
            effect: WeaponTechniqueEffect.DuelistBladeTempest,
            stages: new[] {
                M(AttackKind.LightOne, .38f, 5, .04f, .07f, .025f, 3, 53, 74, AttackHitboxShape.Circular),
                M(AttackKind.LightTwo, .38f, 5, .025f, .07f, .025f, 3, 53, 74, AttackHitboxShape.Circular),
                M(AttackKind.LightThree, .40f, 5, .025f, .07f, .025f, 3, 55, 76, AttackHitboxShape.Circular),
                M(AttackKind.LightFour, .40f, 5, .025f, .07f, .025f, 3, 55, 76, AttackHitboxShape.Circular),
                M(AttackKind.LightOne, .42f, 6, .025f, .07f, .025f, 4, 56, 78, AttackHitboxShape.Circular),
                M(AttackKind.LightFive, .56f, 8, .025f, .08f, .14f, 10, 58, 80, AttackHitboxShape.Circular)
            }),
        T("cross-fang-dash", "Cross Fang Dash", WeaponTechniqueInput.DashSkill, 20, gain: 10,
            effect: WeaponTechniqueEffect.DuelistCrossFangDash,
            stages: new[] { M(AttackKind.LightFive, 1.20f, 18, .04f, .20f, .16f, 22, 68, 48, AttackHitboxShape.Cross, 500) }),
        T("afterimage-execution", "Afterimage Execution", WeaponTechniqueInput.Skill1Skill2, 30, gain: 8,
            effect: WeaponTechniqueEffect.DuelistAfterimageExecution,
            stages: new[] {
                M(AttackKind.LightOne, .24f, 3, .06f, .06f, .04f, 2, 58, 28, AttackHitboxShape.ShortArc),
                M(AttackKind.LightThree, .88f, 12, .04f, .08f, .025f, 10, 72, 38, AttackHitboxShape.StandardArc),
                M(AttackKind.LightFive, .92f, 13, .025f, .08f, .16f, 16, 72, 40, AttackHitboxShape.Cross)
            }),
        T("hundred-fangs", "Hundred Fangs", WeaponTechniqueInput.Skill1Skill3, 34, gain: 8,
            effect: WeaponTechniqueEffect.DuelistHundredFangs,
            stages: new[] {
                M(AttackKind.LightOne, .34f, 4, .05f, .06f, .025f, 2, 82, 56, AttackHitboxShape.FrontalFan, 70),
                M(AttackKind.LightTwo, .34f, 4, .025f, .06f, .025f, 2, 82, 56, AttackHitboxShape.FrontalFan, 54),
                M(AttackKind.LightThree, .36f, 4, .025f, .06f, .025f, 2, 84, 58, AttackHitboxShape.FrontalFan, 48),
                M(AttackKind.LightFour, .36f, 4, .025f, .06f, .025f, 2, 84, 58, AttackHitboxShape.FrontalFan, 42),
                M(AttackKind.LightOne, .38f, 5, .025f, .06f, .025f, 3, 86, 60, AttackHitboxShape.FrontalFan, 36),
                M(AttackKind.LightTwo, .38f, 5, .025f, .06f, .025f, 3, 86, 60, AttackHitboxShape.FrontalFan, 30),
                M(AttackKind.LightFour, .42f, 6, .025f, .07f, .03f, 4, 88, 62, AttackHitboxShape.FrontalFan, 24),
                M(AttackKind.LightFive, 1.12f, 18, .045f, .10f, .18f, 34, 92, 64, AttackHitboxShape.FrontalFan, 18)
            }),
        T("mirage-cyclone", "Mirage Cyclone", WeaponTechniqueInput.Skill2Skill3, 38, gain: 8,
            effect: WeaponTechniqueEffect.DuelistMirageCyclone,
            stages: new[] {
                M(AttackKind.LightOne, .48f, 6, .05f, .07f, .025f, 3, 74, 44, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .50f, 6, .025f, .07f, .025f, 3, 74, 44, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .52f, 7, .025f, .07f, .025f, 4, 78, 48, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFour, .54f, 7, .025f, .07f, .025f, 4, 78, 48, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFive, .68f, 9, .035f, .08f, .035f, 8, 82, 52, AttackHitboxShape.Cross),
                M(AttackKind.Heavy, 1.28f, 22, .08f, .11f, .20f, 26, 92, 68, AttackHitboxShape.TargetZone)
            }),
        T("final-waltz", "Thousand Blades: Final Waltz", WeaponTechniqueInput.Ultimate, 100, full: true,
            effect: WeaponTechniqueEffect.DuelistFinalWaltz,
            stages: new[] {
                M(AttackKind.LightFive, .42f, 6, .22f, .07f, .035f, 2, 86, 58, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .48f, 6, .035f, .07f, .025f, 2, 86, 58, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .50f, 6, .025f, .07f, .025f, 2, 86, 58, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .52f, 7, .025f, .07f, .025f, 3, 88, 60, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFour, .54f, 7, .025f, .07f, .025f, 3, 88, 60, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFive, .56f, 8, .025f, .07f, .025f, 4, 90, 62, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .58f, 8, .025f, .07f, .025f, 4, 90, 62, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .60f, 9, .025f, .07f, .03f, 5, 92, 64, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .64f, 10, .03f, .08f, .08f, 6, 94, 66, AttackHitboxShape.Cross),
                M(AttackKind.Heavy, 1.70f, 34, .20f, .12f, .42f, 38, 102, 76, AttackHitboxShape.TargetZone)
            }));

    public static readonly WeaponTechnique RangerPiercingDraw = T(
        "piercing-draw", "Piercing Draw", WeaponTechniqueInput.Skill1Skill3,
        RangerTuning.HunterDrawBaseStaminaCost,
        effect: WeaponTechniqueEffect.RangerPiercingDraw,
        stages: new[] { R(AttackKind.Heavy, 1.16f, 34, .05f, .04f, .24f, 28, PiercingArrow) });

    public static readonly WeaponTechnique RangerCounterShot = T(
        "counter-shot", "Counter Shot", WeaponTechniqueInput.Skill1Skill2, 8,
        effect: WeaponTechniqueEffect.RangerCounterShot,
        stages: new[] { R(AttackKind.LightFive, 1.40f, 34, .035f, .04f, .18f, 26, PhantomArrow) });

    public static readonly WeaponTechnique RangerArrowStorm = T(
        "arrow-storm", "Arrow Storm", WeaponTechniqueInput.Skill2Skill3, 24,
        effect: WeaponTechniqueEffect.RangerArrowStorm,
        stages: new[] { R(AttackKind.Heavy, .82f, 16, .24f, .05f, .30f, 8, StormArrow) });

    public static readonly WeaponTechnique RangerAerialShot = T(
        "aerial-shot", "Aerial Shot", WeaponTechniqueInput.Skill1, 5,
        effect: WeaponTechniqueEffect.RangerAerialShot,
        stages: new[] {
            R(AttackKind.LightTwo, .78f, 12, .045f, .04f, .13f, 9, AerialArrow),
            R(AttackKind.LightThree, .28f, 7, .035f, .04f, .12f, 6, AerialArrow)
        });

    public static readonly WeaponTechnique RangerSkyfallVolley = T(
        "skyfall-volley", "Skyfall Volley", WeaponTechniqueInput.Skill3, 18,
        effect: WeaponTechniqueEffect.RangerSkyfallVolley,
        stages: new[] {
            R(AttackKind.LightOne, .44f, 10, .05f, .04f, .07f, 7, SkyfallVolleyArrow),
            R(AttackKind.LightTwo, .44f, 10, .04f, .04f, .07f, 7, SkyfallVolleyArrow),
            R(AttackKind.LightThree, .44f, 12, .04f, .04f, .15f, 9, SkyfallVolleyArrow)
        });

    public static readonly WeaponTechnique RangerSkyfallShot = T(
        "skyfall-shot", "Skyfall Shot", WeaponTechniqueInput.Skill2, 7,
        effect: WeaponTechniqueEffect.RangerSkyfallShot,
        stages: new[] {
            R(AttackKind.LightThree, .78f, 12, .05f, .04f, .14f, 10,
                SkyfallVolleyArrow)
        });

    public static readonly WeaponCombatProfile Ranger = new(
        "phantom-archer", WeaponTechniqueInput.Skill1,
        T("quick-shot-combo", "Quick Shot Combo", WeaponTechniqueInput.Skill1, 4,
            effect: WeaponTechniqueEffect.RangerMobileShot,
            manual: true, stageCost: 3,
            stages: new[] {
                R(AttackKind.LightOne, .65f, 8, .035f, .035f, .10f, 6, Arrow),
                R(AttackKind.LightTwo, .70f, 9, .035f, .035f, .10f, 7, Arrow),
                R(AttackKind.LightThree, .80f, 11, .04f, .035f, .11f, 8, Arrow),
                R(AttackKind.LightFour, .52f, 7, .045f, .035f, .045f, 5, Arrow),
                R(AttackKind.LightFour, .52f, 7, .04f, .035f, .11f, 5, Arrow),
                R(AttackKind.LightFive, 1.30f, 27, .12f, .04f, .28f, 24, PhantomFinalArrow)
            }),
        T("evasive-shot", "Evasive Shot", WeaponTechniqueInput.Skill2, 7,
            effect: WeaponTechniqueEffect.RangerEvasiveShot,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .01f, .01f, .08f, 0,
                    1, 1, AttackHitboxShape.ShortArc,
                    RangerTuning.EvasiveStepSpeed),
                R(AttackKind.LightTwo, .76f, 11, .04f, .04f, .13f, 9,
                    Arrow)
            }),
        T("phantom-barrage", "Phantom Barrage", WeaponTechniqueInput.Skill3, 20,
            effect: WeaponTechniqueEffect.RangerPhantomBarrage,
            stages: new[] {
                R(AttackKind.LightOne, .42f, 7, .06f, .035f, .06f, 5, PhantomArrow, 210),
                R(AttackKind.LightTwo, .42f, 7, .035f, .035f, .06f, 5, PhantomArrow),
                R(AttackKind.LightThree, .42f, 7, .035f, .035f, .08f, 5, PhantomArrow),
                R(AttackKind.LightFour, .65f, 15, .055f, .04f, .14f, 15, PhantomFinalArrow, 190),
                R(AttackKind.LightFour, .20f, 4, .03f, .035f, .05f, 4, PhantomArrow),
                R(AttackKind.LightFive, .70f, 18, .07f, .04f, .22f, 18, PhantomFinalArrow)
            }),
        T("backstep-shot", "Backstep Shot", WeaponTechniqueInput.DashSkill, 10,
            effect: WeaponTechniqueEffect.RangerBackstepShot,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .01f, .01f, .09f, 0,
                    1, 1, AttackHitboxShape.ShortArc, 390),
                R(AttackKind.LightThree, .98f, 15, .055f, .05f, .18f, 13,
                    Arrow)
            }),
        T("twin-fang", "Twin Fang", WeaponTechniqueInput.Skill1Skill2, 13,
            effect: WeaponTechniqueEffect.RangerTwinFang,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .01f, .01f, .07f, 0,
                    1, 1, AttackHitboxShape.ShortArc, 285),
                R(AttackKind.LightTwo, .62f, 9, .035f, .035f, .055f, 7,
                    PhantomArrow),
                R(AttackKind.LightThree, .68f, 11, .035f, .04f, .11f, 9,
                    PhantomArrow)
            }),
        T("piercing-volley", "Piercing Volley", WeaponTechniqueInput.Skill1Skill3, 22,
            effect: WeaponTechniqueEffect.RangerPiercingVolley,
            stages: new[] {
                R(AttackKind.LightOne, .55f, 8, .04f, .035f, .055f, 5,
                    Arrow),
                R(AttackKind.LightTwo, .55f, 8, .035f, .035f, .08f, 5,
                    Arrow),
                R(AttackKind.Heavy, 1.22f, 40, .18f, .05f, .24f, 26,
                    PiercingArrow)
            }),
        T("phantom-retreat", "Phantom Retreat", WeaponTechniqueInput.Skill2Skill3, 20,
            effect: WeaponTechniqueEffect.RangerPhantomRetreat,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .01f, .01f, .10f, 0,
                    1, 1, AttackHitboxShape.ShortArc, 330),
                R(AttackKind.LightTwo, .48f, 7, .035f, .035f, .055f, 5,
                    PhantomArrow),
                R(AttackKind.LightThree, .48f, 7, .035f, .035f, .055f, 5,
                    PhantomArrow),
                R(AttackKind.LightFour, .68f, 13, .045f, .04f, .14f, 12,
                    PhantomArrow)
            }),
        T("phantom-hunt", "Phantom Hunt", WeaponTechniqueInput.Ultimate, 100,
            full: true,
            effect: WeaponTechniqueEffect.RangerPhantomHunt,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .05f, .01f, .24f, 0,
                    1, 1, AttackHitboxShape.ShortArc),
                R(AttackKind.LightOne, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .07f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightTwo, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightThree, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightFour, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightFive, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightOne, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightTwo, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightThree, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightFour, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightFive, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightOne, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .045f, 2, PhantomArrow),
                R(AttackKind.LightTwo, RangerTuning.PhantomHuntArrowDamageMultiplier, 5, .035f, .025f, .08f, 2, PhantomArrow),
                R(AttackKind.Heavy, RangerTuning.PhantomHuntExecutionDamageMultiplier, 76, .28f, .055f, .30f, 42,
                    PhantomFinalArrow),
                M(AttackKind.LightFive, 0f, 0, .02f, .01f, .32f, 0,
                    1, 1, AttackHitboxShape.ShortArc)
            }));

    public static readonly WeaponCombatProfile IronVanguard = new(
        "iron-vanguard", WeaponTechniqueInput.Skill1,
        T("iron-sweep", "Iron Sweep", WeaponTechniqueInput.Skill1, 8,
            effect: WeaponTechniqueEffect.GreatswordCombo,
            manual: true, stageCost: 8,
            stages: new[] {
                M(AttackKind.LightOne, 1.10f, 34, .16f, .14f, .26f, 18, 102, 68, AttackHitboxShape.WideArc, 92),
                M(AttackKind.LightTwo, 1.20f, 46, .17f, .14f, .28f, 22, 104, 72, AttackHitboxShape.WideArc, 74),
                M(AttackKind.LightThree, 1.35f, 72, .25f, .15f, .38f, 30, 101, 78, AttackHitboxShape.WideArc, 28),
                M(AttackKind.Heavy, 1.80f, 108, .38f, .17f, .55f, 48, 106, 90, AttackHitboxShape.Chop, 14)
            }),
        T("iron-resolve", "Iron Resolve", WeaponTechniqueInput.Skill2, 5,
            guard: true, effect: WeaponTechniqueEffect.GreatswordIronResolve,
            stages: new[] {
                M(AttackKind.LightTwo, 0f, 0, .04f, 8f, .16f, 0, 72, 82, AttackHitboxShape.Thrust)
            }),
        T("whirling-judgment", "Whirling Judgment", WeaponTechniqueInput.Skill3, 26,
            effect: WeaponTechniqueEffect.GreatswordWhirlingJudgment,
            stages: new[] {
                M(AttackKind.LightOne, .35f, 24, .18f, .09f, .18f, 8, 94, 66, AttackHitboxShape.WideArc),
                M(AttackKind.LightTwo, .35f, 24, .02f, .09f, .18f, 8, 94, 66, AttackHitboxShape.WideArc),
                M(AttackKind.LightOne, .35f, 25, .02f, .09f, .18f, 9, 96, 68, AttackHitboxShape.WideArc),
                M(AttackKind.LightTwo, .35f, 25, .02f, .09f, .18f, 9, 96, 68, AttackHitboxShape.WideArc),
                M(AttackKind.LightOne, .35f, 26, .02f, .09f, .18f, 10, 98, 70, AttackHitboxShape.WideArc),
                M(AttackKind.LightTwo, .35f, 26, .02f, .09f, .18f, 10, 98, 70, AttackHitboxShape.WideArc),
                M(AttackKind.Heavy, .75f, 54, .10f, .13f, .34f, 28, 108, 82, AttackHitboxShape.WideArc)
            }),
        T("vanguard-assault", "Vanguard Assault", WeaponTechniqueInput.DashSkill, 22,
            effect: WeaponTechniqueEffect.GreatswordVanguardAssault,
            manual: true,
            stages: new[] {
                M(AttackKind.LightOne, .90f, 58, .14f, .14f, .24f, 28, 98, 74, AttackHitboxShape.WideArc, 265),
                M(AttackKind.Heavy, 1.20f, 82, .22f, .15f, .42f, 38, 104, 84, AttackHitboxShape.Chop, 24)
            }),
        T("titans-reckoning", "Titan's Reckoning", WeaponTechniqueInput.Skill1Skill2, 20,
            guard: true, effect: WeaponTechniqueEffect.GreatswordTitanReckoning,
            stages: new[] {
                M(AttackKind.Heavy, 1.40f, 94, .18f, .15f, .42f, 36, 106, 84, AttackHitboxShape.WideArc, 42)
            }),
        T("kings-rupture", "King's Rupture", WeaponTechniqueInput.Skill1Skill3, 30,
            effect: WeaponTechniqueEffect.GreatswordKingsRupture,
            stages: new[] {
                M(AttackKind.LightOne, .55f, 48, .16f, .12f, .10f, 14, 98, 76, AttackHitboxShape.WideArc),
                M(AttackKind.Heavy, 1.10f, 90, .18f, .15f, .18f, 34, 108, 88, AttackHitboxShape.Chop),
                M(AttackKind.Heavy, .55f, 74, .08f, .12f, .48f, 28, GreatswordTuning.GroundFractureRange, 34, AttackHitboxShape.Thrust)
            }),
        T("iron-dominion", "Iron Dominion", WeaponTechniqueInput.Skill2Skill3, 26,
            guard: true, effect: WeaponTechniqueEffect.GreatswordIronDominion,
            stages: new[] {
                M(AttackKind.LightOne, .25f, 34, .18f, .10f, .10f, 18, 82, 54, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, .80f, 62, .10f, .12f, .12f, 30, 104, 76, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, .90f, 70, .10f, .12f, .14f, 34, 108, 80, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, 1.10f, 88, .14f, .14f, .38f, 42, 112, 84, AttackHitboxShape.WideArc)
            }),
        T("final-verdict", "Final Verdict", WeaponTechniqueInput.Ultimate, 100,
            full: true, effect: WeaponTechniqueEffect.GreatswordFinalVerdict,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .05f, .01f, .34f, 0, 72, 50, AttackHitboxShape.Thrust),
                M(AttackKind.Heavy, GreatswordTuning.FinalVerdictAscentDamageMultiplier, 92, .12f, .13f, .16f, 18, 112, 86, AttackHitboxShape.WideArc),
                M(AttackKind.Heavy, GreatswordTuning.FinalVerdictCleaveDamageMultiplier, 132, .22f, .16f, .20f, 34, 116, 92, AttackHitboxShape.Chop),
                M(AttackKind.Heavy, GreatswordTuning.FinalVerdictSpectralDamageMultiplier, 176, .28f, .18f, .22f, 46, 124, 104, AttackHitboxShape.Chop),
                M(AttackKind.Heavy, 0f, 0, .04f, .06f, .15f, 0, 68, 46, AttackHitboxShape.Circular),
                M(AttackKind.LightOne, 0f, 0, .02f, .01f, .38f, 0, 68, 46, AttackHitboxShape.Circular)
            }));

    public static readonly WeaponTechnique IronSovereignFallingBastion =
        T("falling-bastion", "Falling Bastion", WeaponTechniqueInput.Skill2, 18,
            effect: WeaponTechniqueEffect.GreatswordFallingBastion,
            stages: new[] {
                M(AttackKind.Heavy, 1.20f, 78, .08f, .78f, .34f, 30, 98, 70, AttackHitboxShape.WideArc)
            });

    public static readonly WeaponTechnique IronVanguardSpinStop =
        T("whirling-judgment-stop", "Whirling Judgment",
            WeaponTechniqueInput.Skill3, 0,
            effect: WeaponTechniqueEffect.GreatswordWhirlingJudgment,
            stages: new[] {
                M(AttackKind.LightTwo, 0f, 0, .03f, .01f, .16f, 0, 80, 54, AttackHitboxShape.WideArc)
            });

    public static readonly WeaponTechnique SovereignHeavenReversal =
        T("heaven-reversal", "Heaven Reversal", WeaponTechniqueInput.Skill2, 16,
            effect: WeaponTechniqueEffect.SovereignHeavenReversal,
            stages: new[] {
                M(AttackKind.Heavy, 1.58f, 52, .12f, .13f, .34f, 34, 94, 66, AttackHitboxShape.Chop)
            });

    public static readonly WeaponCombatProfile Sovereign = new(
        "forbidden-sovereign", WeaponTechniqueInput.Skill1,
        T("sovereign-martial-arts", "Sovereign Martial Arts",
            WeaponTechniqueInput.Skill1, 5,
            effect: WeaponTechniqueEffect.SovereignMartialArts,
            manual: true, stageCost: 4,
            stages: new[] {
                M(AttackKind.LightOne, .70f, 12, .06f, .07f, .12f, 8, 62, 38, AttackHitboxShape.ShortArc, 54),
                M(AttackKind.LightTwo, .75f, 14, .07f, .07f, .12f, 9, 64, 40, AttackHitboxShape.ShortArc, 48),
                M(AttackKind.LightThree, .85f, 17, .08f, .08f, .13f, 11, 76, 44, AttackHitboxShape.Thrust, 58),
                M(AttackKind.LightFour, .90f, 20, .09f, .09f, .15f, 13, 78, 52, AttackHitboxShape.WideArc, 62),
                M(AttackKind.LightFive, 1.00f, 24, .10f, .09f, .16f, 16, 82, 48, AttackHitboxShape.Thrust, 60),
                M(AttackKind.LightOne, 1.05f, 28, .12f, .10f, .18f, 18, 84, 52, AttackHitboxShape.Chop, 42),
                M(AttackKind.LightTwo, 1.15f, 34, .13f, .11f, .21f, 22, 92, 58, AttackHitboxShape.Circular, 34),
                M(AttackKind.Heavy, 1.60f, 58, .18f, .13f, .38f, 46, 104, 76, AttackHitboxShape.Circular, 24)
            }),
        T("omnipresent-step", "Omnipresent Step", WeaponTechniqueInput.Skill2, 12,
            effect: WeaponTechniqueEffect.SovereignOmnipresentStep,
            stages: new[] { M(AttackKind.LightOne, 0f, 0, .02f, .01f, .20f, 0, 1, 1, AttackHitboxShape.ShortArc) }),
        T("throne-manifestation", "Throne Manifestation", WeaponTechniqueInput.Skill3, 22,
            effect: WeaponTechniqueEffect.SovereignThroneManifestation,
            stages: new[] { M(AttackKind.LightOne, 0f, 0, .15f, .01f, .25f, 0, 1, 1, AttackHitboxShape.Circular) }),
        T("zero-distance-strike", "Zero-Distance Strike", WeaponTechniqueInput.DashSkill, 20,
            effect: WeaponTechniqueEffect.SovereignZeroDistanceStrike,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .03f, .01f, .08f, 0, 1, 1, AttackHitboxShape.ShortArc, 320),
                M(AttackKind.Heavy, 1.65f, 58, .10f, .12f, .30f, 38, 96, 64, AttackHitboxShape.Thrust)
            }),
        T("paradox-impact", "Paradox Impact", WeaponTechniqueInput.Skill1Skill2, 26,
            effect: WeaponTechniqueEffect.SovereignParadoxImpact,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .05f, .01f, .05f, 0, 1, 1, AttackHitboxShape.ShortArc),
                M(AttackKind.LightOne, .55f, 18, .06f, .07f, .06f, 8, 110, 48, AttackHitboxShape.Thrust),
                M(AttackKind.LightTwo, .55f, 18, .04f, .07f, .06f, 8, 110, 48, AttackHitboxShape.Thrust),
                M(AttackKind.LightThree, .55f, 18, .04f, .07f, .08f, 8, 110, 48, AttackHitboxShape.Thrust),
                M(AttackKind.LightFour, 0f, 0, .03f, .01f, .08f, 0, 1, 1, AttackHitboxShape.ShortArc),
                M(AttackKind.Heavy, 1.45f, 52, .11f, .12f, .30f, 38, 94, 66, AttackHitboxShape.WideArc)
            }),
        T("event-horizon-palm", "Event Horizon Palm", WeaponTechniqueInput.Skill1Skill3, 30,
            effect: WeaponTechniqueEffect.SovereignEventHorizonPalm,
            stages: new[] { M(AttackKind.Heavy, 1.90f, 76, .16f, .15f, .42f, 42, 132, 78, AttackHitboxShape.Thrust) }),
        T("infinite-rift-theatre", "Infinite Rift Theatre", WeaponTechniqueInput.Skill2Skill3, 34,
            effect: WeaponTechniqueEffect.SovereignInfiniteRiftTheatre,
            stages: new[] {
                M(AttackKind.LightOne, .65f, 20, .08f, .08f, .08f, 10, 108, 50, AttackHitboxShape.Thrust),
                M(AttackKind.LightTwo, .65f, 20, .06f, .08f, .08f, 10, 108, 54, AttackHitboxShape.WideArc),
                M(AttackKind.LightThree, .65f, 22, .06f, .08f, .08f, 12, 110, 52, AttackHitboxShape.Thrust),
                M(AttackKind.LightFour, .65f, 22, .06f, .09f, .10f, 12, 112, 58, AttackHitboxShape.WideArc),
                M(AttackKind.Heavy, 1.00f, 54, .12f, .13f, .34f, 34, 126, 80, AttackHitboxShape.Circular)
            }),
        T("golden-sovereign-cataclysm", "Golden Sovereign Cataclysm",
            WeaponTechniqueInput.Ultimate, 100, full: true,
            effect: WeaponTechniqueEffect.SovereignGoldenCataclysm,
            stages: GoldenCataclysmStages()));

    public static readonly WeaponCombatProfile Raider = new(
        "raider", WeaponTechniqueInput.Skill1,
        T("savage-cleave", "Savage Cleave", WeaponTechniqueInput.Skill1, 6,
            effect: WeaponTechniqueEffect.RaiderSavageCleave,
            manual: true, stageCost: 6,
            stages: new[] {
                M(AttackKind.LightOne, 1.00f, 32, .13f, .12f, .16f, 12, 76, 62, AttackHitboxShape.StandardArc, 28),
                M(AttackKind.LightTwo, 1.14f, 42, .15f, .12f, .18f, 16, 78, 64, AttackHitboxShape.StandardArc, 30),
                M(AttackKind.Heavy, 1.42f, 68, .23f, .13f, .35f, 30, 82, 74, AttackHitboxShape.Chop, 96)
            }),
        T("hooking-axe", "Hooking Axe", WeaponTechniqueInput.Skill2, 18,
            effect: WeaponTechniqueEffect.RaiderHookingAxe,
            stages: new[] { M(AttackKind.LightTwo, .92f, 54, .19f, .11f, .31f, 0, 82, 40, AttackHitboxShape.Thrust, 35) }),
        T("buckler-ram", "Buckler Ram", WeaponTechniqueInput.Skill3, 20,
            effect: WeaponTechniqueEffect.RaiderBucklerRam,
            stages: new[] { M(AttackKind.LightThree, .88f, 60, .10f, .13f, .26f, 0, 58, 64, AttackHitboxShape.Thrust, 225) }),
        T("ravagers-rush", "Ravager's Rush", WeaponTechniqueInput.DashSkill, 26,
            effect: WeaponTechniqueEffect.RaiderRavagersRush,
            stages: new[] { M(AttackKind.LightThree, 1.12f, 72, .10f, .15f, .31f, 0, 68, 66, AttackHitboxShape.Thrust, 385) }),
        T("executioners-grip", "Executioner's Grip", WeaponTechniqueInput.Skill1Skill2, 34,
            effect: WeaponTechniqueEffect.RaiderExecutionersGrip,
            stages: new[] {
                M(AttackKind.LightOne, .82f, 28, .11f, .09f, .08f, 8, 76, 58, AttackHitboxShape.Chop, 38),
                M(AttackKind.LightTwo, .30f, 46, .08f, .09f, .08f, 0, 70, 42, AttackHitboxShape.Thrust),
                M(AttackKind.Heavy, 1.44f, 62, .11f, .12f, .34f, 24, 82, 64, AttackHitboxShape.StandardArc)
            }),
        T("skullbreaker", "Skullbreaker", WeaponTechniqueInput.Skill1Skill3, 36,
            effect: WeaponTechniqueEffect.RaiderSkullbreaker,
            stages: new[] {
                M(AttackKind.LightThree, .62f, 58, .08f, .11f, .09f, 0, 58, 62, AttackHitboxShape.Thrust, 170),
                M(AttackKind.Heavy, 1.92f, 86, .20f, .13f, .42f, 34, 86, 72, AttackHitboxShape.Chop)
            }),
        T("crowd-crusher", "Crowd Crusher", WeaponTechniqueInput.Skill2Skill3, 40,
            effect: WeaponTechniqueEffect.RaiderCrowdCrusher,
            stages: new[] {
                M(AttackKind.LightTwo, .42f, 34, .11f, .10f, .08f, 0, 80, 42, AttackHitboxShape.Thrust),
                M(AttackKind.LightFour, .25f, 30, .07f, .09f, .07f, 0, 64, 60, AttackHitboxShape.StandardArc),
                M(AttackKind.LightThree, .72f, 58, .08f, .12f, .30f, 0, 66, 66, AttackHitboxShape.Thrust)
            }),
        T("warbringers-dominion", "Warbringer's Dominion", WeaponTechniqueInput.Ultimate, 100,
            full: true, effect: WeaponTechniqueEffect.RaiderWarbringersDominion,
            stages: new[] {
                M(AttackKind.LightOne, .20f, 24, .17f, .13f, .09f, 0, 118, 118, AttackHitboxShape.Circular),
                M(AttackKind.LightTwo, .72f, 50, .10f, .16f, .09f, 0, 112, 96, AttackHitboxShape.WideArc),
                M(AttackKind.LightThree, .88f, 68, .08f, .16f, .12f, 0, 78, 72, AttackHitboxShape.Thrust, 330),
                M(AttackKind.Heavy, 2.72f, 126, .31f, .15f, .62f, 40, 112, 88, AttackHitboxShape.FrontalFan)
            }));

    public static readonly WeaponTechnique AncientStaffAstralDescent =
        T("astral-descent", "Astral Descent", WeaponTechniqueInput.Skill2, 16,
            effect: WeaponTechniqueEffect.AncientStaffAstralDescent,
            stages: new[] {
                M(AttackKind.LightThree, .70f, 30, .10f, .10f, .08f,
                    12, 74, 72, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, .60f, 38, .08f, .12f, .24f,
                    20, 104, 74, AttackHitboxShape.Circular)
            });

    public static readonly WeaponCombatProfile Spellblade = new(
        "ancient-staff", WeaponTechniqueInput.Skill1,
        T("arcane-bolt-chain", "Arcane Bolt Chain", WeaponTechniqueInput.Skill1, 5,
            effect: WeaponTechniqueEffect.AncientStaffArcaneBoltChain,
            gain: 1, manual: true, stageCost: 4,
            stages: new[] {
                R(AttackKind.LightOne, .60f, 10, .06f, .04f, .12f, 3, AncientRuneSpark),
                R(AttackKind.LightTwo, .70f, 12, .07f, .04f, .12f, 4, AncientArcanePulse),
                R(AttackKind.LightThree, .80f, 14, .08f, .05f, .13f, 5, AncientTwinGlyph),
                R(AttackKind.LightFour, .95f, 22, .10f, .05f, .16f, 8, AncientSpiralLance),
                R(AttackKind.LightFive, 1.25f, 36, .15f, .07f, .28f, 18, AncientElderBurst)
            }),
        T("aegis-ward", "Aegis Ward", WeaponTechniqueInput.Skill2, 6,
            guard: true, effect: WeaponTechniqueEffect.AncientStaffAegisWard,
            stages: new[] {
                M(AttackKind.LightTwo, 0f, 0, .03f, 3.8f, .08f,
                    0, 72, 88, AttackHitboxShape.Thrust)
            }),
        T("thunder-spirit", "Thunder Spirit", WeaponTechniqueInput.Skill3, 18,
            effect: WeaponTechniqueEffect.AncientStaffThunderSpirit,
            stages: new[] {
                M(AttackKind.LightThree, 0f, 0, .15f, .06f, .24f,
                    0, 1, 1, AttackHitboxShape.ShortArc)
            }),
        T("blink-cast", "Blink Cast", WeaponTechniqueInput.DashSkill, 20,
            effect: WeaponTechniqueEffect.AncientStaffBlinkCast,
            stages: new[] {
                R(AttackKind.Heavy, 1.10f, 36, .09f, .06f, .25f,
                    12, AncientBlinkLance)
            }),
        T("fourth-veil", "Fourth Veil", WeaponTechniqueInput.DashSkill2, 18,
            effect: WeaponTechniqueEffect.AncientStaffFourthVeil,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .06f, .04f, .14f,
                    0, 1, 1, AttackHitboxShape.ShortArc)
            }),
        T("rune-prison", "Rune Prison", WeaponTechniqueInput.Skill1Skill2, 26,
            effect: WeaponTechniqueEffect.AncientStaffRunePrison, gain: 1,
            stages: new[] {
                M(AttackKind.LightOne, .50f, 20, .18f, .09f, .20f,
                    0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.Heavy, .90f, 42, .16f, .11f, .30f,
                    0, 1, 1, AttackHitboxShape.TargetZone)
            }),
        T("spirit-bastion", "Spirit Bastion", WeaponTechniqueInput.Skill2Skill3, 34,
            guard: true, effect: WeaponTechniqueEffect.AncientStaffSpiritBastion,
            gain: 1, stages: new[] {
                M(AttackKind.LightOne, .40f, 18, .14f, .10f, .08f, 0, 1, 1, AttackHitboxShape.Circular),
                M(AttackKind.LightTwo, .35f, 16, .08f, .08f, .07f, 0, 1, 1, AttackHitboxShape.Circular),
                M(AttackKind.LightThree, .35f, 16, .07f, .08f, .07f, 0, 1, 1, AttackHitboxShape.Circular),
                M(AttackKind.LightFour, .35f, 16, .07f, .08f, .09f, 0, 1, 1, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, .90f, 52, .16f, .12f, .34f, 22, 1, 1, AttackHitboxShape.Circular)
            }),
        T("elder-invocation", "Elder Invocation", WeaponTechniqueInput.Skill1Skill3, 38,
            effect: WeaponTechniqueEffect.AncientStaffElderInvocation,
            gain: 1, stages: new[] {
                M(AttackKind.LightOne, .35f, 12, .18f, .08f, .10f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .60f, 24, .10f, .08f, .10f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .60f, 24, .10f, .08f, .14f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.Heavy, 1.20f, 64, .20f, .12f, .38f, 16, 1, 1, AttackHitboxShape.TargetZone)
            }),
        T("heavenfall-thunder-gate", "Heavenfall Thunder Gate",
            WeaponTechniqueInput.Ultimate, 100, full: true,
            effect: WeaponTechniqueEffect.AncientStaffHeavenfallThunderGate,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .18f, .06f, .10f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .60f, 38, .18f, .10f, .16f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .50f, 32, .12f, .09f, .14f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .35f, 18, .09f, .07f, .08f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .35f, 18, .08f, .07f, .08f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .35f, 18, .08f, .07f, .08f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFour, .35f, 18, .08f, .07f, .08f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFive, .35f, 18, .08f, .07f, .18f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.Heavy, 1.80f, 108, .30f, .14f, .52f, 0, 1, 1, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, 0f, 0, .03f, .02f, .30f, 0, 1, 1, AttackHitboxShape.ShortArc)
            }));

    public static readonly WeaponCombatProfile Breaker = new(
        "breaker", WeaponTechniqueInput.Skill1,
        T("iron-crush", "Iron Crush", WeaponTechniqueInput.Skill1, 8,
            effect: WeaponTechniqueEffect.BreakerIronCrush,
            manual: true, stageCost: 7,
            stages: new[] {
                M(AttackKind.LightOne, 1.24f, 48, .18f, .13f, .26f, 42, 82, 62, AttackHitboxShape.WideArc, 16),
                M(AttackKind.LightTwo, 1.48f, 68, .20f, .14f, .28f, 50, 86, 66, AttackHitboxShape.WideArc, 13),
                M(AttackKind.LightThree, 1.76f, 96, .32f, .16f, .44f, 64, 76, 82, AttackHitboxShape.Chop, 8)
            }),
        T("pendulum-swing", "Pendulum Swing", WeaponTechniqueInput.Skill2, 15,
            effect: WeaponTechniqueEffect.BreakerPendulumSwing,
            stages: new[] {
                M(AttackKind.Heavy, 1.22f, 62, .18f, .18f, .50f, 58, 92, 88, AttackHitboxShape.WideArc, 8)
            }),
        T("earthbreaker", "Earthbreaker", WeaponTechniqueInput.Skill3, 30,
            effect: WeaponTechniqueEffect.BreakerEarthbreaker,
            stages: new[] {
                M(AttackKind.Heavy, 2.08f, 118, .43f, .16f, .62f, 70, 78, 84, AttackHitboxShape.Chop)
            }),
        T("battering-rush", "Battering Rush", WeaponTechniqueInput.DashSkill, 28,
            effect: WeaponTechniqueEffect.BreakerBatteringRush,
            stages: new[] {
                M(AttackKind.Heavy, 1.38f, 72, .18f, .15f, .44f, 62, 78, 64, AttackHitboxShape.Thrust, 305)
            }),
        T("titans-backhand", "Titan's Backhand", WeaponTechniqueInput.Skill1Skill2, 36,
            effect: WeaponTechniqueEffect.BreakerTitansBackhand,
            stages: new[] {
                M(AttackKind.Heavy, 2.18f, 112, .48f, .18f, .60f, 88, 112, 84, AttackHitboxShape.WideArc, 8)
            }),
        T("anvil-fall", "Anvil Fall", WeaponTechniqueInput.Skill1Skill3, 40,
            effect: WeaponTechniqueEffect.BreakerAnvilFall,
            stages: new[] {
                M(AttackKind.Heavy, 2.52f, 152, .54f, .15f, .68f, 82, 70, 72, AttackHitboxShape.Chop, 24)
            }),
        T("cataclysm-wheel", "Cataclysm Wheel", WeaponTechniqueInput.Skill2Skill3, 44,
            effect: WeaponTechniqueEffect.BreakerCataclysmWheel,
            stages: new[] {
                M(AttackKind.Heavy, 2.20f, 126, .58f, .17f, .72f, 86, 96, 82, AttackHitboxShape.WideArc, 10)
            }),
        T("worldbreaker", "Worldbreaker", WeaponTechniqueInput.Ultimate, 100,
            full: true, effect: WeaponTechniqueEffect.BreakerWorldbreaker,
            stages: new[] {
                M(AttackKind.Heavy, 4.35f, 245, 1.55f, .18f, 1.05f, 138, 92, 96, AttackHitboxShape.Chop)
            }));

    public static readonly WeaponCombatProfile Reckoner = new(
        "reckoner", WeaponTechniqueInput.Skill1,
        T("chain-lash", "Chain Lash", WeaponTechniqueInput.Skill1, 7,
            effect: WeaponTechniqueEffect.ReckonerChainLash,
            manual: true, stageCost: 6,
            stages: new[] {
                M(AttackKind.LightOne, 1.00f, 24, .16f, .13f, .20f, 24, 132, 34, AttackHitboxShape.StandardArc, 20),
                M(AttackKind.LightTwo, 1.08f, 27, .15f, .13f, .21f, 27, 138, 34, AttackHitboxShape.StandardArc, 18),
                M(AttackKind.LightThree, 1.38f, 38, .22f, .16f, .30f, 38, 190, 36, AttackHitboxShape.Thrust, 74)
            }),
        T("orbiting-maelstrom", "Orbiting Maelstrom", WeaponTechniqueInput.Skill2,
            ReckonerTuning.OrbitActivationStamina,
            effect: WeaponTechniqueEffect.ReckonerOrbitingMaelstrom,
            stages: new[] {
                M(AttackKind.LightTwo, .62f, 18, .10f, 5.20f, .02f, 15, 128, 36, AttackHitboxShape.Circular),
                M(AttackKind.LightTwo, 0f, 0, .02f, .08f, .22f, 0, 118, 34, AttackHitboxShape.Circular)
            }),
        T("chain-harpoon", "Chain Harpoon", WeaponTechniqueInput.Skill3, 22,
            effect: WeaponTechniqueEffect.ReckonerChainHarpoon,
            stages: new[] {
                M(AttackKind.LightThree, 1.10f, 34, .24f, .34f, .04f, 20, ReckonerTuning.HarpoonMaximumDistance, 36, AttackHitboxShape.Thrust),
                M(AttackKind.LightFour, .88f, 28, .05f, .30f, .24f, 16, ReckonerTuning.HarpoonMaximumDistance, 36, AttackHitboxShape.Thrust)
            }),
        T("reapers-passage", "Reaper's Passage", WeaponTechniqueInput.DashSkill, 24,
            effect: WeaponTechniqueEffect.ReckonerReapersPassage,
            stages: new[] {
                M(AttackKind.LightFour, 1.28f, 42, .12f, .30f, .27f, 42, 154, 42, AttackHitboxShape.WideArc, 350)
            }),
        T("crescent-requiem", "Crescent Requiem", WeaponTechniqueInput.Skill1Skill2, 34,
            effect: WeaponTechniqueEffect.ReckonerCrescentRequiem,
            stages: new[] {
                M(AttackKind.LightOne, .72f, 20, .14f, .16f, .05f, 14, 74, 34, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, 1.42f, 52, .08f, .24f, .34f, 52, 188, 40, AttackHitboxShape.WideArc, 24)
            }),
        T("serpents-fang", "Serpent's Fang", WeaponTechniqueInput.Skill1Skill3, 32,
            effect: WeaponTechniqueEffect.ReckonerSerpentsFang,
            stages: new[] {
                M(AttackKind.LightThree, 1.34f, 46, .20f, .38f, .30f, 34, 214, 36, AttackHitboxShape.Thrust, 18)
            }),
        T("vortex-snare", "Vortex Snare", WeaponTechniqueInput.Skill2Skill3, 38,
            effect: WeaponTechniqueEffect.ReckonerVortexSnare,
            stages: new[] {
                M(AttackKind.LightThree, .55f, 18, .16f, .20f, .03f, 8, 210, 34, AttackHitboxShape.Thrust),
                M(AttackKind.LightOne, .48f, 24, .03f, .24f, .03f, 6, 76, 34, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .50f, 25, .03f, .24f, .03f, 6, 76, 34, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .52f, 26, .03f, .23f, .03f, 7, 78, 34, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .54f, 27, .03f, .22f, .03f, 7, 80, 34, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .56f, 28, .03f, .21f, .03f, 8, 82, 34, AttackHitboxShape.TargetZone),
                M(AttackKind.Heavy, 1.20f, 54, .06f, .24f, .32f, 44, 210, 40, AttackHitboxShape.Thrust)
            }),
        T("chains-of-judgment", "Chains of Judgment", WeaponTechniqueInput.Ultimate, 100,
            full: true, effect: WeaponTechniqueEffect.ReckonerChainsOfJudgment,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .42f, .12f, .04f, 0, 150, 34, AttackHitboxShape.Circular),
                M(AttackKind.LightOne, .66f, 30, .04f, .24f, .04f, 12, 178, 38, AttackHitboxShape.Circular),
                M(AttackKind.LightTwo, .70f, 32, .04f, .23f, .04f, 12, 188, 38, AttackHitboxShape.Circular),
                M(AttackKind.LightThree, .74f, 34, .04f, .22f, .04f, 14, 194, 40, AttackHitboxShape.Circular),
                M(AttackKind.LightFour, .78f, 36, .04f, .21f, .04f, 14, 200, 40, AttackHitboxShape.Circular),
                M(AttackKind.LightFive, .82f, 38, .04f, .20f, .04f, 16, 206, 42, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, .92f, 82, .12f, .20f, .14f, 8, 190, 54, AttackHitboxShape.Circular),
                M(AttackKind.Heavy, 2.45f, 128, .26f, .18f, .66f, 78, 176, 112, AttackHitboxShape.TargetZone, 42)
            }));

    private static WeaponTechnique T(string id, string name, WeaponTechniqueInput input,
        float stamina, params AttackDefinition[] stages) =>
        new(id, name, input, stamina, stages);

    private static WeaponTechnique T(string id, string name, WeaponTechniqueInput input,
        float stamina, AttackDefinition stage) =>
        new(id, name, input, stamina, new[] { stage });

    private static WeaponTechnique T(string id, string name, WeaponTechniqueInput input,
        float stamina, AttackDefinition first, AttackDefinition second) =>
        new(id, name, input, stamina, new[] { first, second });

    private static WeaponTechnique T(string id, string name, WeaponTechniqueInput input,
        float stamina, AttackDefinition first, AttackDefinition second, AttackDefinition third) =>
        new(id, name, input, stamina, new[] { first, second, third });

    private static WeaponTechnique T(string id, string name, WeaponTechniqueInput input,
        float stamina, AttackDefinition[] stages, bool full = false, bool guard = false,
        WeaponTechniqueEffect effect = WeaponTechniqueEffect.None, int gain = 0, int cost = 0,
        bool manual = false, float stageCost = 0f) =>
        new(id, name, input, stamina, stages, full, guard, effect, gain, cost,
            requiresRepeatedInput: manual,
            additionalStageStaminaCost: stageCost);

    private static AttackDefinition M(AttackKind kind, float damage, float poise,
        float startup, float active, float recovery, float knockback, float range,
        float thickness, AttackHitboxShape shape = AttackHitboxShape.StandardArc,
        float movement = 0f) =>
        new(kind, damage, poise, 0f, startup, active, recovery, knockback,
            range, thickness, shape, movement);

    private static AttackDefinition R(AttackKind kind, float damage, float poise,
        float startup, float active, float recovery, float knockback,
        ProjectileDefinition projectile, float movement = 0f) =>
        new(kind, damage, poise, 0f, startup, active, recovery, knockback,
            1f, 1f, AttackHitboxShape.None, movement,
            projectile.Type == ProjectileType.Arrow
                ? AttackDelivery.Arrow
                : AttackDelivery.ArcaneProjectile,
            projectile);

    private static AttackDefinition[] RapidFive() => new[]
    {
        M(AttackKind.LightOne, .58f, 6, .04f, .06f, .03f, 5, 56, 36),
        M(AttackKind.LightTwo, .6f, 6, .03f, .06f, .03f, 5, 56, 36),
        M(AttackKind.LightThree, .63f, 7, .03f, .06f, .03f, 6, 58, 38),
        M(AttackKind.LightFour, .66f, 7, .03f, .06f, .04f, 7, 60, 40),
        M(AttackKind.LightFive, 1.05f, 16, .05f, .09f, .18f, 20, 68, 46)
    };

    private static AttackDefinition[] GoldenCataclysmStages()
    {
        var stages = new AttackDefinition[19];
        stages[0] = M(AttackKind.LightOne, .30f, 16, .08f, .08f, .12f,
            0, 130, 64, AttackHitboxShape.TargetZone);
        for (int index = 0; index < AbyssRingTuning.UltimateStrikeCount; index++)
        {
            float interval = index < 4 ? .25f : index < 8 ? .17f :
                index < 12 ? .11f : .07f;
            stages[index + 1] = M(
                (AttackKind)((int)AttackKind.LightOne + index % 5),
                .30f, 14 + index, interval * .38f, interval * .30f,
                interval * .32f, 4, 132, 58, AttackHitboxShape.TargetZone);
        }
        stages[17] = M(AttackKind.Heavy, 2.10f, 130, .30f, .16f, .46f,
            64, 148, 106, AttackHitboxShape.TargetZone);
        stages[18] = M(AttackKind.LightOne, 0f, 0, .02f, .01f, .32f,
            0, 1, 1, AttackHitboxShape.ShortArc);
        return stages;
    }
}
