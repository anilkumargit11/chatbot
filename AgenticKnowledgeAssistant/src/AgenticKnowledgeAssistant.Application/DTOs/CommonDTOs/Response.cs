using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AgenticKnowledgeAssistant.DTO.CommonDTOs;

public sealed class Response<T>
{
    public int ReturnCode { get; set; }
    public string ReturnMessage { get; set; } = string.Empty;
    public string ResponseTime { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    public object? Headers { get; set; }

    [JsonPropertyName("success")]
    public bool Success => ReturnCode is >= 200 and <= 299;

    [JsonPropertyName("message")]
    public string Message => string.IsNullOrWhiteSpace(ReturnMessage)
        ? (Success ? "Request completed successfully" : "Request failed")
        : ReturnMessage;

    [JsonPropertyName("totalCount")]
    public int TotalCount => CountDataItems(Data);

    private static int CountDataItems(object? data)
    {
        if (data is null)
        {
            return 0;
        }

        if (data is string)
        {
            return 1;
        }

        var structuredData = data.GetType().GetProperty("StructuredData", BindingFlags.Instance | BindingFlags.Public)?.GetValue(data);
        var structuredTotalCount = structuredData?.GetType().GetProperty("TotalCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(structuredData);
        if (structuredTotalCount is int totalCount)
        {
            return totalCount;
        }

        if (data is IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return count;
        }

        return 1;
    }
}
