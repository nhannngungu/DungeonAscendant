using System;
using DungeonAscendant.Dungeon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.World;

public enum AncientScholarTombState
{
    Unavailable,
    Unopened,
    Opening,
    Question,
    Reaction,
    RewardChoice,
    Completion,
    Completed
}

public enum AncientScholarRewardCategory
{
    None,
    Weapon,
    Armor,
    Relic
}

public sealed class AncientScholarQuestion
{
    public string Title { get; }
    public string[] Lines { get; }
    public string[] Answers { get; }
    public int CorrectAnswerIndex { get; }
    public string CorrectReaction { get; }

    public AncientScholarQuestion(
        string title,
        string[] lines,
        string[] answers,
        int correctAnswerIndex,
        string correctReaction)
    {
        Title = title;
        Lines = lines;
        Answers = answers;
        CorrectAnswerIndex = correctAnswerIndex;
        CorrectReaction = correctReaction;
    }
}

/// <summary>
/// Run-scoped state for the non-hostile scholar encounter. Placement is
/// derived from the authored Map 2 rooms so the map layout remains unchanged.
/// </summary>
public sealed class AncientScholarTombSystem
{
    public const float InteractionRadius = 118f;
    public const float SafeRadius = 205f;
    public const float OpeningDurationSeconds = 1.05f;
    public const int GuardBarracksZoneIndex = 5;

    public static readonly string[] RewardCategoryLabels =
    {
        "ANCIENT WEAPON",
        "ANCIENT ARMOR",
        "ANCIENT RELIC"
    };

    private const string WrongReaction =
        "Wrong. Even the dead remember better than you.";

    private static readonly AncientScholarQuestion[] Questions =
    {
        new(
            "THE LOOP OF TIME",
            new[]
            {
                "Consider this C# code:",
                "for (int i = 0; i < 3; i++)",
                "{",
                "    Console.Write(i + \" \");",
                "}",
                "What will be printed?"
            },
            new[] { "A. 1 2 3", "B. 0 1 2", "C. 0 1 2 3" },
            1,
            "Hm... your mind has not yet decayed."),
        new(
            "THE DAY YOU ENTERED THIS WORLD",
            new[] { "On what day was the Wanderer born?" },
            new[]
            {
                "A. 01/01/2003",
                "B. 01/01/2004",
                "C. 10/01/2004"
            },
            1,
            "Even your birth has not escaped the memory of the dead."),
        new(
            "BEASTS OF TWO AND FOUR LEGS",
            new[]
            {
                "Vừa gà vừa chó,",
                "bó lại cho tròn.",
                "Ba mươi sáu con,",
                "một trăm chân chẵn.",
                "",
                "Hỏi có bao nhiêu con gà và bao nhiêu con chó?"
            },
            new[]
            {
                "A. 22 gà — 14 chó",
                "B. 20 gà — 16 chó",
                "C. 24 gà — 12 chó"
            },
            0,
            "Three truths. Three answers. You may claim what the dead no longer need.")
    };

    private float _openingTime;
    private bool _completedThisRun;
    private bool _rewardRequestPending;

    public AncientScholarTombState State { get; private set; } =
        AncientScholarTombState.Unavailable;
    public Vector2 Position { get; private set; }
    public Rectangle CoffinBounds { get; private set; }
    public Rectangle SafeZoneBounds { get; private set; }
    public int QuestionIndex { get; private set; }
    public int SelectedAnswerIndex { get; private set; }
    public int CorrectAnswerCount { get; private set; }
    public int SelectedRewardIndex { get; private set; }
    public string ReactionText { get; private set; } = string.Empty;
    public string CompletionText { get; private set; } = string.Empty;
    public AncientScholarRewardCategory RequestedRewardCategory
    {
        get;
        private set;
    }
    public AncientScholarQuestion CurrentQuestion =>
        Questions[Math.Clamp(QuestionIndex, 0, Questions.Length - 1)];
    public int QuestionCount => Questions.Length;
    public float OpeningProgress => Math.Clamp(
        _openingTime / OpeningDurationSeconds,
        0f,
        1f);
    public float ScholarRevealProgress => State == AncientScholarTombState.Unopened ||
        State == AncientScholarTombState.Unavailable
            ? 0f
            : State == AncientScholarTombState.Opening
                ? Math.Clamp((OpeningProgress - .42f) / .58f, 0f, 1f)
                : 1f;
    public bool IsAvailable => State != AncientScholarTombState.Unavailable;
    public bool IsOpen => State is not AncientScholarTombState.Unavailable and
        not AncientScholarTombState.Unopened;
    public bool IsModalActive => State is
        AncientScholarTombState.Opening or
        AncientScholarTombState.Question or
        AncientScholarTombState.Reaction or
        AncientScholarTombState.RewardChoice or
        AncientScholarTombState.Completion;

    public void ResetForNewRun()
    {
        _completedThisRun = false;
        Position = Vector2.Zero;
        CoffinBounds = Rectangle.Empty;
        SafeZoneBounds = Rectangle.Empty;
        ResetRuntimeState(AncientScholarTombState.Unavailable);
    }

    public void Configure(DungeonMap map)
    {
        if (map?.IsAncientCatacombs != true ||
            map.CatacombZones.Count <= GuardBarracksZoneIndex)
        {
            State = AncientScholarTombState.Unavailable;
            Position = Vector2.Zero;
            CoffinBounds = Rectangle.Empty;
            SafeZoneBounds = Rectangle.Empty;
            return;
        }

        CatacombZoneDefinition barracks =
            map.CatacombZones[GuardBarracksZoneIndex];
        Position = new Vector2(
            barracks.Bounds.Left + 76f,
            barracks.GroundY);
        CoffinBounds = new Rectangle(
            (int)Position.X - 67,
            barracks.GroundY - 54,
            134,
            54);
        SafeZoneBounds = new Rectangle(
            (int)(Position.X - SafeRadius),
            (int)(Position.Y - SafeRadius),
            (int)(SafeRadius * 2f),
            (int)(SafeRadius * 2f));
        ResetRuntimeState(
            _completedThisRun
                ? AncientScholarTombState.Completed
                : AncientScholarTombState.Unopened);
    }

