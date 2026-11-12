using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Menus
{
    public class MenuListItemDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class MenuDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public List<MenuItemDto> Items { get; set; } = new();
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool OpenInNewTab { get; set; }
        public List<MenuItemDto> Children { get; set; } = new();
    }

    public class CreateMenuDto
    {
        [Required(ErrorMessage = "کلید منو الزامی است")]
        [MaxLength(50, ErrorMessage = "کلید منو نمی‌تواند بیشتر از ۵۰ کاراکتر باشد")]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "کلید منو فقط می‌تواند شامل حروف کوچک انگلیسی، عدد و خط تیره باشد")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام منو الزامی است")]
        [MaxLength(100, ErrorMessage = "نام منو نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;
    }

    public class PatchMenuDto
    {
        [MaxLength(100, ErrorMessage = "نام منو نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد")]
        public string? Name { get; set; }
    }

    public class SaveMenuItemsDto
    {
        [Required(ErrorMessage = "لیست آیتم‌ها الزامی است")]
        public List<MenuItemInputDto> Items { get; set; } = new();
    }

    public class MenuItemInputDto
    {
        [Required(ErrorMessage = "عنوان آیتم منو الزامی است")]
        [MaxLength(150, ErrorMessage = "عنوان آیتم منو نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "آدرس آیتم منو الزامی است")]
        [MaxLength(500, ErrorMessage = "آدرس آیتم منو نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string Url { get; set; } = string.Empty;

        public bool OpenInNewTab { get; set; }

        public List<MenuItemInputDto>? Children { get; set; }
    }
}
