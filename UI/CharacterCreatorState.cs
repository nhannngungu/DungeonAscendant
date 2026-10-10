using System;
using System.Text;
using DungeonAscendant.Localization;
using DungeonAscendant.Player;

namespace DungeonAscendant.UI;

public enum CharacterCreatorRow
{
    CharacterName,
    BodyType,
    BodyBuild,
    Face,
    SkinTone,
    Hair,
    HairColor,
    FacialHair,
    FaceDetail,
    TorsoWidth,
    ArmThickness,
    LegThickness,
    ApparentHeight,
    PreviewFacing,
    Randomize,
    ResetDefault,
    Confirm
}

/// <summary>
/// Keyboard navigation and draft state for the character-creation screen.
/// </summary>
public sealed class CharacterCreatorState
{
    private static readonly string[] CharacterNames =
    {
        "ALDRIC",
        "MARA",
        "ROWAN",
        "SEREN",
        "CORVIN",
        "LYRA",
        "GARRAN",
        "ELARA",
        "THORNE",
        "VESPER",
        "BRAN",
        "ISOLDE"
    };
    private readonly Random _random = new();

    public CharacterAppearance Draft { get; private set; } =
        CharacterAppearance.CreateDefault();
    public CharacterCreatorRow SelectedRow { get; private set; }
    public FacingDirection PreviewFacing { get; private set; } =
        FacingDirection.Right;
    public string CharacterName { get; private set; } = CharacterNames[0];

    public void Begin(int slotIndex)
    {
        Draft = CharacterAppearance.CreateDefault();
        SelectedRow = CharacterCreatorRow.CharacterName;
        PreviewFacing = FacingDirection.Right;
        CharacterName = CharacterNames[
            Math.Clamp(slotIndex, 0, CharacterNames.Length - 1)];
    }

    public void MoveSelection(int delta)
    {
        int count = Enum.GetValues<CharacterCreatorRow>().Length;
        int next = ((int)SelectedRow + delta) % count;
        if (next < 0)
            next += count;
        SelectedRow = (CharacterCreatorRow)next;
    }

    public void Adjust(int delta)
    {
        switch (SelectedRow)
        {
            case CharacterCreatorRow.CharacterName:
                CharacterName = CycleName(CharacterName, delta);
                break;
            case CharacterCreatorRow.BodyType:
                Draft.BodyType = Cycle(Draft.BodyType, delta);
                Draft.Normalize();
                break;
            case CharacterCreatorRow.BodyBuild:
                Draft.BodyBuild = Cycle(Draft.BodyBuild, delta);
                break;
            case CharacterCreatorRow.Face:
                Draft.FacePreset = Cycle(Draft.FacePreset, delta);
                break;
            case CharacterCreatorRow.SkinTone:
                Draft.SkinTone = Cycle(Draft.SkinTone, delta);
                break;
            case CharacterCreatorRow.Hair:
                Draft.HairStyle = Cycle(Draft.HairStyle, delta);
                break;
            case CharacterCreatorRow.HairColor:
                Draft.HairColor = Cycle(Draft.HairColor, delta);
                break;
            case CharacterCreatorRow.FacialHair:
                Draft.FacialHair = Draft.BodyType == CharacterBodyType.Female
                    ? FacialHairStyle.None
                    : Cycle(Draft.FacialHair, delta);
                break;
            case CharacterCreatorRow.FaceDetail:
                Draft.FaceDetail = Cycle(Draft.FaceDetail, delta);
                break;
            case CharacterCreatorRow.TorsoWidth:
                Draft.TorsoWidth = Cycle(Draft.TorsoWidth, delta);
                break;
            case CharacterCreatorRow.ArmThickness:
                Draft.ArmThickness = Cycle(Draft.ArmThickness, delta);
                break;
            case CharacterCreatorRow.LegThickness:
                Draft.LegThickness = Cycle(Draft.LegThickness, delta);
                break;
            case CharacterCreatorRow.ApparentHeight:
                Draft.ApparentHeight = Cycle(Draft.ApparentHeight, delta);
                break;
            case CharacterCreatorRow.PreviewFacing:
                PreviewFacing = PreviewFacing == FacingDirection.Right
                    ? FacingDirection.Left
                    : FacingDirection.Right;
                break;
        }
    }

