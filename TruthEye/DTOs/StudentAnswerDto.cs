using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class StudentAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
    }
}
