using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Stock;

// ── Header ────────────────────────────────────────────────────────────────

public class LedgerHeaderDto
{
    public int      LinesToday     { get; set; }
    public int      PeopleToday    { get; set; }
    public int      MachinesToday  { get; set; }
    public int      InUnits        { get; set; }
    public int      InProducts     { get; set; }
    public int      OutUnits       { get; set; }
    public int      HandCorrections { get; set; }
    public int      FlaggedOpen    { get; set; }
    public int?     OldestFlaggedDays { get; set; }
    public long     TotalLines     { get; set; }
    /// <summary>Last time the whole chain was recomputed.</summary>
    public DateTime? LastCheckAt   { get; set; }
    public bool     LastCheckOk    { get; set; } = true;
    public int      LastCheckLines { get; set; }
}

// ── All movements ─────────────────────────────────────────────────────────

public class GetLedgerLinesInput
{
    [MaxLength(128)] public string? Filter { get; set; }
    public LedgerMovement? Movement { get; set; }
    public int?  ProductId   { get; set; }
    public int?  WarehouseId { get; set; }
    public Guid? UserId      { get; set; }
    /// <summary>Only lines that are waiting on someone.</summary>
    public bool  FlaggedOnly { get; set; }
    public int?  Days        { get; set; }
    public int   SkipCount   { get; set; }
    [Range(1, 500)] public int MaxResultCount { get; set; } = 12;
}

public class LedgerLineDto
{
    public int      Id            { get; set; }
    public DateTime Time          { get; set; }
    public int      ProductId     { get; set; }
    public string   ProductName   { get; set; } = string.Empty;
    public string?  Sku           { get; set; }
    public string   WarehouseName { get; set; } = string.Empty;
    public LedgerMovement Movement { get; set; }
    public string   MovementLabel { get; set; } = string.Empty;
    public int      Change        { get; set; }
    public int      QuantityBefore { get; set; }
    public int      QuantityAfter { get; set; }
    public string   UserName      { get; set; } = string.Empty;
    public string?  IpAddress     { get; set; }
    public string?  DocumentNumber { get; set; }
    public LedgerFlags Flags      { get; set; }
    public List<string> FlagLabels { get; set; } = new();
    public bool     Reviewed      { get; set; }
}

public class LedgerLinesPageDto
{
    public long TotalCount { get; set; }
    public List<LedgerLineDto> Items { get; set; } = new();
}

// ── One line, in full ─────────────────────────────────────────────────────

public class LedgerFieldChangeDto
{
    public string  Field  { get; set; } = string.Empty;
    public string  Was    { get; set; } = string.Empty;
    public string  Became { get; set; } = string.Empty;
    public bool    Changed { get; set; }
}

public class LedgerPaperDto
{
    public string  Icon   { get; set; } = "file";
    public string  Title  { get; set; } = string.Empty;
    public string? Detail { get; set; }
    /// <summary>stock-entry | ledger-line | none</summary>
    public string  Link   { get; set; } = "none";
    public int?    LinkId { get; set; }
}

public class LedgerReviewDto
{
    public DateTime Time     { get; set; }
    public string   UserName { get; set; } = string.Empty;
    public string   Action   { get; set; } = string.Empty;
    public string?  Note     { get; set; }
}

public class LedgerLineDetailDto
{
    public LedgerLineDto Line { get; set; } = new();
    public DateTime Time          { get; set; }
    public string?  TimeZone      { get; set; }
    public string   WarehouseName { get; set; } = string.Empty;
    public decimal  UnitCost      { get; set; }
    public decimal  ValueBefore   { get; set; }
    public decimal  ValueAfter    { get; set; }
    public List<string> Serials   { get; set; } = new();
    public string?  Reason        { get; set; }

