using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Cart
{
    public class MergeCartItemDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }
}