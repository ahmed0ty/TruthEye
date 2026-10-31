namespace TruthEye.Entities
{
    public class SystemLog
    {
        public int Id { get; set; }

        /// <summary>مين نفّذ العملية (أي دور، مش الأدمن بس).</summary>
        public int? AdminId { get; set; }
        public int? FacultyId { get; set; }
        public string? ActorRole { get; set; }

        public string Action { get; set; }        // Create / Update / Disable / Delete ...
        public string EntityType { get; set; }    // Student / Doctor / Proctor / Staff / Course ...
        public int? EntityId { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }

        /// <summary>JSON: الحالة قبل وبعد، وهي اللي بيتبني عليها زرار التراجع.</summary>
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public bool IsReverted { get; set; }
        public int? RevertedBy { get; set; }
        public DateTime? RevertedAt { get; set; }

        public DateTime Timestamp { get; set; }
    }
}