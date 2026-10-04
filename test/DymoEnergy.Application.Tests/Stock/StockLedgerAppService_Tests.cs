using System;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Products;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Stock;

public abstract class StockLedgerAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IStockLedgerAppService _ledger;
    private readonly IStockEntryAppService _stock;

    protected StockLedgerAppService_Tests()
    {
        _ledger = GetRequiredService<IStockLedgerAppService>();
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

    private async Task<(int Ctg, int Dhk)> WarehousesAsync()
    {
        var w = (await _stock.GetOverviewAsync()).Warehouses;
        return (w.Single(x => x.IsDefault).Id, w.Single(x => !x.IsDefault).Id);
    }

    private async Task<int> ReceiveAsync(int warehouse, int product, int qty, decimal cost)
    {
        var draft = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.StockIn, Date = DateTime.Today, WarehouseId = warehouse, InvoiceNumber = "INV-1",
            Lines = new() { new SaveStockEntryLineDto { ProductId = product, Quantity = qty, UnitCost = cost } },
        });
        return (await _stock.PostAsync(draft.Id)).Id;
    }

    [Fact]
    public async Task Should_Write_A_Sealed_Line_For_Every_Movement()
    {
        var (ctg, dhk) = await WarehousesAsync();
        var panel = await AddProductAsync("Mono PERC Solar Panel 550W", "PNL-MONO-550", 0);

        await ReceiveAsync(ctg, panel, 20, 18500);

        var page = await _ledger.GetLinesAsync(new GetLedgerLinesInput());
        var received = page.Items.Single();
        received.Movement.ShouldBe(LedgerMovement.Received);
        received.Change.ShouldBe(20);
        received.QuantityBefore.ShouldBe(0);
        received.QuantityAfter.ShouldBe(20);
        received.ProductName.ShouldBe("Mono PERC Solar Panel 550W");
        received.DocumentNumber.ShouldNotBeNullOrWhiteSpace();

        // A transfer moves stock twice, so it writes two lines at the same moment.
        var move = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.Transfer, Date = DateTime.Today, WarehouseId = ctg, ToWarehouseId = dhk, Reference = "For the Dhaka showroom",
            Lines = new() { new SaveStockEntryLineDto { ProductId = panel, Quantity = 6 } },
        });
        await _stock.PostAsync(move.Id);

        var after = await _ledger.GetLinesAsync(new GetLedgerLinesInput());
        after.TotalCount.ShouldBe(3);
        after.Items.Count(l => l.Movement == LedgerMovement.TransferredOut).ShouldBe(1);
        var into = after.Items.Single(l => l.Movement == LedgerMovement.TransferredIn);
        into.Change.ShouldBe(6);
        into.QuantityBefore.ShouldBe(0);
        into.QuantityAfter.ShouldBe(6);

        var detail = await _ledger.GetLineAsync(received.Id);
        detail.SealOk.ShouldBeTrue();
        detail.PreviousHash.ShouldBe(LedgerConsts.GenesisHash);   // the very first line
        detail.ValueBefore.ShouldBe(0);
        detail.ValueAfter.ShouldBe(20 * 18500);
        detail.Changes.ShouldContain(c => c.Field == "Quantity on hand" && c.Became == "20");
        detail.CameFrom.ShouldBe("Stock entry screen → Post");

        var header = await _ledger.GetHeaderAsync();
        header.TotalLines.ShouldBe(3);
        header.LinesToday.ShouldBe(3);
        header.InUnits.ShouldBe(20);
    }

    [Fact]
    public async Task Should_Say_The_Chain_Is_Sound_And_Notice_When_A_Line_Is_Changed()
    {
        var (ctg, _) = await WarehousesAsync();
        var battery = await AddProductAsync("Tubular Battery 200Ah", "BAT-TUB-200", 0);
        await ReceiveAsync(ctg, battery, 14, 49000);
        await ReceiveAsync(ctg, battery, 2, 49000);

        var good = await _ledger.VerifyChainAsync();
        good.Ok.ShouldBeTrue();
        good.LinesChecked.ShouldBe(2);
        good.Message.ShouldContain("seals match");

        // Someone edits an old quantity straight in the database.
        var tamperedId = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var repo = GetRequiredService<IRepository<StockLedgerLine, int>>();
            var line = (await repo.GetListAsync()).OrderBy(l => l.Id).First();
            line.QuantityAfter = 99;
            await repo.UpdateAsync(line, autoSave: true);
            tamperedId = line.Id;
        });

        var bad = await _ledger.VerifyChainAsync();
        bad.Ok.ShouldBeFalse();
        bad.FirstBadLineId.ShouldBe(tamperedId);
        bad.Message.ShouldContain("does not match its seal");
        (await _ledger.GetLineAsync(tamperedId)).SealOk.ShouldBeFalse();

        var proof = await _ledger.GetProofAsync();
        proof.LastCheck!.Ok.ShouldBeFalse();
        proof.TotalLines.ShouldBe(2);
        proof.Seals.ShouldContain(s => s.IsFirst);
    }

    [Fact]
    public async Task Should_Flag_A_Hand_Correction_With_No_Reason_And_Let_Someone_Sign_It_Off()
    {
        var (ctg, _) = await WarehousesAsync();
        var inverter = await AddProductAsync("IPS Inverter 500W", "INV-IPS-500", 0);
        await ReceiveAsync(ctg, inverter, 10, 9000);

        // A count correction with nothing typed in and no photo.
        var count = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.Adjustment, Date = DateTime.Today, WarehouseId = ctg,
            Lines = new() { new SaveStockEntryLineDto { ProductId = inverter, CountedQuantity = 8 } },
        });
        await _stock.PostAsync(count.Id);

        var needs = await _ledger.GetNeedsLookAsync();
        needs.OpenCount.ShouldBeGreaterThan(0);
        var flagged = needs.Lines.First();
        flagged.Line.Movement.ShouldBe(LedgerMovement.CountCorrection);
        flagged.Line.FlagLabels.ShouldContain("No reason typed");
        flagged.Line.FlagLabels.ShouldContain("No photo attached");
        needs.Correctors.ShouldContain(c => c.Count == 1);

        var before = needs.OpenCount;
        var reviewed = await _ledger.ReviewLineAsync(flagged.Line.Id, new ReviewLedgerLineDto { Action = "checked", Note = "Two were swollen, moved to the damaged rack." });
        reviewed.Reviews.Single().Note.ShouldBe("Two were swollen, moved to the damaged rack.");
        reviewed.Line.Reviewed.ShouldBeTrue();
        (await _ledger.GetNeedsLookAsync()).OpenCount.ShouldBe(before - 1);

        // Signing it off never rewrites the line itself.
        (await _ledger.VerifyChainAsync()).Ok.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Show_One_Products_Story_And_Keep_Settings()
    {
        var (ctg, _) = await WarehousesAsync();
        var cable = await AddProductAsync("DC Cable 4mm 100m", "CBL-DC-4", 0);
        await ReceiveAsync(ctg, cable, 30, 1200);

        var sold = await _stock.CreateAsync(new SaveStockEntryDto
        {
            Type = StockEntryType.StockOut, Date = DateTime.Today, WarehouseId = ctg, OutReason = StockOutReason.Sold, Reference = "INV-2026-0007",
            Lines = new() { new SaveStockEntryLineDto { ProductId = cable, Quantity = 4 } },
        });
        await _stock.PostAsync(sold.Id);

        (await _ledger.GetProductOptionsAsync()).ShouldContain(p => p.Id == cable);
        var story = await _ledger.GetProductAsync(cable, 30);
        story.InStockNow.ShouldBe(26);
        story.MovesIn.ShouldBe(1);
        story.MovesOut.ShouldBe(1);
        story.StockValue.ShouldBe(26 * 1200);
        story.AverageCost.ShouldBe(1200);
        story.Receipts.Single().Quantity.ShouldBe(30);
        story.Balance.Last().Quantity.ShouldBe(26);
        story.Lines.First().Movement.ShouldBe(LedgerMovement.Sold);

        var setting = (await _ledger.GetNeedsLookAsync()).Setting;
        setting.FlagNoPhoto.ShouldBeTrue();
        setting.LargeValueOver.ShouldBe(20000);
        setting.FlagNoPhoto = false;
        setting.LargeValueOver = 50000;
        setting.KeepYears = 7;
        var saved = await _ledger.UpdateSettingAsync(setting);
        saved.FlagNoPhoto.ShouldBeFalse();
        saved.LargeValueOver.ShouldBe(50000);
        (await _ledger.GetProofAsync()).Setting.KeepYears.ShouldBe(7);
    }

    [Fact]
    public async Task Should_Write_An_Opening_Line_For_Stock_That_Predates_The_Ledger()
    {
        // Stock that was on the shelf before the ledger existed, with no entry behind it.
        var panel = await AddProductAsync("Panel 450W", "PNL-450", 42);

        var header = await _ledger.GetHeaderAsync();
        header.TotalLines.ShouldBe(1);

        var opening = (await _ledger.GetLinesAsync(new GetLedgerLinesInput())).Items.Single();
        opening.Movement.ShouldBe(LedgerMovement.Opening);
        opening.QuantityBefore.ShouldBe(0);
        opening.QuantityAfter.ShouldBe(42);
        opening.ProductName.ShouldBe("Panel 450W");

        var detail = await _ledger.GetLineAsync(opening.Id);
        detail.Reason.ShouldContain("on the shelf when the ledger started");
        detail.SealOk.ShouldBeTrue();

        // Opening the page again must not write it a second time.
        await _ledger.GetHeaderAsync();
        (await _ledger.GetLinesAsync(new GetLedgerLinesInput())).TotalCount.ShouldBe(1);

        // The next real movement carries on from the opening quantity.
        var (ctg, _) = await WarehousesAsync();
        await ReceiveAsync(ctg, panel, 8, 17000);
        var received = (await _ledger.GetLinesAsync(new GetLedgerLinesInput())).Items.First();
        received.QuantityBefore.ShouldBe(42);
        received.QuantityAfter.ShouldBe(50);
        (await _ledger.VerifyChainAsync()).Ok.ShouldBeTrue();
    }
}
