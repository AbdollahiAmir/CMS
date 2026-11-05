using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        [DataType(DataType.Password)]
        [MaxLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}