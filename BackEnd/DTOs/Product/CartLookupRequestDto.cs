using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.ProductDTOs
{
    public class CartLookupRequestDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public List<int> ProductIds { get; set; } = new();
    }
}