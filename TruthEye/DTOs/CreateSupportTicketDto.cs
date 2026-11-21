using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class CreateSupportTicketDto
    {
        [Required, StringLength(50)]
        public string Category { get; set; }
        [Required, StringLength(20)]
        public string Priority { get; set; }
        [Required, StringLength(200)]
        public string Subject { get; set; }
        [Required, StringLength(4000)]
        public string Description { get; set; }
    }
}
