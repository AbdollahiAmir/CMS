using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Brands
{
    public class PatchBrandDto
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(150)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public IFormFile? LogoFile { get; set; }
    }
}