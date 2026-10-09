if (System.Array.Exists(
    args,
    argument => argument == "--validate-armor"))
{
    DungeonAscendant.Items.ArmorProgressionValidation.ValidateOrThrow();
    System.Console.WriteLine("Armor progression validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-inventory"))
{
    DungeonAscendant.UI.InventoryGridNavigation.ValidateOrThrow();
    DungeonAscendant.UI.InventoryTabRules.ValidateOrThrow();
    DungeonAscendant.Core.InventorySessionValidation.ValidateOrThrow();
    System.Console.WriteLine("Inventory tab and grid validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-enemy-sequence"))
{
    DungeonAscendant.Core.SequentialEnemyTestValidation.ValidateOrThrow();
    System.Console.WriteLine("Wild Forest showcase validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-enemy-visuals"))
{
    DungeonAscendant.Core.EnemyVisualValidation.ValidateOrThrow();
    System.Console.WriteLine("Enemy visual validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-map"))
{
    DungeonAscendant.Dungeon.WildForestMapValidation.ValidateOrThrow();
    System.Console.WriteLine("Authored Wild Forest map validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-wilderness"))
{
    DungeonAscendant.Dungeon.WildForestMapValidation.ValidateOrThrow();
    DungeonAscendant.World.WildForestEncounterValidation.ValidateOrThrow();
    System.Console.WriteLine("Wild Forest map, encounter, and introduction validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-god-mode"))
{
    DungeonAscendant.Core.DebugGodModeValidation.ValidateOrThrow();
    System.Console.WriteLine("Debug God Mode validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-entry-spawn"))
{
    DungeonAscendant.Core.MapEntrySpawnValidation.ValidateOrThrow();
    System.Console.WriteLine("Map entry spawn validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-developer-panel"))
{
    DungeonAscendant.Core.DeveloperPanelValidation.ValidateOrThrow();
    System.Console.WriteLine("Developer panel validation passed.");
    return;
}

if (System.Array.Exists(args, argument => argument == "--validate-catacombs"))
{
    DungeonAscendant.Dungeon.CatacombMapValidation.ValidateOrThrow();
    System.Console.WriteLine("Ancient Catacombs validation passed.");
    return;
}

if (System.Array.Exists(
    args,
    argument => argument == "--validate-equipment"))
{
    DungeonAscendant.Combat.WeaponArchetypeValidation.ValidateOrThrow();
    DungeonAscendant.Combat.EquipmentRebuildValidation.ValidateOrThrow();
    System.Console.WriteLine("Equipment and all seven weapon combat validations passed.");
    return;
}

using var game = new DungeonAscendant.Game1();
game.Run();
