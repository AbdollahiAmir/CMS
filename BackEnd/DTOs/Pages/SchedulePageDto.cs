using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Pages
{
    public class SchedulePageDto
    {
        public DateTimeOffset? ScheduledAt { get; set; }

        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;
    }
}
