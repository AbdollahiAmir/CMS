using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(40)]
        public string OrderNumber { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public AppUser User { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal TotalAmount { get; set; }

        [Required, MaxLength(150)]
        public string RecipientName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(500)]
        public string? CustomerNote { get; set; }

        [MaxLength(100)]
        public string? TrackingCode { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}