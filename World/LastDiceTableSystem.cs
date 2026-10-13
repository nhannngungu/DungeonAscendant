using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.World;

public enum LastDiceTableState
{
    Unavailable,
    Ready,
    Preparing,
    Selecting,
    Revealing,
    RoundResult,
    Completion,
    Completed
}

public enum LastDiceChoice
{
    Xiu,
    Tai
}

public enum LastDiceOutcome
{
    Xiu,
    Tai,
    Triple
}

/// <summary>
/// Owns the three-round Last Dice Table challenge. Dice are rolled exactly
/// once when a choice is confirmed; reveal input only moves the cover.
/// </summary>
public sealed class LastDiceTableSystem
{
    public const int RoundCount = 3;
    public const int MarketRuinsZoneIndex = 2;
    public const float InteractionRadius = 126f;
    public const float RevealSpeed = .16f;
    public const float PreparationDurationSeconds = 1.35f;

    private readonly Random _random;
    private readonly int[] _dice = new int[3];
    private float _preparationTime;
    private float _persistedRevealCheckpoint;
    private bool _started;
    private bool _roundCommitted;
    private bool _roundResolved;
    private bool _completionReached;
    private bool _completed;
    private bool _rewardGranted;
    private bool _rewardRequestPending;
    private bool _saveRequested;
    private int _rewardWeaponIndex = -1;

    public LastDiceTableState State { get; private set; } =
        LastDiceTableState.Unavailable;
    public Vector2 Position { get; private set; }
    public Rectangle TableBounds { get; private set; }
    public int RoundIndex { get; private set; }
    public int Wins { get; private set; }
    public LastDiceChoice Choice { get; private set; }
    public float RevealProgress { get; private set; }
    public IReadOnlyList<int> Dice => _dice;
    public int Total => _dice[0] + _dice[1] + _dice[2];
    public bool IsTriple => _dice[0] > 0 &&
        _dice[0] == _dice[1] && _dice[1] == _dice[2];
    public LastDiceOutcome Outcome => IsTriple
        ? LastDiceOutcome.Triple
        : Total <= 10 ? LastDiceOutcome.Xiu : LastDiceOutcome.Tai;
    public bool PlayerWonRound => _roundResolved &&
        Evaluate(Choice, _dice[0], _dice[1], _dice[2]);
    public bool IsModalActive => State is
        LastDiceTableState.Preparing or
        LastDiceTableState.Selecting or
        LastDiceTableState.Revealing or
        LastDiceTableState.RoundResult or
        LastDiceTableState.Completion;
    public bool IsChoiceLocked => _roundCommitted;
    public bool Started => _started;
    public bool RoundCommitted => _roundCommitted;
    public bool RoundResolved => _roundResolved;
    public bool CompletionReached => _completionReached;
    public bool IsCompleted => _completed;
    public bool RewardGranted => _rewardGranted;
    public int RewardWeaponIndex => _rewardWeaponIndex;
    public float PreparationProgress => Math.Clamp(
        _preparationTime / PreparationDurationSeconds, 0f, 1f);

    public LastDiceTableSystem(int? randomSeed = null)
    {
        _random = randomSeed.HasValue
            ? new Random(unchecked(randomSeed.Value ^ 0x4C415354))
            : new Random();
    }

    public void ResetForNewRun()
    {
        Position = Vector2.Zero;
        TableBounds = Rectangle.Empty;
        _started = false;
        _roundCommitted = false;
        _roundResolved = false;
        _completionReached = false;
        _completed = false;
        _rewardGranted = false;
        _rewardRequestPending = false;
        _saveRequested = false;
        _rewardWeaponIndex = -1;
        _preparationTime = 0f;
        RoundIndex = 0;
        Wins = 0;
        Choice = LastDiceChoice.Xiu;
        RevealProgress = 0f;
        _persistedRevealCheckpoint = 0f;
        Array.Clear(_dice);
        State = LastDiceTableState.Unavailable;
    }

    public void Configure(DungeonMap map)
    {
        if (map?.IsFallenCity != true ||
            map.FallenCityZones.Count <= MarketRuinsZoneIndex)
        {
            Position = Vector2.Zero;
            TableBounds = Rectangle.Empty;
            State = LastDiceTableState.Unavailable;
            return;
        }

        FallenCityZoneDefinition market =
            map.FallenCityZones[MarketRuinsZoneIndex];
        Position = new Vector2(market.Bounds.Right - 128f, market.GroundY);
        TableBounds = new Rectangle(
            (int)Position.X - 86,
            market.GroundY - 66,
            172,
            66);
        RestoreVisibleState();
    }

