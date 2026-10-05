namespace DymoEnergy.Customers;

public enum CustomerType
{
    Household = 1,
    Business = 2,
    /// <summary>Buys to resell, usually on terms.</summary>
    Dealer = 3,
    /// <summary>School, mosque, NGO, government office.</summary>
    Institution = 4,
}

public enum CustomerStatus
{
    Active = 1,
    /// <summary>Has not bought for a long time, or asked to be left alone.</summary>
    Inactive = 2,
    /// <summary>Do not sell on terms: bad debt or a dispute.</summary>
    Blocked = 3,
}

/// <summary>How this customer first reached us.</summary>
public enum CustomerSource
{
    Storefront = 1,
    Showroom = 2,
    QuoteRequest = 3,
    Referral = 4,
    FieldSale = 5,
    /// <summary>Built from orders that were taken before customers were kept.</summary>
    PastOrders = 6,
    Other = 7,
}

public static class CustomerConsts
{
    public const int MaxName = 256;
    public const int MaxShort = 128;
    public const int MaxText = 2000;
    /// <summary>A customer who has not bought within this many days counts as gone quiet.</summary>
    public const int QuietAfterDays = 180;
}
