namespace TruthEye.DTOs
{
    public class UpdateProctorDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
