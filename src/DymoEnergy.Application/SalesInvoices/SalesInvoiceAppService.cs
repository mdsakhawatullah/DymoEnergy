using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.SalesInvoices;

[Authorize(DymoEnergyPermissions.SalesInvoices.Default)]
public class SalesInvoiceAppService : ApplicationService, ISalesInvoiceAppService
{
    // Serializes invoice-number generation + insert so two concurrent requests
    // cannot read the same sequence counter before either has saved.
    private static readonly SemaphoreSlim _invoiceLock = new(1, 1);

    // Anything below this is treated as zero when comparing money (floating point noise).
    private const double Epsilon = 0.005;

    private readonly IRepository<SalesInvoice, int>        _invoiceRepository;
    private readonly IRepository<SalesInvoiceItem, int>    _itemRepository;
    private readonly IRepository<SalesInvoicePayment, int> _paymentRepository;

    public SalesInvoiceAppService(
        IRepository<SalesInvoice, int>        invoiceRepository,
        IRepository<SalesInvoiceItem, int>    itemRepository,
        IRepository<SalesInvoicePayment, int> paymentRepository)
    {
        _invoiceRepository = invoiceRepository;
        _itemRepository    = itemRepository;
        _paymentRepository = paymentRepository;
    }

    // ── READ ─────────────────────────────────────────────────────────────

    [AllowAnonymous]
    public async Task<SalesInvoiceDto> GetAsync(int id)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        var dto     = MapToDto(invoice);

