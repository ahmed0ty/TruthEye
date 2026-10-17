namespace TruthEye.DTOs
{
    /// <summary>
    /// Data transfer object for Proctor registration and responses
    /// </summary>
    public class ProctorDto: UserDto
    {
        public string Department { get; set; } = string.Empty;
        public bool IsOnDuty { get; set; }
        public int ActiveSessionsCount { get; set; }
        public DateTime? LastDutyDate { get; set; }
        public double AverageResponseTime { get; set; }
    }
}
