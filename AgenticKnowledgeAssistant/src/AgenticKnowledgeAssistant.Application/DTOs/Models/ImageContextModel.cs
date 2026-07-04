namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class ImageContextModel
{
    public Guid ImageId { get; set; } = Guid.NewGuid();
    public Guid SessionGuid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string OcrText { get; set; } = string.Empty;
    public IReadOnlyList<ImageEntityModel> Entities { get; set; } = Array.Empty<ImageEntityModel>();
    public IReadOnlyList<ImageTableModel> Tables { get; set; } = Array.Empty<ImageTableModel>();
    public IReadOnlyList<string> Lines { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ImageEntityModel
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string BoundingBox { get; set; } = string.Empty;
}

public sealed class ImageTableModel
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, string>>();
}
