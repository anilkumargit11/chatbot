namespace AgenticKnowledgeAssistant.Common.Extensions;

public static class StringExtensions
{
    public static string ToPreview(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}