        var itemQuery = await _itemRepository.GetQueryableAsync();
        var names = await AsyncExecuter.ToListAsync(
            itemQuery.Where(i => i.InvoiceId == id)
                     .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id)
                     .Select(i => i.ProductName));
        dto.ItemCount     = names.Count;
        dto.FirstItemName = names.FirstOrDefault();
        return dto;
    }

    [AllowAnonymous]
    public async Task<DymoPagedResultDto<SalesInvoiceDto>> GetListDataAsync(SalesInvoiceFilterDto input)
    {
        var query = ApplyFilters(await _invoiceRepository.GetQueryableAsync(),
            input.Filter, input.PortalId, input.DateFrom, input.DateTo);

        if (input.Status.HasValue)
            query = query.Where(i => i.Status == input.Status.Value);

        if (input.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == input.CustomerId.Value);

        if (input.PaymentState.HasValue)
            query = ApplyPaymentState(query, input.PaymentState.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var invoices = await AsyncExecuter.ToListAsync(query);
        var dtos     = invoices.Select(MapToDto).ToList();

        await FillItemSummariesAsync(dtos);

        return new DymoPagedResultDto<SalesInvoiceDto>(totalCount, dtos);
    }

    [AllowAnonymous]
    public async Task<SalesInvoiceSummaryDto> GetSummaryAsync(SalesInvoiceSummaryInputDto input)
    {
        var query = ApplyFilters(await _invoiceRepository.GetQueryableAsync(),
            input.Filter, input.PortalId, input.DateFrom, input.DateTo);

        var billable = query.Where(i =>
            i.Status != SalesInvoiceStatus.Draft &&
            i.Status != SalesInvoiceStatus.Cancelled &&
            i.Status != SalesInvoiceStatus.Refunded);

        var open = query.Where(i =>
            i.Status == SalesInvoiceStatus.Issued ||
            i.Status == SalesInvoiceStatus.PartiallyPaid ||
            i.Status == SalesInvoiceStatus.Overdue);

        return new SalesInvoiceSummaryDto
        {
            SalesTotal   = await AsyncExecuter.SumAsync(billable, i => i.GrandTotal),
            Collected    = await AsyncExecuter.SumAsync(billable, i => i.AmountPaid),
            StillDue     = await AsyncExecuter.SumAsync(open,     i => i.BalanceDue),
            AllCount     = await AsyncExecuter.CountAsync(query),
            PaidCount    = await AsyncExecuter.CountAsync(ApplyPaymentState(query, SalesInvoicePaymentState.Paid)),
            DueCount     = await AsyncExecuter.CountAsync(ApplyPaymentState(query, SalesInvoicePaymentState.Due)),
            OverdueCount = await AsyncExecuter.CountAsync(ApplyPaymentState(query, SalesInvoicePaymentState.Overdue)),
        };
    }

    [AllowAnonymous]
    public async Task<List<SalesInvoiceItemDto>> GetSaleInvoiceItemAsync(int invoiceId)
    {
        var query = await _itemRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query.Where(i => i.InvoiceId == invoiceId).OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id));
        return items.Select(MapItemToDto).ToList();
    }

    public async Task<List<SalesInvoicePaymentDto>> GetPaymentsAsync(int id)
    {
        var query    = await _paymentRepository.GetQueryableAsync();
        var payments = await AsyncExecuter.ToListAsync(
            query.Where(p => p.InvoiceId == id).OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.Id));
        return payments.Select(MapPaymentToDto).ToList();
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.SalesInvoices.Create)]
    public async Task<SalesInvoiceDto> CreateInvoiceDataAsync(CreateUpdateSalesInvoiceDto input)
    {
        var items = input.Items
            .Select((dto, idx) => { var item = new SalesInvoiceItem(); ApplyItemInput(item, dto, idx); return item; })
            .ToList();

        await _invoiceLock.WaitAsync();
        try
        {
            var invoice = new SalesInvoice();
            ApplyInput(invoice, input);
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();

            // Opening payment (e.g. deposit taken at the counter)
            var initialPayment = input.AmountPaid > Epsilon && invoice.Status != SalesInvoiceStatus.Draft
                ? input.AmountPaid
                : 0;
            invoice.AmountPaid = initialPayment;
            if (initialPayment > 0)
            {
                invoice.PaymentMethod = input.PaymentMethod ?? SalesInvoicePaymentMethod.Cash;
                invoice.PaymentDate   = input.PaymentDate ?? invoice.InvoiceDate;
            }

            RecalculateTotals(invoice, items);
            if (invoice.AmountPaid > invoice.GrandTotal + Epsilon)
                throw new UserFriendlyException("Amount paid cannot exceed the invoice total.");

            await _invoiceRepository.InsertAsync(invoice, autoSave: true);

            if (items.Count > 0)
            {
                items.ForEach(i => i.InvoiceId = invoice.Id);
                await _itemRepository.InsertManyAsync(items, autoSave: true);
            }

            if (initialPayment > 0)
            {
                await _paymentRepository.InsertAsync(new SalesInvoicePayment
                {
                    InvoiceId = invoice.Id,
                    Amount    = initialPayment,
                    Method    = invoice.PaymentMethod!.Value,
                    PaidOn    = invoice.PaymentDate!.Value,
                    Note      = "Paid at checkout",
                }, autoSave: true);
            }

            var dto = MapToDto(invoice);
            dto.ItemCount     = items.Count;
            dto.FirstItemName = items.FirstOrDefault()?.ProductName;
            return dto;
        }
        finally
        {
            _invoiceLock.Release();
        }
    }

    [Authorize(DymoEnergyPermissions.SalesInvoices.Edit)]
    public async Task<SalesInvoiceDto> UpdateAsync(int id, CreateUpdateSalesInvoiceDto input)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        ApplyInput(invoice, input);
        // AmountPaid is owned by the payments ledger — ignore input.AmountPaid here.

        // ── Sync items ────────────────────────────────────────────────────
        var itemQuery    = await _itemRepository.GetQueryableAsync();
        var currentItems = await AsyncExecuter.ToListAsync(
            itemQuery.Where(i => i.InvoiceId == id));

        var keepIds  = input.Items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
        var toDelete = currentItems.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _itemRepository.DeleteManyAsync(toDelete, autoSave: true);

        var finalItems = new List<SalesInvoiceItem>();
        var toUpdate   = new List<SalesInvoiceItem>();
        var toInsert   = new List<SalesInvoiceItem>();
        for (var idx = 0; idx < input.Items.Count; idx++)
        {
            var dto      = input.Items[idx];
            var existing = dto.Id is > 0 ? currentItems.FirstOrDefault(i => i.Id == dto.Id) : null;
            var item     = existing ?? new SalesInvoiceItem { InvoiceId = id };
            ApplyItemInput(item, dto, idx);
            (existing != null ? toUpdate : toInsert).Add(item);
            finalItems.Add(item);
        }
        if (toUpdate.Count > 0)
            await _itemRepository.UpdateManyAsync(toUpdate, autoSave: true);
        if (toInsert.Count > 0)
            await _itemRepository.InsertManyAsync(toInsert, autoSave: true);

        RecalculateTotals(invoice, finalItems);
        await _invoiceRepository.UpdateAsync(invoice, autoSave: true);

        var result = MapToDto(invoice);
        result.ItemCount     = finalItems.Count;
        result.FirstItemName = finalItems.FirstOrDefault()?.ProductName;
        return result;
    }

    [Authorize(DymoEnergyPermissions.SalesInvoices.Delete)]
    public async Task DeleteAsync(int id)
    {
        // Items and payments are independent aggregates — no cascade
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var items     = await AsyncExecuter.ToListAsync(itemQuery.Where(i => i.InvoiceId == id));
        if (items.Count > 0)
            await _itemRepository.DeleteManyAsync(items, autoSave: true);

        var paymentQuery = await _paymentRepository.GetQueryableAsync();
        var payments     = await AsyncExecuter.ToListAsync(paymentQuery.Where(p => p.InvoiceId == id));
        if (payments.Count > 0)
            await _paymentRepository.DeleteManyAsync(payments, autoSave: true);

        await _invoiceRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PAYMENTS ──────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.SalesInvoices.Edit)]
    public async Task<SalesInvoiceDto> CollectPaymentAsync(int id, CollectSalesInvoicePaymentDto input)
    {
        var invoice = await _invoiceRepository.GetAsync(id);

        if (invoice.Status is SalesInvoiceStatus.Cancelled or SalesInvoiceStatus.Refunded)
            throw new UserFriendlyException("Payments cannot be collected on a cancelled or refunded invoice.");
        if (invoice.BalanceDue <= Epsilon)
            throw new UserFriendlyException("This invoice is already fully paid.");
        if (input.Amount > invoice.BalanceDue + Epsilon)
            throw new UserFriendlyException($"Amount exceeds the outstanding balance of {invoice.BalanceDue:N2}.");

        var paidOn = input.PaidOn ?? Clock.Now;

        await _paymentRepository.InsertAsync(new SalesInvoicePayment
        {
            InvoiceId       = id,
            Amount          = Math.Round(input.Amount, 2),
            Method          = input.Method,
            PaidOn          = paidOn,
            ReferenceNumber = input.ReferenceNumber,
            Note            = input.Note,
        }, autoSave: true);

        // Collecting money on a draft implicitly issues it
        if (invoice.Status == SalesInvoiceStatus.Draft)
            invoice.Status = SalesInvoiceStatus.Issued;

        invoice.AmountPaid    = Math.Round(invoice.AmountPaid + input.Amount, 2);
        invoice.PaymentMethod = input.Method;
        invoice.PaymentDate   = paidOn;
        UpdateBalanceAndStatus(invoice);

        await _invoiceRepository.UpdateAsync(invoice, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(DymoEnergyPermissions.SalesInvoices.Edit)]
    public async Task<SalesInvoiceDto> DeletePaymentAsync(int id, int paymentId)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        var payment = await _paymentRepository.GetAsync(paymentId);
        if (payment.InvoiceId != id)
            throw new UserFriendlyException("Payment does not belong to this invoice.");

        await _paymentRepository.DeleteAsync(payment, autoSave: true);

        var query  = await _paymentRepository.GetQueryableAsync();
        var latest = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(p => p.InvoiceId == id).OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.Id));

        invoice.AmountPaid    = Math.Max(0, Math.Round(invoice.AmountPaid - payment.Amount, 2));
        invoice.PaymentMethod = latest?.Method;
        invoice.PaymentDate   = latest?.PaidOn;
        UpdateBalanceAndStatus(invoice);

        await _invoiceRepository.UpdateAsync(invoice, autoSave: true);
        return await GetAsync(id);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static IQueryable<SalesInvoice> ApplyFilters(
        IQueryable<SalesInvoice> query, string? filter, int? portalId, DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim();
            query = query.Where(i =>
                (i.InvoiceNumber   != null && i.InvoiceNumber.Contains(f))   ||
                (i.CustomerName    != null && i.CustomerName.Contains(f))    ||
                (i.CustomerPhone   != null && i.CustomerPhone.Contains(f))   ||
                (i.CustomerEmail   != null && i.CustomerEmail.Contains(f))   ||
                (i.ReferenceNumber != null && i.ReferenceNumber.Contains(f)));
        }

        if (portalId.HasValue)
            query = query.Where(i => i.PortalId == portalId.Value);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate <= dateTo.Value);

        return query;
    }

    private IQueryable<SalesInvoice> ApplyPaymentState(IQueryable<SalesInvoice> query, SalesInvoicePaymentState state)
    {
        var today = Clock.Now.Date;
        return state switch
        {
            SalesInvoicePaymentState.Paid => query.Where(i => i.Status == SalesInvoiceStatus.Paid),

            SalesInvoicePaymentState.Due => query.Where(i =>
                (i.Status == SalesInvoiceStatus.Issued ||
                 i.Status == SalesInvoiceStatus.PartiallyPaid ||
                 i.Status == SalesInvoiceStatus.Overdue) &&
                (i.DueDate == null || i.DueDate >= today)),

            SalesInvoicePaymentState.Overdue => query.Where(i =>
                (i.Status == SalesInvoiceStatus.Issued ||
                 i.Status == SalesInvoiceStatus.PartiallyPaid ||
                 i.Status == SalesInvoiceStatus.Overdue) &&
                i.DueDate != null && i.DueDate < today),

            _ => query,
        };
    }

    private async Task FillItemSummariesAsync(List<SalesInvoiceDto> dtos)
    {
        if (dtos.Count == 0) return;

        var ids       = dtos.Select(d => d.Id).ToList();
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var rows      = await AsyncExecuter.ToListAsync(
            itemQuery.Where(i => ids.Contains(i.InvoiceId))
                     .Select(i => new { i.InvoiceId, i.ProductName, i.DisplayOrder, i.Id }));

        var byInvoice = rows
            .GroupBy(r => r.InvoiceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id).ToList());

        foreach (var dto in dtos)
        {
            if (!byInvoice.TryGetValue(dto.Id, out var list)) continue;
            dto.ItemCount     = list.Count;
            dto.FirstItemName = list[0].ProductName;
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var prefix = $"INV-{Clock.Now:yyyy}-";
        var query  = await _invoiceRepository.GetQueryableAsync();

        // Pull the raw number strings so we can parse the sequence suffix in memory.
        // Using MAX on the suffix (not COUNT) means soft-deleted invoices never
        // cause the sequence to go backwards and reuse an old number.
        var numbers = await AsyncExecuter.ToListAsync(
            query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.StartsWith(prefix))
                 .Select(i => i.InvoiceNumber));

        var maxSeq = numbers
            .Select(n => int.TryParse(n!.AsSpan(prefix.Length), out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxSeq + 1):D5}";
    }

    /// <summary>
    /// Computes line amounts, invoice totals, balance and status. The server is the
    /// source of truth for money — client-sent totals are ignored.
    /// </summary>
    private static void RecalculateTotals(SalesInvoice inv, List<SalesInvoiceItem> items)
    {
        double subtotal = 0, lineDiscounts = 0, tax = 0, net = 0, lines = 0;

        foreach (var item in items)
        {
            var gross = item.Quantity * item.UnitPrice;
            var disc  = item.DiscountPercent > 0
                ? gross * item.DiscountPercent / 100
                : Math.Min(Math.Max(item.DiscountAmount, 0), gross);
            var itemNet = gross - disc;

            double itemTax, lineTotal;
            if (inv.TaxInclusive)
            {
                itemTax   = item.TaxRate > 0 ? itemNet - itemNet / (1 + item.TaxRate / 100) : 0;
                lineTotal = itemNet;
            }
            else
            {
                itemTax   = itemNet * item.TaxRate / 100;
                lineTotal = itemNet + itemTax;
            }

            item.DiscountAmount = Math.Round(disc, 2);
            item.TaxAmount      = Math.Round(itemTax, 2);
            item.LineTotal      = Math.Round(lineTotal, 2);

            subtotal      += gross;
            lineDiscounts += disc;
            tax           += itemTax;
            net           += itemNet;
            lines         += lineTotal;
        }

        var additional = Math.Max(0, inv.AdditionalDiscount);
        // With VAT-inclusive prices the invoice discount also reduces the VAT portion
        if (inv.TaxInclusive && additional > 0 && net > 0)
            tax *= Math.Max(0, net - additional) / net;

        inv.AdditionalDiscount = Math.Round(additional, 2);
        inv.Subtotal           = Math.Round(subtotal, 2);
        inv.DiscountTotal      = Math.Round(lineDiscounts + additional, 2);
        inv.TaxTotal           = Math.Round(tax, 2);
        inv.GrandTotal         = Math.Round(Math.Max(0, lines - additional + inv.ShippingCost), 2);

        UpdateBalanceAndStatus(inv);
    }

    private static void UpdateBalanceAndStatus(SalesInvoice inv)
    {
        inv.BalanceDue = Math.Round(Math.Max(0, inv.GrandTotal - inv.AmountPaid), 2);

        // Draft / Cancelled / Refunded are manual states; everything else is derived.
        // Overdue is never stored — it depends on today's date and is computed on read.
        if (inv.Status is SalesInvoiceStatus.Draft or SalesInvoiceStatus.Cancelled or SalesInvoiceStatus.Refunded)
            return;

        inv.Status = inv.BalanceDue <= Epsilon ? SalesInvoiceStatus.Paid
                   : inv.AmountPaid > Epsilon  ? SalesInvoiceStatus.PartiallyPaid
                   :                             SalesInvoiceStatus.Issued;
    }

    private static void ApplyInput(SalesInvoice inv, CreateUpdateSalesInvoiceDto input)
    {
        inv.PortalId           = input.PortalId;
        // InvoiceNumber is auto-generated on create and immutable — do not overwrite
        inv.InvoiceDate        = input.InvoiceDate;
        inv.DueDate            = input.DueDate;
        inv.CustomerId         = input.CustomerId;
        inv.CustomerName       = input.CustomerName;
        inv.CustomerEmail      = input.CustomerEmail;
        inv.CustomerPhone      = input.CustomerPhone;
        inv.BillingAddress     = input.BillingAddress;
        inv.ShippingAddress    = input.ShippingAddress;
        inv.ReferenceNumber    = input.ReferenceNumber;
        inv.CurrencyCode       = string.IsNullOrWhiteSpace(input.CurrencyCode) ? "BDT" : input.CurrencyCode;
        inv.Channel            = input.Channel;
        inv.ShippingCost       = Math.Max(0, input.ShippingCost);
        inv.AdditionalDiscount = input.AdditionalDiscount;
        inv.DiscountNote       = input.DiscountNote;
        inv.TaxInclusive       = input.TaxInclusive;
        inv.Notes              = input.Notes;
        inv.Terms              = input.Terms;

        // Only manual states are accepted; paid/partially-paid/overdue collapse to Issued
        // and are re-derived from the balance.
        inv.Status = input.Status is SalesInvoiceStatus.Draft or SalesInvoiceStatus.Cancelled or SalesInvoiceStatus.Refunded
            ? input.Status
            : SalesInvoiceStatus.Issued;
    }

    private static void ApplyItemInput(SalesInvoiceItem item, CreateUpdateSalesInvoiceItemDto dto, int order)
    {
        item.ProductId       = dto.ProductId;
        item.ProductName     = dto.ProductName;
        item.Sku             = dto.Sku;
        item.Description     = dto.Description;
        item.Quantity        = Math.Max(0, dto.Quantity);
        item.UnitPrice       = Math.Max(0, dto.UnitPrice);
        item.DiscountPercent = Math.Clamp(dto.DiscountPercent, 0, 100);
        item.DiscountAmount  = dto.DiscountAmount;
        item.TaxRate         = Math.Max(0, dto.TaxRate);
        item.SerialNumbers   = dto.SerialNumbers;
        item.Warranty        = dto.Warranty;
        item.DisplayOrder    = order;
        // TaxAmount / LineTotal are computed in RecalculateTotals
    }

    private SalesInvoiceStatus EffectiveStatus(SalesInvoice inv)
    {
        var isOpen = inv.Status is SalesInvoiceStatus.Issued or SalesInvoiceStatus.PartiallyPaid or SalesInvoiceStatus.Overdue;
        if (!isOpen) return inv.Status;
        if (inv.DueDate.HasValue && inv.DueDate.Value.Date < Clock.Now.Date) return SalesInvoiceStatus.Overdue;
        // Legacy rows may have Overdue stored — fall back to the derived state
        if (inv.Status == SalesInvoiceStatus.Overdue)
            return inv.AmountPaid > Epsilon ? SalesInvoiceStatus.PartiallyPaid : SalesInvoiceStatus.Issued;
        return inv.Status;
    }

    private SalesInvoiceDto MapToDto(SalesInvoice inv) => new()
    {
        Id                   = inv.Id,
        CreationTime         = inv.CreationTime,
        CreatorId            = inv.CreatorId,
        LastModificationTime = inv.LastModificationTime,
        LastModifierId       = inv.LastModifierId,
        IsDeleted            = inv.IsDeleted,
        DeletionTime         = inv.DeletionTime,
        DeleterId            = inv.DeleterId,
        PortalId             = inv.PortalId,
        InvoiceNumber        = inv.InvoiceNumber,
        InvoiceDate          = inv.InvoiceDate,
        DueDate              = inv.DueDate,
        CustomerId           = inv.CustomerId,
        CustomerName         = inv.CustomerName,
        CustomerEmail        = inv.CustomerEmail,
        CustomerPhone        = inv.CustomerPhone,
        BillingAddress       = inv.BillingAddress,
        ShippingAddress      = inv.ShippingAddress,
        ReferenceNumber      = inv.ReferenceNumber,
        CurrencyCode         = inv.CurrencyCode,
        Channel              = inv.Channel,
        Subtotal             = inv.Subtotal,
        DiscountTotal        = inv.DiscountTotal,
        AdditionalDiscount   = inv.AdditionalDiscount,
        DiscountNote         = inv.DiscountNote,
        TaxInclusive         = inv.TaxInclusive,
        TaxTotal             = inv.TaxTotal,
        ShippingCost         = inv.ShippingCost,
        GrandTotal           = inv.GrandTotal,
        AmountPaid           = inv.AmountPaid,
        BalanceDue           = inv.BalanceDue,
        Status               = EffectiveStatus(inv),
        PaymentMethod        = inv.PaymentMethod,
        PaymentDate          = inv.PaymentDate,
        Notes                = inv.Notes,
        Terms                = inv.Terms,
    };

    private static SalesInvoiceItemDto MapItemToDto(SalesInvoiceItem i) => new()
    {
        Id                   = i.Id,
        CreationTime         = i.CreationTime,
        CreatorId            = i.CreatorId,
        LastModificationTime = i.LastModificationTime,
        LastModifierId       = i.LastModifierId,
        IsDeleted            = i.IsDeleted,
        DeletionTime         = i.DeletionTime,
        DeleterId            = i.DeleterId,
        InvoiceId            = i.InvoiceId,
        ProductId            = i.ProductId,
        ProductName          = i.ProductName,
        Sku                  = i.Sku,
        Description          = i.Description,
        Quantity             = i.Quantity,
        UnitPrice            = i.UnitPrice,
        DiscountPercent      = i.DiscountPercent,
        DiscountAmount       = i.DiscountAmount,
        TaxRate              = i.TaxRate,
        TaxAmount            = i.TaxAmount,
        LineTotal            = i.LineTotal,
        SerialNumbers        = i.SerialNumbers,
        Warranty             = i.Warranty,
        DisplayOrder         = i.DisplayOrder,
    };

    private static SalesInvoicePaymentDto MapPaymentToDto(SalesInvoicePayment p) => new()
    {
        Id              = p.Id,
        CreationTime    = p.CreationTime,
        CreatorId       = p.CreatorId,
        InvoiceId       = p.InvoiceId,
        Amount          = p.Amount,
        Method          = p.Method,
        PaidOn          = p.PaidOn,
        ReferenceNumber = p.ReferenceNumber,
        Note            = p.Note,
    };
}
