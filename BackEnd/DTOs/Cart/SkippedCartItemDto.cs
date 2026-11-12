namespace BackEnd.DTOs.Cart
{
    public class SkippedCartItemDto
    {
        public int ProductId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int? AvailableQuantity { get; set; }
    }
}