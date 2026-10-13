using System;
using DungeonAscendant.Items;
using DungeonAscendant.Localization;
using Microsoft.Xna.Framework.Input;

namespace DungeonAscendant.Core;

public enum DeveloperCommand
{
    ToggleGodMode,
    HealPlayer,
    RefillStamina,
    ClearCurse,
    ClearCorruption,
    ResetPlayerCombat,
    GrantAllWeapons,
    TeleportMap,
    TeleportZone,
    ResetCurrentEncounter,
    ResetScholarChallenge,
    ResetLastDiceChallenge,
    KillActiveEnemies,
    ForcePigPhotoDrop,
    ResetPigPhotoCollected,
    ToggleMap01Cleared,
    ToggleMap02Cleared,
    ToggleMap03Cleared,
    ResetCitySystems,
    ResetCorruptionAnchors,
    ResetCurrentBoss,
    ResetEnemyIntroductions,
    CycleBowFocus,
    ToggleBowUltimate,
    ForceBowPerfectDodge,
    ResetBowRuntime,
    ToggleMapDebug,
    ToggleCombatDebug
}

public sealed class DeveloperPanel
{
    public static readonly DeveloperCommand[] Entries =
    {
        DeveloperCommand.ToggleGodMode,
        DeveloperCommand.HealPlayer,
        DeveloperCommand.RefillStamina,
        DeveloperCommand.ClearCurse,
        DeveloperCommand.ClearCorruption,
        DeveloperCommand.ResetPlayerCombat,
        DeveloperCommand.GrantAllWeapons,
        DeveloperCommand.TeleportMap,
        DeveloperCommand.TeleportZone,
        DeveloperCommand.ResetCurrentEncounter,
        DeveloperCommand.ResetScholarChallenge,
        DeveloperCommand.ResetLastDiceChallenge,
        DeveloperCommand.KillActiveEnemies,
        DeveloperCommand.ForcePigPhotoDrop,
        DeveloperCommand.ResetPigPhotoCollected,
        DeveloperCommand.ToggleMap01Cleared,
        DeveloperCommand.ToggleMap02Cleared,
        DeveloperCommand.ToggleMap03Cleared,
        DeveloperCommand.ResetCitySystems,
        DeveloperCommand.ResetCorruptionAnchors,
        DeveloperCommand.ResetCurrentBoss,
        DeveloperCommand.ResetEnemyIntroductions,
        DeveloperCommand.CycleBowFocus,
        DeveloperCommand.ToggleBowUltimate,
        DeveloperCommand.ForceBowPerfectDodge,
        DeveloperCommand.ResetBowRuntime,
        DeveloperCommand.ToggleMapDebug,
        DeveloperCommand.ToggleCombatDebug
    };

    private static readonly string[] MapNames =
    {
        "MAP 1 - CORRUPTED WILDERNESS",
        "MAP 2 - ANCIENT CATACOMBS",
        "MAP 3 - FALLEN CITY",
        "MAP 4 - DEMON LANDS"
    };

    private static readonly string[][] ZoneNames =
    {
        new[]
        {
            "FOREST OUTSKIRTS", "GOBLIN ENCAMPMENT", "WEBWOOD",
            "THORNLANDS", "CORRUPTED GROVE", "WAR CAMP",
            "MOTHERS NEST", "ANCIENT SANCTUARY"
        },
        new[]
        {
            "TOMB ENTRANCE", "OSSUARY CORRIDORS", "ARCHER GALLERIES",
            "ROT PITS", "WRAITH HALLS", "GUARD BARRACKS",
            "CURSED KNIGHT MAUSOLEUM", "SOUL CHAPEL",
            "DEATH KNIGHT WAR TOMB", "FALLEN HALL",
            "ANCIENT SCHOLAR TOMB"
        },
        new[]
        {
            "city.zone.broken_outer_gate", "city.zone.refugee_street",
            "city.zone.market_ruins", "city.zone.watch_district",
            "city.zone.burned_quarter", "city.zone.noble_avenue",
            "city.zone.execution_square", "city.zone.royal_guard_barracks",
            "city.zone.palace_approach", "city.zone.ruined_throne_plaza"
        },
        new[]
        {
            "demon.zone.blackened_frontier", "demon.zone.ash_wastes",
            "demon.zone.flesh_ravine", "demon.zone.rift_fields",
            "demon.zone.bone_forest", "demon.zone.infernal_quarry",
            "demon.zone.blood_shrine", "demon.zone.legion_camp",
            "demon.zone.gate_of_horns", "demon.zone.infernal_heart"
        }
    };

