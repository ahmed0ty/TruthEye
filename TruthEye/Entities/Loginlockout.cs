using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TruthEye.Entities
{
    // Failed-login counter per identifier (email or national ID). Stored in the database,
    // so it survives restarts and is shared by every instance of the API.
    [Index(nameof(Key), IsUnique = true)]
    public class LoginLockout
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        public int Failures { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}