using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class StartExamSessionDto
    {
        [Required]
        public int ExamId { get; set; }
        public int StudentId { get; set; }
    }
}
