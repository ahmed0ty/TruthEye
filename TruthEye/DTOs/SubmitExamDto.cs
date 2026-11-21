using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class SubmitExamDto
    {
        [Range(1, int.MaxValue)]
        public int ExamSessionId { get; set; }
        [Required] 
        public List<StudentAnswerDto> Answers { get; set; }
    }
}
