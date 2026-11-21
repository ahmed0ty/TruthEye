using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class StartExamSessionDto
    {
        [Range(1, int.MaxValue)]
        public int ExamId { get; set; }
        public int StudentId { get; set; }
    }
}
