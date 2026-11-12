using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Cart
{
    public class MergeCartDto
    {
        [MaxLength(50)]
        public List<MergeCartItemDto> Items { get; set; } = new();
    }
}