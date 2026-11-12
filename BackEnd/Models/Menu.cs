using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackEnd.Models
{
    public class Menu
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Key { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
    }
}
