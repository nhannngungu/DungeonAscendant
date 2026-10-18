using Microsoft.Xna.Framework;

namespace DungeonAscendant.Combat;

/// <summary>
/// The definitive canonical weapon move table. Values are intentionally centralized
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
    private static readonly ProjectileDefinition AstralCross = new(
        ProjectileType.ArcaneBolt, 920f, new Vector2(72f, 72f), 2.2f,
        ProjectileTrajectoryType.Penetrating, remainingPierces: 2,
        impactRadius: 34f);
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
    public static readonly WeaponCombatProfile KnightSwordAndShield = new(
        "knight-sword-and-shield", WeaponTechniqueInput.Skill1,
        T("knights-combo", "Knight's Combo", WeaponTechniqueInput.Skill1, 6,
            new[] {
                M(AttackKind.LightOne, 1f, 18, .09f, .10f, .17f, 16, 78, 48, AttackHitboxShape.StandardArc, 42),
                M(AttackKind.LightTwo, 1.05f, 20, .10f, .10f, .18f, 18, 80, 50, AttackHitboxShape.StandardArc, 38),
                M(AttackKind.LightThree, 1.15f, 24, .12f, .10f, .20f, 20, 96, 40, AttackHitboxShape.Thrust, 56),
                M(AttackKind.LightFour, 1.45f, 40, .18f, .13f, .34f, 38, 88, 60, AttackHitboxShape.Chop, 115)
            }, manual: true, stageCost: 6,
            effect: WeaponTechniqueEffect.KnightCombo),
        T("shield-guard", "Shield Guard", WeaponTechniqueInput.Skill2, 4,
            new[] { M(AttackKind.LightTwo, 0f, 0, 0f, .30f, .08f, 0, 54, 72, AttackHitboxShape.Thrust) },
            guard: true, effect: WeaponTechniqueEffect.KnightShieldGuard),
        T("shield-bash", "Shield Bash", WeaponTechniqueInput.Skill3, 14,
            new[] { M(AttackKind.LightThree, .90f, 64, .10f, .11f, .24f, 46, 64, 62, AttackHitboxShape.Thrust, 125) },
            effect: WeaponTechniqueEffect.KnightShieldBash),
        T("vanguard-slash", "Vanguard Slash", WeaponTechniqueInput.DashSkill, 24,
            new[] { M(AttackKind.LightOne, 1.55f, 38, .06f, .13f, .25f, 38, 104, 48, AttackHitboxShape.StandardArc, 390) },
            effect: WeaponTechniqueEffect.KnightVanguardSlash),
        T("guard-counter", "Guard Counter", WeaponTechniqueInput.Skill1Skill2, 26,
            new[] {
                M(AttackKind.LightOne, .35f, 48, .07f, .11f, .08f, 32, 62, 66, AttackHitboxShape.Thrust, 110),
                M(AttackKind.LightTwo, .75f, 30, .08f, .10f, .10f, 22, 82, 50, AttackHitboxShape.StandardArc, 48),
                M(AttackKind.LightThree, .95f, 38, .09f, .11f, .22f, 30, 86, 54, AttackHitboxShape.StandardArc, 38)
            }, guard: true, effect: WeaponTechniqueEffect.KnightGuardCounter),
        T("blade-breaker", "Blade Breaker", WeaponTechniqueInput.Skill1Skill3, 32,
            new[] {
                M(AttackKind.LightOne, .85f, 30, .10f, .11f, .10f, 20, 84, 52, AttackHitboxShape.StandardArc, 42),
                M(AttackKind.LightTwo, .50f, 54, .08f, .10f, .10f, 36, 62, 66, AttackHitboxShape.Thrust, 105),
                M(AttackKind.Heavy, 1.25f, 44, .13f, .12f, .30f, 30, 100, 42, AttackHitboxShape.Thrust, 58)
            }, effect: WeaponTechniqueEffect.KnightBladeBreaker),
        T("shield-wall", "Shield Wall", WeaponTechniqueInput.Skill2Skill3, 30,
            new[] {
                M(AttackKind.LightOne, 0f, 0, .06f, .28f, .06f, 0, 58, 76, AttackHitboxShape.Thrust),
                M(AttackKind.LightTwo, .55f, 64, .08f, .12f, .08f, 50, 66, 72, AttackHitboxShape.Thrust, 135),
                M(AttackKind.LightThree, .45f, 34, .04f, .10f, .24f, 34, 84, 74, AttackHitboxShape.WideArc, 25)
            }, guard: true, effect: WeaponTechniqueEffect.KnightShieldWall),
        T("knights-final-oath", "KNIGHT'S FINAL OATH", WeaponTechniqueInput.Ultimate, 100,
            new[] {
                M(AttackKind.LightOne, 0f, 0, .02f, .18f, .08f, 0, 58, 76, AttackHitboxShape.Thrust),
                M(AttackKind.LightTwo, .80f, 74, .06f, .12f, .08f, 24, 104, 68, AttackHitboxShape.Thrust, 350),
                M(AttackKind.LightOne, .55f, 28, .05f, .09f, .06f, 12, 92, 48, AttackHitboxShape.StandardArc, 40),
                M(AttackKind.LightTwo, .55f, 28, .04f, .09f, .06f, 12, 92, 48, AttackHitboxShape.StandardArc, 38),
                M(AttackKind.LightThree, .70f, 36, .05f, .10f, .08f, 16, 98, 48, AttackHitboxShape.Thrust, 48),
                M(AttackKind.LightFour, 0f, 0, .03f, .05f, .12f, 0, 72, 50, AttackHitboxShape.StandardArc),
                M(AttackKind.Heavy, 1.90f, 96, .16f, .14f, .22f, 44, 108, 76, AttackHitboxShape.Chop, 45),
                M(AttackKind.LightThree, .40f, 42, .03f, .10f, .12f, 30, 96, 84, AttackHitboxShape.Circular),
                M(AttackKind.LightOne, 0f, 0, .02f, .08f, .24f, 0, 58, 76, AttackHitboxShape.Thrust)
            }, full: true, guard: true,
            effect: WeaponTechniqueEffect.KnightFinalOath));

    public static readonly WeaponTechnique KnightGuardRiposte = T(
        "guard-riposte", "Guard Riposte", WeaponTechniqueInput.Skill1, 0,
        new[] { M(AttackKind.LightThree, 1.60f, 52, .035f, .11f, .20f,
            34, 98, 44, AttackHitboxShape.Thrust, 90) },
        effect: WeaponTechniqueEffect.KnightGuardRiposte);

    public static readonly WeaponTechnique KnightFallingShield = T(
        "falling-shield", "Falling Shield", WeaponTechniqueInput.Skill2, 16,
        new[] { M(AttackKind.Heavy, 1.25f, 66, .05f, .12f, .24f,
            42, 82, 76, AttackHitboxShape.Circular, 90) },
        guard: true, effect: WeaponTechniqueEffect.KnightFallingShield);

    public static readonly WeaponTechnique DuelistPhantomRiposte = T(
        "phantom-riposte", "Phantom Riposte", WeaponTechniqueInput.Skill1, 0,
        effect: WeaponTechniqueEffect.DuelistPhantomRiposte,
        stages: new[] {
            M(AttackKind.LightFive, 1.35f, 28, .035f, .10f, .16f, 24,
                86, 54, AttackHitboxShape.Cross, 165)
        });

    public static readonly WeaponTechnique DuelistFallingStar = T(
        "falling-star", "Falling Star", WeaponTechniqueInput.Skill2, 18,
        effect: WeaponTechniqueEffect.DuelistFallingStar,
        stages: new[] {
            M(AttackKind.Heavy, 1.50f, 34, .08f, .14f, .24f, 30,
                84, 76, AttackHitboxShape.Chop, 120)
        });

    public static readonly WeaponCombatProfile AstralDuelist = new(
        "astral-phantom-duelist", WeaponTechniqueInput.Skill1,
        T("starburst-sword-art", "Starburst Sword Art",
            WeaponTechniqueInput.Skill1, 3, gain: 5, manual: true, stageCost: 3,
            effect: WeaponTechniqueEffect.DuelistStarburstSwordArt,
            stages: new[] {
                M(AttackKind.LightOne, .65f, 7, .045f, .07f, .08f, 5, 64, 34, AttackHitboxShape.StandardArc, 44),
                M(AttackKind.LightTwo, .70f, 7, .04f, .07f, .08f, 5, 62, 32, AttackHitboxShape.ShortArc, 40),
                M(AttackKind.LightThree, .75f, 8, .04f, .075f, .075f, 6, 66, 38, AttackHitboxShape.StandardArc, 46),
                M(AttackKind.LightFour, .85f, 9, .045f, .08f, .08f, 7, 68, 42, AttackHitboxShape.WideArc, 42),
                M(AttackKind.LightOne, .90f, 10, .04f, .08f, .075f, 7, 66, 38, AttackHitboxShape.ShortArc, 48),
                M(AttackKind.LightTwo, .95f, 11, .04f, .085f, .08f, 8, 70, 42, AttackHitboxShape.StandardArc, 46),
                M(AttackKind.LightFour, 1.10f, 14, .05f, .09f, .09f, 12, 74, 50, AttackHitboxShape.WideArc, 52),
                M(AttackKind.LightFive, 1.55f, 24, .07f, .12f, .19f, 28, 82, 60, AttackHitboxShape.Cross, 58)
            }),
        T("phantom-parry", "Phantom Parry", WeaponTechniqueInput.Skill2, 8,
            guard: true, effect: WeaponTechniqueEffect.DuelistPhantomParry,
            stages: new[] {
                M(AttackKind.LightTwo, 0f, 0, .01f, .28f, .13f, 0, 64, 72, AttackHitboxShape.Cross)
            }),
        T("blade-overdrive", "Blade Overdrive", WeaponTechniqueInput.Skill3, 20,
            effect: WeaponTechniqueEffect.DuelistBladeOverdrive,
            stages: new[] {
                M(AttackKind.LightFour, 0f, 0, .08f, .10f, .20f, 0, 54, 70, AttackHitboxShape.Circular)
            }),
        T("flash-blade", "Flash Blade", WeaponTechniqueInput.DashSkill, 20,
            effect: WeaponTechniqueEffect.DuelistFlashBlade,
            stages: new[] {
                M(AttackKind.LightOne, .85f, 14, .035f, .09f, .055f, 8, 76, 42, AttackHitboxShape.Thrust, 480),
                M(AttackKind.LightFive, 1.05f, 22, .045f, .10f, .17f, 20, 82, 54, AttackHitboxShape.Cross, 90)
            }),
        T("cross-requiem", "Cross Requiem", WeaponTechniqueInput.Skill1Skill2, 28,
            guard: true, effect: WeaponTechniqueEffect.DuelistCrossRequiem,
            stages: new[] {
                M(AttackKind.LightOne, 0f, 0, .02f, .15f, .035f, 0, 62, 70, AttackHitboxShape.Cross),
                M(AttackKind.LightTwo, .55f, 9, .035f, .075f, .045f, 5, 72, 42, AttackHitboxShape.StandardArc, 80),
                M(AttackKind.LightFour, .75f, 13, .035f, .08f, .05f, 8, 76, 48, AttackHitboxShape.WideArc, 75),
                M(AttackKind.LightFive, 1.45f, 30, .055f, .11f, .20f, 30, 88, 62, AttackHitboxShape.Cross, 55)
            }),
        T("heaven-splitter", "Heaven Splitter", WeaponTechniqueInput.Skill1Skill3, 34,
            effect: WeaponTechniqueEffect.DuelistHeavenSplitter,
            stages: new[] {
                R(AttackKind.Heavy, 2.20f, 54, .08f, .08f, .34f, 38, AstralCross)
            }),
        T("thousand-edge-waltz", "Thousand Edge Waltz", WeaponTechniqueInput.Skill2Skill3, 36,
            effect: WeaponTechniqueEffect.DuelistThousandEdgeWaltz,
            stages: new[] {
                M(AttackKind.LightOne, .45f, 7, .04f, .07f, .035f, 3, 76, 54, AttackHitboxShape.Circular, 95),
                M(AttackKind.LightTwo, .45f, 7, .03f, .07f, .035f, 3, 76, 54, AttackHitboxShape.Circular, 85),
                M(AttackKind.LightThree, .45f, 7, .03f, .07f, .03f, 3, 78, 56, AttackHitboxShape.Circular, 78),
                M(AttackKind.LightFour, .45f, 7, .03f, .07f, .03f, 3, 78, 56, AttackHitboxShape.Circular, 72),
                M(AttackKind.LightOne, .45f, 7, .03f, .07f, .03f, 3, 80, 58, AttackHitboxShape.Circular, 68),
                M(AttackKind.LightTwo, .45f, 7, .03f, .07f, .035f, 3, 80, 58, AttackHitboxShape.Circular, 62),
                M(AttackKind.LightFive, 1.10f, 24, .055f, .11f, .19f, 24, 90, 68, AttackHitboxShape.Cross, 48)
            }),
        T("infinite-starburst-ascension", "Infinite Starburst Ascension",
            WeaponTechniqueInput.Ultimate, 100, full: true,
            effect: WeaponTechniqueEffect.DuelistInfiniteStarburst,
            stages: InfiniteStarburstStages()));

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

    public static readonly WeaponTechnique ScytheCrescentDive =
        T("crescent-dive", "Crescent Dive", WeaponTechniqueInput.Skill2, 14,
            effect: WeaponTechniqueEffect.ScytheCrescentDive,
            stages: new[] {
                M(AttackKind.Heavy, 1.25f, 32, .08f, .12f, .22f, 18, 96, 76, AttackHitboxShape.WideArc, 235)
            });

    public static readonly WeaponTechnique ScytheWraithCut =
        T("wraith-cut", "Wraith Cut", WeaponTechniqueInput.Skill1, 0,
            effect: WeaponTechniqueEffect.ScytheWraithCut,
            stages: new[] {
                M(AttackKind.LightFive, 1.45f, 28, .045f, .10f, .18f, 16, 105, 68, AttackHitboxShape.WideArc, 115)
            });

    public static readonly WeaponCombatProfile Scythe = new(
        "shadow-reaper", WeaponTechniqueInput.Skill1,
        T("reapers-dance", "Reaper's Dance", WeaponTechniqueInput.Skill1, 4,
            effect: WeaponTechniqueEffect.ScytheReapersDance,
            manual: true, stageCost: 4,
            stages: new[] {
                M(AttackKind.LightOne, .65f, 12, .05f, .075f, .13f, 7, 92, 54, AttackHitboxShape.WideArc, 68),
                M(AttackKind.LightTwo, .70f, 13, .055f, .08f, .14f, 8, 94, 56, AttackHitboxShape.WideArc, 62),
                M(AttackKind.LightThree, .85f, 16, .065f, .09f, .15f, 9, 98, 70, AttackHitboxShape.Circular, 72),
                M(AttackKind.LightFour, .95f, 18, .07f, .10f, .16f, 10, 102, 74, AttackHitboxShape.Circular, 58),
                M(AttackKind.LightFive, 1.05f, 22, .08f, .10f, .19f, 12, 96, 62, AttackHitboxShape.WideArc, 38),
                M(AttackKind.Heavy, 1.55f, 36, .09f, .12f, .24f, 22, 108, 78, AttackHitboxShape.WideArc, 34)
            }),
        T("shadow-slip", "Shadow Slip", WeaponTechniqueInput.Skill2, 12,
            effect: WeaponTechniqueEffect.ScytheShadowSlip,
            stages: new[] {
                M(AttackKind.LightTwo, 0f, 0, .02f, .14f, .10f, 0, 68, 42, AttackHitboxShape.ShortArc, 520)
            }),
        T("death-spiral", "Death Spiral", WeaponTechniqueInput.Skill3, 24,
            effect: WeaponTechniqueEffect.ScytheDeathSpiral,
            stages: new[] {
                M(AttackKind.LightOne, .40f, 10, .12f, .10f, .08f, 4, 91, 72, AttackHitboxShape.Circular, 24),
                M(AttackKind.LightTwo, .40f, 10, .07f, .10f, .08f, 4, 92, 72, AttackHitboxShape.Circular, 22),
                M(AttackKind.LightThree, .40f, 10, .07f, .10f, .08f, 4, 93, 74, AttackHitboxShape.Circular, 22),
                M(AttackKind.LightFour, .40f, 11, .07f, .10f, .08f, 4, 94, 74, AttackHitboxShape.Circular, 20),
                M(AttackKind.LightFive, .40f, 11, .07f, .10f, .08f, 4, 96, 76, AttackHitboxShape.Circular, 18),
                M(AttackKind.Heavy, .70f, 22, .08f, .12f, .24f, 14, 104, 78, AttackHitboxShape.WideArc, 18)
            }),
        T("shadow-pass", "Shadow Pass", WeaponTechniqueInput.DashSkill, 20,
            effect: WeaponTechniqueEffect.ScytheShadowPass,
            stages: new[] {
                M(AttackKind.LightOne, .85f, 18, .045f, .09f, .08f, 7, 91, 48, AttackHitboxShape.WideArc, 410),
                M(AttackKind.LightFive, 1.05f, 25, .06f, .10f, .18f, 13, 98, 60, AttackHitboxShape.WideArc, 110)
            }),
        T("phantom-cross", "Phantom Cross", WeaponTechniqueInput.Skill1Skill2, 26,
            effect: WeaponTechniqueEffect.ScythePhantomCross,
            stages: new[] {
                M(AttackKind.LightTwo, .80f, 17, .08f, .10f, .10f, 8, 92, 58, AttackHitboxShape.WideArc, 185),
                M(AttackKind.Heavy, 1.20f, 29, .09f, .12f, .23f, 18, 104, 70, AttackHitboxShape.Cross, 95)
            }),
        T("abyssal-requiem", "Abyssal Requiem", WeaponTechniqueInput.Skill1Skill3, 34,
            effect: WeaponTechniqueEffect.ScytheAbyssalRequiem,
            stages: new[] {
                M(AttackKind.LightOne, .40f, 12, .08f, .09f, .08f, 0, 118, 40, AttackHitboxShape.Thrust, 80),
                M(AttackKind.LightThree, .90f, 23, .08f, .10f, .09f, 8, 96, 70, AttackHitboxShape.WideArc, 105),
                M(AttackKind.LightFour, .65f, 16, .06f, .09f, .07f, 6, 94, 72, AttackHitboxShape.Circular, 38),
                M(AttackKind.LightFour, .65f, 16, .06f, .09f, .07f, 6, 94, 72, AttackHitboxShape.Circular, 34),
                M(AttackKind.Heavy, 1.50f, 42, .09f, .12f, .24f, 24, 108, 78, AttackHitboxShape.WideArc, 42),
                M(AttackKind.LightFive, .65f, 20, .045f, .08f, .16f, 10, 112, 68, AttackHitboxShape.WideArc, 20)
            }),
        T("eclipse-waltz", "Eclipse Waltz", WeaponTechniqueInput.Skill2Skill3, 32,
            effect: WeaponTechniqueEffect.ScytheEclipseWaltz,
            stages: new[] {
                M(AttackKind.LightOne, .55f, 12, .06f, .09f, .08f, 5, 88, 64, AttackHitboxShape.Circular, 115),
                M(AttackKind.LightTwo, .65f, 14, .06f, .09f, .08f, 5, 92, 68, AttackHitboxShape.Circular, 90),
                M(AttackKind.LightThree, .60f, 14, .055f, .09f, .07f, 5, 94, 70, AttackHitboxShape.Circular, 70),
                M(AttackKind.LightFour, .60f, 14, .055f, .09f, .08f, 5, 96, 72, AttackHitboxShape.Circular, 66),
                M(AttackKind.Heavy, .90f, 28, .08f, .12f, .22f, 16, 104, 76, AttackHitboxShape.WideArc, 48)
            }),
        T("umbra-devourer", "UMBRA DEVOURER", WeaponTechniqueInput.Ultimate, 100,
            full: true, effect: WeaponTechniqueEffect.ScytheUmbraDevourer,
            stages: new[] {
                M(AttackKind.LightOne, .70f, 24, .08f, .10f, .08f, 0, 104, 70, AttackHitboxShape.TargetZone),
                M(AttackKind.LightTwo, .45f, 16, .08f, .09f, .07f, 0, 104, 70, AttackHitboxShape.TargetZone),
                M(AttackKind.LightThree, .45f, 16, .07f, .09f, .07f, 0, 104, 70, AttackHitboxShape.TargetZone),
                M(AttackKind.LightFour, .45f, 16, .07f, .09f, .08f, 0, 104, 70, AttackHitboxShape.TargetZone),
                M(AttackKind.LightOne, .90f, 28, .07f, .10f, .07f, 0, 108, 74, AttackHitboxShape.Cross),
                M(AttackKind.LightTwo, .90f, 28, .06f, .10f, .08f, 0, 108, 74, AttackHitboxShape.Cross),
                M(AttackKind.Heavy, 1.40f, 46, .10f, .12f, .28f, 0, 116, 82, AttackHitboxShape.WideArc),
                M(AttackKind.LightOne, 0f, 0, .02f, .02f, .18f, 0, 1, 1, AttackHitboxShape.ShortArc)
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

    private static AttackDefinition[] InfiniteStarburstStages()
    {
        var stages = new AttackDefinition[DuelistTuning.UltimateStrikeCount + 1];
        for (int index = 0; index < DuelistTuning.UltimateStrikeCount; index++)
        {
            float interval = index < 8 ? .19f : index < 16 ? .145f :
                index < 24 ? .105f : .075f;
            float damage = index < 8 ? .12f : index < 16 ? .13f :
                index < 24 ? .14f : .16f;
            stages[index] = M(
                (AttackKind)((int)AttackKind.LightOne + index % 5),
                damage, 10,
                interval * .30f, interval * .38f, interval * .32f,
                2, 112, 62, AttackHitboxShape.TargetZone);
        }
        stages[DuelistTuning.UltimateStrikeCount] = M(
            AttackKind.Heavy, 2.40f, 72, .72f, .14f, .64f, 46,
            124, 104, AttackHitboxShape.Cross);
        return stages;
    }
}
