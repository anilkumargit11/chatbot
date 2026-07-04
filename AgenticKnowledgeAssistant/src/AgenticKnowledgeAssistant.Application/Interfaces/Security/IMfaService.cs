namespace AgenticKnowledgeAssistant.Security.Authentication;

public interface IMfaService
{
    string GenerateAuthenticatorSecret();
    string GetQrCodeUri(string email, string secret);
    bool VerifyTotp(string secret, string code);
    string GenerateOtp();
    bool VerifyOtp(string actualCode, string expectedCode);
    string[] GenerateBackupCodes();
}
