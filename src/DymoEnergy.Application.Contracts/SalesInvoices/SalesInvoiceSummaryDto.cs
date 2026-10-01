namespace DymoEnergy.SalesInvoices;

/// <summary>Header stats and tab counts for the Sale invoices page.</summary>
public class SalesInvoiceSummaryDto
{
    /// <summary>Sum of GrandTotal for billable invoices (not draft / cancelled).</summary>
    public double SalesTotal { get; set; }
    public double Collected  { get; set; }
    public double StillDue   { get; set; }

    public int AllCount     { get; set; }
    public int PaidCount    { get; set; }
    public int DueCount     { get; set; }
    public int OverdueCount { get; set; }
}