    public bool CanInteract(Vector2 playerPosition) =>
        State == LastDiceTableState.Ready && !_completed &&
        Vector2.DistanceSquared(playerPosition, Position) <=
            InteractionRadius * InteractionRadius;

    public bool TryOpen(Vector2 playerPosition)
    {
        if (!CanInteract(playerPosition))
            return false;

        if (!_started)
        {
            _started = true;
            BeginRoundPreparation();
        }
        else
        {
            State = LastDiceTableState.Selecting;
        }
        _saveRequested = true;
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

        if (State == LastDiceTableState.Preparing)
        {
            _preparationTime += elapsed;
            if (_preparationTime >= PreparationDurationSeconds)
                State = LastDiceTableState.Selecting;
            return;
        }

        if (State == LastDiceTableState.Selecting)
        {
            if (Pressed(keyboard, previousKeyboard, Keys.A) ||
                Pressed(keyboard, previousKeyboard, Keys.Left) ||
                Pressed(keyboard, previousKeyboard, Keys.W) ||
                Pressed(keyboard, previousKeyboard, Keys.Up))
            {
                Choice = LastDiceChoice.Xiu;
            }
            if (Pressed(keyboard, previousKeyboard, Keys.D) ||
                Pressed(keyboard, previousKeyboard, Keys.Right) ||
                Pressed(keyboard, previousKeyboard, Keys.S) ||
                Pressed(keyboard, previousKeyboard, Keys.Down))
            {
                Choice = LastDiceChoice.Tai;
            }

            if (Pressed(keyboard, previousKeyboard, Keys.Escape))
            {
                State = LastDiceTableState.Ready;
                _saveRequested = true;
                return;
            }

            if (Pressed(keyboard, previousKeyboard, Keys.Enter))
                CommitRound();
            return;
        }

        if (State == LastDiceTableState.Revealing)
        {
            bool revealHeld = keyboard.IsKeyDown(Keys.E) ||
                keyboard.IsKeyDown(Keys.Enter);
            if (!revealHeld)
            {
                bool wasHeld = previousKeyboard.IsKeyDown(Keys.E) ||
                    previousKeyboard.IsKeyDown(Keys.Enter);
                if (wasHeld && RevealProgress != _persistedRevealCheckpoint)
                {
                    _persistedRevealCheckpoint = RevealProgress;
                    _saveRequested = true;
                }
                return;
            }

            RevealProgress = Math.Clamp(
                RevealProgress + RevealSpeed * elapsed,
                0f,
                1f);
            if (RevealProgress >= 1f ||
                RevealProgress - _persistedRevealCheckpoint >= .05f)
            {
                _saveRequested = true;
                _persistedRevealCheckpoint = RevealProgress;
            }
            if (RevealProgress >= 1f)
                ResolveRound();
            return;
        }

        if (State == LastDiceTableState.RoundResult &&
            Pressed(keyboard, previousKeyboard, Keys.Enter))
        {
            if (RoundIndex + 1 < RoundCount)
            {
                RoundIndex++;
                BeginRoundPreparation();
            }
            else
            {
                _completionReached = true;
                State = LastDiceTableState.Completion;
                if (Wins >= 2 && !_rewardGranted)
                {
                    if (_rewardWeaponIndex < 0)
                        _rewardWeaponIndex = _random.Next(
                            EquipmentCatalog.RewardWeaponSampleCount);
                    _rewardRequestPending = true;
                }
            }
            _saveRequested = true;
            return;
        }

        if (State == LastDiceTableState.Completion &&
            Pressed(keyboard, previousKeyboard, Keys.Enter) &&
            !_rewardRequestPending)
        {
            _completed = true;
            State = LastDiceTableState.Completed;
            _saveRequested = true;
        }
    }

    public bool TryConsumeRewardRequest(out int wins, out int weaponIndex)
    {
        wins = Wins;
        weaponIndex = _rewardWeaponIndex;
        if (!_rewardRequestPending || _rewardGranted || Wins < 2)
            return false;

        _rewardRequestPending = false;
        _rewardGranted = true;
        _saveRequested = true;
        return true;
    }

    public bool ConsumeSaveRequest()
    {
        bool requested = _saveRequested;
        _saveRequested = false;
        return requested;
    }

