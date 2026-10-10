using System;
using System.Collections.Generic;

namespace DungeonAscendant.Lore;

public sealed class LoreNoteDefinition
{
    public string Id { get; }
    public string Title { get; }
    public string Source { get; }
    public string Content { get; }
    public IReadOnlyList<string> ContentLines { get; }

    public LoreNoteDefinition(
        string id,
        string title,
        string source,
        string content,
        int wrapWidth = 48)
    {
        Id = string.IsNullOrWhiteSpace(id) ? "lore-note" : id;
        Title = string.IsNullOrWhiteSpace(title) ? "Untitled Record" : title;
        Source = source ?? string.Empty;
        Content = content ?? string.Empty;
        ContentLines = WrapContent(Content, Math.Max(20, wrapWidth));
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
}

public static class LoreCatalog
{
    public static readonly LoreNoteDefinition StarBornHero = new(
        "star-born-hero",
        "The Star-Born Hero",
        "Corrupted Wilderness — Ancient Treant",
        "Five hundred years ago,\n" +
        "a star fell from the heavens.\n\n" +
        "From that light, a warrior was born into this world on\n" +
        "01/01/2004.\n\n" +
        "The warrior defeated the Demon King\n" +
        "and protected the world from an age of darkness.\n\n" +
        "Yet after victory,\n" +
        "the hero's name slowly disappeared from history...");
}
