using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class TwoFactorLoginDto
    {
        [Required(ErrorMessage = "Sign-in step is missing")]
        public string ChallengeToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(16, ErrorMessage = "Verification code is too long")]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorCodeDto
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(16, ErrorMessage = "Verification code is too long")]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorDisableDto
    {
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(16, ErrorMessage = "Verification code is too long")]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>السر بيتعرض مرة واحدة في الإعداد عشان يتدخل يدوي أو يتحوّل لـ QR.</summary>
    public sealed record TwoFactorSetupDto(string Secret, string OtpAuthUri);
}
