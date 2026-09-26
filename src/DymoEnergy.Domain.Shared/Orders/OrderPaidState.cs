namespace DymoEnergy.Orders;

/// <summary>How much of an order's grand total has been settled.</summary>
public enum OrderPaidState
{
    /// <summary>Nothing outstanding.</summary>
    PaidInFull = 1,

    /// <summary>Some money received, a balance still due.</summary>
    PartlyPaid = 2,

    /// <summary>Nothing received yet.</summary>
    Unpaid = 3,
}
