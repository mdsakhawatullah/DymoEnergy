namespace DymoEnergy.Orders;

public enum OrderStage
{
    New          = 1,
    Confirmed    = 2,
    Processing   = 3,
    ReadyToShip  = 4,
    Dispatched   = 5,
    InTransit    = 6,
    Delivered    = 7,
    Completed    = 8,
    Closed       = 9,
}
