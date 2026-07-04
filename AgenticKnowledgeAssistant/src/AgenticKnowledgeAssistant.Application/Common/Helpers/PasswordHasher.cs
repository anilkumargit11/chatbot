namespace AgenticKnowledgeAssistant.Security.Encryption;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }
}
