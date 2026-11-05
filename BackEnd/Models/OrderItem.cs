using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public int? ProductId { get; set; }

        public Product? Product { get; set; }

        [Required, MaxLength(250)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Sku { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}