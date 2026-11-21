using System.Data;

namespace TruthEye.Entities
{
    public class User: BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string NationalId { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImage { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TotpSecretProtected { get; set; }   // سر TOTP مشفّر (AES-GCM)
        public long? TotpLastStep { get; set; }            // آخر step اتصرف، عشان الكود ما يتعادش
        public string? RecoveryCodeHashes { get; set; }    // SHA256 لأكواد الاسترجاع، مفصولة بفاصلة
        public int RoleId { get; set; }
        public virtual Role Role { get; set; }
        public virtual ICollection<ExamSession> ExamSessions { get; set; }
        public virtual ICollection<ChatMessage> ChatMessages { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<ChatMessage> SentMessages { get; set; }
        public virtual ICollection<ChatMessage> ReceivedMessages { get; set; }
        
    }
}
