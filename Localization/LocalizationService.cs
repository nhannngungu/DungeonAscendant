using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace DungeonAscendant.Localization;

public static class Loc
{
    private sealed class ApplicationSettings
    {
        public Language Language { get; set; } = Language.English;
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = .55f;
        public float SfxVolume { get; set; } = .75f;
    }

    private static readonly IReadOnlyDictionary<string, string> English =
        EnglishStrings.Values;
    private static readonly IReadOnlyDictionary<string, string> Vietnamese =
        VietnameseStrings.Values;
    private static string _settingsPath = GetDefaultSettingsPath();

    private static ApplicationSettings _settings = LoadSettings();

    public static Language CurrentLanguage => _settings.Language;
    public static float MasterVolume => _settings.MasterVolume;
    public static float MusicVolume => _settings.MusicVolume;
    public static float SfxVolume => _settings.SfxVolume;

    public static string Get(string key)
    {
        IReadOnlyDictionary<string, string> selected =
            CurrentLanguage == Language.Vietnamese ? Vietnamese : English;
        if (selected.TryGetValue(key, out string value))
            return value;
        if (English.TryGetValue(key, out value))
            return value;
        return key;
    }

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static void SetLanguage(Language language)
    {
        if (!Enum.IsDefined(language))
            language = Language.English;
        _settings.Language = language;
        SaveSettings();
    }

    public static void SetSfxVolume(float volume)
    {
        _settings.SfxVolume = Math.Clamp(volume, 0f, 1f);
        SaveSettings();
    }

    public static void SetMasterVolume(float volume)
    {
        _settings.MasterVolume = Math.Clamp(volume, 0f, 1f);
        SaveSettings();
    }

    public static void SetMusicVolume(float volume)
    {
        _settings.MusicVolume = Math.Clamp(volume, 0f, 1f);
        SaveSettings();
    }

    internal static IReadOnlyDictionary<string, string> GetStrings(Language language) =>
        language == Language.Vietnamese ? Vietnamese : English;

    internal static void UseSettingsPathForValidation(string path)
    {
        _settingsPath = path;
        _settings = LoadSettings();
    }

    private static ApplicationSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new ApplicationSettings();
            ApplicationSettings settings = JsonSerializer.Deserialize<ApplicationSettings>(
                File.ReadAllText(_settingsPath));
            settings ??= new ApplicationSettings();
            if (!Enum.IsDefined(settings.Language))
                settings.Language = Language.English;
            settings.MasterVolume = Math.Clamp(settings.MasterVolume, 0f, 1f);
            settings.MusicVolume = Math.Clamp(settings.MusicVolume, 0f, 1f);
            settings.SfxVolume = Math.Clamp(settings.SfxVolume, 0f, 1f);
            return settings;
        }
        catch (IOException)
        {
            return new ApplicationSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new ApplicationSettings();
        }
        catch (JsonException)
        {
            return new ApplicationSettings();
        }
    }

    private static void SaveSettings()
    {
        try
        {
            string directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            string temporaryPath = _settingsPath + ".tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(
                    _settings,
                    new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _settingsPath, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string GetDefaultSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DungeonAscendant",
        "settings.json");
}
