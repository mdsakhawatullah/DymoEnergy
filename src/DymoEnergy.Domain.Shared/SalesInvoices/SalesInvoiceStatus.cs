namespace DymoEnergy.SalesInvoices;

public enum SalesInvoiceStatus
{
    Draft         = 1,
    Issued        = 2,
    Paid          = 3,
    PartiallyPaid = 4,
    Overdue       = 5,
    Cancelled     = 6,
    Refunded      = 7,
}
