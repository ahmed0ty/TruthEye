using System.ComponentModel.DataAnnotations;

namespace TruthEye.Entities
{
    /// <summary>رابط إعادة تعيين الباسورد. بنخزن الـ hash بس، والرمز الأصلي بيتبعت للبريد.</summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public int RequestedBy { get; set; }
    }
}