    private KeyboardState _previousKeyboard;

    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; }
    public int TargetMap { get; private set; } = 1;
    public int TargetZone { get; private set; }
    public string Feedback { get; private set; } = "READY";
    public DeveloperCommand SelectedCommand => Entries[SelectedIndex];

    public bool Update(GameSession session, KeyboardState keyboard)
    {
        bool toggle = Pressed(keyboard, Keys.F10);
        if (!IsOpen)
        {
            if (toggle && session.State == GameState.Playing)
            {
                IsOpen = true;
                TargetMap = Math.Clamp(session.DungeonDepth, 1, 4);
                TargetZone = Math.Max(0, session.GetCurrentZoneIndex());
                Feedback = "GAMEPLAY PAUSED";
                session.Player.Combat.CancelActions(restoreStamina: false);
            }

            _previousKeyboard = keyboard;
            return IsOpen;
        }

        if (toggle || Pressed(keyboard, Keys.Escape))
        {
            IsOpen = false;
            Feedback = "READY";
            _previousKeyboard = keyboard;
            return true;
        }

        if (Pressed(keyboard, Keys.W) || Pressed(keyboard, Keys.Up))
            SelectedIndex = (SelectedIndex + Entries.Length - 1) % Entries.Length;
        if (Pressed(keyboard, Keys.S) || Pressed(keyboard, Keys.Down))
            SelectedIndex = (SelectedIndex + 1) % Entries.Length;

        int direction = 0;
        if (Pressed(keyboard, Keys.A) || Pressed(keyboard, Keys.Left))
            direction = -1;
        if (Pressed(keyboard, Keys.D) || Pressed(keyboard, Keys.Right))
            direction = 1;
        if (direction != 0)
            ChangeValue(session, direction);

        if (Pressed(keyboard, Keys.Enter))
            Activate(session);

        _previousKeyboard = keyboard;
        return true;
    }

    public string GetMapName() => MapNames[TargetMap - 1];
    public string GetZoneName() => TargetMap >= 3
        ? Loc.Get(ZoneNames[TargetMap - 1][TargetZone])
        : ZoneNames[TargetMap - 1][TargetZone];
    public int GetZoneCount() => ZoneNames[TargetMap - 1].Length;

    public static string GetSection(DeveloperCommand command) => command switch
    {
        <= DeveloperCommand.GrantAllWeapons => "PLAYER",
        <= DeveloperCommand.ResetPigPhotoCollected => "WORLD",
        <= DeveloperCommand.ResetEnemyIntroductions => "PROGRESSION",
        _ => "DEBUG"
    };

    public static string GetLabel(DeveloperCommand command) => command switch
    {
        DeveloperCommand.ToggleGodMode => "GOD MODE",
        DeveloperCommand.HealPlayer => "HEAL PLAYER",
        DeveloperCommand.RefillStamina => "REFILL STAMINA",
        DeveloperCommand.ClearCurse => "CLEAR CURSE",
        DeveloperCommand.ClearCorruption => "RESET CORRUPTION",
        DeveloperCommand.ResetPlayerCombat => "RESET COMBAT STATE",
        DeveloperCommand.GrantAllWeapons => "GRANT ALL WEAPONS",
        DeveloperCommand.TeleportMap => "TELEPORT MAP",
        DeveloperCommand.TeleportZone => "TELEPORT ZONE",
        DeveloperCommand.ResetCurrentEncounter => "RESET CURRENT ENCOUNTER",
        DeveloperCommand.ResetScholarChallenge => "RESET SCHOLAR CHALLENGE",
        DeveloperCommand.ResetLastDiceChallenge => "RESET LAST DICE TABLE",
        DeveloperCommand.KillActiveEnemies => "KILL ACTIVE ENEMIES",
        DeveloperCommand.ForcePigPhotoDrop => "FORCE FALLEN KNIGHT PHOTO",
        DeveloperCommand.ResetPigPhotoCollected => "RESET PIG PHOTO",
        DeveloperCommand.ToggleMap01Cleared => "MAP 1 CLEARED",
        DeveloperCommand.ToggleMap02Cleared => "MAP 2 CLEARED",
        DeveloperCommand.ToggleMap03Cleared => "MAP 3 CLEARED",
        DeveloperCommand.ResetCitySystems => "RESET CITY ALARM / COLLAPSE",
        DeveloperCommand.ResetCorruptionAnchors => "RESET CORRUPTION ANCHORS",
        DeveloperCommand.ResetCurrentBoss => "RESET CURRENT BOSS",
        DeveloperCommand.ResetEnemyIntroductions => "RESET ENEMY INTRODUCTIONS",
        DeveloperCommand.CycleBowFocus => "BOW FOCUS 0 / 35 / 70 / 100",
        DeveloperCommand.ToggleBowUltimate => "REFILL BOW ULTIMATE STAMINA",
        DeveloperCommand.ForceBowPerfectDodge => "BOW FORCE PERFECT DODGE",
        DeveloperCommand.ResetBowRuntime => "RESET BOW RUNTIME",
        DeveloperCommand.ToggleMapDebug => "MAP DEBUG",
        DeveloperCommand.ToggleCombatDebug => "COLLISION COMBAT DEBUG",
        _ => command.ToString().ToUpperInvariant()
    };

    public string GetValue(GameSession session, DeveloperCommand command) =>
        command switch
        {
            DeveloperCommand.ToggleGodMode => OnOff(session.DebugGodMode),
            DeveloperCommand.TeleportMap => GetMapName(),
            DeveloperCommand.TeleportZone => GetZoneName(),
            DeveloperCommand.ToggleMap01Cleared => YesNo(session.Map01Cleared),
            DeveloperCommand.ToggleMap02Cleared => YesNo(session.Map02Cleared),
            DeveloperCommand.ToggleMap03Cleared => YesNo(session.Map03Cleared),
            DeveloperCommand.ResetCitySystems => $"ALARM {session.CityAlarm.Level} / 3",
            DeveloperCommand.ResetPigPhotoCollected =>
                session.Map2PigPhotoCollected ? "COLLECTED" : "AVAILABLE",
            DeveloperCommand.GrantAllWeapons =>
                $"{session.Player.CountOwnedImplementedWeapons()} / " +
                EquipmentCatalog.WeaponSampleCount,
            DeveloperCommand.CycleBowFocus =>
                $"{session.Player.Combat.Resources.HunterFocus:0}",
            DeveloperCommand.ToggleBowUltimate =>
                $"{session.Player.Combat.Stamina.Current:0} / " +
                $"{session.Player.Combat.Stamina.Maximum:0}",
            DeveloperCommand.ToggleMapDebug => OnOff(session.ShowMapDebug),
            DeveloperCommand.ToggleCombatDebug => OnOff(session.ShowCombatDebug),
            _ => string.Empty
        };

    private void ChangeValue(GameSession session, int direction)
    {
        if (SelectedCommand == DeveloperCommand.TeleportMap)
        {
            TargetMap = TargetMap % MapNames.Length + 1;
            TargetZone = 0;
            Feedback = "SELECT MAP THEN ENTER";
            return;
        }

        if (SelectedCommand == DeveloperCommand.TeleportZone)
        {
            int count = GetZoneCount();
            TargetZone = (TargetZone + direction + count) % count;
            Feedback = "SELECT ZONE THEN ENTER";
            return;
        }

        if (IsToggle(SelectedCommand))
            Activate(session);
    }

    private void Activate(GameSession session)
    {
        session.ExecuteDeveloperCommand(SelectedCommand, TargetMap, TargetZone);
        Feedback = GetLabel(SelectedCommand) + " COMPLETE";
        if (SelectedCommand is DeveloperCommand.TeleportMap or DeveloperCommand.TeleportZone)
        {
            TargetMap = Math.Clamp(session.DungeonDepth, 1, 4);
            TargetZone = Math.Max(0, session.GetCurrentZoneIndex());
        }
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private static bool IsToggle(DeveloperCommand command) => command is
        DeveloperCommand.ToggleGodMode or
        DeveloperCommand.ToggleMap01Cleared or
        DeveloperCommand.ToggleMap02Cleared or
        DeveloperCommand.ToggleMap03Cleared or
        DeveloperCommand.ToggleMapDebug or
        DeveloperCommand.ToggleCombatDebug;

    private static string OnOff(bool value) => value ? "ON" : "OFF";
    private static string YesNo(bool value) => value ? "YES" : "NO";
}
