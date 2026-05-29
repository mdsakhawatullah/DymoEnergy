using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.SalesInvoices;

[Authorize(DymoEnergyPermissions.SalesInvoices.Default)]
public class SalesInvoiceAppService : ApplicationService, ISalesInvoiceAppService
{
    private readonly IRepository<SalesInvoice, int>     _invoiceRepository;
    private readonly IRepository<SalesInvoiceItem, int> _itemRepository;

    public SalesInvoiceAppService(
        IRepository<SalesInvoice, int>     invoiceRepository,
        IRepository<SalesInvoiceItem, int> itemRepository)
    {
        _invoiceRepository = invoiceRepository;
        _itemRepository    = itemRepository;
    }

    // ── READ ─────────────────────────────────────────────────────────────

    [AllowAnonymous]
    public async Task<SalesInvoiceDto> GetAsync(int id)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        return MapToDto(invoice);
    }

    [AllowAnonymous]
    public async Task<DymoPagedResultDto<SalesInvoiceDto>> GetListDataAsync(SalesInvoiceFilterDto input)
    {
        var query = await _invoiceRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(i =>
                (i.InvoiceNumber  != null && i.InvoiceNumber.Contains(input.Filter))  ||
                (i.CustomerName   != null && i.CustomerName.Contains(input.Filter))   ||
                (i.CustomerEmail  != null && i.CustomerEmail.Contains(input.Filter))  ||
                (i.ReferenceNumber != null && i.ReferenceNumber.Contains(input.Filter)));

        if (input.DateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate >= input.DateFrom.Value);

        if (input.DateTo.HasValue)
            query = query.Where(i => i.InvoiceDate <= input.DateTo.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var invoices = await AsyncExecuter.ToListAsync(query);

        return new DymoPagedResultDto<SalesInvoiceDto>(totalCount, invoices.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    public async Task<List<SalesInvoiceItemDto>> GetSaleInvoiceItemAsync(int invoiceId)
    {
        var query = await _itemRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query.Where(i => i.InvoiceId == invoiceId).OrderBy(i => i.DisplayOrder));
        return items.Select(MapItemToDto).ToList();
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.SalesInvoices.Create)]
    public async Task<SalesInvoiceDto> CreateInvoiceDataAsync(CreateUpdateSalesInvoiceDto input)
    {
        var invoice = new SalesInvoice();
        ApplyInput(invoice, input);
        invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();

        await _invoiceRepository.InsertAsync(invoice, autoSave: true);

        if (input.Items.Count > 0)
        {
            var items = input.Items
                .Select((dto, idx) => MapToItem(dto, invoice.Id, idx))
                .ToList();
            await _itemRepository.InsertManyAsync(items, autoSave: true);
        }

        return MapToDto(invoice);
    }

    [Authorize(DymoEnergyPermissions.SalesInvoices.Edit)]
    public async Task<SalesInvoiceDto> UpdateAsync(int id, CreateUpdateSalesInvoiceDto input)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        ApplyInput(invoice, input);
        await _invoiceRepository.UpdateAsync(invoice, autoSave: true);

        // ── Sync items ────────────────────────────────────────────────────
        var itemQuery    = await _itemRepository.GetQueryableAsync();
        var currentItems = await AsyncExecuter.ToListAsync(
            itemQuery.Where(i => i.InvoiceId == id));

        var inputWithId = input.Items.Where(i => i.Id is > 0).ToList();
        var inputNew    = input.Items.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        var toDelete = currentItems.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _itemRepository.DeleteManyAsync(toDelete, autoSave: true);

        var toUpdate = new List<SalesInvoiceItem>();
        foreach (var dto in inputWithId)
        {
            var existing = currentItems.FirstOrDefault(i => i.Id == dto.Id);
            if (existing == null) continue;
            ApplyItemInput(existing, dto);
            toUpdate.Add(existing);
        }
        if (toUpdate.Count > 0)
            await _itemRepository.UpdateManyAsync(toUpdate, autoSave: true);

        var toInsert = inputNew
            .Select((dto, idx) => MapToItem(dto, id, toUpdate.Count + idx))
            .ToList();
        if (toInsert.Count > 0)
            await _itemRepository.InsertManyAsync(toInsert, autoSave: true);

        return MapToDto(invoice);
    }

    [Authorize(DymoEnergyPermissions.SalesInvoices.Delete)]
    public async Task DeleteAsync(int id)
    {
        // Delete all items first (independent aggregate — no cascade)
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var items     = await AsyncExecuter.ToListAsync(itemQuery.Where(i => i.InvoiceId == id));
        if (items.Count > 0)
            await _itemRepository.DeleteManyAsync(items, autoSave: true);

        await _invoiceRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var today  = DateTime.Today;
        var prefix = $"INV-{today:yyyyMMdd}-";
        var query  = await _invoiceRepository.GetQueryableAsync();
        var count  = await AsyncExecuter.CountAsync(
            query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.StartsWith(prefix)));
        return $"{prefix}{(count + 1):D4}";
    }

    private static void ApplyInput(SalesInvoice inv, CreateUpdateSalesInvoiceDto input)
    {
        inv.PortalId        = input.PortalId;
        // InvoiceNumber is auto-generated on create and immutable — do not overwrite
        inv.InvoiceDate     = input.InvoiceDate;
        inv.DueDate         = input.DueDate;
        inv.CustomerId      = input.CustomerId;
        inv.CustomerName    = input.CustomerName;
        inv.CustomerEmail   = input.CustomerEmail;
        inv.CustomerPhone   = input.CustomerPhone;
        inv.BillingAddress  = input.BillingAddress;
        inv.ShippingAddress = input.ShippingAddress;
        inv.ReferenceNumber = input.ReferenceNumber;
        inv.CurrencyCode    = input.CurrencyCode ?? "BDT";
        inv.Subtotal        = input.Subtotal;
        inv.DiscountTotal   = input.DiscountTotal;
        inv.TaxTotal        = input.TaxTotal;
        inv.ShippingCost    = input.ShippingCost;
        inv.GrandTotal      = input.GrandTotal;
        inv.AmountPaid      = input.AmountPaid;
        inv.BalanceDue      = input.BalanceDue;
        inv.Status          = input.Status;
        inv.PaymentMethod   = input.PaymentMethod;
        inv.PaymentDate     = input.PaymentDate;
        inv.Notes           = input.Notes;
        inv.Terms           = input.Terms;
    }

    private static SalesInvoiceItem MapToItem(CreateUpdateSalesInvoiceItemDto dto, int invoiceId, int order) => new()
    {
        InvoiceId       = invoiceId,
        ProductId       = dto.ProductId,
        ProductName     = dto.ProductName,
        Sku             = dto.Sku,
        Description     = dto.Description,
        Quantity        = dto.Quantity,
        UnitPrice       = dto.UnitPrice,
        DiscountPercent = dto.DiscountPercent,
        DiscountAmount  = dto.DiscountAmount,
        TaxRate         = dto.TaxRate,
        TaxAmount       = dto.TaxAmount,
        LineTotal       = dto.LineTotal,
        DisplayOrder    = dto.DisplayOrder > 0 ? dto.DisplayOrder : order,
    };

    private static void ApplyItemInput(SalesInvoiceItem item, CreateUpdateSalesInvoiceItemDto dto)
    {
        item.ProductId       = dto.ProductId;
        item.ProductName     = dto.ProductName;
        item.Sku             = dto.Sku;
        item.Description     = dto.Description;
        item.Quantity        = dto.Quantity;
        item.UnitPrice       = dto.UnitPrice;
        item.DiscountPercent = dto.DiscountPercent;
        item.DiscountAmount  = dto.DiscountAmount;
        item.TaxRate         = dto.TaxRate;
        item.TaxAmount       = dto.TaxAmount;
        item.LineTotal       = dto.LineTotal;
        item.DisplayOrder    = dto.DisplayOrder;
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice inv) => new()
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
        Subtotal             = inv.Subtotal,
        DiscountTotal        = inv.DiscountTotal,
        TaxTotal             = inv.TaxTotal,
        ShippingCost         = inv.ShippingCost,
        GrandTotal           = inv.GrandTotal,
        AmountPaid           = inv.AmountPaid,
        BalanceDue           = inv.BalanceDue,
        Status               = inv.Status,
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
        DisplayOrder         = i.DisplayOrder,
    };

}
