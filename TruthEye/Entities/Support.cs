namespace TruthEye.Entities
{
    public class Support : User
    {
        /// <summary>
        /// Data transfer object for Proctor registration and responses
        /// </summary>
        public string Department { get; set; }
        public string Specialization { get; set; }
        public bool IsOnline { get; set; }
        public int TicketsHandled { get; set; }
        public int OpenTickets { get; set; }
        public DateTime? LastTicketHandled { get; set; }
        public double AverageResolutionTime { get; set; }
        public string ShiftTiming { get; set; }


        public virtual ICollection<SupportTicket> SupportTickets { get; set; }
    }
}
