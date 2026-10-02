using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Compliance;

// ── Settings ──────────────────────────────────────────────────────────────

public class ComplianceLabelDto
{
    public string Key     { get; set; } = string.Empty;
    public string Group   { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Value   { get; set; } = string.Empty;
}

public class ComplianceSettingDto
{
    public string AccentColor  { get; set; } = "#0E6B3F";
    public int    ExpiringDays { get; set; } = 30;
    public string? ImportRequiredDocs { get; set; }
    /// <summary>Every editable label with its current text (defaults filled in).</summary>
    public List<ComplianceLabelDto> Labels { get; set; } = new();
}

public class UpdateComplianceSettingDto
{
    [Required, MaxLength(16)] public string AccentColor { get; set; } = "#0E6B3F";
    [Range(1, 365)] public int ExpiringDays { get; set; } = 30;
    [MaxLength(2000)] public string? ImportRequiredDocs { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
}

// ── Licences ──────────────────────────────────────────────────────────────

public class ComplianceLicenceDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Number      { get; set; }
    public string? IssuedBy    { get; set; }
    public string? Owner       { get; set; }
    public DateTime? IssuedOn   { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool    HasExpiry   { get; set; }
    public int     Order       { get; set; }

    public int?    DaysLeft    { get; set; }
    /// <summary>ok | soon | expired | none (never expires) | unset (date missing)</summary>
    public string  Tone        { get; set; } = "ok";
    public string  StatusText  { get; set; } = string.Empty;
    /// <summary>0–1 share of the validity period still left.</summary>
    public double  Fraction    { get; set; }
}

public class CreateUpdateComplianceLicenceDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(256)]  public string? Number   { get; set; }
    [MaxLength(256)]  public string? IssuedBy { get; set; }
    [MaxLength(256)]  public string? Owner    { get; set; }
    public DateTime? IssuedOn   { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool HasExpiry { get; set; } = true;
    public int  Order     { get; set; }
}

// ── Filings ───────────────────────────────────────────────────────────────

public class ComplianceFilingDto
{
    public int     Id      { get; set; }
    public string  Title   { get; set; } = string.Empty;
    public string? Detail  { get; set; }
    public DateTime DueDate { get; set; }
    public ComplianceFilingStatus Status { get; set; }
    public string? Owner       { get; set; }
    public string? ActionLabel { get; set; }
    public int     Order       { get; set; }

    public int     DueInDays   { get; set; }
    /// <summary>ok | soon | overdue | done</summary>
    public string  Tone        { get; set; } = "ok";
}

public class CreateUpdateComplianceFilingDto
{
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Detail { get; set; }
    public DateTime DueDate { get; set; }
    public ComplianceFilingStatus Status { get; set; }
    [MaxLength(256)] public string? Owner       { get; set; }
    [MaxLength(128)] public string? ActionLabel { get; set; }
    public int Order { get; set; }
}

public class UpdateComplianceFilingStatusDto
{
    public ComplianceFilingStatus Status { get; set; }
}

public class ComplianceFilingStatsDto
{
    public int     DueThisMonth { get; set; }
    public int?    NextDueDays  { get; set; }
    public int     OnTime       { get; set; }
    public int     OnTimeTotal  { get; set; }
    public int     LateCount    { get; set; }
    public string? LateTitle    { get; set; }
    public string? LateDetail   { get; set; }
}

// ── Certificates ──────────────────────────────────────────────────────────

public class ComplianceCertificateDto
{
    public int     Id          { get; set; }
    public string  ProductName { get; set; } = string.Empty;
    public string? Category    { get; set; }
    public string? Supplier    { get; set; }
    public List<string> Badges { get; set; } = new();
    public string? TestReport  { get; set; }
    public DateTime? ValidUntil { get; set; }

