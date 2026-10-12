using System;
using System.Collections.Generic;
using DungeonAscendant.Localization;

namespace DungeonAscendant.Lore;

public sealed class LoreNoteDefinition
{
    public string Id { get; }
    public string Title => Loc.Get(TitleKey);
    public string Source => Id == "star-born-hero"
        ? Loc.Get("notes.star_born_hero.source")
        : Loc.Get(SourceKey);
    public string Content => Loc.Get(ContentKey);
    public IReadOnlyList<string> ContentLines
    {
        get
        {
            if (_cachedLanguage != Loc.CurrentLanguage)
            {
                _cachedLanguage = Loc.CurrentLanguage;
                _cachedLines = WrapContent(Content, _wrapWidth);
            }
            return _cachedLines;
        }
    }
    public string TitleKey { get; }
    public string SourceKey { get; }
    public string ContentKey { get; }
    public string ImageAssetKey { get; }

    private readonly int _wrapWidth;
    private Language? _cachedLanguage;
    private IReadOnlyList<string> _cachedLines = Array.Empty<string>();

    public LoreNoteDefinition(
        string id,
        string titleKey,
        string sourceKey,
        string contentKey,
        int wrapWidth = 48,
        string imageAssetKey = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? "lore-note" : id;
        TitleKey = titleKey ?? string.Empty;
        SourceKey = sourceKey ?? string.Empty;
        ContentKey = contentKey ?? string.Empty;
        ImageAssetKey = imageAssetKey ?? string.Empty;
        _wrapWidth = Math.Max(20, wrapWidth);
    }

    private static IReadOnlyList<string> WrapContent(
        string content,
        int maximumCharacters)
    {
        var lines = new List<string>();
        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        string[] paragraphs = normalized.Split('\n');

        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            string[] words = paragraph.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);
            string line = string.Empty;
            foreach (string word in words)
            {
                if (line.Length == 0)
                {
                    line = word;
                    continue;
                }

                if (line.Length + 1 + word.Length <= maximumCharacters)
                {
                    line += " " + word;
                    continue;
                }

                lines.Add(line);
                line = word;
            }

            if (line.Length > 0)
                lines.Add(line);
        }

        return lines;
    }
}

public sealed class LoreNote
{
    public LoreNoteDefinition Definition { get; }
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public string Source => Definition.Source;
    public string Content => Definition.Content;
    public IReadOnlyList<string> ContentLines => Definition.ContentLines;
    public string ImageAssetKey => Definition.ImageAssetKey;
    public bool IsRead { get; private set; }

    public LoreNote(LoreNoteDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}

public sealed class LoreCollection
{
    private readonly List<LoreNote> _notes = new();

    public IReadOnlyList<LoreNote> Notes => _notes;
    public int Count => _notes.Count;

    public bool Contains(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return false;

        foreach (LoreNote note in _notes)
        {
            if (note.Id == noteId)
                return true;
        }

        return false;
    }

    public bool TryCollect(LoreNoteDefinition definition, out LoreNote note)
    {
        note = null;
        if (definition == null || Contains(definition.Id))
            return false;

        note = new LoreNote(definition);
        _notes.Add(note);
        return true;
    }

    public bool Remove(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return false;

        for (int index = 0; index < _notes.Count; index++)
        {
            if (_notes[index].Id != noteId)
                continue;
            _notes.RemoveAt(index);
            return true;
        }

        return false;
    }
}

public static class LoreCatalog
{
    public const string PigPhotoImageAssetKey = "EasterEggs/pig_photo";

    public static readonly LoreNoteDefinition StarBornHero = new(
        "star-born-hero",
        "notes.star_born_hero.title",
        "Corrupted Wilderness — Ancient Treant",
        "notes.star_born_hero.content" /*
        "a star fell from the heavens.\n\n" +
        "From that light, a warrior was born into this world on\n" +
        "01/01/2004.\n\n" +
        "The warrior defeated the Demon King\n" +
        "and protected the world from an age of darkness.\n\n" +
        "Yet after victory,\n" +
        "the hero's name slowly disappeared from history..." */);

    public static readonly LoreNoteDefinition MysteriousPhotograph = new(
        "mysterious-photograph",
        "notes.pig_photo.title",
        string.Empty,
        "notes.pig_photo.content",
        wrapWidth: 42,
        imageAssetKey: PigPhotoImageAssetKey);

    public static LoreNoteDefinition Resolve(string id)
    {
        return id switch
        {
            "star-born-hero" => StarBornHero,
            "mysterious-photograph" => MysteriousPhotograph,
            _ => null
        };
    }
}
