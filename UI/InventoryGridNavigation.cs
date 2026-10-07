using System;

namespace DungeonAscendant.UI;

public static class InventoryGridNavigation
{
    public const int ColumnCount = 4;

    public static int Move(
        int selectedIndex,
        int itemCount,
        int rowDelta,
        int columnDelta)
    {
        if (itemCount <= 0)
            return 0;

        int currentIndex = Math.Clamp(selectedIndex, 0, itemCount - 1);
        int currentRow = currentIndex / ColumnCount;
        int currentColumn = currentIndex % ColumnCount;
        int lastRow = (itemCount - 1) / ColumnCount;
        int targetRow = Math.Clamp(currentRow + rowDelta, 0, lastRow);
        int targetColumn = Math.Clamp(
            currentColumn + columnDelta,
            0,
            ColumnCount - 1);

        if (targetRow == currentRow && targetColumn == currentColumn)
            return currentIndex;

        int firstIndexInTargetRow = targetRow * ColumnCount;
        int itemsInTargetRow = Math.Min(
            ColumnCount,
            itemCount - firstIndexInTargetRow);

        if (rowDelta != 0)
            targetColumn = Math.Min(targetColumn, itemsInTargetRow - 1);

        int targetIndex = firstIndexInTargetRow + targetColumn;
        return targetIndex >= 0 && targetIndex < itemCount
            ? targetIndex
            : currentIndex;
    }

    public static void ValidateOrThrow()
    {
        Require(Move(0, 10, 0, 1) == 1, "right");
        Require(Move(1, 10, 0, -1) == 0, "left");
        Require(Move(1, 10, 1, 0) == 5, "down");
        Require(Move(5, 10, -1, 0) == 1, "up");
        Require(Move(0, 10, 0, -1) == 0, "left edge");
        Require(Move(3, 10, 0, 1) == 3, "right edge");
        Require(Move(2, 10, -1, 0) == 2, "top edge");
        Require(Move(8, 10, 1, 0) == 8, "bottom edge");
        Require(Move(7, 10, 1, 0) == 9, "partial last row");
        Require(Move(9, 10, 0, 1) == 9, "partial row edge");
        Require(Move(99, 10, 0, 0) == 9, "index clamp");
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Inventory grid navigation failed: {scenario}.");
        }
    }
}
