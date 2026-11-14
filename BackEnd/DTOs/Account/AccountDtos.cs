using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Account
{
    public class ProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
    }

    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }
    }

    public class ChangeEmailDto
    {
        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public class RequestPhoneCodeDto
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class VerifyPhoneDto
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "کد تایید الزامی است")]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
    }
}
