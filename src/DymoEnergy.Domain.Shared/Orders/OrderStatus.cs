namespace DymoEnergy.Orders;

public enum OrderStatus
{
    Pending    = 1,
    Confirmed  = 2,
    Processing = 3,
    OnHold     = 4,
    Shipped    = 5,
    Delivered  = 6,
    Cancelled  = 7,
    Refunded   = 8,
    Returned   = 9,
}
