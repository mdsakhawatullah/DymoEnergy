using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Stock;

/// <summary>
/// One change to one quantity, written by the server and never changed again. Each line is sealed with a
/// fingerprint made from its own contents and the fingerprint of the line before it, so altering an old
/// line breaks every seal after it. A mistake is fixed by posting a reversing line, never by editing.
/// </summary>
public class StockLedgerLine : Entity<int>
{
    // ── What moved ────────────────────────────────────────────────────────
    public int      ProductId     { get; set; }
    public string   ProductName   { get; set; } = string.Empty;
    public string?  Sku           { get; set; }
    public int      WarehouseId   { get; set; }
    public string   WarehouseName { get; set; } = string.Empty;
    public LedgerMovement Movement { get; set; }
    /// <summary>Signed: + into stock, − out of stock.</summary>
    public int      Change        { get; set; }
    public int      QuantityBefore { get; set; }
    public int      QuantityAfter  { get; set; }
    /// <summary>Cost of one unit at the moment of the move.</summary>
    public decimal  UnitCost      { get; set; }
    public decimal  ValueBefore   { get; set; }
    public decimal  ValueAfter    { get; set; }
    /// <summary>Serial numbers this line touched, one per line.</summary>
    public string?  Serials       { get; set; }

    // ── When ──────────────────────────────────────────────────────────────
    public DateTime Time          { get; set; }
    /// <summary>The clock the time was written on, e.g. "UTC+6, Dhaka".</summary>
    public string?  TimeZone      { get; set; }

    // ── Who ───────────────────────────────────────────────────────────────
    public Guid?    UserId        { get; set; }
    public string   UserName      { get; set; } = string.Empty;
    public string?  UserEmail     { get; set; }
    /// <summary>Their role at that moment — kept here because roles change later.</summary>
    public string?  UserRole      { get; set; }
    public DateTime? SignedInAt   { get; set; }
    public bool     TwoStepUsed   { get; set; }
    public Guid?    ApprovedById  { get; set; }
    public string?  ApprovedByName { get; set; }

    // ── Which machine ─────────────────────────────────────────────────────
    public LedgerSource Source    { get; set; }
    public string?  IpAddress     { get; set; }
    public string?  Device        { get; set; }
    public string?  SessionId     { get; set; }
    /// <summary>The screen or endpoint the change came from.</summary>
    public string?  CameFrom      { get; set; }

    // ── Why ───────────────────────────────────────────────────────────────
    public string?  Reason        { get; set; }
    /// <summary>The paper behind it: stock entry number, invoice number, job number.</summary>
    public string?  DocumentType  { get; set; }
    public string?  DocumentNumber { get; set; }
    public int?     StockEntryId  { get; set; }
    /// <summary>Set on a reversing line: the line it puts right.</summary>
    public int?     ReversesLineId { get; set; }

    // ── Watching ──────────────────────────────────────────────────────────
    public LedgerFlags Flags      { get; set; }

    // ── Seal ──────────────────────────────────────────────────────────────
    /// <summary>SHA-256 of this line's contents together with <see cref="PreviousHash"/>.</summary>
    public string   Hash          { get; set; } = string.Empty;
    public string   PreviousHash  { get; set; } = string.Empty;
    public string?  ServerName    { get; set; }
    public string?  RequestId     { get; set; }
}

/// <summary>Someone looked at a flagged line and said what they did about it. Kept apart so lines stay untouched.</summary>
public class StockLedgerReview : Entity<int>
{
    public int      LineId     { get; set; }
    public DateTime Time       { get; set; }
    public Guid?    UserId     { get; set; }
    public string   UserName   { get; set; } = string.Empty;
    /// <summary>checked | asked | reversed</summary>
    public string   Action     { get; set; } = "checked";
    public string?  Note       { get; set; }
}

/// <summary>The result of walking the whole chain and recomputing every seal.</summary>
public class StockLedgerCheck : Entity<int>
{
    public DateTime Time        { get; set; }
    public int      LinesChecked { get; set; }
    public bool     Ok          { get; set; }
    /// <summary>The first line whose seal did not match, when the check failed.</summary>
    public int?     FirstBadLineId { get; set; }
    public int      DurationMs  { get; set; }
    /// <summary>schedule | export | person</summary>
    public string   StartedBy   { get; set; } = "schedule";
    public string?  UserName    { get; set; }
}

/// <summary>The one row that holds what gets flagged and what the ledger promises.</summary>
public class StockLedgerSetting : FullAuditedAggregateRoot<int>
{
    // What gets flagged
    public bool FlagNoReason        { get; set; } = true;
    public bool FlagNoPhoto         { get; set; } = true;
    public bool FlagQuicklyReversed { get; set; } = true;
    public int  QuicklyReversedMinutes { get; set; } = 10;
    public bool FlagOutsideHours    { get; set; } = true;
    public int  WorkingFromHour     { get; set; } = 8;
    public int  WorkingToHour       { get; set; } = 21;
    public bool FlagBelowZero       { get; set; } = true;
    public bool FlagLargeValue      { get; set; } = true;
    public decimal LargeValueOver   { get; set; } = 20000;

    // Rules that protect the ledger
    public bool AskPasswordAgain    { get; set; } = true;
    public bool TwoPeopleForBigWriteOffs { get; set; } = true;
    public bool RequireTwoStep      { get; set; }
    public bool OnlyFromOfficeNetworks { get; set; }
    /// <summary>Comma separated IP prefixes allowed when the rule above is on.</summary>
    public string? AllowedNetworks  { get; set; }
    public bool EmailOwnerOnReversal { get; set; } = true;

    // Keeping
    public int  KeepYears           { get; set; } = LedgerConsts.DefaultKeepYears;
    public DateTime? LastExportAt   { get; set; }
    public string?   LastExportBy   { get; set; }
}
