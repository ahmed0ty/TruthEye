namespace TruthEye.Entities
{
    public class SuperAdmin: User
    {
        public DateTime? LastSystemBackup { get; set; }
        public int TotalActionsPerformed { get; set; }

        public virtual ICollection<AIConfig> ConfiguredAISettings { get; set; }
        public virtual ICollection<SystemLog> SystemLogs { get; set; }
    }
}
