namespace TruthEye.DTOs
{
    public class SupportTicketDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Resolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string UserName { get; set; }
        public string AssignedToName { get; set; }
    }
}