    public bool CanInspect(Vector2 playerPosition) =>
        State == AncientScholarTombState.Unopened &&
        Vector2.DistanceSquared(playerPosition, Position) <=
            InteractionRadius * InteractionRadius;

    public bool IsPlayerInsideSafeZone(Vector2 playerPosition) =>
        IsAvailable &&
        Vector2.DistanceSquared(playerPosition, Position) <=
            SafeRadius * SafeRadius;

    public bool TryInspect(Vector2 playerPosition)
    {
        if (!CanInspect(playerPosition))
            return false;

        _openingTime = 0f;
        State = AncientScholarTombState.Opening;
        return true;
    }

    public void Update(
        GameTime gameTime,
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        float elapsed = MathF.Min(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            .05f);

        if (State == AncientScholarTombState.Opening)
        {
            _openingTime += elapsed;
            if (_openingTime >= OpeningDurationSeconds)
                State = AncientScholarTombState.Question;
            return;
        }

        if (State == AncientScholarTombState.Question)
        {
            SelectedAnswerIndex = UpdateSelection(
                keyboard,
                previousKeyboard,
                3,
                SelectedAnswerIndex);
            if (Pressed(keyboard, previousKeyboard, Keys.Enter))
                ConfirmAnswer();
            return;
        }

        if (State == AncientScholarTombState.Reaction)
        {
            if (!Pressed(keyboard, previousKeyboard, Keys.Enter))
                return;

            if (QuestionIndex + 1 < Questions.Length)
            {
                QuestionIndex++;
                SelectedAnswerIndex = 0;
                State = AncientScholarTombState.Question;
            }
            else if (CorrectAnswerCount == Questions.Length)
            {
                SelectedRewardIndex = 0;
                State = AncientScholarTombState.RewardChoice;
            }
            else
            {
                QueueScoreReward();
            }
            return;
        }

        if (State == AncientScholarTombState.RewardChoice)
        {
            SelectedRewardIndex = UpdateSelection(
                keyboard,
                previousKeyboard,
                3,
                SelectedRewardIndex);
            if (!Pressed(keyboard, previousKeyboard, Keys.Enter))
                return;

            RequestedRewardCategory = (AncientScholarRewardCategory)
                (SelectedRewardIndex + 1);
            _rewardRequestPending = true;
            State = AncientScholarTombState.Completion;
            return;
        }

        if (State == AncientScholarTombState.Completion &&
            Pressed(keyboard, previousKeyboard, Keys.Enter) &&
            !_rewardRequestPending)
        {
            State = AncientScholarTombState.Completed;
            _completedThisRun = true;
        }
    }

    public bool TryConsumeRewardRequest(
        out int score,
        out AncientScholarRewardCategory category)
    {
        score = CorrectAnswerCount;
        category = RequestedRewardCategory;
        if (!_rewardRequestPending)
            return false;

        _rewardRequestPending = false;
        return true;
    }

    public void SetCompletionText(string text)
    {
        CompletionText = text ?? string.Empty;
    }

    public void ResetChallengeForDebug()
    {
        _completedThisRun = false;
        if (IsAvailable)
            ResetRuntimeState(AncientScholarTombState.Unopened);
    }

    private void ConfirmAnswer()
    {
        bool correct = SelectedAnswerIndex == CurrentQuestion.CorrectAnswerIndex;
        if (correct)
            CorrectAnswerCount++;
        ReactionText = correct
            ? CurrentQuestion.CorrectReaction
            : WrongReaction;
        State = AncientScholarTombState.Reaction;
    }

    private void QueueScoreReward()
    {
        if (CorrectAnswerCount == 0)
        {
            RequestedRewardCategory = AncientScholarRewardCategory.None;
            CompletionText = "The tomb offers no reward.";
            State = AncientScholarTombState.Completion;
            return;
        }

        RequestedRewardCategory = AncientScholarRewardCategory.Armor;
        _rewardRequestPending = true;
        State = AncientScholarTombState.Completion;
    }

    private void ResetRuntimeState(AncientScholarTombState state)
    {
        State = state;
        _openingTime = state == AncientScholarTombState.Completed
            ? OpeningDurationSeconds
            : 0f;
        QuestionIndex = 0;
        SelectedAnswerIndex = 0;
        CorrectAnswerCount = 0;
        SelectedRewardIndex = 0;
        RequestedRewardCategory = AncientScholarRewardCategory.None;
        ReactionText = string.Empty;
        CompletionText = string.Empty;
        _rewardRequestPending = false;
    }

    private static int UpdateSelection(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int count,
        int selection)
    {
        if (Pressed(keyboard, previousKeyboard, Keys.W) ||
            Pressed(keyboard, previousKeyboard, Keys.Up))
        {
            selection = (selection + count - 1) % count;
        }
        if (Pressed(keyboard, previousKeyboard, Keys.S) ||
            Pressed(keyboard, previousKeyboard, Keys.Down))
        {
            selection = (selection + 1) % count;
        }

        return selection;
    }

    private static bool Pressed(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key) =>
        keyboard.IsKeyDown(key) && !previousKeyboard.IsKeyDown(key);
}
