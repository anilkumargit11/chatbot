using System.Security.Cryptography;
using System.Text;

namespace AgenticKnowledgeAssistant.Security.Authentication;

public sealed class MfaService : IMfaService
{
    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public string GenerateAuthenticatorSecret()
    {
        var bytes = new byte[20];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return ToBase32String(bytes);
    }

    public string GetQrCodeUri(string email, string secret)
    {
        var issuer = Uri.EscapeDataString("AgenticKnowledgeAssistant");
        var account = Uri.EscapeDataString(email);
        return $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            return false;
        }

        try
        {
            var key = FromBase32String(secret);
            var timestamp = DateTime.UtcNow;
            var unixTime = (long)(timestamp - DateTime.UnixEpoch).TotalSeconds;
            var currentStep = unixTime / 30;

            // Allow window of 1 step before/after (30 seconds margin)
            for (var i = -1; i <= 1; i++)
            {
                var stepCode = GenerateTotpForStep(key, currentStep + i);
                if (stepCode == code)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000; // 6-digit code
        return number.ToString();
    }

    public bool VerifyOtp(string actualCode, string expectedCode)
    {
        return !string.IsNullOrWhiteSpace(actualCode) &&
               actualCode.Trim() == expectedCode.Trim();
    }

    public string[] GenerateBackupCodes()
    {
        var codes = new string[8];
        using var rng = RandomNumberGenerator.Create();
        for (var i = 0; i < 8; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = BitConverter.ToUInt32(bytes, 0) % 90000000 + 10000000; // 8-digit backup code
            codes[i] = code.ToString();
        }
        return codes;
    }

    private static string ToBase32String(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        int currentByte = 0, digit = 0;
        
        foreach (var b in data)
        {
            currentByte = (currentByte << 8) | b;
            digit += 8;
            while (digit >= 5)
            {
                digit -= 5;
                sb.Append(Base32Chars[(currentByte >> digit) & 31]);
            }
        }

        if (digit > 0)
        {
            sb.Append(Base32Chars[(currentByte << (5 - digit)) & 31]);
        }

        return sb.ToString();
    }

    private static byte[] FromBase32String(string base32)
    {
        var normalized = base32.ToUpperInvariant().Replace(" ", string.Empty);
        var bytes = new List<byte>();
        int currentByte = 0, bits = 0;

        foreach (var c in normalized)
        {
            var index = Array.IndexOf(Base32Chars, c);
            if (index < 0)
            {
                continue; // Ignore padding or invalid characters
            }

            currentByte = (currentByte << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(currentByte >> bits));
            }
        }

        return bytes.ToArray();
    }

    private static string GenerateTotpForStep(byte[] key, long step)
    {
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(stepBytes);
        }

        // Pad if needed
        var challenge = new byte[8];
        Array.Copy(stepBytes, challenge, stepBytes.Length);

        byte[] hash;
        using (var hmac = new HMACSHA1(key))
        {
            hash = hmac.ComputeHash(challenge);
        }

        var offset = hash[^1] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24)
                   | ((hash[offset + 1] & 0xff) << 16)
                   | ((hash[offset + 2] & 0xff) << 8)
                   | (hash[offset + 3] & 0xff);

        var otp = binary % 1000000;
        return otp.ToString("D6");
    }
}
