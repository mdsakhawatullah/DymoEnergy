namespace DymoEnergy.Shipping;

public static class ShippingConsts
{
    public const int MaxName    = 256;
    public const int MaxShort   = 128;
    public const int MaxText    = 2000;
    /// <summary>Encrypted values are longer than the plain text.</summary>
    public const int MaxEncrypted = 4000;
}

/// <summary>Which courier integration an account uses. Only Pathao talks to an API today.</summary>
public enum CourierProvider
{
    Pathao       = 1,
    Steadfast    = 2,
    RedX         = 3,
    ECourier     = 4,
    OwnDelivery  = 5,
    Pickup       = 6,
}

public enum CourierEnvironment
{
    Live    = 1,
    Sandbox = 2,
}

/// <summary>Which editable list a ShippingListItem belongs to.</summary>
public enum ShippingItemKind
{
    BigItem         = 1,
    ReturnPolicy    = 2,
    PackingRule     = 3,
    CustomerMessage = 4,
}
