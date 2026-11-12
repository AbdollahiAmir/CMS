using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.PostCategories
{
    public class CreatePostCategoryDto
    {
        [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
        [MaxLength(150, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "اسلاگ نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string? Slug { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? Description { get; set; }

        [MaxLength(250, ErrorMessage = "عنوان سئو نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? SeoTitle { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات متا نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? MetaDescription { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }
    }
}