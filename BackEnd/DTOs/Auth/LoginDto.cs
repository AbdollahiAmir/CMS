using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [MaxLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}