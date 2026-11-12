using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Media
{
    public class UpdateMediaDto
    {
        [MaxLength(250, ErrorMessage = "متن جایگزین نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? AltText { get; set; }
    }
}
