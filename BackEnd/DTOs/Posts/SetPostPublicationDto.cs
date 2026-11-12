using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Posts
{
    public class SetPostPublicationDto
    {
        public bool IsPublished { get; set; }

        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;
    }
}