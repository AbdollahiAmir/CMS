using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackEnd.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        public int MenuId { get; set; }

        [JsonIgnore]
        public Menu Menu { get; set; } = null!;

        public int? ParentId { get; set; }

        [JsonIgnore]
        public MenuItem? Parent { get; set; }

        [JsonIgnore]
        public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool OpenInNewTab { get; set; }
    }
}
