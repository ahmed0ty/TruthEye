using TruthEye.Exceptions;

namespace TruthEye.Security
{
    public static class PasswordPolicy
    {
        public static void Enforce(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                throw new BadRequestException("Password must be at least 8 characters");
            if (password.Length > 128)
                throw new BadRequestException("Password is too long");
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                throw new BadRequestException("Password must contain both letters and numbers");
        }
    }
}