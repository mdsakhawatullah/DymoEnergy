using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Companies;

public class CompanyDto : FullAuditedEntityDto<int>
{
    // ── General ───────────────────────────────────────────────────────────
    public string  CompanyName { get; set; } = string.Empty;
    public string? CompanyCode { get; set; }
    public string? Email       { get; set; }
    public string? Phone       { get; set; }
    public string? Website     { get; set; }
    public string? TaxId       { get; set; }

    public string? Address    { get; set; }
    public string? City       { get; set; }
    public string? State      { get; set; }
    public string? PostalCode { get; set; }
    public string? Country    { get; set; }

    public string? ContactPersonName  { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? ContactPersonPhone { get; set; }

    public string?       Notes  { get; set; }
    public CompanyStatus Status { get; set; }

    // ── Statements / Parent-Child ────────────────────────────────────────
    public string? SendStatementTo { get; set; }
    public bool    IsParentCompany { get; set; }
    public bool    HasChildCompany { get; set; }
    public int?    ParentCompanyId { get; set; }

    // ── Shipping Fee ──────────────────────────────────────────────────────
    public double  DefaultShippingFee    { get; set; }
    public double? FreeShippingThreshold { get; set; }
    public bool    IsFlatRateShipping    { get; set; }
    public string  ShippingCurrencyCode  { get; set; } = "BDT";

    // ── Integration ───────────────────────────────────────────────────────
    public string?   ExternalAccountId { get; set; }
    public string?   ApiKey            { get; set; }
    public string?   WebhookUrl        { get; set; }
    public bool      IsSyncEnabled     { get; set; }
    public DateTime? LastSyncedAt      { get; set; }
}
