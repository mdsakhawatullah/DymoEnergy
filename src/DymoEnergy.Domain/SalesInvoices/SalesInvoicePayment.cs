using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.SalesInvoices;

/// <summary>One payment collected against a sales invoice (partial or full).</summary>
public class SalesInvoicePayment : FullAuditedAggregateRoot<int>
{
    // ── FK (no navigation) ──────────────────────────────────────────────────
    public int InvoiceId { get; set; }

    public double                    Amount          { get; set; }
    public SalesInvoicePaymentMethod Method          { get; set; } = SalesInvoicePaymentMethod.Cash;
    public DateTime                  PaidOn          { get; set; }
    /// <summary>Transaction id (bKash TrxID, cheque no., bank ref…).</summary>
    public string?                   ReferenceNumber { get; set; }
    public string?                   Note            { get; set; }
}
