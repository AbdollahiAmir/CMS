using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Tags
{
    public class CreateTagDto
    {
        [Required(ErrorMessage = "نام تگ الزامی است")]
        [MaxLength(100, ErrorMessage = "نام تگ نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }
    }
}