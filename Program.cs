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

using var game = new DungeonAscendant.Game1();
game.Run();
