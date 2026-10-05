using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Customers;

// ── List ──────────────────────────────────────────────────────────────────

public class GetCustomersInput
{
    [MaxLength(128)] public string? Filter { get; set; }
    public CustomerType?   Type   { get; set; }
    public CustomerStatus? Status { get; set; }
    [MaxLength(128)] public string? City { get; set; }
    /// <summary>Only customers with money still owed on an invoice.</summary>
    public bool OwesMoney { get; set; }
    /// <summary>Only customers who have not bought for a long time.</summary>
    public bool GoneQuiet { get; set; }
    [MaxLength(32)] public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    [Range(1, 500)] public int MaxResultCount { get; set; } = 12;
}

public class CustomerListItemDto
{
    public int       Id          { get; set; }
    public string    Name        { get; set; } = string.Empty;
    public string?   Phone       { get; set; }
    public string?   Email       { get; set; }
    public CustomerType   Type   { get; set; }
    public CustomerStatus Status { get; set; }
    public string?   CompanyName { get; set; }
    public string?   Where       { get; set; }
    public string?   AssignedTo  { get; set; }
    public List<string> Tags     { get; set; } = new();

    public int       Orders      { get; set; }
    public decimal   TotalSpent  { get; set; }
    public decimal   Owed        { get; set; }
    public DateTime? LastOrderAt { get; set; }
    /// <summary>Days since the last order; null when they have never ordered.</summary>
    public int?      QuietDays   { get; set; }
}

public class CustomerCountsDto
{
    public int All        { get; set; }
    public int Households { get; set; }
    public int Businesses { get; set; }
    public int OwesMoney  { get; set; }
    public int GoneQuiet  { get; set; }
}

public class CustomersPageDto
{
    public long TotalCount { get; set; }
    public List<CustomerListItemDto> Items { get; set; } = new();
    public CustomerCountsDto Counts { get; set; } = new();
}

// ── Overview ──────────────────────────────────────────────────────────────

public class CustomerOverviewDto
{
    public int     Total          { get; set; }
    public int     NewThisMonth   { get; set; }
    public int     BuyingThisMonth { get; set; }
    public decimal OwedTotal      { get; set; }
    public int     OwedCount      { get; set; }
    public decimal SoldThisMonth  { get; set; }
    public int     RepeatCount    { get; set; }
    public int     RepeatPercent  { get; set; }
    public int     QuietCount     { get; set; }
    /// <summary>Orders that still have no customer behind them.</summary>
    public int     UnlinkedOrders { get; set; }
    /// <summary>How many customers those unlinked orders would make.</summary>
    public int     WouldCreate    { get; set; }
    public List<string> Cities    { get; set; } = new();
}

// ── One customer ──────────────────────────────────────────────────────────

public class CustomerOrderDto
{
    public int       Id      { get; set; }
    public string    Number  { get; set; } = string.Empty;
    public DateTime  Date    { get; set; }
    public string    Status  { get; set; } = string.Empty;
    public string?   Items   { get; set; }
    public decimal   Total   { get; set; }
    public decimal   Due     { get; set; }
    /// <summary>True when the order was matched by phone rather than linked outright.</summary>
    public bool      Loose   { get; set; }
}

public class CustomerInvoiceDto
{
    public int       Id      { get; set; }
    public string    Number  { get; set; } = string.Empty;
    public DateTime  Date    { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal   Total   { get; set; }
    public decimal   Paid    { get; set; }
    public decimal   Due     { get; set; }
    public int?      OverdueDays { get; set; }
}

public class CustomerQuoteDto
{
    public int      Id       { get; set; }
    public DateTime Date     { get; set; }
    public string   Status   { get; set; } = string.Empty;
    public string?  Interest { get; set; }
    public string?  Location { get; set; }
}

public class CustomerDto
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string?  Phone     { get; set; }
    public string?  Email     { get; set; }
    public CustomerType   Type   { get; set; }
    public CustomerStatus Status { get; set; }
    public CustomerSource Source { get; set; }
    public string?  CompanyName { get; set; }
    public string?  TaxId     { get; set; }
    public string?  Address   { get; set; }
    public string?  Area      { get; set; }
    public string?  City      { get; set; }
    public string?  District  { get; set; }
    public string?  AssignedTo { get; set; }
    public string?  Note      { get; set; }
    public List<string> Tags  { get; set; } = new();
    public decimal  CreditLimit { get; set; }
    public int      PaymentTermDays { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime  CreatedAt { get; set; }

    // Rolled up from what they actually bought
    public int       OrderCount   { get; set; }
    public decimal   TotalSpent   { get; set; }
    public decimal   Owed         { get; set; }
    public decimal   AverageOrder { get; set; }
    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastOrderAt  { get; set; }
    public int?      QuietDays    { get; set; }
    public decimal   OverCreditBy { get; set; }

    public List<CustomerOrderDto>   Orders   { get; set; } = new();
    public List<CustomerInvoiceDto> Invoices { get; set; } = new();
    public List<CustomerQuoteDto>   Quotes   { get; set; } = new();
    /// <summary>Orders that match this customer's phone but are not linked to them yet.</summary>
    public int LooseOrders { get; set; }
}

public class CreateUpdateCustomerDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(32)]  public string? Phone { get; set; }
    [MaxLength(256)] public string? Email { get; set; }
    public CustomerType   Type   { get; set; } = CustomerType.Household;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public CustomerSource Source { get; set; } = CustomerSource.Showroom;
    [MaxLength(256)] public string? CompanyName { get; set; }
    [MaxLength(64)]  public string? TaxId   { get; set; }
    [MaxLength(512)] public string? Address { get; set; }
    [MaxLength(128)] public string? Area    { get; set; }
    [MaxLength(128)] public string? City    { get; set; }
    [MaxLength(128)] public string? District { get; set; }
    [MaxLength(256)] public string? AssignedTo { get; set; }
    [MaxLength(2000)] public string? Note  { get; set; }
    [MaxLength(512)]  public string? Tags  { get; set; }
    [Range(0, 100000000)] public decimal CreditLimit { get; set; }
    [Range(0, 365)] public int PaymentTermDays { get; set; }
    /// <summary>Attach past orders that carry the same phone number.</summary>
    public bool LinkMatchingOrders { get; set; } = true;
}

// ── Building customers from past orders ───────────────────────────────────

public class ImportCustomersResultDto
{
    public int Created { get; set; }
    public int OrdersLinked { get; set; }
    public int InvoicesLinked { get; set; }
    public int Skipped { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LinkOrdersResultDto
{
    public int Orders   { get; set; }
    public int Invoices { get; set; }
}
