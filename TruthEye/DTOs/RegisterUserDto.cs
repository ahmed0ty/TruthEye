using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        // Required unless SendSetupLink is true (checked by PrepareInitialPassword in the service)
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be 8 to 128 characters")]
        public string? Password { get; set; }

        /// When true, the account gets a random password nobody sees and the user receives an email link to set their own.
        public bool SendSetupLink { get; set; }

        [Required(ErrorMessage = "National ID is required")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be 14 characters")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }
    }
}