    // Who
    public Guid?    UserId        { get; set; }
    public string?  UserEmail     { get; set; }
    public string?  UserRole      { get; set; }
    public DateTime? SignedInAt   { get; set; }
    /// <summary>"4 hours 28 minutes before this line", or null when the sign-in time is unknown.</summary>
    public string?  SignedInText  { get; set; }
    public bool     TwoStepUsed   { get; set; }
    public string?  ApprovedByName { get; set; }
    public bool     NeededApproval { get; set; }

    // Machine
    public string?  IpAddress     { get; set; }
    public string?  Device        { get; set; }
    public string?  SessionId     { get; set; }
    public string?  CameFrom      { get; set; }
    public LedgerSource Source    { get; set; }
    public string   SourceLabel   { get; set; } = string.Empty;

    public List<LedgerFieldChangeDto> Changes { get; set; } = new();
    public List<LedgerPaperDto> Paper { get; set; } = new();
    public List<LedgerReviewDto> Reviews { get; set; } = new();

    // Seal
    public string   Hash          { get; set; } = string.Empty;
    public string   PreviousHash  { get; set; } = string.Empty;
    public bool     SealOk        { get; set; }
    public string?  ServerName    { get; set; }
    public string?  RequestId     { get; set; }
    public int?     PreviousLineId { get; set; }
    public int?     NextLineId    { get; set; }
}

// ── One product ───────────────────────────────────────────────────────────

public class LedgerProductOptionDto
{
    public int     Id   { get; set; }
    public string  Name { get; set; } = string.Empty;
    public string? Sku  { get; set; }
}

public class LedgerBalancePointDto
{
    public DateTime Date     { get; set; }
    public int      Quantity { get; set; }
    public int      In       { get; set; }
    public int      Out      { get; set; }
}

public class LedgerCostLayerDto
{
    public int      LineId   { get; set; }
    public DateTime Date     { get; set; }
    public string   Title    { get; set; } = string.Empty;
    public string?  Detail   { get; set; }
    public int      Quantity { get; set; }
    public decimal  UnitCost { get; set; }
}

public class LedgerProductDto
{
    public LedgerProductOptionDto Product { get; set; } = new();
    public bool     TracksSerials { get; set; }
    public int      InStockNow   { get; set; }
    public string   WhereText    { get; set; } = string.Empty;
    public int      LinesInRange { get; set; }
    public int      MovesIn      { get; set; }
    public int      MovesOut     { get; set; }
    public int      HandCorrections { get; set; }
    public string?  HandCorrectionsBy { get; set; }
    public decimal  StockValue   { get; set; }
    public decimal  AverageCost  { get; set; }
    public int?     ReorderLevel { get; set; }
    public List<LedgerBalancePointDto> Balance { get; set; } = new();
    public List<LedgerLineDto> Lines { get; set; } = new();
    public List<LedgerCostLayerDto> Receipts { get; set; } = new();
}

// ── Needs a look ──────────────────────────────────────────────────────────

public class LedgerFlaggedDto
{
    public LedgerLineDto Line { get; set; } = new();
    public string  Title   { get; set; } = string.Empty;
    public string  Text    { get; set; } = string.Empty;
    /// <summary>photo | both-lines | open | investigate | reset</summary>
    public string  Action  { get; set; } = "open";
    public string  ActionLabel { get; set; } = "Open line";
    /// <summary>The other half of a quickly-reversed pair.</summary>
    public int?    PairLineId { get; set; }
}

