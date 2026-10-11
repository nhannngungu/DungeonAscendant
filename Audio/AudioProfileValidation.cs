using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;

namespace DungeonAscendant.Audio;

public static class AudioProfileValidation
{
    private static readonly WeaponFamily[] Weapons = { WeaponFamily.LongSword, WeaponFamily.DualSwords, WeaponFamily.HunterBow, WeaponFamily.WarAxe, WeaponFamily.ArcaneWarStaff, WeaponFamily.SpikedMace, WeaponFamily.ChainFlail };

    public static void ValidateOrThrow()
    {
        var mapped = new HashSet<string>(AudioProfiles.EnumerateAssets(), StringComparer.OrdinalIgnoreCase);
        foreach (WeaponFamily weapon in Weapons)
            foreach (PlayerSoundEvent soundEvent in Enum.GetValues<PlayerSoundEvent>())
                Need(AudioProfiles.GetPlayer(weapon).GetClips(soundEvent).Count > 0, $"Missing {weapon} {soundEvent} mapping.");
        foreach (EnemyType enemy in Enum.GetValues<EnemyType>())
            foreach (EnemySoundEvent soundEvent in Enum.GetValues<EnemySoundEvent>())
                Need(AudioProfiles.GetEnemy(enemy).GetClips(soundEvent).Count > 0, $"Missing {enemy} {soundEvent} mapping.");
        foreach (MusicState state in Enum.GetValues<MusicState>())
            if (state != MusicState.None) Need(AudioProfiles.GetMusic(state) != null, $"Missing music state {state}.");
        foreach (AmbienceState state in Enum.GetValues<AmbienceState>())
            if (state != AmbienceState.None) Need(AudioProfiles.GetAmbience(state) != null, $"Missing ambience state {state}.");

        string manifestPath = Path.Combine("Content", "Audio", "audio_manifest.json");
        Need(File.Exists(manifestPath), "Audio manifest was not copied into Content/Audio.");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        foreach (JsonProperty section in manifest.RootElement.EnumerateObject())
        {
            if (section.NameEquals("music"))
                continue;
            ValidateManifestPaths(section.Value, mapped);
        }

        string epicManifestPath = Path.Combine("Content", "Audio", "epic_music_manifest.json");
        Need(File.Exists(epicManifestPath), "Epic music manifest was not copied into Content/Audio.");
        using JsonDocument epicManifest = JsonDocument.Parse(File.ReadAllText(epicManifestPath));
        ValidateManifestPaths(epicManifest.RootElement, mapped);
    }

    private static void ValidateManifestPaths(JsonElement root, HashSet<string> mapped)
    {
        foreach (string sourcePath in EnumeratePaths(root))
        {
            string asset = sourcePath.Replace('\\','/').Replace("Content/", "");
            asset = asset[..^Path.GetExtension(asset).Length];
            Need(mapped.Contains(asset), $"Manifest asset is not routed by a semantic profile: {asset}");
            string sourceFile = sourcePath.Replace('/', Path.DirectorySeparatorChar);
            string compiledFile = Path.Combine(
                AppContext.BaseDirectory,
                "Content",
                asset.Replace('/', Path.DirectorySeparatorChar) + ".xnb");
            Need(File.Exists(sourceFile) || File.Exists(compiledFile),
                $"Manifest audio is missing as source WAV or compiled XNB: {sourcePath}");
        }
    }

    private static IEnumerable<string> EnumeratePaths(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) { yield return element.GetString(); yield break; }
        if (element.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty property in element.EnumerateObject()) foreach (string value in EnumeratePaths(property.Value)) yield return value;
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement child in element.EnumerateArray()) foreach (string value in EnumeratePaths(child)) yield return value;
    }

    private static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
