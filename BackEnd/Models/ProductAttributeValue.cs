using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class ProductAttributeValue
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public int AttributeId { get; set; }

        public ProductAttribute Attribute { get; set; } = null!;

        [Required, MaxLength(250)]
        public string Value { get; set; } = string.Empty;
    }
}
