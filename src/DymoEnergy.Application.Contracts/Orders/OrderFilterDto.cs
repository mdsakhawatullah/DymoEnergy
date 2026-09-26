using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Orders;

public class OrderFilterDto : PagedAndSortedResultRequestDto
{
    public string?           Filter       { get; set; }
    public OrderStatus?      Status       { get; set; }
    public OrderStage?       Stage        { get; set; }
    public OrderPriority?    Priority     { get; set; }
    public OrderPaymentType? PaymentType  { get; set; }
    public OrderPaidState?   PaidState    { get; set; }
    public int?              CustomerId   { get; set; }
    public int?              PortalId     { get; set; }
    public DateTime?         DateFrom     { get; set; }
    public DateTime?         DateTo       { get; set; }
}
