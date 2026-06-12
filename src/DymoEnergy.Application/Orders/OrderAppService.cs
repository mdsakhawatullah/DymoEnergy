using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Orders;

[Authorize(DymoEnergyPermissions.Orders.Default)]
public class OrderAppService : ApplicationService, IOrderAppService
{
    private readonly IRepository<Order, int>     _orderRepository;
    private readonly IRepository<OrderItem, int> _itemRepository;

    public OrderAppService(
        IRepository<Order, int>     orderRepository,
        IRepository<OrderItem, int> itemRepository)
    {
        _orderRepository = orderRepository;
        _itemRepository  = itemRepository;
    }

    // ── READ ──────────────────────────────────────────────────────────────

    [AllowAnonymous]
    public async Task<OrderDto> GetAsync(int id)
    {
        var order = await _orderRepository.GetAsync(id);
        return MapToDto(order);
    }

    [AllowAnonymous]
    public async Task<DymoPagedResultDto<OrderDto>> GetListDataAsync(OrderFilterDto input)
    {
        var query = await _orderRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(o =>
                (o.OrderNumber      != null && o.OrderNumber.Contains(input.Filter))     ||
                (o.CustomerName     != null && o.CustomerName.Contains(input.Filter))    ||
                (o.CustomerEmail    != null && o.CustomerEmail.Contains(input.Filter))   ||
                (o.CustomerPhone    != null && o.CustomerPhone.Contains(input.Filter))   ||
                (o.CustomerReference != null && o.CustomerReference.Contains(input.Filter)));

        if (input.Status.HasValue)      query = query.Where(o => o.Status      == input.Status);
        if (input.Stage.HasValue)       query = query.Where(o => o.Stage       == input.Stage);
        if (input.Priority.HasValue)    query = query.Where(o => o.Priority    == input.Priority);
        if (input.PaymentType.HasValue) query = query.Where(o => o.PaymentType == input.PaymentType);
        if (input.CustomerId.HasValue)  query = query.Where(o => o.CustomerId  == input.CustomerId);
        if (input.PortalId.HasValue)    query = query.Where(o => o.PortalId    == input.PortalId);
        if (input.DateFrom.HasValue)    query = query.Where(o => o.OrderDate   >= input.DateFrom.Value);
        if (input.DateTo.HasValue)      query = query.Where(o => o.OrderDate   <= input.DateTo.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var orders = await AsyncExecuter.ToListAsync(query);
        return new DymoPagedResultDto<OrderDto>(totalCount, orders.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    public async Task<List<OrderItemDto>> GetOrderItemsAsync(int id)
    {
        var query = await _itemRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query.Where(i => i.OrderId == id).OrderBy(i => i.DisplayOrder));
        return items.Select(MapItemToDto).ToList();
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [AllowAnonymous]
    public async Task<OrderDto> CreateOrderDataAsync(CreateUpdateOrderDto input)
    {
        var order = new Order();
        ApplyInput(order, input);

        // Insert first so the database assigns the auto-increment Id
        await _orderRepository.InsertAsync(order, autoSave: true);

        // Build order number from today's date + the new Id  (e.g. ORD-20260601-0007)
        order.OrderNumber = $"ORD-{DateTime.Today:yyyyMMdd}-{order.Id:D4}";
        await _orderRepository.UpdateAsync(order, autoSave: true);

        if (input.Items.Count > 0)
        {
            var items = input.Items
                .Select((dto, idx) => MapToItem(dto, order.Id, idx))
                .ToList();
            await _itemRepository.InsertManyAsync(items, autoSave: true);
        }

        return MapToDto(order);
    }

    [Authorize(DymoEnergyPermissions.Orders.Edit)]
    public async Task<OrderDto> UpdateAsync(int id, CreateUpdateOrderDto input)
    {
        var order = await _orderRepository.GetAsync(id);
        ApplyInput(order, input);
        await _orderRepository.UpdateAsync(order, autoSave: true);

        // ── Sync items ────────────────────────────────────────────────────
        var itemQuery    = await _itemRepository.GetQueryableAsync();
        var currentItems = await AsyncExecuter.ToListAsync(
            itemQuery.Where(i => i.OrderId == id));

        var inputWithId = input.Items.Where(i => i.Id is > 0).ToList();
        var inputNew    = input.Items.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        var toDelete = currentItems.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _itemRepository.DeleteManyAsync(toDelete, autoSave: true);

        var toUpdate = new List<OrderItem>();
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

        return MapToDto(order);
    }

    [Authorize(DymoEnergyPermissions.Orders.Delete)]
    public async Task DeleteAsync(int id)
    {
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var items     = await AsyncExecuter.ToListAsync(itemQuery.Where(i => i.OrderId == id));
        if (items.Count > 0)
            await _itemRepository.DeleteManyAsync(items, autoSave: true);

        await _orderRepository.DeleteAsync(id, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.Orders.Edit)]
    public async Task<OrderDto> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _orderRepository.GetAsync(id);
        order.Status = status;
        await _orderRepository.UpdateAsync(order, autoSave: true);
        return MapToDto(order);
    }

    [Authorize(DymoEnergyPermissions.Orders.Edit)]
    public async Task<OrderDto> UpdateStageAsync(int id, OrderStage stage)
    {
        var order = await _orderRepository.GetAsync(id);
        order.Stage = stage;
        await _orderRepository.UpdateAsync(order, autoSave: true);
        return MapToDto(order);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static void ApplyInput(Order o, CreateUpdateOrderDto input)
    {
        o.PortalId              = input.PortalId;
        o.OrderDate             = input.OrderDate;
        o.EstimatedDeliveryDate = input.EstimatedDeliveryDate;
        o.ActualDeliveryDate    = input.ActualDeliveryDate;
        o.CustomerId            = input.CustomerId;
        o.CustomerName          = input.CustomerName;
        o.CustomerEmail         = input.CustomerEmail;
        o.CustomerPhone         = input.CustomerPhone;
        o.CustomerReference     = input.CustomerReference;
        o.BillingAddress        = input.BillingAddress;
        o.DeliveryAddress       = input.DeliveryAddress;
        o.DeliveryContact       = input.DeliveryContact;
        o.DeliveryPhone         = input.DeliveryPhone;
        o.Status                = input.Status;
        o.Stage                 = input.Stage;
        o.Priority              = input.Priority;
        o.ShipmentType          = input.ShipmentType;
        o.PaymentType           = input.PaymentType;
        o.CreateMethod          = input.CreateMethod;
        o.CreatedHow            = input.CreatedHow;
        o.VoucherCode           = input.VoucherCode;
        o.VoucherAmount         = input.VoucherAmount;
        o.CurrencyCode          = input.CurrencyCode ?? "BDT";
        o.Subtotal              = input.Subtotal;
        o.DiscountTotal         = input.DiscountTotal;
        o.TaxRate               = input.TaxRate;
        o.TaxTotal              = input.TaxTotal;
        o.ShippingCost          = input.ShippingCost;
        o.GrandTotal            = input.GrandTotal;
        o.AmountPaid            = input.AmountPaid;
        o.BalanceDue            = input.BalanceDue;
        o.PaymentDate           = input.PaymentDate;
        o.Notes                 = input.Notes;
        o.NotesInvoice          = input.NotesInvoice;
        o.Terms                 = input.Terms;
        o.InternalNotes         = input.InternalNotes;
    }

    private static OrderItem MapToItem(CreateUpdateOrderItemDto dto, int orderId, int order) => new()
    {
        OrderId         = orderId,
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

    private static void ApplyItemInput(OrderItem item, CreateUpdateOrderItemDto dto)
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

    private static OrderDto MapToDto(Order o) => new()
    {
        Id                    = o.Id,
        CreationTime          = o.CreationTime,
        CreatorId             = o.CreatorId,
        LastModificationTime  = o.LastModificationTime,
        LastModifierId        = o.LastModifierId,
        IsDeleted             = o.IsDeleted,
        DeletionTime          = o.DeletionTime,
        DeleterId             = o.DeleterId,
        PortalId              = o.PortalId,
        OrderNumber           = o.OrderNumber,
        OrderDate             = o.OrderDate,
        EstimatedDeliveryDate = o.EstimatedDeliveryDate,
        ActualDeliveryDate    = o.ActualDeliveryDate,
        CustomerId            = o.CustomerId,
        CustomerName          = o.CustomerName,
        CustomerEmail         = o.CustomerEmail,
        CustomerPhone         = o.CustomerPhone,
        CustomerReference     = o.CustomerReference,
        BillingAddress        = o.BillingAddress,
        DeliveryAddress       = o.DeliveryAddress,
        DeliveryContact       = o.DeliveryContact,
        DeliveryPhone         = o.DeliveryPhone,
        Status                = o.Status,
        Stage                 = o.Stage,
        Priority              = o.Priority,
        ShipmentType          = o.ShipmentType,
        PaymentType           = o.PaymentType,
        CreateMethod          = o.CreateMethod,
        CreatedHow            = o.CreatedHow,
        VoucherCode           = o.VoucherCode,
        VoucherAmount         = o.VoucherAmount,
        CurrencyCode          = o.CurrencyCode,
        Subtotal              = o.Subtotal,
        DiscountTotal         = o.DiscountTotal,
        TaxRate               = o.TaxRate,
        TaxTotal              = o.TaxTotal,
        ShippingCost          = o.ShippingCost,
        GrandTotal            = o.GrandTotal,
        AmountPaid            = o.AmountPaid,
        BalanceDue            = o.BalanceDue,
        PaymentDate           = o.PaymentDate,
        Notes                 = o.Notes,
        NotesInvoice          = o.NotesInvoice,
        Terms                 = o.Terms,
        InternalNotes         = o.InternalNotes,
    };

    private static OrderItemDto MapItemToDto(OrderItem i) => new()
    {
        Id                   = i.Id,
        CreationTime         = i.CreationTime,
        CreatorId            = i.CreatorId,
        LastModificationTime = i.LastModificationTime,
        LastModifierId       = i.LastModifierId,
        IsDeleted            = i.IsDeleted,
        DeletionTime         = i.DeletionTime,
        DeleterId            = i.DeleterId,
        OrderId              = i.OrderId,
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
