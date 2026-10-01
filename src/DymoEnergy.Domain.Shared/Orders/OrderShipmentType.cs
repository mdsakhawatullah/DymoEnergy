namespace DymoEnergy.Orders;

public enum OrderShipmentType
{
    Standard      = 1,
    Express       = 2,
    Overnight     = 3,
    Pickup        = 4,
    LocalDelivery = 5,
    Freight       = 6,
    /// <summary>Our team delivers and installs.</summary>
    DeliveryAndInstall = 7,
}
