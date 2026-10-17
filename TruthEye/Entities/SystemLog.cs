namespace TruthEye.Entities
{
    public class SystemLog
    {
        public int Id { get; set; }
        public int? AdminId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }

    //    public virtual Admin Admin { get; set; }
    }
}
