using TruthEye.DTOs;

namespace TruthEye.Entities
{
    public class NextQuestionRequest
    {
        public int CurrentQuestionId { get; set; }
        public StudentAnswerDto Answer { get; set; } = new();
    }
}
