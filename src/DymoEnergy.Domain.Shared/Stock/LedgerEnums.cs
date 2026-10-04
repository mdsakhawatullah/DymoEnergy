using System;

namespace DymoEnergy.Stock;

/// <summary>What happened to the stock in one ledger line.</summary>
public enum LedgerMovement
{
    Opening = 1,
    Received = 2,
    Sold = 3,
    UsedOnJob = 4,
    TransferredOut = 5,
    TransferredIn = 6,
    CountCorrection = 7,
    Damaged = 8,
    ReturnedToSupplier = 9,
    Lost = 10,
    InternalUse = 11,
    Reversal = 12,
    Other = 13,
}

/// <summary>Where the line came from.</summary>
public enum LedgerSource
{
    /// <summary>A person using the admin screens.</summary>
    Screen = 1,
    Pos = 2,
    Storefront = 3,
    /// <summary>An API key or another system.</summary>
    Api = 4,
    Import = 5,
    /// <summary>The app itself, with no person behind it.</summary>
    System = 6,
}

/// <summary>Why a line is worth a second look. Several can apply to one line.</summary>
[Flags]
public enum LedgerFlags
{
    None = 0,
    /// <summary>A hand correction with no reason typed in.</summary>
    NoReason = 1,
    /// <summary>A correction or write-off with no photo attached.</summary>
    NoPhoto = 2,
    /// <summary>Undone again within the "quickly reversed" minutes.</summary>
    QuicklyReversed = 4,
    /// <summary>Posted outside working hours.</summary>
    OutsideHours = 8,
    /// <summary>The move was blocked because stock would have gone below zero.</summary>
    WouldGoBelowZero = 16,
    /// <summary>Worth more than the big-value limit.</summary>
    LargeValue = 32,
    /// <summary>A serial number went out that the ledger never received.</summary>
    SerialNotReceived = 64,
    /// <summary>Needed a second person to approve and did not get one.</summary>
    NotApproved = 128,
}

public static class LedgerConsts
{
    /// <summary>The seal of the line before the very first one.</summary>
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";
    public const int HashLength = 64;
    public const int MaxReason = 512;
    public const int MaxName = 256;
    public const int MaxShort = 128;
    /// <summary>Lines are never deleted; this is only what the keeping card promises.</summary>
    public const int DefaultKeepYears = 10;
}
