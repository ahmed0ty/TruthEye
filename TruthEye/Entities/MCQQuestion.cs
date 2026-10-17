namespace TruthEye.Entities
{
    public class MCQQuestion : Question
    {
        public virtual required ICollection<MCQOption> Options { get; set; } = new List<MCQOption>();
    }
}