    public void Restore(
        bool started,
        bool completed,
        int roundIndex,
        int wins,
        bool roundCommitted,
        bool roundResolved,
        bool completionReached,
        LastDiceChoice choice,
        int die1,
        int die2,
        int die3,
        float revealProgress,
        bool rewardGranted,
        int rewardWeaponIndex)
    {
        _started = started || completed || roundCommitted;
        _completed = completed;
        RoundIndex = Math.Clamp(roundIndex, 0, RoundCount - 1);
        Wins = Math.Clamp(wins, 0, RoundCount);
        _roundCommitted = roundCommitted;
        _roundResolved = roundResolved && roundCommitted;
        _completionReached = completionReached || completed;
        Choice = Enum.IsDefined(choice) ? choice : LastDiceChoice.Xiu;
        _dice[0] = Math.Clamp(die1, roundCommitted ? 1 : 0, 6);
        _dice[1] = Math.Clamp(die2, roundCommitted ? 1 : 0, 6);
        _dice[2] = Math.Clamp(die3, roundCommitted ? 1 : 0, 6);
        RevealProgress = roundCommitted
            ? Math.Clamp(revealProgress, 0f, 1f)
            : 0f;
        _persistedRevealCheckpoint = RevealProgress;
        _rewardGranted = rewardGranted;
        _rewardWeaponIndex = Math.Clamp(
            rewardWeaponIndex,
            -1,
            EquipmentCatalog.RewardWeaponSampleCount - 1);
        _rewardRequestPending = !rewardGranted && Wins >= 2 &&
            _completionReached && RoundIndex == RoundCount - 1 && _roundResolved &&
            RevealProgress >= 1f;
        _saveRequested = false;
        RestoreVisibleState();
    }

    public void ResetChallengeForDebug()
    {
        bool available = TableBounds != Rectangle.Empty;
        _started = false;
        _completed = false;
        _roundCommitted = false;
        _roundResolved = false;
        _completionReached = false;
        _rewardGranted = false;
        _rewardRequestPending = false;
        _rewardWeaponIndex = -1;
        _preparationTime = 0f;
        RoundIndex = 0;
        Wins = 0;
        Choice = LastDiceChoice.Xiu;
        RevealProgress = 0f;
        _persistedRevealCheckpoint = 0f;
        Array.Clear(_dice);
        State = available
            ? LastDiceTableState.Ready
            : LastDiceTableState.Unavailable;
        _saveRequested = true;
    }

    public static bool Evaluate(
        LastDiceChoice choice,
        int die1,
        int die2,
        int die3)
    {
        if (die1 == die2 && die2 == die3)
            return false;
        int total = die1 + die2 + die3;
        return choice == LastDiceChoice.Xiu
            ? total is >= 4 and <= 10
            : total is >= 11 and <= 17;
    }

    public static ItemRarity? GetRewardRarity(int wins) => wins switch
    {
        >= 3 => ItemRarity.Legendary,
        2 => ItemRarity.Epic,
        _ => null
    };

    private void CommitRound()
    {
        if (_roundCommitted)
            return;
        for (int index = 0; index < _dice.Length; index++)
            _dice[index] = _random.Next(1, 7);
        _roundCommitted = true;
        _roundResolved = false;
        RevealProgress = 0f;
        _persistedRevealCheckpoint = 0f;
        State = LastDiceTableState.Revealing;
        _saveRequested = true;
    }

    private void ResolveRound()
    {
        if (_roundResolved)
            return;
        RevealProgress = 1f;
        _roundResolved = true;
        if (Evaluate(Choice, _dice[0], _dice[1], _dice[2]))
            Wins++;
        State = LastDiceTableState.RoundResult;
        _saveRequested = true;
    }

    private void BeginRoundPreparation()
    {
        _preparationTime = 0f;
        _completionReached = false;
        _roundCommitted = false;
        _roundResolved = false;
        Choice = LastDiceChoice.Xiu;
        RevealProgress = 0f;
        _persistedRevealCheckpoint = 0f;
        Array.Clear(_dice);
        State = LastDiceTableState.Preparing;
    }

    private void RestoreVisibleState()
    {
        if (TableBounds == Rectangle.Empty)
        {
            State = LastDiceTableState.Unavailable;
        }
        else if (_completed)
        {
            State = LastDiceTableState.Completed;
        }
        else if (_completionReached)
        {
            State = LastDiceTableState.Completion;
        }
        else if (_roundCommitted && _roundResolved)
        {
            State = LastDiceTableState.RoundResult;
        }
        else if (_roundCommitted)
        {
            State = LastDiceTableState.Revealing;
        }
        else if (_started)
        {
            State = LastDiceTableState.Selecting;
        }
        else
        {
            State = LastDiceTableState.Ready;
        }
    }

    private static bool Pressed(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key) => keyboard.IsKeyDown(key) &&
        !previousKeyboard.IsKeyDown(key);
}