    public int     FileCount   { get; set; }
    /// <summary>ok | soon | expired | missing</summary>
    public string  Tone        { get; set; } = "ok";
    public string  ValidText   { get; set; } = string.Empty;
    public bool    NeedsAttention { get; set; }
}

public class CreateUpdateComplianceCertificateDto
{
    [Required, MaxLength(256)] public string ProductName { get; set; } = string.Empty;
    [MaxLength(128)] public string? Category   { get; set; }
    [MaxLength(256)] public string? Supplier   { get; set; }
    public List<string> Badges { get; set; } = new();
    [MaxLength(256)] public string? TestReport { get; set; }
    public DateTime? ValidUntil { get; set; }
}

// ── Generic lists ─────────────────────────────────────────────────────────

public class ComplianceListItemDto
{
    public int    Id    { get; set; }
    public ComplianceItemKind Kind { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extra       { get; set; }
    public string? Color       { get; set; }
    public int?    Number      { get; set; }
    public bool    Flag        { get; set; }
    public int     Order       { get; set; }
}

public class CreateUpdateComplianceListItemDto
{
    public ComplianceItemKind Kind { get; set; }
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(2000)] public string? Extra       { get; set; }
    [MaxLength(16)]   public string? Color       { get; set; }
    public int?  Number { get; set; }
    public bool  Flag   { get; set; }
    public int   Order  { get; set; }
}

// ── Project packs ─────────────────────────────────────────────────────────

public class ComplianceDocCellDto
{
    public int DocTypeId { get; set; }
    public ComplianceDocStatus Status { get; set; }
}

public class ComplianceProjectPackDto
{
    public int     ProjectId  { get; set; }
    public string  Code       { get; set; } = string.Empty;
    public string  Name       { get; set; } = string.Empty;
    public string  StageName  { get; set; } = string.Empty;
    public string  StageColor { get; set; } = string.Empty;
    public bool    IsHandedOver { get; set; }
    public int     DonePercent  { get; set; }
    public List<ComplianceDocCellDto> Cells { get; set; } = new();
}

public class SetComplianceProjectDocumentDto
{
    public int ProjectId { get; set; }
    public int DocTypeId { get; set; }
    public ComplianceDocStatus Status { get; set; }
}

// ── Files ─────────────────────────────────────────────────────────────────

public class ComplianceFileDto
{
    public int     Id        { get; set; }
    public ComplianceFileOwner OwnerKind { get; set; }
    public int     OwnerId   { get; set; }
    public string  FileName  { get; set; } = string.Empty;
    public string  Url       { get; set; } = string.Empty;
    public long    SizeBytes { get; set; }
    public DateTime CreationTime { get; set; }
}

public class AddComplianceFileDto
{
    public ComplianceFileOwner OwnerKind { get; set; }
    public int OwnerId { get; set; }
    [Required, MaxLength(256)]  public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(1024)] public string Url      { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    /// <summary>Delete the owner's earlier files first (the "Replace" button).</summary>
    public bool ReplaceExisting { get; set; }
}

// ── Whole page ────────────────────────────────────────────────────────────

public class ComplianceSectionSummaryDto
{
    public int    Done  { get; set; }
    public int    Total { get; set; }
    public string Note  { get; set; } = string.Empty;
    /// <summary>red | amber | green</summary>
    public string Tone  { get; set; } = "green";
    /// <summary>Number shown on the tab chip.</summary>
    public int    Badge { get; set; }
}

public class ComplianceAlertDto
{
    public string Text { get; set; } = string.Empty;
    public int?   LicenceId { get; set; }
}

public class ComplianceSummaryDto
{
    /// <summary>good | almost | bad</summary>
    public string StatusKey { get; set; } = "good";
    public string StatusDetail { get; set; } = string.Empty;
    public ComplianceSectionSummaryDto Licences     { get; set; } = new();
    public ComplianceSectionSummaryDto Filings      { get; set; } = new();
    public ComplianceSectionSummaryDto Certificates { get; set; } = new();
    public ComplianceSectionSummaryDto Packs        { get; set; } = new();
    public ComplianceAlertDto? Alert { get; set; }
}

public class CompliancePageDto
{
    public ComplianceSettingDto Setting { get; set; } = new();
    public ComplianceSummaryDto Summary { get; set; } = new();
    public List<ComplianceLicenceDto>      Licences     { get; set; } = new();
    public List<ComplianceFilingDto>       Filings      { get; set; } = new();
    public ComplianceFilingStatsDto        FilingStats  { get; set; } = new();
    public List<ComplianceCertificateDto>  Certificates { get; set; } = new();
    public List<ComplianceListItemDto>     Items        { get; set; } = new();
    public List<ComplianceProjectPackDto>  Packs        { get; set; } = new();
    public List<ComplianceFileDto>         Files        { get; set; } = new();
}