    public void Randomize()
    {
        Draft.Randomize(_random);
        CharacterName = CharacterNames[_random.Next(CharacterNames.Length)];
    }

    public void ResetToDefault()
    {
        Draft = CharacterAppearance.CreateDefault();
        PreviewFacing = FacingDirection.Right;
        CharacterName = CharacterNames[0];
    }

    public static string GetRowLabel(CharacterCreatorRow row)
    {
        string key = row switch
        {
            CharacterCreatorRow.CharacterName => "name",
            CharacterCreatorRow.BodyType => "body_type",
            CharacterCreatorRow.BodyBuild => "body_build",
            CharacterCreatorRow.Face => "face",
            CharacterCreatorRow.SkinTone => "skin_tone",
            CharacterCreatorRow.Hair => "hair",
            CharacterCreatorRow.HairColor => "hair_color",
            CharacterCreatorRow.FacialHair => "facial_hair",
            CharacterCreatorRow.FaceDetail => "face_detail",
            CharacterCreatorRow.TorsoWidth => "torso_width",
            CharacterCreatorRow.ArmThickness => "arm_thickness",
            CharacterCreatorRow.LegThickness => "leg_thickness",
            CharacterCreatorRow.ApparentHeight => "height",
            CharacterCreatorRow.PreviewFacing => "preview_facing",
            CharacterCreatorRow.Randomize => "randomize",
            CharacterCreatorRow.ResetDefault => "reset_default",
            _ => "confirm"
        };
        return Loc.Get("character.row." + key);
    }

    public string GetValueLabel(CharacterCreatorRow row)
    {
        return row switch
        {
            CharacterCreatorRow.CharacterName => CharacterName,
            CharacterCreatorRow.BodyType => SplitName(Draft.BodyType),
            CharacterCreatorRow.BodyBuild => SplitName(Draft.BodyBuild),
            CharacterCreatorRow.Face => SplitName(Draft.FacePreset),
            CharacterCreatorRow.SkinTone => SplitName(Draft.SkinTone),
            CharacterCreatorRow.Hair => SplitName(Draft.HairStyle),
            CharacterCreatorRow.HairColor => SplitName(Draft.HairColor),
            CharacterCreatorRow.FacialHair => SplitName(Draft.FacialHair),
            CharacterCreatorRow.FaceDetail => SplitName(Draft.FaceDetail),
            CharacterCreatorRow.TorsoWidth => SplitName(Draft.TorsoWidth),
            CharacterCreatorRow.ArmThickness => SplitName(Draft.ArmThickness),
            CharacterCreatorRow.LegThickness => SplitName(Draft.LegThickness),
            CharacterCreatorRow.ApparentHeight => SplitName(Draft.ApparentHeight),
            CharacterCreatorRow.PreviewFacing =>
                Loc.Get(PreviewFacing == FacingDirection.Right
                    ? "character.value.right"
                    : "character.value.left"),
            _ => string.Empty
        };
    }

    private static T Cycle<T>(T current, int delta) where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        int currentIndex = Array.IndexOf(values, current);
        int next = (currentIndex + delta) % values.Length;
        if (next < 0)
            next += values.Length;
        return values[next];
    }

    private static string CycleName(string current, int delta)
    {
        int currentIndex = Array.IndexOf(CharacterNames, current);
        int next = (Math.Max(0, currentIndex) + delta) % CharacterNames.Length;
        if (next < 0)
            next += CharacterNames.Length;
        return CharacterNames[next];
    }

    private static string SplitName<T>(T value) where T : struct, Enum
    {
        return Loc.Get("character.value." + ToSnakeCase(value.ToString()));
    }

    private static string ToSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 4);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (index > 0 && char.IsUpper(character))
                result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
