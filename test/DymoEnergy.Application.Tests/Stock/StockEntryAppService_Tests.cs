using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Products;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Stock;

public abstract class StockEntryAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IStockEntryAppService _stock;

    protected StockEntryAppService_Tests()
    {
        _stock = GetRequiredService<IStockEntryAppService>();
    }

    private async Task<int> AddProductAsync(string name, string sku, int stock)
    {
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var p = await GetRequiredService<IRepository<Product, int>>()
                .InsertAsync(new Product { Name = name, Sku = sku, StockQuantity = stock, IsActive = true, Price = 100 }, autoSave: true);
            id = p.Id;
        });
        return id;
    }

    private async Task<int> StockOfAsync(int productId)
    {
        var q = 0;
        await WithUnitOfWorkAsync(async () => q = (await GetRequiredService<IRepository<Product, int>>().GetAsync(productId)).StockQuantity);
        return q;
    }

    private async Task<(int Ctg, int Dhk)> WarehousesAsync()
    {
        var w = (await _stock.GetOverviewAsync()).Warehouses;
        return (w.Single(x => x.IsDefault).Id, w.Single(x => !x.IsDefault).Id);
    }

    private static SaveStockEntryDto In(int warehouseId, int? supplierId, decimal transport, params (int Product, int Qty, decimal Cost, string[] Serials)[] lines) => new()
    {
        Type = StockEntryType.StockIn, Date = DateTime.Today, WarehouseId = warehouseId, SupplierId = supplierId, InvoiceNumber = "INV-7790",
        TransportCost = transport,
        Lines = lines.Select(l => new SaveStockEntryLineDto { ProductId = l.Product, Quantity = l.Qty, UnitCost = l.Cost, Serials = l.Serials.ToList() }).ToList(),
    };

    [Fact]
    public async Task Should_Receive_Stock_With_Landed_Cost_And_Opening_Balances()
    {
        var (ctg, _) = await WarehousesAsync();
        var panel = await AddProductAsync("Mono PERC Solar Panel 550W", "SP-550W-001", 120);
        var ctrl = await AddProductAsync("PWM Charge Controller 10A", "CS10A", 6);
        var supplier = await _stock.CreateSupplierAsync(new CreateUpdateStockSupplierDto { Name = "[Supplier A] Trading" });

        (await _stock.GetNextNumberAsync()).ShouldBe($"SE-{DateTime.Now.Year}-0001");
        var draft = await _stock.CreateAsync(In(ctg, supplier.Id, 3500, (panel, 100, 14200, new[] { "PN-1", "PN-2" }), (ctrl, 30, 800, Array.Empty<string>())));
        draft.Status.ShouldBe(StockEntryStatus.Draft);
        draft.Lines.Single(l => l.ProductId == panel).InStockNow.ShouldBe(120);   // opening stock lands in the default warehouse
        draft.Total.ShouldBe(1_420_000 + 24_000 + 3500);
        (await StockOfAsync(panel)).ShouldBe(120);                                  // drafts do not touch stock

        var posted = await _stock.PostAsync(draft.Id);
        posted.Status.ShouldBe(StockEntryStatus.Posted);
        var line = posted.Lines.Single(l => l.ProductId == panel);
        line.StockBefore.ShouldBe(120);
        line.StockAfter.ShouldBe(220);
        // 3500 transport split by value: the panels are 1,420,000 of 1,444,000.
        line.LandedUnitCost.ShouldBe(Math.Round(14200m + 3500m * 1_420_000m / 1_444_000m / 100m, 2));
        line.TracksSerials.ShouldBeTrue();
        (await StockOfAsync(panel)).ShouldBe(220);
        (await StockOfAsync(ctrl)).ShouldBe(36);

        await Should.ThrowAsync<UserFriendlyException>(() => _stock.UpdateAsync(draft.Id, In(ctg, null, 0, (panel, 1, 1, Array.Empty<string>()))));
        await Should.ThrowAsync<UserFriendlyException>(() => _stock.DeleteAsync(draft.Id));

        var overview = await _stock.GetOverviewAsync();
        overview.ReceivedUnits.ShouldBe(130);
        overview.ReceivedEntries.ShouldBe(1);
        overview.Restock.ShouldNotContain(r => r.ProductId == panel);

        var page = await _stock.GetEntriesAsync(new GetStockEntriesInput { Filter = "solar panel" });
        page.Items.Single().Title.ShouldBe("[Supplier A] Trading");
        page.Items.Single().Units.ShouldBe(130);
        page.Counts.StockIn.ShouldBe(1);

        // The same serial cannot come in twice while it is on the shelf.
        var again = await _stock.CreateAsync(In(ctg, null, 0, (panel, 1, 14200, new[] { "PN-1" })));
        await Should.ThrowAsync<UserFriendlyException>(() => _stock.PostAsync(again.Id));
        (await StockOfAsync(panel)).ShouldBe(220);
    }

    [Fact]
    public async Task Should_Refuse_To_Take_Out_More_Than_There_Is_And_Move_Between_Warehouses()
    {
        var (ctg, dhk) = await WarehousesAsync();
        var battery = await AddProductAsync("Lithium Battery 12V 100Ah", "LB-100AH-12V", 0);
        await _stock.PostAsync((await _stock.CreateAsync(In(ctg, null, 0, (battery, 10, 24500, new[] { "B1", "B2", "B3" })))).Id);

        var tooMany = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.StockOut, Date = DateTime.Today, WarehouseId = ctg, OutReason = StockOutReason.Installed,
            Lines = new() { new SaveStockEntryLineDto { ProductId = battery, Quantity = 11 } },
        });
        await Should.ThrowAsync<UserFriendlyException>(() => _stock.PostAsync(tooMany.Id));

        var move = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.Transfer, Date = DateTime.Today, WarehouseId = ctg, ToWarehouseId = dhk, Reference = "For the Dhaka showroom",
            Lines = new() { new SaveStockEntryLineDto { ProductId = battery, Quantity = 4, Serials = new() { "B1" } } },
        });
        var moved = await _stock.PostAsync(move.Id);
        moved.Lines.Single().StockAfter.ShouldBe(6);
        moved.Lines.Single().ToStockAfter.ShouldBe(4);
        (await StockOfAsync(battery)).ShouldBe(10);   // total unchanged
        (await _stock.GetProductsAsync(new GetStockProductsInput { Ids = new() { battery }, WarehouseId = ctg })).Single().InStock.ShouldBe(6);

        // B1 is now in Dhaka, so it cannot leave from Chattogram.
        var wrongSerial = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.StockOut, Date = DateTime.Today, WarehouseId = ctg, OutReason = StockOutReason.Sold,
            Lines = new() { new SaveStockEntryLineDto { ProductId = battery, Quantity = 1, Serials = new() { "B1" } } },
        });
        var why = await Should.ThrowAsync<UserFriendlyException>(() => _stock.PostAsync(wrongSerial.Id));
        (await StockOfAsync(battery)).ShouldBe(10);   // a refused entry changes nothing
        (await _stock.GetProductsAsync(new GetStockProductsInput { Ids = new() { battery }, WarehouseId = ctg })).Single().InStock.ShouldBe(6);

        var sold = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.StockOut, Date = DateTime.Today, WarehouseId = ctg, OutReason = StockOutReason.Sold,
            Lines = new() { new SaveStockEntryLineDto { ProductId = battery, Quantity = 2, Serials = new() { "B2" } } },
        });
        var soldPosted = await _stock.PostAsync(sold.Id);
        soldPosted.Lines.Single().UnitCost.ShouldBe(24500);
        soldPosted.Lines.Single().LineTotal.ShouldBe(49000);
        (await StockOfAsync(battery)).ShouldBe(8);

        var overview = await _stock.GetOverviewAsync();
        overview.OutUnits.ShouldBe(2);
        overview.OutParts.Single().Label.ShouldBe("sold");
        overview.StockValue.ShouldBe(8 * 24500);
    }

    [Fact]
    public async Task Should_Fix_Stock_After_A_Count_And_Reverse_Entries()
    {
        var (ctg, _) = await WarehousesAsync();
        var inverter = await AddProductAsync("Inverter 300W", "INV-300", 8);

        var count = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.Adjustment, Date = DateTime.Today, WarehouseId = ctg, Reference = "monthly count",
            Lines = new() { new SaveStockEntryLineDto { ProductId = inverter, CountedQuantity = 6 } },
        });
        (await _stock.GetEntriesAsync(new GetStockEntriesInput { Drafts = true })).Items.Single().Units.ShouldBe(-2);
        var counted = await _stock.PostAsync(count.Id);
        counted.Lines.Single().Change.ShouldBe(-2);
        (await StockOfAsync(inverter)).ShouldBe(6);

        var received = await _stock.PostAsync((await _stock.CreateAsync(In(ctg, null, 0, (inverter, 5, 9000, new[] { "I-1" })))).Id);
        (await StockOfAsync(inverter)).ShouldBe(11);

        var reversal = await _stock.ReverseAsync(received.Id);
        reversal.ReversesNumber.ShouldBe(received.Number);
        reversal.Lines.Single().Change.ShouldBe(-5);
        (await StockOfAsync(inverter)).ShouldBe(6);
        (await _stock.GetAsync(received.Id)).Status.ShouldBe(StockEntryStatus.Reversed);
        (await _stock.GetProductsAsync(new GetStockProductsInput { Ids = new() { inverter } })).Single().TracksSerials.ShouldBeFalse();
        await Should.ThrowAsync<UserFriendlyException>(() => _stock.ReverseAsync(received.Id));

        // Undoing the count puts the two units back.
        await _stock.ReverseAsync(counted.Id);
        (await StockOfAsync(inverter)).ShouldBe(8);
    }

    [Fact]
    public async Task Should_Book_Product_Page_Stock_Changes_As_An_Adjustment()
    {
        var (ctg, _) = await WarehousesAsync();
        var panel = await AddProductAsync("Panel 400W", "SP-400", 3);
        await _stock.PostAsync((await _stock.CreateAsync(In(ctg, null, 0, (panel, 2, 10000, Array.Empty<string>())))).Id);

        var products = GetRequiredService<IProductAppService>();
        var dto = await products.GetAsync(panel);
        await products.UpdateAsync(panel, new CreateUpdateProductDto { Name = dto.Name, Sku = dto.Sku, Price = dto.Price, StockQuantity = 9, IsActive = true });

        (await StockOfAsync(panel)).ShouldBe(9);
        var adj = (await _stock.GetEntriesAsync(new GetStockEntriesInput { Type = StockEntryType.Adjustment })).Items.Single();
        adj.Units.ShouldBe(4);
        adj.Status.ShouldBe(StockEntryStatus.Posted);
    }
}
