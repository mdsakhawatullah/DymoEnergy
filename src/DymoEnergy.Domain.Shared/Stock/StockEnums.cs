namespace DymoEnergy.Stock;

public enum StockEntryType
{
    /// <summary>Goods received from a supplier.</summary>
    StockIn = 1,
    /// <summary>Sold, installed, damaged, returned to the supplier or otherwise used.</summary>
    StockOut = 2,
    /// <summary>Moved from one warehouse to another; total stock does not change.</summary>
    Transfer = 3,
    /// <summary>Stock set to what was counted on the shelf.</summary>
    Adjustment = 4,
}

public enum StockEntryStatus
{
    Draft = 1,
    Posted = 2,
    /// <summary>Posted, then undone by a reversing entry.</summary>
    Reversed = 3,
}

/// <summary>Why stock went out. Stored on stock-out entries so "out this month" can be broken down.</summary>
public enum StockOutReason
{
    Sold = 1,
    Installed = 2,
    Damaged = 3,
    ReturnedToSupplier = 4,
    Lost = 5,
    InternalUse = 6,
    Other = 7,
}

public static class StockConsts
{
    /// <summary>Products at or under this many units count as "low".</summary>
    public const int LowStockThreshold = 10;
    public const int MaxLines = 200;
    public const int MaxSerialsPerLine = 2000;
    public const int SerialMaxLength = 64;
}
