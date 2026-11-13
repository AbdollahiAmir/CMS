using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.ProductDTOs
{
    public class ProductImageDto
    {
        public int Id { get; set; }
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class UploadProductImageDto
    {
        [Required(ErrorMessage = "تصویر الزامی است")]
        public IFormFile Image { get; set; } = null!;

        [MaxLength(250)]
        public string? AltText { get; set; }
    }

    public class UpdateProductImageDto
    {
        [MaxLength(250)]
        public string? AltText { get; set; }
    }

    public class ReorderProductImagesDto
    {
        [Required(ErrorMessage = "فهرست تصاویر الزامی است")]
        public List<int> ImageIds { get; set; } = new();
    }
}
