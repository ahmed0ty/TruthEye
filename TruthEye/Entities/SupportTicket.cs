namespace TruthEye.Entities
{
    public class SupportTicket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int? AssignedToSupportId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public virtual User User { get; set; }
        public virtual Support AssignedToSupport { get; set; }
    }
}