using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [MaxLength(256)]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ComparePassword { get; set; } = string.Empty;
    }
}