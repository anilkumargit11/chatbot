using System.Text;
using System.Text.Json;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.Common.Constants;
using AgenticKnowledgeAssistant.Common.Extensions;
using AgenticKnowledgeAssistant.Common.Helpers;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class DocumentBAL : IDocumentBAL
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private readonly IDocumentDAL _documentDAL;
    private readonly IAgentDAL _agentDAL;
    private readonly IAgentBAL _agentBAL;
    private readonly ICommonBAL _commonBAL;
    private readonly ILogger<DocumentBAL> _logger;

    public DocumentBAL(IDocumentDAL documentDAL,IAgentDAL agentDAL,IAgentBAL agentBAL,ICommonBAL commonBAL,ILogger<DocumentBAL> logger)
    {
        _documentDAL = documentDAL;
        _agentDAL = agentDAL;
        _agentBAL = agentBAL;
        _commonBAL = commonBAL;
        _logger = logger;
    }

    public async Task<Response<object>> UploadDocument(IFormFile? file, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            var validationMessage = ValidateFile(file);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, validationMessage, startTime);
            }

            ArgumentNullException.ThrowIfNull(file);
            var content = await ExtractTextAsync(file, cancellationToken);
            var documentId = await _documentDAL.SaveDocumentDB(new DocumentModel
            {
                Title = Path.GetFileName(file.FileName),
                Content = content,
                CreatedDate = DateTime.UtcNow
            }, cancellationToken);

            var embedding = await _agentBAL.GenerateEmbeddingAsync(content, cancellationToken);
            if (embedding.Length > 0)
            {
                await _agentDAL.SaveEmbeddingDB(new EmbeddingModel
                {
                    DocumentId = documentId,
                    VectorData = JsonSerializer.Serialize(embedding)
                }, cancellationToken);
            }

            return _commonBAL.Success(new { message = "Document uploaded successfully", fileName = file.FileName, documentId }, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentBAL.UploadDocument failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "Failed to upload document", startTime);
        }
    }

    public async Task<Response<object>> GetDocuments(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var documents = await _documentDAL.GetDocumentsDB(cancellationToken);
        return _commonBAL.Success(documents.Select(ToSummary).ToArray(), startTime);
    }

    public async Task<Response<object>> SearchDocuments(string query, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Search query is required", startTime);
        }

        var documents = await _documentDAL.SearchDocumentsDB(query, cancellationToken);
        return _commonBAL.Success(documents.Select(ToSummary).ToArray(), startTime);
    }

    public async Task<Response<object>> DeleteDocument(int id, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var deleted = await _documentDAL.DeleteDocumentDB(id, cancellationToken);
        return deleted
            ? _commonBAL.Success(new { deleted = true }, startTime)
            : _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.NotFound, "Document not found", startTime);
    }

    private static string ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "File is required";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "File size cannot exceed 25 MB";
        }

        if (!FileValidationHelper.IsSupportedDocument(file.FileName))
        {
            return "Only PDF, DOCX, and TXT files are supported";
        }

        return string.Empty;
    }

    private static async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return extension switch
        {
            ".pdf" => ExtractPdfText(memoryStream),
            ".docx" => ExtractDocxText(memoryStream),
            ".txt" => Encoding.UTF8.GetString(memoryStream.ToArray()),
            _ => string.Empty
        };
    }

    private static string ExtractPdfText(Stream stream)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(stream);

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return NormalizeExtractedText(builder.ToString());
    }

    private static string ExtractDocxText(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var paragraphs = document.MainDocumentPart?.Document.Body?
            .Descendants<Paragraph>()
            .Select(paragraph => paragraph.InnerText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray() ?? Array.Empty<string>();

        return NormalizeExtractedText(string.Join(Environment.NewLine, paragraphs));
    }

    private static string NormalizeExtractedText(string text)
    {
        return string.Join(
            Environment.NewLine,
            text.Replace("\0", string.Empty)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static DocumentSummaryDTO ToSummary(DocumentModel document)
    {
        return new DocumentSummaryDTO
        {
            Id = document.Id,
            Title = document.Title,
            Preview = document.Content.ToPreview(ApplicationConstants.DocumentPreviewLength),
            CreatedDate = document.CreatedDate
        };
    }
}
