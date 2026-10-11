using System;
using DungeonAscendant.Localization;

namespace DungeonAscendant.UI;

public enum SettingsRow
{
    Language,
    MasterVolume,
    MusicVolume,
    SfxVolume,
    DisplayMode,
    Resolution
}

public sealed class LanguageSettingsState
{
    public bool IsOpen { get; private set; }
    public Language SelectedLanguage { get; private set; }
    public SettingsRow SelectedRow { get; private set; }

    public void Open()
    {
        SelectedLanguage = Loc.CurrentLanguage;
        SelectedRow = SettingsRow.Language;
        IsOpen = true;
    }

    public void MoveRow(int delta)
    {
        int count = Enum.GetValues<SettingsRow>().Length;
        SelectedRow = (SettingsRow)(((int)SelectedRow + delta + count) % count);
    }

    public void Adjust(int delta)
    {
        if (SelectedRow == SettingsRow.Language)
        {
            SelectedLanguage = SelectedLanguage == Language.English
                ? Language.Vietnamese
                : Language.English;
            Loc.SetLanguage(SelectedLanguage);
            return;
        }

        if (SelectedRow == SettingsRow.DisplayMode)
        {
            int count = Enum.GetValues<GameDisplayMode>().Length;
            Loc.SetDisplayMode((GameDisplayMode)(((int)Loc.DisplayMode + delta + count) % count));
            return;
        }

        if (SelectedRow == SettingsRow.Resolution)
        {
            (int Width, int Height)[] resolutions =
            {
                (800, 480), (1280, 720), (1600, 900),
                (1920, 1080), (2560, 1440), (3840, 2160)
            };
            int current = 0;
            int bestDistance = int.MaxValue;
            for (int index = 0; index < resolutions.Length; index++)
            {
                int distance = Math.Abs(resolutions[index].Width - Loc.WindowWidth) +
                    Math.Abs(resolutions[index].Height - Loc.WindowHeight);
                if (distance < bestDistance) { bestDistance = distance; current = index; }
            }
            int next = (current + delta + resolutions.Length) % resolutions.Length;
            Loc.SetWindowResolution(resolutions[next].Width, resolutions[next].Height);
            return;
        }

        float step = delta < 0 ? -.1f : .1f;
        float Adjusted(float value) => MathF.Round(
            Math.Clamp(value + step, 0f, 1f) * 10f) / 10f;
        if (SelectedRow == SettingsRow.MasterVolume)
            Loc.SetMasterVolume(Adjusted(Loc.MasterVolume));
        else if (SelectedRow == SettingsRow.MusicVolume)
            Loc.SetMusicVolume(Adjusted(Loc.MusicVolume));
        else
            Loc.SetSfxVolume(Adjusted(Loc.SfxVolume));
    }

    public void Close() => IsOpen = false;
}
