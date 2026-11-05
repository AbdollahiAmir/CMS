using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Orders
{
    public class CreateOrderDto
    {
        [Required]
        [MaxLength(150)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(500)]
        public string? CustomerNote { get; set; }
    }
}