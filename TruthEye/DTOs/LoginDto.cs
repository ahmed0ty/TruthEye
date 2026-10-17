using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    /// <summary>
    /// Login request data transfer object
    /// </summary>
    public class LoginDto
    {

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be 14 characters")]
        public string? NationalId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }
    }
}