public class LedgerHandCorrectorDto
{
    public Guid?   UserId  { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public string? Role    { get; set; }
    public int     Count   { get; set; }
    public decimal Value   { get; set; }
    public int     Percent { get; set; }
}

public class LedgerNeedsLookDto
{
    public List<LedgerFlaggedDto> Lines { get; set; } = new();
    public int     OpenCount { get; set; }
    public int?    OldestDays { get; set; }
    public string? OldestText { get; set; }
    public List<LedgerHandCorrectorDto> Correctors { get; set; } = new();
    public string? CorrectorNote { get; set; }
    public LedgerSettingDto Setting { get; set; } = new();
}

// ── Who has access ────────────────────────────────────────────────────────

public class LedgerPersonDto
{
    public Guid     Id        { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string?  Email     { get; set; }
    public string   Role      { get; set; } = string.Empty;
    public string   MayDo     { get; set; } = string.Empty;
    public DateTime? LastSignedIn { get; set; }
    public string?  LastIp    { get; set; }
    public string?  LastDevice { get; set; }
    public bool     TwoStep   { get; set; }
    public int      LinesPosted { get; set; }
}

public class LedgerSignInAttemptDto
{
    public DateTime Time    { get; set; }
    public string   Who     { get; set; } = string.Empty;
    public string   What    { get; set; } = string.Empty;
    public string?  Ip      { get; set; }
    public string?  Device  { get; set; }
    /// <summary>ok | blocked | warn</summary>
    public string   Tone    { get; set; } = "warn";
    public string   Result  { get; set; } = string.Empty;
}

public class LedgerMachineDto
{
    public LedgerSource Source { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public string  Detail  { get; set; } = string.Empty;
    public int     Lines   { get; set; }
    public DateTime? LastUsed { get; set; }
}

public class LedgerAccessDto
{
    public List<LedgerPersonDto> People { get; set; } = new();
    public List<LedgerSignInAttemptDto> Attempts { get; set; } = new();
    public List<LedgerMachineDto> Machines { get; set; } = new();
    public LedgerSettingDto Setting { get; set; } = new();
    public bool SecurityLogsAvailable { get; set; }
}

// ── Proof & keeping ───────────────────────────────────────────────────────

public class LedgerSealDto
{
    public int      LineId { get; set; }
    public string   Title  { get; set; } = string.Empty;
    public DateTime Time   { get; set; }
    public string   Hash   { get; set; } = string.Empty;
    public string   PreviousHash { get; set; } = string.Empty;
    public bool     IsFirst { get; set; }
}

public class LedgerCheckResultDto
{
    public bool     Ok        { get; set; }
    public int      LinesChecked { get; set; }
    public DateTime Time      { get; set; }
    public int      DurationMs { get; set; }
    public int?     FirstBadLineId { get; set; }
    public string   Message   { get; set; } = string.Empty;
}

public class LedgerProofDto
{
    public List<LedgerSealDto> Seals { get; set; } = new();
    public LedgerCheckResultDto? LastCheck { get; set; }
    public long     TotalLines { get; set; }
    public LedgerSettingDto Setting { get; set; } = new();
    public DateTime? LastExportAt { get; set; }
    public string?  LastExportBy { get; set; }
}

// ── Settings ──────────────────────────────────────────────────────────────

public class LedgerSettingDto
{
    public bool FlagNoReason        { get; set; }
    public bool FlagNoPhoto         { get; set; }
    public bool FlagQuicklyReversed { get; set; }
    [Range(1, 240)] public int QuicklyReversedMinutes { get; set; } = 10;
    public bool FlagOutsideHours    { get; set; }
    [Range(0, 23)] public int WorkingFromHour { get; set; } = 8;
    [Range(1, 24)] public int WorkingToHour   { get; set; } = 21;
    public bool FlagBelowZero       { get; set; }
    public bool FlagLargeValue      { get; set; }
    [Range(0, 100000000)] public decimal LargeValueOver { get; set; } = 20000;

    public bool AskPasswordAgain    { get; set; }
    public bool TwoPeopleForBigWriteOffs { get; set; }
    public bool RequireTwoStep      { get; set; }
    public bool OnlyFromOfficeNetworks { get; set; }
    [MaxLength(512)] public string? AllowedNetworks { get; set; }
    public bool EmailOwnerOnReversal { get; set; }

    [Range(1, 50)] public int KeepYears { get; set; } = 10;
}

public class ReviewLedgerLineDto
{
    /// <summary>checked | asked | reversed</summary>
    [Required, MaxLength(16)] public string Action { get; set; } = "checked";
    [MaxLength(512)] public string? Note { get; set; }
}
