using BackEnd.Models;
using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Orders
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus? Status { get; set; }

        [MaxLength(100)]
        public string? TrackingCode { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}