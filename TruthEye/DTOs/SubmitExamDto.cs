using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class SubmitExamDto
    {
        [Required]
        public int ExamSessionId { get; set; }
        [Required] 
        public List<StudentAnswerDto> Answers { get; set; }
    }
}
