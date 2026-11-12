using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Posts
{
    public class PatchPostDto
    {
        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "عنوان مقاله نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? Title { get; set; }

        [MaxLength(250, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }

        [MaxLength(500, ErrorMessage = "خلاصه مقاله نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? Excerpt { get; set; }

        public string? Content { get; set; }

        [MaxLength(250, ErrorMessage = "عنوان SEO نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? SeoTitle { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات متا نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? MetaDescription { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "شناسه دسته‌بندی معتبر نیست")]
        public int? CategoryId { get; set; }

        public bool ClearCategory { get; set; }

        public List<int>? TagIds { get; set; }
    }
}