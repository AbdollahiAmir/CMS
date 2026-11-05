using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Brands
{
    public class CreateBrandDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IFormFile? LogoFile { get; set; }
    }
}