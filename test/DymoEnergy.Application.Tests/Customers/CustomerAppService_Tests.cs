using System;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Orders;
using DymoEnergy.SalesInvoices;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Customers;

public abstract class CustomerAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ICustomerAppService _customers;

    protected CustomerAppService_Tests()
    {
        _customers = GetRequiredService<ICustomerAppService>();
    }

    private async Task<int> AddOrderAsync(string number, string? name, string? phone, double total, double due, DateTime? date = null)
    {
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var o = await GetRequiredService<IRepository<Order, int>>().InsertAsync(new Order
            {
                OrderNumber = number, OrderDate = date ?? DateTime.Today, Status = OrderStatus.Confirmed,
                CustomerName = name, CustomerPhone = phone, DeliveryAddress = "House 12, Agrabad, Chattogram",
                GrandTotal = total, BalanceDue = due,
            }, autoSave: true);
            id = o.Id;
        });
        return id;
    }

    private async Task AddInvoiceAsync(string number, string phone, double total, double due)
    {
        await WithUnitOfWorkAsync(async () =>
            await GetRequiredService<IRepository<SalesInvoice, int>>().InsertAsync(new SalesInvoice
            {
                InvoiceNumber = number, InvoiceDate = DateTime.Today, DueDate = DateTime.Today.AddDays(-5),
                CustomerPhone = phone, GrandTotal = total, AmountPaid = total - due, BalanceDue = due,
            }, autoSave: true));
    }

    [Theory]
    [InlineData("+880 1712-345678", "01712345678")]
    [InlineData("01712345678", "01712345678")]
    [InlineData("8801712345678", "01712345678")]
    [InlineData("1712345678", "01712345678")]
    [InlineData("12345", null)]
    [InlineData(null, null)]
    public void Should_Read_The_Same_Phone_Written_Different_Ways(string? raw, string? expected) =>
        CustomerAppService.PhoneKeyOf(raw).ShouldBe(expected);

    [Fact]
    public async Task Should_Pick_Up_Past_Orders_When_A_Customer_Is_Added()
    {
        // Orders taken before customers were kept: the same person, phone written two ways.
        await AddOrderAsync("ORD-1", "Md. Sakhawat Ullah", "01812345678", 25000, 0, DateTime.Today.AddDays(-40));
        await AddOrderAsync("ORD-2", "Sakhawat", "+880 1812-345678", 15000, 4000, DateTime.Today.AddDays(-5));
        await AddOrderAsync("ORD-3", "Someone Else", "01999999999", 9000, 0);
        await AddInvoiceAsync("INV-1", "01812345678", 15000, 4000);

        var created = await _customers.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Md. Sakhawat Ullah", Phone = "01812345678", City = "Chattogram", Area = "Agrabad",
            Type = CustomerType.Household, Tags = "solar, repeat",
        });

        created.OrderCount.ShouldBe(2);
        created.TotalSpent.ShouldBe(40000);
        created.Owed.ShouldBe(4000);
        created.AverageOrder.ShouldBe(20000);
        created.LooseOrders.ShouldBe(0);                     // both were attached outright
        created.FirstOrderAt!.Value.Date.ShouldBe(DateTime.Today.AddDays(-40));
        created.Tags.ShouldBe(new[] { "solar", "repeat" });
        created.Invoices.Single().OverdueDays.ShouldBe(5);

        // The other person's order was left alone.
        var list = await _customers.GetListAsync(new GetCustomersInput());
        list.Items.Single().Orders.ShouldBe(2);

        // The same phone cannot be added twice.
        await Should.ThrowAsync<UserFriendlyException>(() => _customers.CreateAsync(
            new CreateUpdateCustomerDto { Name = "Duplicate", Phone = "+8801812345678" }));
    }

    [Fact]
    public async Task Should_Build_Customers_Out_Of_Past_Orders()
    {
        await AddOrderAsync("ORD-A", "Rafiq Islam", "01712345678", 12000, 0, DateTime.Today.AddDays(-200));
        await AddOrderAsync("ORD-B", "Rafiq Islam", "01712345678", 8000, 2000, DateTime.Today.AddDays(-3));
        await AddOrderAsync("ORD-C", "Nadia Akter", "01911111111", 30000, 0);
        await AddOrderAsync("ORD-D", "Walk-in", null, 500, 0);          // nothing to match on
        await AddOrderAsync("ORD-E", "Bad number", "12345", 700, 0);    // not a usable phone

        var before = await _customers.GetOverviewAsync();
        before.UnlinkedOrders.ShouldBe(3);
        before.WouldCreate.ShouldBe(2);

        var result = await _customers.ImportFromOrdersAsync();
        result.Created.ShouldBe(2);
        result.OrdersLinked.ShouldBe(3);
        result.Skipped.ShouldBe(1);                                      // the unusable number
        result.Message.ShouldContain("Made 2 customers");

        var list = await _customers.GetListAsync(new GetCustomersInput { Sorting = "spent" });
        var rafiq = list.Items.Single(c => c.Name == "Rafiq Islam");
        rafiq.Orders.ShouldBe(2);
        rafiq.TotalSpent.ShouldBe(20000);
        rafiq.Owed.ShouldBe(2000);

        // The newest order gives the name, so the record starts from the best we hold.
        var detail = await _customers.GetAsync(rafiq.Id);
        detail.Source.ShouldBe(CustomerSource.PastOrders);
        detail.FirstSeen!.Value.Date.ShouldBe(DateTime.Today.AddDays(-200));

        // Running it again makes nothing new.
        var again = await _customers.ImportFromOrdersAsync();
        again.Created.ShouldBe(0);
        again.Message.ShouldContain("No new customers");
        (await _customers.GetListAsync(new GetCustomersInput())).TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Filter_Count_And_Keep_Orders_When_A_Customer_Goes()
    {
        await AddOrderAsync("ORD-Q", "Quiet Buyer", "01712000001", 5000, 0, DateTime.Today.AddDays(-300));
        await AddOrderAsync("ORD-R", "Owing Shop", "01712000002", 60000, 15000, DateTime.Today.AddDays(-2));

        var quiet = await _customers.CreateAsync(new CreateUpdateCustomerDto { Name = "Quiet Buyer", Phone = "01712000001", City = "Dhaka" });
        var shop = await _customers.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Owing Shop", Phone = "01712000002", Type = CustomerType.Dealer, CompanyName = "Owing Shop Ltd",
            City = "Chattogram", CreditLimit = 10000,
        });

        shop.OverCreditBy.ShouldBe(5000);                 // owes 15,000 against a 10,000 limit
        shop.CompanyName.ShouldBe("Owing Shop Ltd");

        var page = await _customers.GetListAsync(new GetCustomersInput());
        page.Counts.All.ShouldBe(2);
        page.Counts.Households.ShouldBe(1);
        page.Counts.Businesses.ShouldBe(1);
        page.Counts.OwesMoney.ShouldBe(1);
        page.Counts.GoneQuiet.ShouldBe(1);

        (await _customers.GetListAsync(new GetCustomersInput { OwesMoney = true })).Items.Single().Name.ShouldBe("Owing Shop");
        (await _customers.GetListAsync(new GetCustomersInput { GoneQuiet = true })).Items.Single().Name.ShouldBe("Quiet Buyer");
        (await _customers.GetListAsync(new GetCustomersInput { City = "chattogram" })).Items.Single().Name.ShouldBe("Owing Shop");
        (await _customers.GetListAsync(new GetCustomersInput { Filter = "+880 1712-000002" })).Items.Single().Name.ShouldBe("Owing Shop");

        var overview = await _customers.GetOverviewAsync();
        overview.Total.ShouldBe(2);
        overview.OwedTotal.ShouldBe(15000);
        overview.QuietCount.ShouldBe(1);

        // Deleting a customer must not take their orders with them.
        await _customers.DeleteAsync(quiet.Id);
        (await _customers.GetListAsync(new GetCustomersInput())).TotalCount.ShouldBe(1);
        await WithUnitOfWorkAsync(async () =>
        {
            var orders = await GetRequiredService<IRepository<Order, int>>().GetListAsync();
            orders.Count.ShouldBe(2);
            orders.Single(o => o.OrderNumber == "ORD-Q").CustomerId.ShouldBeNull();
        });
    }
}
