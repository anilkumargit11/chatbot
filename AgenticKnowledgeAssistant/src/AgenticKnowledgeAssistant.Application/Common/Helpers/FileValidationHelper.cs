namespace AgenticKnowledgeAssistant.Common.Helpers;

public static class FileValidationHelper
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".txt"
    };

    public static bool IsSupportedDocument(string fileName)
    {
        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }
}
