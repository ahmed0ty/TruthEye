using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm new password is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
