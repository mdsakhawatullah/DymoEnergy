using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Compliance;

/// <summary>Single-row settings: accent colour, thresholds and every editable label (stored as JSON).</summary>
public class ComplianceSetting : AuditedAggregateRoot<int>
{
    public string  AccentColor        { get; set; } = "#0E6B3F";
    /// <summary>Days before expiry at which something counts as "due soon".</summary>
    public int     ExpiringDays       { get; set; } = 30;
    /// <summary>Comma separated documents every import shipment should hold.</summary>
    public string? ImportRequiredDocs { get; set; }
    /// <summary>JSON object key → text; keys missing here fall back to built-in defaults.</summary>
    public string? LabelsJson         { get; set; }
}

public class ComplianceLicence : FullAuditedAggregateRoot<int>
{
    public string    Name        { get; set; } = string.Empty;
    public string?   Description { get; set; }
    public string?   Number      { get; set; }
    public string?   IssuedBy    { get; set; }
    public string?   Owner       { get; set; }
    public DateTime? IssuedOn    { get; set; }
    public DateTime? ValidUntil  { get; set; }
    /// <summary>False for registrations that never lapse (e.g. a tax number).</summary>
    public bool      HasExpiry   { get; set; } = true;
    public int       Order       { get; set; }
}

public class ComplianceFiling : FullAuditedAggregateRoot<int>
{
    public string  Title       { get; set; } = string.Empty;
    public string? Detail      { get; set; }
    public DateTime DueDate    { get; set; }
    public ComplianceFilingStatus Status { get; set; } = ComplianceFilingStatus.NotStarted;
    public string? Owner       { get; set; }
    /// <summary>Text of the row button, e.g. "Open report" or "Start".</summary>
    public string? ActionLabel { get; set; }
    public int     Order       { get; set; }
}

public class ComplianceCertificate : FullAuditedAggregateRoot<int>
{
    public string    ProductName { get; set; } = string.Empty;
    public string?   Category    { get; set; }
    public string?   Supplier    { get; set; }
    /// <summary>Comma separated badge codes, e.g. "IEC 61215, Factory".</summary>
    public string?   Badges      { get; set; }
    public string?   TestReport  { get; set; }
    public DateTime? ValidUntil  { get; set; }
}

/// <summary>Generic ordered list entry; <see cref="Kind"/> decides what the other fields mean.</summary>
public class ComplianceListItem : FullAuditedAggregateRoot<int>
{
    public ComplianceItemKind Kind { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extra       { get; set; }
    public string? Color       { get; set; }
    public int?    Number      { get; set; }
    public bool    Flag        { get; set; }
    public int     Order       { get; set; }
}

public class ComplianceProjectDocument : AggregateRoot<int>
{
    public int ProjectId { get; set; }
    public int DocTypeId { get; set; }
    public ComplianceDocStatus Status { get; set; }
}

public class ComplianceFile : FullAuditedAggregateRoot<int>
{
    public ComplianceFileOwner OwnerKind { get; set; }
    public int     OwnerId   { get; set; }
    public string  FileName  { get; set; } = string.Empty;
    public string  Url       { get; set; } = string.Empty;
    public long    SizeBytes { get; set; }
}
