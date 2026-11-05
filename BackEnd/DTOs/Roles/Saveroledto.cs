using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Roles
{
    public class SaveRoleDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public List<string> Permissions { get; set; } = new();
    }
}