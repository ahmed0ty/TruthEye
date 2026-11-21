using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class StudentAnswerDto
    {
        [Range(1, int.MaxValue)]
        public int QuestionId { get; set; }
        [StringLength(5000)]
        public string AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
    }
}
