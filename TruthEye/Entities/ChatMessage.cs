namespace TruthEye.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; } = false;
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}
