namespace TruthEye.Entities
{
    public class UserSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Sid { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string? Ip { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}