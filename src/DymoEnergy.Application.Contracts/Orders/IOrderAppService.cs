using System.Collections.Generic;
using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Orders;

public interface IOrderAppService : IApplicationService
{
    Task<OrderDto>                       GetAsync(int id);
    Task<DymoPagedResultDto<OrderDto>>   GetListDataAsync(OrderFilterDto input);
    Task<OrderDto>                       CreateOrderDataAsync(CreateUpdateOrderDto input);
    Task<OrderDto>                       UpdateAsync(int id, CreateUpdateOrderDto input);
    Task                                 DeleteAsync(int id);
    Task<List<OrderItemDto>>             GetOrderItemsAsync(int id);
    Task<OrderDto>                       UpdateStatusAsync(int id, OrderStatus status);
    Task<OrderDto>                       UpdateStageAsync(int id, OrderStage stage);
}
