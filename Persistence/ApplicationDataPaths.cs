using System;
using System.IO;

namespace DungeonAscendant.Persistence;

public static class ApplicationDataPaths
{
    public const string OverrideEnvironmentVariable =
        "DUNGEON_ASCENDANT_USER_DATA";

    public static string Root
    {
        get
        {
            string overridePath = Environment.GetEnvironmentVariable(
                OverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "DungeonAscendant");
        }
    }

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string SaveSlotsFile => Path.Combine(Root, "save-slots.json");
    public static string StartupLogFile => Path.Combine(Root, "Logs", "startup.log");
}
