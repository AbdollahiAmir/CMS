using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Posts
{
    public class CreatePostDto
    {
        [Required(ErrorMessage = "عنوان مقاله الزامی است")]
        [MaxLength(250, ErrorMessage = "عنوان مقاله نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }

        [MaxLength(500, ErrorMessage = "خلاصه مقاله نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? Excerpt { get; set; }

        public string Content { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "عنوان SEO نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? SeoTitle { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات متا نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? MetaDescription { get; set; }

        public int? CategoryId { get; set; }

        public List<int> TagIds { get; set; } = [];
    }
}