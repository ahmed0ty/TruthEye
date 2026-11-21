using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class UpdateProctorDto
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
