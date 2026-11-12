using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Pages
{
    public class CreateContentPageDto
    {
        [Required(ErrorMessage = "عنوان صفحه الزامی است")]
        [MaxLength(250, ErrorMessage = "عنوان صفحه نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }

        public string Content { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "خلاصه صفحه نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? Excerpt { get; set; }

        [MaxLength(250, ErrorMessage = "عنوان SEO نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? SeoTitle { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات متا نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? MetaDescription { get; set; }
    }
}