using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TruthEye.Security
{
    /// <summary>TOTP (RFC 6238): 6 أرقام كل 30 ثانية بـ HMAC-SHA1. متوافق مع Google وMicrosoft Authenticator.</summary>
    public static class TotpCodes
    {
        public const int StepSeconds = 30;
        public const int Digits = 6;
        // بنقبل الكود من الـ step اللي قبله والتالي عشان فرق الساعة بين الموبايل والسيرفر
        private const int Window = 1;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

        public static long StepAt(DateTimeOffset now) => now.ToUnixTimeSeconds() / StepSeconds;

        public static string ComputeCode(byte[] key, long step)
        {
            var counter = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(counter);
            var hash = HMACSHA1.HashData(key, counter);
            int offset = hash[^1] & 0x0F;
            int binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            return (binary % 1_000_000).ToString("D6");
        }

        /// <summary>يرجّع رقم الـ step اللي الكود طابق عليه، أو null. الـ step اللي اتصرف قبل كده ما بيتقبلش تاني.</summary>
        public static long? Match(byte[] key, string code, long? lastStep, long nowStep)
        {
            if (code.Length != Digits || !code.All(c => c is >= '0' and <= '9')) return null;
            var given = Encoding.ASCII.GetBytes(code);
            for (long s = nowStep - Window; s <= nowStep + Window; s++)
            {
                if (lastStep.HasValue && s <= lastStep.Value) continue;
                if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(ComputeCode(key, s)), given))
                    return s;
            }
            return null;
        }

        public static string ToBase32(byte[] data)
        {
            var sb = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = ((buffer << 8) | b) & 0xFFFF;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static string OtpAuthUri(string account, string base32Secret) =>
            $"otpauth://totp/TruthEye:{Uri.EscapeDataString(account)}?secret={base32Secret}&issuer=TruthEye&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    /// <summary>
    /// تشفير سر TOTP (AES-GCM)، توكن التحدي بين خطوتي الدخول، وأكواد الاسترجاع.
    /// المفاتيح مشتقّة من Jwt:Key بفصل بالـ label، فمفيش إعداد جديد. لو Jwt:Key اتغيّر،
    /// أسرار 2FA القديمة مش هتتفك، والعميد أو رئيس الجامعة يقدر يلغي الـ 2FA للحساب.
    /// </summary>
    public sealed class TwoFactorCrypto
    {
        public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
        private const string RecoveryAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private readonly byte[] _encKey;
        private readonly byte[] _macKey;

        public TwoFactorCrypto(string jwtKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(jwtKey);
            var root = Encoding.UTF8.GetBytes(jwtKey);
            _encKey = HMACSHA256.HashData(root, Encoding.UTF8.GetBytes("TruthEye.2FA.encrypt"));
            _macKey = HMACSHA256.HashData(root, Encoding.UTF8.GetBytes("TruthEye.2FA.mac"));
        }

        public string EncryptSecret(byte[] secret)
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[secret.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(_encKey, 16);
            aes.Encrypt(nonce, secret, cipher, tag);

            var all = new byte[nonce.Length + tag.Length + cipher.Length];
            nonce.CopyTo(all, 0);
            tag.CopyTo(all, nonce.Length);
            cipher.CopyTo(all, nonce.Length + tag.Length);
            return Convert.ToBase64String(all);
        }

        public byte[] DecryptSecret(string sealedSecret)
        {
            var all = Convert.FromBase64String(sealedSecret);
            var nonce = all[..12];
            var tag = all[12..28];
            var cipher = all[28..];
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_encKey, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return plain;
        }

        /// <summary>توكن قصير العمر بيقول "الباسورد اتقبل لليوزر ده". مش بيفتح جلسة لوحده.</summary>
        public string IssueChallenge(int userId)
        {
            var exp = DateTimeOffset.UtcNow.Add(ChallengeLifetime).ToUnixTimeSeconds();
            var payload = Encoding.UTF8.GetBytes($"{userId}|{exp}");
            return Base64Url(payload) + "." + Base64Url(HMACSHA256.HashData(_macKey, payload));
        }

        /// <summary>يرجّع الـ userId لو التوكن سليم ولسه في عمره، وإلا null.</summary>
        public int? ReadChallenge(string? token)
        {
            var parts = (token ?? string.Empty).Split('.');
            if (parts.Length != 2) return null;

            byte[] payload, signature;
            try
            {
                payload = FromBase64Url(parts[0]);
                signature = FromBase64Url(parts[1]);
            }
            catch (FormatException)
            {
                return null;
            }

            if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(_macKey, payload), signature)) return null;

            var fields = Encoding.UTF8.GetString(payload).Split('|');
            if (fields.Length != 2 || !int.TryParse(fields[0], out var userId) || !long.TryParse(fields[1], out var exp))
                return null;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return null;
            return userId;
        }

        /// <summary>8 أكواد استرجاع بالشكل XXXX-XXXX (حروف وأرقام من غير اللي بيتلخبطوا زي 0/O و1/I).</summary>
        public static string[] NewRecoveryCodes(int count = 8) =>
            Enumerable.Range(0, count).Select(_ =>
            {
                var c = RandomNumberGenerator.GetItems(RecoveryAlphabet.AsSpan(), 8);
                return $"{new string(c, 0, 4)}-{new string(c, 4, 4)}";
            }).ToArray();

        public static string NormalizeCode(string? code) =>
            new string((code ?? string.Empty).Where(ch => char.IsLetterOrDigit(ch)).ToArray()).ToUpperInvariant();

        public static string HashRecoveryCode(string code) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeCode(code))));

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string s)
        {
            var t = s.Replace('-', '+').Replace('_', '/');
            t = t.PadRight(t.Length + (4 - t.Length % 4) % 4, '=');
            return Convert.FromBase64String(t);
        }
    }
}
