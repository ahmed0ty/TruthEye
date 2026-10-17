using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TruthEye.Entities
{
    [Index(nameof(TokenHash), IsUnique = true)]
    [Index(nameof(UserId))]
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}