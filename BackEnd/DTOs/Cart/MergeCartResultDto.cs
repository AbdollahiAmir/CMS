namespace BackEnd.DTOs.Cart
{
    public class MergeCartResultDto
    {
        public CartDto Cart { get; set; } = new();

        public List<SkippedCartItemDto> Skipped { get; set; } = new();
    }
}