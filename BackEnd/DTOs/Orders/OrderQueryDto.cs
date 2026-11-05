using BackEnd.Models;

namespace BackEnd.DTOs.Orders
{
    public class OrderQueryDto
    {
        public OrderStatus? Status { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}