using System;
using System.IO;

namespace DungeonAscendant.Localization;

public static class LocalizationValidation
{
    public static void ValidateOrThrow()
    {
        var english = Loc.GetStrings(Language.English);
        var vietnamese = Loc.GetStrings(Language.Vietnamese);
        foreach (string key in english.Keys)
        {
            Need(vietnamese.ContainsKey(key), $"Vietnamese string missing: {key}");
            Need(vietnamese[key] != null, $"Vietnamese string null: {key}");
        }
        foreach (string key in vietnamese.Keys)
            Need(english.ContainsKey(key), $"English string missing: {key}");

        string directory = Path.Combine(
            Path.GetTempPath(),
            "DungeonAscendantLocalizationValidation");
        string path = Path.Combine(directory, $"settings-{Guid.NewGuid():N}.json");
        Loc.UseSettingsPathForValidation(path);
        Loc.SetLanguage(Language.Vietnamese);
        Loc.SetMasterVolume(.8f);
        Loc.SetMusicVolume(.5f);
        Loc.SetSfxVolume(.6f);
        Need(Loc.Get("settings.language") == "NGÔN NGỮ",
            "Language did not switch immediately.");
        Loc.UseSettingsPathForValidation(path);
        Need(Loc.CurrentLanguage == Language.Vietnamese,
            "Global language setting did not persist.");
        Need(Math.Abs(Loc.SfxVolume - .6f) < .001f,
            "Global SFX volume did not persist.");
        Need(Math.Abs(Loc.MasterVolume - .8f) < .001f &&
            Math.Abs(Loc.MusicVolume - .5f) < .001f,
            "Global master/music volumes did not persist.");
        Loc.SetLanguage(Language.English);
        Loc.SetMasterVolume(1f);
        Loc.SetMusicVolume(.55f);
        Loc.SetSfxVolume(.75f);
        if (File.Exists(path))
            File.Delete(path);
        if (Directory.Exists(directory) && Directory.GetFiles(directory).Length == 0)
            Directory.Delete(directory);
    }

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
