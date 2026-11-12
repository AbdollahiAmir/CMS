namespace BackEnd.DTOs.ProductDTOs
{
    public class CartLookupItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal UnitPrice { get; set; }
        public int AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public bool HasImage { get; set; }
    }
}