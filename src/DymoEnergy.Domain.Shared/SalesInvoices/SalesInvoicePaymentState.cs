namespace DymoEnergy.SalesInvoices;

/// <summary>
/// Collection state used by the invoice list tabs. Derived from status, balance and due date —
/// never stored.
/// </summary>
public enum SalesInvoicePaymentState
{
    Paid    = 1,
    Due     = 2,
    Overdue = 3,
}
