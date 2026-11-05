using BackEnd.DTOs.Common;
using BackEnd.DTOs.Orders;

namespace BackEnd.IServices
{
    public interface IOrderService
    {
        Task<OrderDto> CreateAsync(string userId, CreateOrderDto dto);
        Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(string userId, OrderQueryDto query);
        Task<OrderDto?> GetMyOrderAsync(string userId, int id);
        Task<OrderDto?> CancelMyOrderAsync(string userId, int id);

        Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryDto query);
        Task<OrderDto?> GetByIdAsync(int id);
        Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
    }
}