using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Category
{
    public class PatchProductCategoryDto
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(150)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public int? ParentId { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsActive { get; set; }

        public IFormFile? ImgFile { get; set; }
    }
}