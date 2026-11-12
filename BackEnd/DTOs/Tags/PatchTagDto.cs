using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Tags
{
    public class PatchTagDto
    {
        [MaxLength(100, ErrorMessage = "نام تگ نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد")]
        public string? Name { get; set; }

        [MaxLength(150, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }
    }
}