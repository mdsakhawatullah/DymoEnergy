using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Orders;
using DymoEnergy.Permissions;
using DymoEnergy.QuoteRequests;
using DymoEnergy.SalesInvoices;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Customers;

[Authorize(DymoEnergyPermissions.Customers.Default)]
public class CustomerAppService : ApplicationService, ICustomerAppService
{
    private readonly IRepository<Customer, int>     _customers;
    private readonly IRepository<Order, int>        _orders;
    private readonly IRepository<OrderItem, int>    _orderItems;
    private readonly IRepository<SalesInvoice, int> _invoices;
    private readonly IRepository<QuoteRequest, int> _quotes;

    public CustomerAppService(
        IRepository<Customer, int> customers, IRepository<Order, int> orders, IRepository<OrderItem, int> orderItems,
        IRepository<SalesInvoice, int> invoices, IRepository<QuoteRequest, int> quotes)
    {
        _customers = customers; _orders = orders; _orderItems = orderItems; _invoices = invoices; _quotes = quotes;
    }

    /// <summary>"+880 1712-345678" and "01712345678" are the same person. Null when it is not a usable mobile number.</summary>
    public static string? PhoneKeyOf(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 13 && digits.StartsWith("880")) digits = digits[2..];
        else if (digits.Length == 14 && digits.StartsWith("8801")) digits = digits[3..];
        if (digits.Length == 10 && digits.StartsWith("1")) digits = "0" + digits;
        return digits.Length == 11 && digits.StartsWith("01") ? digits : null;
    }

    private static List<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags) ? new() : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? WhereText(Customer c) =>
        string.Join(", ", new[] { c.Area, c.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

    // ══ WHAT THEY BOUGHT ═════════════════════════════════════════════════════

    /// <summary>
    /// Totals per customer taken from their orders. Orders carry the customer id once linked; those taken
    /// before customers were kept are matched on the phone number instead, so the totals are not misleading.
    /// </summary>
    private sealed class Rollup
    {
        public int      Orders     { get; set; }
        public decimal  Spent      { get; set; }
        public decimal  Owed       { get; set; }
        public DateTime? First     { get; set; }
        public DateTime? Last      { get; set; }
    }

    private async Task<Dictionary<int, Rollup>> RollupAsync(List<Customer> customers)
    {
        var result = customers.ToDictionary(c => c.Id, _ => new Rollup());
        if (customers.Count == 0) return result;

        var ids = customers.Select(c => c.Id).ToList();
        var keys = customers.Where(c => c.PhoneKey != null).ToDictionary(c => c.PhoneKey!, c => c.Id);

        var orders = await AsyncExecuter.ToListAsync((await _orders.GetQueryableAsync())
            .Where(o => o.Status != OrderStatus.Cancelled && (o.CustomerId != null && ids.Contains(o.CustomerId.Value) || o.CustomerPhone != null))
            .Select(o => new { o.CustomerId, o.CustomerPhone, o.OrderDate, o.GrandTotal, o.BalanceDue }));

        foreach (var o in orders)
        {
            var id = o.CustomerId is { } linked && result.ContainsKey(linked) ? linked
                : PhoneKeyOf(o.CustomerPhone) is { } key && keys.TryGetValue(key, out var matched) ? matched : (int?)null;
            if (id is not { } customerId) continue;
            var r = result[customerId];
            r.Orders++;
            r.Spent += (decimal)o.GrandTotal;
            r.Owed += (decimal)o.BalanceDue;
            if (r.First == null || o.OrderDate < r.First) r.First = o.OrderDate;
            if (r.Last == null || o.OrderDate > r.Last) r.Last = o.OrderDate;
        }
        return result;
    }

    private int? QuietDays(DateTime? last) =>
        last == null ? null : (int)(Clock.Now.Date - last.Value.Date).TotalDays;

    // ══ OVERVIEW ═════════════════════════════════════════════════════════════

    public async Task<CustomerOverviewDto> GetOverviewAsync()
    {
        var all = await _customers.GetListAsync();
        var roll = await RollupAsync(all);
        var monthStart = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);

        var dto = new CustomerOverviewDto
        {
            Total = all.Count,
            NewThisMonth = all.Count(c => c.CreationTime >= monthStart),
            BuyingThisMonth = roll.Count(r => r.Value.Last >= monthStart),
            OwedTotal = Math.Round(roll.Sum(r => r.Value.Owed), 2),
            OwedCount = roll.Count(r => r.Value.Owed > 0),
            RepeatCount = roll.Count(r => r.Value.Orders > 1),
            QuietCount = roll.Count(r => r.Value.Last != null && QuietDays(r.Value.Last) > CustomerConsts.QuietAfterDays),
            Cities = all.Where(c => !string.IsNullOrWhiteSpace(c.City)).Select(c => c.City!).Distinct().OrderBy(c => c).ToList(),
        };
        var bought = roll.Count(r => r.Value.Orders > 0);
        dto.RepeatPercent = bought == 0 ? 0 : (int)Math.Round(dto.RepeatCount * 100.0 / bought);

        var monthOrders = await AsyncExecuter.ToListAsync((await _orders.GetQueryableAsync())
            .Where(o => o.OrderDate >= monthStart && o.Status != OrderStatus.Cancelled).Select(o => o.GrandTotal));
        dto.SoldThisMonth = Math.Round((decimal)monthOrders.Sum(), 2);

        // Orders still standing on their own, and how many customers they would make.
        var loose = await AsyncExecuter.ToListAsync((await _orders.GetQueryableAsync())
            .Where(o => o.CustomerId == null && o.CustomerPhone != null).Select(o => o.CustomerPhone));
        var known = all.Where(c => c.PhoneKey != null).Select(c => c.PhoneKey!).ToHashSet();
        var looseKeys = loose.Select(PhoneKeyOf).Where(k => k != null).Select(k => k!).ToList();
        dto.UnlinkedOrders = looseKeys.Count;
        dto.WouldCreate = looseKeys.Where(k => !known.Contains(k)).Distinct().Count();
        return dto;
    }

    // ══ LIST ═════════════════════════════════════════════════════════════════

    public async Task<CustomersPageDto> GetListAsync(GetCustomersInput input)
    {
        var all = await _customers.GetListAsync();
        var roll = await RollupAsync(all);

        var rows = all.Select(c =>
        {
            var r = roll[c.Id];
            return new CustomerListItemDto
            {
                Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Type = c.Type, Status = c.Status,
                CompanyName = c.CompanyName, Where = WhereText(c), AssignedTo = c.AssignedTo, Tags = SplitTags(c.Tags),
                Orders = r.Orders, TotalSpent = Math.Round(r.Spent, 2), Owed = Math.Round(r.Owed, 2),
                LastOrderAt = r.Last, QuietDays = QuietDays(r.Last),
            };
        }).ToList();

        var counts = new CustomerCountsDto
        {
            All = rows.Count,
            Households = rows.Count(r => r.Type == CustomerType.Household),
            Businesses = rows.Count(r => r.Type != CustomerType.Household),
            OwesMoney = rows.Count(r => r.Owed > 0),
            GoneQuiet = rows.Count(r => r.QuietDays > CustomerConsts.QuietAfterDays),
        };

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLowerInvariant();
            var key = PhoneKeyOf(input.Filter);
            rows = rows.Where(r => r.Name.ToLowerInvariant().Contains(f)
                || (r.Phone ?? "").ToLowerInvariant().Contains(f)
                || (key != null && PhoneKeyOf(r.Phone) == key)
                || (r.Email ?? "").ToLowerInvariant().Contains(f)
                || (r.CompanyName ?? "").ToLowerInvariant().Contains(f)
                || (r.Where ?? "").ToLowerInvariant().Contains(f)
                || r.Tags.Any(t => t.ToLowerInvariant().Contains(f))).ToList();
        }
        if (input.Type is { } type) rows = rows.Where(r => r.Type == type).ToList();
        if (input.Status is { } status) rows = rows.Where(r => r.Status == status).ToList();
        if (!string.IsNullOrWhiteSpace(input.City))
            rows = rows.Where(r => (r.Where ?? "").Contains(input.City.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (input.OwesMoney) rows = rows.Where(r => r.Owed > 0).ToList();
        if (input.GoneQuiet) rows = rows.Where(r => r.QuietDays > CustomerConsts.QuietAfterDays).ToList();

        rows = (input.Sorting ?? "").Trim().ToLowerInvariant() switch
        {
            "name" => rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            "spent" => rows.OrderByDescending(r => r.TotalSpent).ToList(),
            "owed" => rows.OrderByDescending(r => r.Owed).ToList(),
            "orders" => rows.OrderByDescending(r => r.Orders).ToList(),
            "oldest" => rows.OrderBy(r => r.LastOrderAt ?? DateTime.MaxValue).ToList(),
            _ => rows.OrderByDescending(r => r.LastOrderAt ?? DateTime.MinValue).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        return new CustomersPageDto
        {
            TotalCount = rows.Count, Counts = counts,
            Items = rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList(),
        };
    }

    // ══ ONE CUSTOMER ═════════════════════════════════════════════════════════

    public async Task<CustomerDto> GetAsync(int id)
    {
        var c = await _customers.GetAsync(id);
        var dto = MapDetail(c);

        var orders = (await _orders.GetListAsync(o => o.CustomerId == id
            || (o.CustomerId == null && c.PhoneKey != null && o.CustomerPhone != null)))
            .Where(o => o.CustomerId == id || PhoneKeyOf(o.CustomerPhone) == c.PhoneKey)
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToList();

        var orderIds = orders.Select(o => o.Id).ToList();
        var items = orderIds.Count == 0 ? new List<OrderItem>() : await _orderItems.GetListAsync(i => orderIds.Contains(i.OrderId));

        dto.Orders = orders.Take(20).Select(o => new CustomerOrderDto
        {
            Id = o.Id, Number = o.OrderNumber ?? $"#{o.Id}", Date = o.OrderDate, Status = o.Status.ToString(),
            Items = string.Join(", ", items.Where(i => i.OrderId == o.Id).Select(i => i.ProductName).Where(n => n != null).Take(3)),
            Total = (decimal)o.GrandTotal, Due = (decimal)o.BalanceDue, Loose = o.CustomerId == null,
        }).ToList();
        dto.LooseOrders = orders.Count(o => o.CustomerId == null);

        var live = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        dto.OrderCount = live.Count;
        dto.TotalSpent = Math.Round((decimal)live.Sum(o => o.GrandTotal), 2);
        dto.Owed = Math.Round((decimal)live.Sum(o => o.BalanceDue), 2);
        dto.AverageOrder = live.Count == 0 ? 0 : Math.Round(dto.TotalSpent / live.Count, 2);
        dto.FirstOrderAt = live.Count == 0 ? null : live.Min(o => o.OrderDate);
        dto.LastOrderAt = live.Count == 0 ? null : live.Max(o => o.OrderDate);
        dto.QuietDays = QuietDays(dto.LastOrderAt);
        dto.OverCreditBy = c.CreditLimit > 0 && dto.Owed > c.CreditLimit ? Math.Round(dto.Owed - c.CreditLimit, 2) : 0;

        var invoices = (await _invoices.GetListAsync(i => i.CustomerId == id
            || (i.CustomerId == null && c.PhoneKey != null && i.CustomerPhone != null)))
            .Where(i => i.CustomerId == id || PhoneKeyOf(i.CustomerPhone) == c.PhoneKey)
            .OrderByDescending(i => i.InvoiceDate).Take(20).ToList();
        dto.Invoices = invoices.Select(i => new CustomerInvoiceDto
        {
            Id = i.Id, Number = i.InvoiceNumber ?? $"#{i.Id}", Date = i.InvoiceDate, DueDate = i.DueDate,
            Total = (decimal)i.GrandTotal, Paid = (decimal)i.AmountPaid, Due = (decimal)i.BalanceDue,
            OverdueDays = i.BalanceDue > 0 && i.DueDate is { } due && due.Date < Clock.Now.Date
                ? (int)(Clock.Now.Date - due.Date).TotalDays : null,
        }).ToList();

        if (!string.IsNullOrWhiteSpace(c.PhoneKey) || !string.IsNullOrWhiteSpace(c.Email))
        {
            var quotes = (await _quotes.GetListAsync())
                .Where(q => (c.PhoneKey != null && PhoneKeyOf(q.Phone) == c.PhoneKey)
                    || (c.Email != null && q.Email != null && string.Equals(q.Email, c.Email, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(q => q.CreationTime).Take(10).ToList();
            dto.Quotes = quotes.Select(q => new CustomerQuoteDto
            {
                Id = q.Id, Date = q.CreationTime, Status = q.Status.ToString(), Interest = q.Interest, Location = q.Location,
            }).ToList();
        }
        return dto;
    }

    private CustomerDto MapDetail(Customer c) => new()
    {
        Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Type = c.Type, Status = c.Status, Source = c.Source,
        CompanyName = c.CompanyName, TaxId = c.TaxId, Address = c.Address, Area = c.Area, City = c.City, District = c.District,
        AssignedTo = c.AssignedTo, Note = c.Note, Tags = SplitTags(c.Tags), CreditLimit = c.CreditLimit,
        PaymentTermDays = c.PaymentTermDays, FirstSeen = c.FirstSeen, CreatedAt = c.CreationTime,
    };

    // ══ WRITE ════════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Customers.Create)]
    public async Task<CustomerDto> CreateAsync(CreateUpdateCustomerDto input)
    {
        var key = PhoneKeyOf(input.Phone);
        if (key != null && await _customers.AnyAsync(c => c.PhoneKey == key))
            throw new UserFriendlyException("A customer with that phone number already exists. Open them instead of adding a second record.");

        var c = new Customer { FirstSeen = Clock.Now };
        Apply(c, input);
        await _customers.InsertAsync(c, autoSave: true);
        if (input.LinkMatchingOrders) await LinkMatchingAsync(c.Id);
        return await GetAsync(c.Id);
    }

    [Authorize(DymoEnergyPermissions.Customers.Edit)]
    public async Task<CustomerDto> UpdateAsync(int id, CreateUpdateCustomerDto input)
    {
        var c = await _customers.GetAsync(id);
        var key = PhoneKeyOf(input.Phone);
        if (key != null && await _customers.AnyAsync(x => x.PhoneKey == key && x.Id != id))
            throw new UserFriendlyException("Another customer already has that phone number.");

        Apply(c, input);
        await _customers.UpdateAsync(c, autoSave: true);
        if (input.LinkMatchingOrders) await LinkMatchingAsync(id);
        return await GetAsync(id);
    }

    private static void Apply(Customer c, CreateUpdateCustomerDto i)
    {
        c.Name = i.Name.Trim();
        c.Phone = Clean(i.Phone);
        c.PhoneKey = PhoneKeyOf(i.Phone);
        c.Email = Clean(i.Email)?.ToLowerInvariant();
        c.Type = i.Type; c.Status = i.Status; c.Source = i.Source;
        c.CompanyName = i.Type == CustomerType.Household ? null : Clean(i.CompanyName);
        c.TaxId = Clean(i.TaxId);
        c.Address = Clean(i.Address); c.Area = Clean(i.Area); c.City = Clean(i.City); c.District = Clean(i.District);
        c.AssignedTo = Clean(i.AssignedTo); c.Note = Clean(i.Note);
        c.Tags = string.Join(", ", SplitTags(i.Tags));
        if (string.IsNullOrEmpty(c.Tags)) c.Tags = null;
        c.CreditLimit = Math.Round(i.CreditLimit, 2);
        c.PaymentTermDays = i.PaymentTermDays;
    }

    [Authorize(DymoEnergyPermissions.Customers.Delete)]
    public async Task DeleteAsync(int id)
    {
        // The orders stay; they simply stop pointing at a customer.
        var orders = await _orders.GetListAsync(o => o.CustomerId == id);
        foreach (var o in orders) o.CustomerId = null;
        if (orders.Count > 0) await _orders.UpdateManyAsync(orders, autoSave: true);

        var invoices = await _invoices.GetListAsync(i => i.CustomerId == id);
        foreach (var i in invoices) i.CustomerId = null;
        if (invoices.Count > 0) await _invoices.UpdateManyAsync(invoices, autoSave: true);

        await _customers.DeleteAsync(id, autoSave: true);
    }

    // ══ LINKING PAST WORK ════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Customers.Edit)]
    public async Task<LinkOrdersResultDto> LinkMatchingAsync(int id)
    {
        var c = await _customers.GetAsync(id);
        var result = new LinkOrdersResultDto();
        if (c.PhoneKey == null) return result;

        var orders = (await _orders.GetListAsync(o => o.CustomerId == null && o.CustomerPhone != null))
            .Where(o => PhoneKeyOf(o.CustomerPhone) == c.PhoneKey).ToList();
        foreach (var o in orders) o.CustomerId = id;
        if (orders.Count > 0) await _orders.UpdateManyAsync(orders, autoSave: true);
        result.Orders = orders.Count;

        var invoices = (await _invoices.GetListAsync(i => i.CustomerId == null && i.CustomerPhone != null))
            .Where(i => PhoneKeyOf(i.CustomerPhone) == c.PhoneKey).ToList();
        foreach (var i in invoices) i.CustomerId = id;
        if (invoices.Count > 0) await _invoices.UpdateManyAsync(invoices, autoSave: true);
        result.Invoices = invoices.Count;

        var earliest = orders.Select(o => o.OrderDate).DefaultIfEmpty(DateTime.MaxValue).Min();
        if (earliest != DateTime.MaxValue && (c.FirstSeen == null || earliest < c.FirstSeen))
        {
            c.FirstSeen = earliest;
            await _customers.UpdateAsync(c, autoSave: true);
        }
        return result;
    }

    /// <summary>
    /// Builds a customer for every phone number that appears on an order but has no customer yet. The name,
    /// address and city come from that customer's most recent order, so the record starts from the best
    /// information we already hold.
    /// </summary>
    [Authorize(DymoEnergyPermissions.Customers.Create)]
    public async Task<ImportCustomersResultDto> ImportFromOrdersAsync()
    {
        var loose = (await _orders.GetListAsync(o => o.CustomerId == null && o.CustomerPhone != null))
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToList();
        var known = (await _customers.GetListAsync(c => c.PhoneKey != null)).Select(c => c.PhoneKey!).ToHashSet();

        var result = new ImportCustomersResultDto();
        var groups = loose.GroupBy(o => PhoneKeyOf(o.CustomerPhone)).Where(g => g.Key != null).ToList();
        result.Skipped = loose.Count(o => PhoneKeyOf(o.CustomerPhone) == null);

        foreach (var group in groups)
        {
            if (known.Contains(group.Key!)) continue;      // already a customer; LinkMatching handles those
            var newest = group.First();
            var customer = new Customer
            {
                Name = Clean(newest.CustomerName) ?? newest.CustomerPhone!,
                Phone = Clean(newest.CustomerPhone), PhoneKey = group.Key,
                Email = Clean(newest.CustomerEmail)?.ToLowerInvariant(),
                Type = CustomerType.Household, Status = CustomerStatus.Active, Source = CustomerSource.PastOrders,
                Address = Clean(newest.DeliveryAddress) ?? Clean(newest.BillingAddress),
                FirstSeen = group.Min(o => o.OrderDate),
                Note = "Made from past orders. Check the name and address before using it on a new invoice.",
            };
            await _customers.InsertAsync(customer, autoSave: true);
            known.Add(group.Key!);
            result.Created++;

            var linked = await LinkMatchingAsync(customer.Id);
            result.OrdersLinked += linked.Orders;
            result.InvoicesLinked += linked.Invoices;
        }

        result.Message = result.Created == 0
            ? "No new customers to make — every order with a usable phone number already has one."
            : $"Made {result.Created} {(result.Created == 1 ? "customer" : "customers")} and attached {result.OrdersLinked} {(result.OrdersLinked == 1 ? "order" : "orders")}."
              + (result.Skipped > 0 ? $" {result.Skipped} {(result.Skipped == 1 ? "order has" : "orders have")} no usable phone number and were left alone." : "");
        return result;
    }
}
