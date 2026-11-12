using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Media
{
    public class UploadMediaDto
    {
        [Required(ErrorMessage = "تصویر الزامی است")]
        public IFormFile Image { get; set; } = null!;

        [MaxLength(250, ErrorMessage = "متن جایگزین نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? AltText { get; set; }
    }
}
