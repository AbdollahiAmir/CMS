using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.ProductDTOs
{
    public class ProductAttributeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsFilterable { get; set; }
    }

    public class CreateProductAttributeDto
    {
        [Required(ErrorMessage = "نام ویژگی الزامی است"), MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Slug { get; set; }

        public int SortOrder { get; set; }

        public bool IsFilterable { get; set; }
    }

    public class PatchProductAttributeDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsFilterable { get; set; }
    }

    public class ProductAttributeValueDto
    {
        public int AttributeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ProductAttributeValueInputDto
    {
        public int AttributeId { get; set; }

        [MaxLength(250)]
        public string? Value { get; set; }
    }

    public class SetProductAttributesDto
    {
        [Required]
        public List<ProductAttributeValueInputDto> Items { get; set; } = new();
    }

    public class FilterAttributeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
    }
}
