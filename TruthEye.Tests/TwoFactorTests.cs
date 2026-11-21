using System.Text;
using TruthEye.Security;

namespace TruthEye.Tests;

public class TwoFactorTests
{
    [Fact]
    public void Totp_matches_the_rfc_6238_sha1_vector()
    {
        // RFC 6238: المفتاح "12345678901234567890" والوقت 59 ثانية (step 1) = 94287082، وآخر 6 أرقام = 287082
        var key = Encoding.ASCII.GetBytes("12345678901234567890");
        Assert.Equal("287082", TotpCodes.ComputeCode(key, 1));
    }

    [Fact]
    public void Totp_accepts_a_code_once_and_never_replays_it()
    {
        var key = TotpCodes.NewSecret();
        long now = 1000;
        var code = TotpCodes.ComputeCode(key, now);

        Assert.Equal(now, TotpCodes.Match(key, code, null, now));
        Assert.Null(TotpCodes.Match(key, code, now, now));   // اتصرف قبل كده
    }

    [Fact]
    public void Secret_round_trips_and_a_tampered_secret_fails()
    {
        var crypto = new TwoFactorCrypto("test-key-test-key-test-key-test-key-1234567890");
        var secret = TotpCodes.NewSecret();
        var encryptedText = crypto.EncryptSecret(secret);
        Assert.Equal(secret, crypto.DecryptSecret(encryptedText));

        var bytes = Convert.FromBase64String(encryptedText);
        bytes[^1] ^= 0x01;
        Assert.ThrowsAny<Exception>(() => crypto.DecryptSecret(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Challenge_reads_back_the_user_and_rejects_forgery()
    {
        var crypto = new TwoFactorCrypto("test-key-test-key-test-key-test-key-1234567890");
        var token = crypto.IssueChallenge(42);
        Assert.Equal(42, crypto.ReadChallenge(token));

        var forged = (token[0] == 'A' ? "B" : "A") + token[1..];
        Assert.Null(crypto.ReadChallenge(forged));
        Assert.Null(crypto.ReadChallenge("garbage"));
    }

    [Fact]
    public void Recovery_codes_are_formatted_and_match_regardless_of_dashes_and_case()
    {
        var codes = TwoFactorCrypto.NewRecoveryCodes();
        Assert.Equal(8, codes.Length);
        Assert.All(codes, c => Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", c));
        Assert.Equal(
            TwoFactorCrypto.HashRecoveryCode(codes[0]),
            TwoFactorCrypto.HashRecoveryCode(codes[0].Replace("-", "").ToLowerInvariant()));
    }
}
