using System;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Companies;

public class CreateUpdateCompanyDto
{
    // ── General ───────────────────────────────────────────────────────────
    [Required]
    [MaxLength(256)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? CompanyCode { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Website { get; set; }

    [MaxLength(64)]
    public string? TaxId { get; set; }

    [MaxLength(512)]
    public string? Address { get; set; }

    [MaxLength(128)]
    public string? City { get; set; }

    [MaxLength(128)]
    public string? State { get; set; }

    [MaxLength(32)]
    public string? PostalCode { get; set; }

    [MaxLength(128)]
    public string? Country { get; set; }

    [MaxLength(256)]
    public string? ContactPersonName { get; set; }

    [MaxLength(256)]
    public string? ContactPersonEmail { get; set; }

    [MaxLength(32)]
    public string? ContactPersonPhone { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    // ── Statements / Parent-Child ────────────────────────────────────────
    [MaxLength(256)]
    public string? SendStatementTo { get; set; }

    public bool IsParentCompany { get; set; }
    public bool HasChildCompany { get; set; }
    public int? ParentCompanyId { get; set; }

    // ── Shipping Fee ──────────────────────────────────────────────────────
    public double  DefaultShippingFee    { get; set; }
    public double? FreeShippingThreshold { get; set; }
    public bool    IsFlatRateShipping    { get; set; }

    [MaxLength(8)]
    public string ShippingCurrencyCode { get; set; } = "BDT";

    // ── Integration ───────────────────────────────────────────────────────
    [MaxLength(256)]
    public string? ExternalAccountId { get; set; }

    [MaxLength(512)]
    public string? ApiKey { get; set; }

    [MaxLength(512)]
    public string? WebhookUrl { get; set; }

    public bool      IsSyncEnabled { get; set; }
    public DateTime? LastSyncedAt  { get; set; }
}
