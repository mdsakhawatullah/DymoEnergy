using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.SalesInvoices;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization;
using Volo.Abp.Uow;

namespace DymoEnergy.Finance;

public class FinanceExportService : IFinanceExportService
{
    private readonly FinanceCalculator _calc;
    private readonly IAuthorizationService _authorization;

    public FinanceExportService(FinanceCalculator calc, IAuthorizationService authorization)
    {
        _calc = calc;
        _authorization = authorization;
    }

    [UnitOfWork]
    public virtual async Task<(Stream Stream, string FileName)> BuildAsync(string? period)
    {
        if (!(await _authorization.AuthorizeAsync(DymoEnergyPermissions.Finance.Default)).Succeeded)
            throw new AbpAuthorizationException();

        var c = await _calc.LoadAsync();
        var p = FinanceCalculator.ResolvePeriod(period, c.Today);

        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "ledger.csv", Ledger(c, p));
            Add(zip, "sales-invoices.csv", Invoices(c, p));
            Add(zip, "supplier-bills.csv", Bills(c));
            Add(zip, "expenses.csv", Expenses(c, p));
            Add(zip, "profit-and-loss.csv", Pnl(c, p));
            Add(zip, "README.txt", new[]
            {
                new[] { $"Export for {p.Label} ({p.From:yyyy-MM-dd} to {p.To:yyyy-MM-dd}), created {c.Today:yyyy-MM-dd}." },
                new[] { "ledger.csv: every money movement in the period, from invoice payments and the ledger. Transfers between your own accounts are marked." },
                new[] { "sales-invoices.csv: invoices dated in the period.  supplier-bills.csv: all bills, with what has been paid." },
                new[] { "expenses.csv: expenses dated in the period.  profit-and-loss.csv: the statement with the previous period." },
                new[] { "This is a working summary, not a filed account." },
            }, plain: true);
        }
        output.Position = 0;
        return (output, $"accountant-export-{p.Key}-{c.Today:yyyyMMdd}.zip");
    }

    private static void Add(ZipArchive zip, string name, IEnumerable<string[]> rows, bool plain = false)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        foreach (var row in rows)
            writer.Write(plain ? string.Join("", row) + "\r\n" : string.Join(",", row.Select(Cell)) + "\r\n");
    }

    /// <summary>CSV cell with quoting, and a guard so spreadsheets never run user text as a formula.</summary>
    private static string Cell(string? value)
    {
        var s = value ?? string.Empty;
        if (s.Length > 0 && "=+-@\t\r".Contains(s[0]) && !decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            s = "'" + s;
        return s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static string N(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    private static string D(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    private IEnumerable<string[]> Ledger(FinanceContext c, FinancePeriod p)
    {
        yield return new[] { "Date", "Account", "Type", "Category", "What it was", "Money in", "Money out", "Balance after", "Transfer" };
        var names = c.Accounts.ToDictionary(a => a.Id, a => a.Name);
        foreach (var m in c.Moves.Where(m => m.Date >= p.From && m.Date <= p.To).OrderBy(m => m.Date).ThenBy(m => m.RefId))
        {
            yield return new[]
            {
                D(m.Date), m.AccountId.HasValue ? names.GetValueOrDefault(m.AccountId.Value, "") : "(no account)",
                m.Direction == FinanceDirection.In ? "Money in" : "Money out", m.Category ?? "", m.Title,
                m.Direction == FinanceDirection.In ? N(m.Amount) : "", m.Direction == FinanceDirection.Out ? N(m.Amount) : "",
                m.BalanceAfter.HasValue ? N(m.BalanceAfter.Value) : "", m.IsTransfer ? "yes" : "",
            };
        }
    }

    private static IEnumerable<string[]> Invoices(FinanceContext c, FinancePeriod p)
    {
        yield return new[] { "Invoice", "Date", "Customer", "Status", "Total (VAT incl.)", "VAT", "Paid", "Balance due", "Due date" };
        foreach (var i in c.Invoices.Where(i => i.InvoiceDate.Date >= p.From && i.InvoiceDate.Date <= p.To).OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id))
            yield return new[]
            {
                i.InvoiceNumber ?? "", D(i.InvoiceDate), i.CustomerName ?? "", i.Status.ToString(), N(FinanceCalculator.Money(i.GrandTotal)),
                N(FinanceCalculator.Money(i.TaxTotal)), N(FinanceCalculator.Money(i.AmountPaid)), N(FinanceCalculator.Money(i.BalanceDue)), D(i.DueDate),
            };
    }

    private IEnumerable<string[]> Bills(FinanceContext c)
    {
        yield return new[] { "Bill", "Supplier", "What for", "Bill date", "Due date", "Amount", "Paid", "Still owed", "Status", "Category" };
        foreach (var b in _calc.BillRows(c).OrderBy(b => b.DueDate))
            yield return new[] { b.BillNumber ?? "", b.Supplier, b.Description ?? "", D(b.BillDate), D(b.DueDate), N(b.Amount), N(b.Paid), N(b.Remaining), b.Status, b.CategoryName };
    }

    private static IEnumerable<string[]> Expenses(FinanceContext c, FinancePeriod p)
    {
        yield return new[] { "Date", "Category", "What for", "Amount", "Paid from" };
        var cats = c.Categories.ToDictionary(x => x.Id);
        var accounts = c.Accounts.ToDictionary(a => a.Id, a => a.Name);
        foreach (var e in c.Expenses.Where(e => e.Date.Date >= p.From && e.Date.Date <= p.To).OrderBy(e => e.Date).ThenBy(e => e.Id))
        {
            var dto = FinanceCalculator.MapExpense(e, cats, accounts);
            yield return new[] { D(dto.Date), dto.CategoryName, dto.Description, N(dto.Amount), dto.PaidBy };
        }
    }

    private IEnumerable<string[]> Pnl(FinanceContext c, FinancePeriod p)
    {
        var pnl = _calc.Pnl(c, p);
        yield return new[] { "Line", p.Label, p.PrevLabel, "Change %" };
        foreach (var l in pnl.Lines)
            yield return new[] { l.Label, N(l.Current), N(l.Previous), l.ChangePercent?.ToString(CultureInfo.InvariantCulture) ?? "" };
    }
}
