using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Posts
{
    public class SchedulePostDto
    {
        public DateTimeOffset? ScheduledAt { get; set; }

        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;
    }
}
