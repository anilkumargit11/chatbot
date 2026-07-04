using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class ImageContextService : IImageContextService
{
    private readonly ConcurrentDictionary<Guid, List<ImageContextModel>> _sessions = new();

    public Task<ImageContextModel> AddOrUpdateAsync(Guid sessionGuid, string fileName, string contentType, string ocrText, CancellationToken cancellationToken = default)
    {
        var context = new ImageContextModel
        {
            SessionGuid = sessionGuid,
            FileName = fileName,
            ContentType = contentType,
            OcrText = ocrText,
            Lines = SplitLines(ocrText),
            Entities = ExtractEntities(ocrText),
            Tables = ExtractMarkdownTables(ocrText)
        };

        var list = _sessions.GetOrAdd(sessionGuid, _ => new List<ImageContextModel>());
        lock (list)
        {
            list.RemoveAll(item => item.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            list.Add(context);
        }

        return Task.FromResult(context);
    }

    public Task<IReadOnlyList<ImageContextModel>> GetSessionImagesAsync(Guid sessionGuid, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionGuid, out var list))
        {
            return Task.FromResult<IReadOnlyList<ImageContextModel>>(Array.Empty<ImageContextModel>());
        }

        lock (list)
        {
            return Task.FromResult<IReadOnlyList<ImageContextModel>>(list.OrderBy(item => item.CreatedAtUtc).ToArray());
        }
    }

    public Task ClearSessionAsync(Guid sessionGuid, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionGuid, out _);
        return Task.CompletedTask;
    }

    private static IReadOnlyList<string> SplitLines(string text)
    {
        return Regex.Split(text, @"\r?\n")
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static IReadOnlyList<ImageEntityModel> ExtractEntities(string text)
    {
        var entities = new List<ImageEntityModel>();
        foreach (Match match in Regex.Matches(text, @"(?<label>Total|Tax|Discount|Date|Blood Pressure|Patient|Doctor|Physician|Medicine|Diagnosis)\s*[:|]\s*(?<value>[^|\r\n]+)", RegexOptions.IgnoreCase))
        {
            entities.Add(new ImageEntityModel
            {
                Type = match.Groups["label"].Value.Trim(),
                Value = match.Groups["value"].Value.Trim(),
                Location = "OCR text",
                BoundingBox = "Not available"
            });
        }

        foreach (Match amount in Regex.Matches(text, @"[$₹]\s?\d+(?:[,.]\d{2})?"))
        {
            entities.Add(new ImageEntityModel
            {
                Type = "Amount",
                Value = amount.Value,
                Location = "OCR text",
                BoundingBox = "Not available"
            });
        }

        return entities;
    }

    private static IReadOnlyList<ImageTableModel> ExtractMarkdownTables(string text)
    {
        var tables = new List<ImageTableModel>();
        var lines = Regex.Split(text, @"\r?\n").Select(line => line.Trim()).ToArray();

        for (var i = 0; i < lines.Length - 1; i++)
        {
            if (!lines[i].StartsWith('|') || !lines[i + 1].Contains("---", StringComparison.Ordinal))
            {
                continue;
            }

            var headers = SplitTableRow(lines[i]);
            var rows = new List<IReadOnlyDictionary<string, string>>();
            for (var j = i + 2; j < lines.Length && lines[j].StartsWith('|'); j++)
            {
                var cells = SplitTableRow(lines[j]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var c = 0; c < Math.Min(headers.Length, cells.Length); c++)
                {
                    row[headers[c]] = cells[c];
                }

                rows.Add(row);
            }

            if (rows.Count > 0)
            {
                tables.Add(new ImageTableModel { Name = $"Table {tables.Count + 1}", Rows = rows });
            }
        }

        return tables;
    }

    private static string[] SplitTableRow(string line)
    {
        return line.Trim('|')
            .Split('|')
            .Select(cell => cell.Trim())
            .ToArray();
    }
}
