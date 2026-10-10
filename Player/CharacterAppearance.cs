using System;

namespace DungeonAscendant.Player;

public enum CharacterBodyType
{
    Male,
    Female
}

public enum BodyBuild
{
    Slim,
    Normal,
    Broad
}

public enum FacePreset
{
    Weathered,
    Angular,
    Round,
    Narrow,
    Strong
}

public enum SkinTone
{
    Fair,
    Light,
    Warm,
    Tan,
    Brown,
    Deep
}

public enum HairStyle
{
    Short,
    Medium,
    Long,
    Messy,
    TiedBack,
    Undercut,
    Bald
}

public enum HairColor
{
    Black,
    DarkBrown,
    Brown,
    LightBrown,
    Gray,
    White,
    DarkRed
}

public enum FacialHairStyle
{
    None,
    LightStubble,
    ShortBeard,
    FullBeard,
    Mustache
}

public enum FaceDetail
{
    None,
    EyebrowScar,
    CheekScar,
    EyeScar,
    SmallFacialMark
}

public enum VisualProportion
{
    Slim,
    Normal,
    Broad
}

public enum ApparentHeight
{
    Short,
    Average,
    Tall
}

/// <summary>
/// Serializable, gameplay-neutral description of the player's base look.
/// Equipment remains a separate layer and collision always comes from Player.
/// </summary>
public sealed class CharacterAppearance
{
    public CharacterBodyType BodyType { get; set; } = CharacterBodyType.Male;
    public BodyBuild BodyBuild { get; set; } = BodyBuild.Normal;
    public FacePreset FacePreset { get; set; } = FacePreset.Weathered;
    public SkinTone SkinTone { get; set; } = SkinTone.Tan;
    public HairStyle HairStyle { get; set; } = HairStyle.Short;
    public HairColor HairColor { get; set; } = HairColor.DarkBrown;
    public FacialHairStyle FacialHair { get; set; } = FacialHairStyle.None;
    public FaceDetail FaceDetail { get; set; } = FaceDetail.EyebrowScar;
    public VisualProportion TorsoWidth { get; set; } = VisualProportion.Normal;
    public VisualProportion ArmThickness { get; set; } = VisualProportion.Normal;
    public VisualProportion LegThickness { get; set; } = VisualProportion.Normal;
    public ApparentHeight ApparentHeight { get; set; } = ApparentHeight.Average;

    public static CharacterAppearance CreateDefault() => new();

    public CharacterAppearance Clone()
    {
        return new CharacterAppearance
        {
            BodyType = BodyType,
            BodyBuild = BodyBuild,
            FacePreset = FacePreset,
            SkinTone = SkinTone,
            HairStyle = HairStyle,
            HairColor = HairColor,
            FacialHair = FacialHair,
            FaceDetail = FaceDetail,
            TorsoWidth = TorsoWidth,
            ArmThickness = ArmThickness,
            LegThickness = LegThickness,
            ApparentHeight = ApparentHeight
        };
    }

    public void Normalize()
    {
        BodyType = ValidOrDefault(BodyType, CharacterBodyType.Male);
        BodyBuild = ValidOrDefault(BodyBuild, BodyBuild.Normal);
        FacePreset = ValidOrDefault(FacePreset, FacePreset.Weathered);
        SkinTone = ValidOrDefault(SkinTone, SkinTone.Tan);
        HairStyle = ValidOrDefault(HairStyle, HairStyle.Short);
        HairColor = ValidOrDefault(HairColor, HairColor.DarkBrown);
        FacialHair = ValidOrDefault(FacialHair, FacialHairStyle.None);
        FaceDetail = ValidOrDefault(FaceDetail, FaceDetail.EyebrowScar);
        TorsoWidth = ValidOrDefault(TorsoWidth, VisualProportion.Normal);
        ArmThickness = ValidOrDefault(ArmThickness, VisualProportion.Normal);
        LegThickness = ValidOrDefault(LegThickness, VisualProportion.Normal);
        ApparentHeight = ValidOrDefault(ApparentHeight, ApparentHeight.Average);

        // Facial-hair silhouettes are authored for the male head profile.
        if (BodyType == CharacterBodyType.Female)
            FacialHair = FacialHairStyle.None;
    }

    public void Randomize(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        BodyType = Pick<CharacterBodyType>(random);
        BodyBuild = Pick<BodyBuild>(random);
        FacePreset = Pick<FacePreset>(random);
        SkinTone = Pick<SkinTone>(random);
        HairStyle = Pick<HairStyle>(random);
        HairColor = Pick<HairColor>(random);
        FacialHair = BodyType == CharacterBodyType.Female
            ? FacialHairStyle.None
            : Pick<FacialHairStyle>(random);
        FaceDetail = Pick<FaceDetail>(random);
        TorsoWidth = Pick<VisualProportion>(random);
        ArmThickness = Pick<VisualProportion>(random);
        LegThickness = Pick<VisualProportion>(random);
        ApparentHeight = Pick<ApparentHeight>(random);
        Normalize();
    }

    private static T Pick<T>(Random random) where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        return values[random.Next(values.Length)];
    }

    private static T ValidOrDefault<T>(T value, T defaultValue)
        where T : struct, Enum
    {
        return Enum.IsDefined(value) ? value : defaultValue;
    }
}
