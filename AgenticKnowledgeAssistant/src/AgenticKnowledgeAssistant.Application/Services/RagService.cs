using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class RagService : IRagService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;
    private const int ChunkTokenLimit = 700;
    private const int ChunkOverlapTokens = 90;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".txt", ".md", ".markdown", ".csv", ".xlsx", ".xls", ".pptx",
        ".json", ".xml", ".html", ".htm", ".zip", ".sql", ".cs", ".js", ".ts", ".tsx",
        ".jsx", ".py", ".java", ".yml", ".yaml", ".config"
    };

    private readonly IRagRepository _repository;
    private readonly IAgentBAL _agentBAL;
    private readonly ICommonBAL _commonBAL;
    private readonly ILogger<RagService> _logger;

    public RagService(IRagRepository repository, IAgentBAL agentBAL, ICommonBAL commonBAL, ILogger<RagService> logger)
    {
        _repository = repository;
        _agentBAL = agentBAL;
        _commonBAL = commonBAL;
        _logger = logger;
    }

    public async Task<Response<object>> UploadAsync(IFormFile? file, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            EnsureUser(userId);
            var validation = ValidateFile(file);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, validation, startTime);
            }

            ArgumentNullException.ThrowIfNull(file);
            var extracted = await ExtractTextAsync(file, cancellationToken);
            if (string.IsNullOrWhiteSpace(extracted.Text))
            {
                return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, "No readable text could be extracted from this file.", startTime);
            }

            var document = new RagDocumentModel
            {
                UserId = userId,
                FileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSizeBytes = file.Length,
                Title = InferTitle(file.FileName, extracted.Text),
                ProcessingStatus = "Uploaded",
                Summary = BuildExtractiveSummary(extracted.Text),
                Metadata = JsonSerializer.Serialize(extracted.Metadata),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            var documentId = await _repository.CreateDocumentAsync(document, cancellationToken);
            var chunks = BuildChunks(extracted.Text, extracted.PageBreaks).ToArray();
            var embeddingCount = await SaveChunksWithEmbeddingsAsync(documentId, userId, chunks, cancellationToken);
            await _repository.UpdateDocumentStatusAsync(documentId, userId, "ReadyForSearch", chunks.Length, embeddingCount, document.Summary, document.Metadata, cancellationToken);

            _logger.LogInformation("RAG document uploaded and indexed. DocumentId={DocumentId} UserId={UserId} Chunks={Chunks} Embeddings={Embeddings}", documentId, userId, chunks.Length, embeddingCount);
            return _commonBAL.Success(new RagUploadResponseDTO
            {
                DocumentId = documentId,
                FileName = document.FileName,
                Status = "ReadyForSearch",
                ChunkCount = chunks.Length
            }, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG upload failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "RAG upload failed", startTime);
        }
    }

    public async Task<Response<object>> IndexAsync(RagIndexRequestDTO request, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            EnsureUser(userId);
            var documents = request.DocumentId.HasValue
                ? new[] { await _repository.GetDocumentAsync(request.DocumentId.Value, userId, cancellationToken) }.Where(d => d is not null).Cast<RagDocumentModel>().ToArray()
                : await _repository.GetDocumentsAsync(userId, cancellationToken);

            return _commonBAL.Success(new
            {
                indexedDocuments = documents.Count,
                message = "RAG indexing is performed during upload. Re-upload documents to regenerate chunks and embeddings."
            }, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG index request failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "RAG indexing failed", startTime);
        }
    }

    public async Task<Response<object>> SearchAsync(RagSearchRequestDTO request, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            var results = await HybridSearchAsync(request, userId, cancellationToken);
            await _repository.SaveSearchHistoryAsync(userId, request.Query, results.Count, "Hybrid", cancellationToken);
            return _commonBAL.Success(new RagSearchResponseDTO { Results = results }, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG search failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "RAG search failed", startTime);
        }
    }

    public async Task<Response<object>> ChatAsync(RagChatRequestDTO request, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            EnsureUser(userId);
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Question is required", startTime);
            }

            var results = await HybridSearchAsync(new RagSearchRequestDTO
            {
                Query = request.Question,
                DocumentIds = request.DocumentIds,
                TopK = request.TopK
            }, userId, cancellationToken);

            if (results.Count == 0)
            {
                return _commonBAL.Success(new RagChatResponseDTO
                {
                    Answer = "I could not find relevant uploaded enterprise knowledge for this question. Please upload or index the required document, then try again.",
                    Sources = Array.Empty<RagSearchResultModel>(),
                    ConfidenceScore = 0
                }, startTime);
            }

            var context = BuildGroundedContext(results);
            var prompt = $"""
You are an Enterprise RAG assistant. Answer ONLY from the retrieved enterprise knowledge below.
Do not use outside knowledge. Do not invent facts.
If the answer is not supported by the retrieved chunks, say that the uploaded knowledge does not contain enough information.
Always include citations using Document, Page, Section, Chunk, and Confidence.

Retrieved Knowledge:
{context}

Question:
{request.Question}
""";

            var answer = await _agentBAL.GenerateResponseAsync(prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(answer) || answer.Contains("AI provider is not configured", StringComparison.OrdinalIgnoreCase))
            {
                answer = BuildExtractiveAnswer(request.Question, results);
            }

            var confidence = Math.Round(results.Max(r => r.HybridScore), 3);
            return _commonBAL.Success(new RagChatResponseDTO
            {
                Answer = answer,
                Sources = results,
                ConfidenceScore = confidence
            }, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG chat failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "RAG chat failed", startTime);
        }
    }

    public async Task<Response<object>> GetDocumentAsync(long id, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var document = await _repository.GetDocumentAsync(id, userId, cancellationToken);
        return document is null
            ? _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.NotFound, "RAG document not found", startTime)
            : _commonBAL.Success(document, startTime);
    }

    public async Task<Response<object>> DeleteDocumentAsync(long id, int userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var deleted = await _repository.DeleteDocumentAsync(id, userId, cancellationToken);
        return deleted
            ? _commonBAL.Success(new { deleted = true }, startTime)
            : _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.NotFound, "RAG document not found", startTime);
    }

    private async Task<IReadOnlyList<RagSearchResultModel>> HybridSearchAsync(RagSearchRequestDTO request, int userId, CancellationToken cancellationToken)
    {
        EnsureUser(userId);
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new InvalidOperationException("Search query is required.");
        }

        var topK = Math.Clamp(request.TopK, 1, 20);
        var keywordResults = await _repository.KeywordSearchAsync(userId, request.Query, request.DocumentIds, topK * 3, cancellationToken);
        var queryVector = await _agentBAL.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var vectorResults = new List<RagSearchResultModel>();

        if (queryVector.Length > 0)
        {
            var embeddings = await _repository.GetSearchableEmbeddingsAsync(userId, request.DocumentIds, cancellationToken);
            vectorResults = embeddings
                .Select(item =>
                {
                    var vector = JsonSerializer.Deserialize<float[]>(item.VectorData) ?? Array.Empty<float>();
                    item.Chunk.VectorScore = CosineSimilarity(queryVector, vector);
                    return item.Chunk;
                })
                .Where(item => item.VectorScore > 0.10)
                .OrderByDescending(item => item.VectorScore)
                .Take(topK * 3)
                .ToList();
        }

        var merged = keywordResults.Concat(vectorResults)
            .GroupBy(item => item.ChunkId)
            .Select(group =>
            {
                var best = group.OrderByDescending(item => item.KeywordScore + item.VectorScore).First();
                best.KeywordScore = group.Max(item => item.KeywordScore);
                best.VectorScore = group.Max(item => item.VectorScore);
                best.HybridScore = Math.Round((Normalize(best.KeywordScore) * 0.45) + (best.VectorScore * 0.55), 4);
                return best;
            })
            .Where(item => item.HybridScore > 0)
            .OrderByDescending(item => item.HybridScore)
            .ThenBy(item => item.DocumentId)
            .Take(topK)
            .ToArray();

        return merged;
    }

    private async Task<int> SaveChunksWithEmbeddingsAsync(long documentId, int userId, IReadOnlyList<RagChunkModel> chunks, CancellationToken cancellationToken)
    {
        await _repository.DeleteChunksAsync(documentId, userId, cancellationToken);
        var embeddingCount = 0;

        foreach (var chunk in chunks)
        {
            var embedding = await _agentBAL.GenerateEmbeddingAsync(chunk.Content, cancellationToken);
            var embeddingJson = embedding.Length == 0 ? null : JsonSerializer.Serialize(embedding);
            if (embedding.Length > 0)
            {
                embeddingCount++;
            }

            await _repository.SaveChunkAsync(documentId, userId, chunk, embeddingJson, embedding.Length == 0 ? "KeywordOnly" : "OpenAI", cancellationToken);
        }

        return embeddingCount;
    }

    private static IEnumerable<RagChunkModel> BuildChunks(string text, IReadOnlyList<int> pageBreaks)
    {
        var paragraphs = Regex.Split(text, @"(\r?\n){2,}")
            .Select(p => Regex.Replace(p, @"\s+", " ").Trim())
            .Where(p => p.Length > 0)
            .ToArray();

        var buffer = new List<string>();
        var tokenCount = 0;
        var chunkIndex = 0;
        var currentHeading = string.Empty;
        var currentSection = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            if (LooksLikeHeading(paragraph))
            {
                currentHeading = paragraph.Length <= 180 ? paragraph : paragraph[..180];
                currentSection = currentHeading;
            }

            var paragraphTokens = EstimateTokens(paragraph);
            if (tokenCount + paragraphTokens > ChunkTokenLimit && buffer.Count > 0)
            {
                yield return CreateChunk(chunkIndex++, string.Join(Environment.NewLine, buffer), currentSection, currentHeading, pageBreaks);
                var overlap = buffer.TakeLast(Math.Max(1, Math.Min(buffer.Count, ChunkOverlapTokens / 80))).ToList();
                buffer = overlap;
                tokenCount = overlap.Sum(EstimateTokens);
            }

            buffer.Add(paragraph);
            tokenCount += paragraphTokens;
        }

        if (buffer.Count > 0)
        {
            yield return CreateChunk(chunkIndex, string.Join(Environment.NewLine, buffer), currentSection, currentHeading, pageBreaks);
        }
    }

    private static RagChunkModel CreateChunk(int index, string content, string section, string heading, IReadOnlyList<int> pageBreaks)
    {
        var page = pageBreaks.Count == 0 ? null : (int?)Math.Max(1, pageBreaks.Count(p => p <= content.Length));
        return new RagChunkModel
        {
            ChunkIndex = index,
            PageNumber = page,
            Section = section,
            Heading = heading,
            Content = content,
            TokenCount = EstimateTokens(content),
            Metadata = JsonSerializer.Serialize(new { chunking = "paragraph-heading-sliding-window", overlapTokens = ChunkOverlapTokens })
        };
    }

    private static async Task<ExtractedDocument> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => ExtractPdf(memory),
            ".docx" => ExtractDocx(memory),
            ".xlsx" or ".xls" => ExtractSpreadsheet(memory),
            ".pptx" => ExtractPresentation(memory),
            ".zip" => ExtractZip(memory),
            ".html" or ".htm" => ExtractPlain(memory, stripHtml: true),
            _ => ExtractPlain(memory, stripHtml: false)
        };
    }

    private static ExtractedDocument ExtractPdf(Stream stream)
    {
        var builder = new StringBuilder();
        var pageBreaks = new List<int>();
        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            pageBreaks.Add(builder.Length);
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        return new ExtractedDocument(Normalize(builder.ToString()), pageBreaks, new { pages = document.NumberOfPages });
    }

    private static ExtractedDocument ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var text = string.Join(Environment.NewLine, document.MainDocumentPart?.Document.Body?.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText) ?? Array.Empty<string>());
        return new ExtractedDocument(Normalize(text), Array.Empty<int>(), new { format = "docx" });
    }

    private static ExtractedDocument ExtractSpreadsheet(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var sharedStrings = document.WorkbookPart?.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().Select(x => x.InnerText).ToArray() ?? Array.Empty<string>();
        var rows = document.WorkbookPart?.WorksheetParts.SelectMany(part => part.Worksheet.Descendants<Row>()).Select(row => string.Join(" | ", row.Descendants<Cell>().Select(cell => ResolveCell(cell, sharedStrings)))) ?? Array.Empty<string>();
        return new ExtractedDocument(Normalize(string.Join(Environment.NewLine, rows)), Array.Empty<int>(), new { format = "excel" });
    }

    private static ExtractedDocument ExtractPresentation(Stream stream)
    {
        using var document = PresentationDocument.Open(stream, false);
        var text = string.Join(Environment.NewLine, document.PresentationPart?.SlideParts.SelectMany(slide => slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()).Select(t => t.Text) ?? Array.Empty<string>());
        return new ExtractedDocument(Normalize(text), Array.Empty<int>(), new { format = "powerpoint" });
    }

    private static ExtractedDocument ExtractZip(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var builder = new StringBuilder();
        foreach (var entry in archive.Entries.Where(e => SupportedExtensions.Contains(Path.GetExtension(e.FullName))).Take(50))
        {
            builder.AppendLine($"# File: {entry.FullName}");
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            builder.AppendLine(reader.ReadToEnd());
        }

        return new ExtractedDocument(Normalize(builder.ToString()), Array.Empty<int>(), new { format = "zip", files = archive.Entries.Count });
    }

    private static ExtractedDocument ExtractPlain(Stream stream, bool stripHtml)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        if (stripHtml)
        {
            text = Regex.Replace(text, "<script[\\s\\S]*?</script>|<style[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", " ");
        }

        return new ExtractedDocument(Normalize(text), Array.Empty<int>(), new { format = "text" });
    }

    private static string ResolveCell(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        var value = cell.CellValue?.Text ?? string.Empty;
        return cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count
            ? sharedStrings[index]
            : value;
    }

    private static string ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "File is required.";
        if (file.Length > MaxFileSizeBytes) return "File size cannot exceed 50 MB.";
        if (!SupportedExtensions.Contains(Path.GetExtension(file.FileName))) return "This file type is not supported by Enterprise RAG.";
        return string.Empty;
    }

    private static void EnsureUser(int userId)
    {
        if (userId <= 0) throw new UnauthorizedAccessException("Authenticated user is required.");
    }

    private static bool LooksLikeHeading(string value)
        => value.Length <= 140 && (Regex.IsMatch(value, @"^\d+(\.\d+)*\s+\w+") || Regex.IsMatch(value, @"^[A-Z][A-Za-z0-9 /&-]{3,}$"));

    private static int EstimateTokens(string value) => Math.Max(1, value.Length / 4);
    private static double Normalize(double value) => value <= 0 ? 0 : Math.Min(1, value / 10d);

    private static string Normalize(string text)
        => string.Join(Environment.NewLine, text.Replace("\0", string.Empty).Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line)));

    private static string InferTitle(string fileName, string text)
        => Regex.Split(text, @"\r?\n").FirstOrDefault(line => line.Length is > 5 and < 160) ?? Path.GetFileNameWithoutExtension(fileName);

    private static string BuildExtractiveSummary(string text)
        => string.Join(" ", Regex.Split(text, @"(?<=[.!?])\s+").Where(s => s.Length > 30).Take(4)).Trim();

    private static string BuildGroundedContext(IEnumerable<RagSearchResultModel> results)
        => string.Join(Environment.NewLine + "---" + Environment.NewLine, results.Select(r => $"Document: {r.FileName}\nPage: {r.PageNumber?.ToString() ?? "N/A"}\nSection: {EmptyToDefault(r.Section, "N/A")}\nChunk: {r.ChunkId}\nConfidence: {r.HybridScore:0.000}\nContent:\n{r.Content}"));

    private static string BuildExtractiveAnswer(string question, IReadOnlyList<RagSearchResultModel> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Grounded Answer");
        builder.AppendLine();
        builder.AppendLine("The uploaded enterprise knowledge contains the following relevant information:");
        builder.AppendLine();
        foreach (var result in results.Take(5))
        {
            builder.AppendLine($"- {Trim(result.Content, 450)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Sources");
        foreach (var result in results.Take(5))
        {
            builder.AppendLine($"- Document: {result.FileName}; Page: {result.PageNumber?.ToString() ?? "N/A"}; Section: {EmptyToDefault(result.Section, "N/A")}; Chunk: {result.ChunkId}; Confidence: {result.HybridScore:0.000}");
        }

        return builder.ToString().Trim();
    }

    private static string EmptyToDefault(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..length].TrimEnd() + "...";

    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length) return 0;
        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        return leftNorm == 0 || rightNorm == 0 ? 0 : dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }

    private sealed record ExtractedDocument(string Text, IReadOnlyList<int> PageBreaks, object Metadata);
}
