namespace TruthEye.DTOs
{
    /// <summary>
    /// Data transfer object for Doctor registration and responses
    /// </summary>
    public class SupportDto : UserDto
    {
        public string Department { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int TicketsHandled { get; set; }
        public int OpenTickets { get; set; }
        public DateTime? LastTicketHandled { get; set; }
        public double AverageResolutionTime { get; set; }
        public string ShiftTiming { get; set; } = string.Empty;
    }
}
