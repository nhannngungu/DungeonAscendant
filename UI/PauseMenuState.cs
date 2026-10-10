using System;

namespace DungeonAscendant.UI;

public enum PauseMenuEntry
{
    Resume,
    Settings,
    ReturnToCharacterSelect
}

public sealed class PauseMenuState
{
    public PauseMenuEntry SelectedEntry { get; private set; }
    public bool IsReturnConfirmationOpen { get; private set; }
    public bool ConfirmReturn { get; private set; }

    public void Begin()
    {
        SelectedEntry = PauseMenuEntry.Resume;
        IsReturnConfirmationOpen = false;
        ConfirmReturn = false;
    }

    public void Move(int delta)
    {
        int count = Enum.GetValues<PauseMenuEntry>().Length;
        int next = ((int)SelectedEntry + delta) % count;
        if (next < 0)
            next += count;
        SelectedEntry = (PauseMenuEntry)next;
    }

    public void OpenReturnConfirmation()
    {
        IsReturnConfirmationOpen = true;
        ConfirmReturn = false;
    }

    public void ToggleConfirmation()
    {
        ConfirmReturn = !ConfirmReturn;
    }

    public void CancelConfirmation()
    {
        IsReturnConfirmationOpen = false;
        ConfirmReturn = false;
    }
}
