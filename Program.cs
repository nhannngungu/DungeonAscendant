DungeonAscendant.Core.StartupLog.Initialize();

try
{
    if (RunRequestedValidation(args, out string validationName))
    {
        DungeonAscendant.Core.StartupLog.Write(
            $"[SHUTDOWN] Validation '{validationName}' completed; Program.Main ending normally.");
    }
    else
    {
        DungeonAscendant.Core.StartupLog.Write("Creating Game1.");
        using var game = new DungeonAscendant.Game1();
        DungeonAscendant.Core.StartupLog.Write(
            "Game loop started; calling Game.Run().");
        game.Run();
        DungeonAscendant.Core.StartupLog.Write(
            "[SHUTDOWN] Game.Run returned normally.");
    }
}
catch (System.Exception exception)
{
    DungeonAscendant.Core.StartupLog.Exception(
        "Fatal startup/game-loop exception",
        exception);
    System.Environment.ExitCode = 1;
    throw;
}

static bool RunRequestedValidation(string[] arguments, out string name)
{
    bool Has(string value) => System.Array.Exists(
        arguments,
        argument => argument == value);

    name = string.Empty;
    if (Has("--validate-armor"))
    {
        DungeonAscendant.Items.ArmorProgressionValidation.ValidateOrThrow();
        name = "armor";
        System.Console.WriteLine("Armor progression validation passed.");
    }
    else if (Has("--validate-inventory"))
    {
        DungeonAscendant.UI.InventoryGridNavigation.ValidateOrThrow();
        DungeonAscendant.UI.InventoryTabRules.ValidateOrThrow();
        DungeonAscendant.Core.InventorySessionValidation.ValidateOrThrow();
        name = "inventory";
        System.Console.WriteLine("Inventory tab and grid validation passed.");
    }
    else if (Has("--validate-enemy-sequence"))
    {
        DungeonAscendant.Core.SequentialEnemyTestValidation.ValidateOrThrow();
        name = "enemy-sequence";
        System.Console.WriteLine("Wild Forest showcase validation passed.");
    }
    else if (Has("--validate-enemy-visuals"))
    {
        DungeonAscendant.Core.EnemyVisualValidation.ValidateOrThrow();
        name = "enemy-visuals";
        System.Console.WriteLine("Enemy visual validation passed.");
    }
    else if (Has("--validate-map"))
    {
        DungeonAscendant.Dungeon.WildForestMapValidation.ValidateOrThrow();
        name = "map";
        System.Console.WriteLine("Authored Wild Forest map validation passed.");
    }
    else if (Has("--validate-wilderness"))
    {
        DungeonAscendant.Dungeon.WildForestMapValidation.ValidateOrThrow();
        DungeonAscendant.World.WildForestEncounterValidation.ValidateOrThrow();
        name = "wilderness";
        System.Console.WriteLine(
            "Wild Forest map, encounter, and introduction validation passed.");
    }
    else if (Has("--validate-god-mode"))
    {
        DungeonAscendant.Core.DebugGodModeValidation.ValidateOrThrow();
        name = "god-mode";
        System.Console.WriteLine("Debug God Mode validation passed.");
    }
    else if (Has("--validate-entry-spawn"))
    {
        DungeonAscendant.Core.MapEntrySpawnValidation.ValidateOrThrow();
        name = "entry-spawn";
        System.Console.WriteLine("Map entry spawn validation passed.");
    }
    else if (Has("--validate-developer-panel"))
    {
        DungeonAscendant.Core.DeveloperPanelValidation.ValidateOrThrow();
        name = "developer-panel";
        System.Console.WriteLine("Developer panel validation passed.");
    }
    else if (Has("--validate-catacombs"))
    {
        DungeonAscendant.Dungeon.CatacombMapValidation.ValidateOrThrow();
        name = "catacombs";
        System.Console.WriteLine("Ancient Catacombs validation passed.");
    }
    else if (Has("--validate-fallen-city"))
    {
        DungeonAscendant.Dungeon.FallenCityMapValidation.ValidateOrThrow();
        name = "fallen-city";
        System.Console.WriteLine(
            "Fallen City map and progression validation passed.");
    }
    else if (Has("--validate-demon-lands"))
    {
        DungeonAscendant.Dungeon.DemonLandsMapValidation.ValidateOrThrow();
        name = "demon-lands";
        System.Console.WriteLine(
            "Demon Lands map, corruption, anchors, enemies, boss, and admin validation passed.");
    }
    else if (Has("--validate-last-dice"))
    {
        DungeonAscendant.Core.LastDiceTableValidation.ValidateOrThrow();
        name = "last-dice";
        System.Console.WriteLine("Last Dice Table validation passed.");
    }
    else if (Has("--validate-equipment"))
    {
        DungeonAscendant.Combat.WeaponArchetypeValidation.ValidateOrThrow();
        DungeonAscendant.Combat.EquipmentRebuildValidation.ValidateOrThrow();
        name = "equipment";
        System.Console.WriteLine(
            "Equipment and all seven weapon combat validations passed.");
    }
    else if (Has("--validate-save-slots"))
    {
        DungeonAscendant.Persistence.SaveSlotValidation.ValidateOrThrow();
        name = "save-slots";
        System.Console.WriteLine(
            "Character select and save-slot validation passed.");
    }
    else if (Has("--validate-localization"))
    {
        DungeonAscendant.Localization.LocalizationValidation.ValidateOrThrow();
        name = "localization";
        System.Console.WriteLine(
            "English/Vietnamese localization validation passed.");
    }
    else if (Has("--validate-audio"))
    {
        DungeonAscendant.Audio.AudioProfileValidation.ValidateOrThrow();
        name = "audio";
        System.Console.WriteLine(
            "Full Map 1/Map 2 audio manifest and profile validation passed.");
    }

    return name.Length > 0;
}
