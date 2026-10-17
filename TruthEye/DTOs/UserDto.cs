using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "National ID is required")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be 14 characters")]
        public string NationalId { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string ProfileImage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Role { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;


    }
}
