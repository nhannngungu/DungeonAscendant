using System;

namespace DungeonAscendant.UI;

public enum MainMenuEntry
{
    NewGame,
    Settings
}

public sealed class MainMenuState
{
    public MainMenuEntry SelectedEntry { get; private set; }

    public void Move(int delta)
    {
        int count = Enum.GetValues<MainMenuEntry>().Length;
        int next = ((int)SelectedEntry + delta) % count;
        if (next < 0)
            next += count;
        SelectedEntry = (MainMenuEntry)next;
    }
}
