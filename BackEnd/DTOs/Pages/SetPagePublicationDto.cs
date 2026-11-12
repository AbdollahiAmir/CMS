using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Pages
{
    public class SetPagePublicationDto
    {
        public bool IsPublished { get; set; }

        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;
    }
}