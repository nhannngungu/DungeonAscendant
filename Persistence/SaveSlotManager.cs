using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonAscendant.Persistence;

/// <summary>
/// Owns the versioned on-disk representation of the three character slots.
/// Failed or malformed saves never prevent the title screen from opening.
/// </summary>
public sealed class SaveSlotManager
{
    public const int SlotCount = 3;
    private const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _savePath;
    private readonly SaveSlotData[] _slots = new SaveSlotData[SlotCount];

    public IReadOnlyList<SaveSlotData> Slots => _slots;
    public string LastError { get; private set; } = string.Empty;

    public SaveSlotManager(string savePath = null)
    {
        _savePath = string.IsNullOrWhiteSpace(savePath)
            ? GetDefaultSavePath()
            : Path.GetFullPath(savePath);
        Load();
    }

    public SaveSlotData GetSlot(int slotIndex)
    {
        return _slots[Math.Clamp(slotIndex, 0, SlotCount - 1)];
    }

    public bool Store(SaveSlotData slot)
    {
        if (slot == null || slot.SlotIndex < 0 || slot.SlotIndex >= SlotCount)
            return false;

        slot.Normalize(slot.SlotIndex);
        _slots[slot.SlotIndex] = slot;
        return Save();
    }

    public bool Delete(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        _slots[slotIndex] = SaveSlotData.Empty(slotIndex);
        return Save();
    }

    public void Reload()
    {
        Load();
    }

    private void Load()
    {
        FillEmptySlots();
        LastError = string.Empty;

        try
        {
            if (!File.Exists(_savePath))
                return;

            string json = File.ReadAllText(_savePath);
            SaveSlotFile data = JsonSerializer.Deserialize<SaveSlotFile>(json, JsonOptions);
            if (data?.Slots == null)
                return;

            for (int index = 0; index < SlotCount; index++)
            {
                SaveSlotData loaded = index < data.Slots.Count
                    ? data.Slots[index]
                    : null;
                _slots[index] = loaded ?? SaveSlotData.Empty(index);
                _slots[index].Normalize(index);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = exception.Message;
            FillEmptySlots();
        }
    }

    private bool Save()
    {
        LastError = string.Empty;

        try
        {
            string directory = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var data = new SaveSlotFile
            {
                Version = CurrentVersion,
                Slots = new List<SaveSlotData>(_slots)
            };
            string json = JsonSerializer.Serialize(data, JsonOptions);
            string temporaryPath = _savePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _savePath, true);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            LastError = exception.Message;
            return false;
        }
    }

    private void FillEmptySlots()
    {
        for (int index = 0; index < SlotCount; index++)
            _slots[index] = SaveSlotData.Empty(index);
    }

    private static string GetDefaultSavePath()
    {
        string root = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "DungeonAscendant", "save-slots.json");
    }

    private sealed class SaveSlotFile
    {
        public int Version { get; set; } = CurrentVersion;
        public List<SaveSlotData> Slots { get; set; } = new();
    }
}
