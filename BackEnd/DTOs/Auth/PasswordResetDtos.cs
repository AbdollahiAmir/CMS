using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ConfirmEmailDto
    {
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string Token { get; set; } = string.Empty;
    }
}
