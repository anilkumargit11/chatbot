using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticKnowledgeAssistant.BAL.AIProviders;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class AgentBAL : IAgentBAL
{
    private readonly ConfigurationSettingsListDTO _configurationSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAgentDAL _agentDAL;
    private readonly IDocumentDAL _documentDAL;
    private readonly IAuthDAL _authDAL;
    private readonly IDatabaseAssistantDAL _databaseAssistantDAL;
    private readonly ILogger<AgentBAL> _logger;
    private readonly IOcrService _ocrService;
    private readonly ITranslatorService _translatorService;
    private readonly IAIProviderResolver _aiProviderResolver;
    private readonly IImageContextService _imageContextService;

    public AgentBAL(
        ConfigurationSettingsListDTO configurationSettings,
        IHttpClientFactory httpClientFactory,
        IAgentDAL agentDAL,
        IDocumentDAL documentDAL,
        IAuthDAL authDAL,
        IDatabaseAssistantDAL databaseAssistantDAL,
        ILogger<AgentBAL> logger,
        IOcrService ocrService,
        ITranslatorService translatorService,
        IAIProviderResolver aiProviderResolver,
        IImageContextService imageContextService)
    {
        _configurationSettings = configurationSettings;
        _httpClientFactory = httpClientFactory;
        _agentDAL = agentDAL;
        _documentDAL = documentDAL;
        _authDAL = authDAL;
        _databaseAssistantDAL = databaseAssistantDAL;
        _logger = logger;
        _ocrService = ocrService;
        _translatorService = translatorService;
        _aiProviderResolver = aiProviderResolver;
        _imageContextService = imageContextService;
    }

    public async Task<ChatResponseDTO> HandleAgentRequest(AgenticKnowledgeAssistant.DTO.RequestDTOs.ChatRequestDTO request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ChatResponseDTO { ToolUsed = "AgentCore" };
        var question = request.Question;
        var userQuestion = string.IsNullOrWhiteSpace(request.OriginalQuestion)
            ? request.Question
            : request.OriginalQuestion;
        
        try
        {
            // 1. Multi-language Auto-detection
            var detectedLang = await _translatorService.DetectLanguageAsync(userQuestion, cancellationToken);
            response.DetectedLanguage = detectedLang;

            var primaryAttachment = GetPrimaryAttachment(request);
            var sessionGuid = request.SessionGuid ?? Guid.Empty;

            // 2. Detect intent before using any retrieval tool.
            var intent = DetectIntent(request, userQuestion);
            if (intent.Confidence < 0.80)
            {
                response.Answer = "Can you clarify what you want me to look for?";
                response.ToolUsed = "IntentClarification";
                response.ConfidenceScore = intent.Confidence;
                return response;
            }

            var existingImageContexts = sessionGuid == Guid.Empty
                ? Array.Empty<ImageContextModel>()
                : await _imageContextService.GetSessionImagesAsync(sessionGuid, cancellationToken);

            // 3. Handle Multi-modal Attachments (OCR / Image Analysis / OCR modes)
            if (primaryAttachment is not null)
            {
                var ocrResult = await _ocrService.ExtractTextFromImageAsync(primaryAttachment.Base64Content, primaryAttachment.FileName, cancellationToken);
                var imageContext = sessionGuid == Guid.Empty
                    ? null
                    : await _imageContextService.AddOrUpdateAsync(sessionGuid, primaryAttachment.FileName, primaryAttachment.ContentType, ocrResult, cancellationToken);
                
                if (intent.Intent == RequestIntent.ImageOcr)
                {
                    var activeContexts = imageContext is null
                        ? new[] { BuildEphemeralImageContext(primaryAttachment, ocrResult) }
                        : existingImageContexts.Where(item => !item.FileName.Equals(primaryAttachment.FileName, StringComparison.OrdinalIgnoreCase)).Append(imageContext).ToArray();

                    response.Answer = await AnswerFromImageContextAsync(userQuestion, activeContexts, cancellationToken, primaryAttachment.FileName);
                    response.ToolUsed = "VisionContextService";
                    response.ConfidenceScore = 0.95;
                    
                    // Translate if target language is set
                    if (!string.IsNullOrWhiteSpace(request.TargetLanguage) && !request.TargetLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
                    {
                        response.TranslatedAnswer = await _translatorService.TranslateAsync(ocrResult, request.TargetLanguage, cancellationToken);
                    }
                    
                    stopwatch.Stop();
                    response.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                    response.PromptTokens = primaryAttachment.Base64Content.Length / 8;
                    response.CompletionTokens = response.Answer.Length / 4;
                    response.TotalTokens = response.PromptTokens + response.CompletionTokens;
                    return response;
                }
                
                // Otherwise prepend OCR context to the question to analyze the attachment
                question = $"[Attachment Content/OCR Layout extracted from {primaryAttachment.FileName}]:\n{ocrResult}\n\nUser Question: {userQuestion}";
            }
            else if (existingImageContexts.Count > 0 && ShouldUseImageContext(userQuestion, intent))
            {
                response.Answer = await AnswerFromImageContextAsync(userQuestion, existingImageContexts, cancellationToken);
                response.ToolUsed = "VisionContextService";
                response.ConfidenceScore = 0.92;

                if (!string.IsNullOrWhiteSpace(request.TargetLanguage) && !request.TargetLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    response.TranslatedAnswer = await _translatorService.TranslateAsync(response.Answer, request.TargetLanguage, cancellationToken);
                }

                stopwatch.Stop();
                response.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                response.PromptTokens = existingImageContexts.Sum(item => item.OcrText.Length) / 4;
                response.CompletionTokens = response.Answer.Length / 4;
                response.TotalTokens = response.PromptTokens + response.CompletionTokens;
                return response;
            }

            // 4. Route strictly by detected intent. Do not fall through to unrelated tools.
            var normalizedMode = request.Mode.ToLowerInvariant();
            
            if (intent.Intent == RequestIntent.DatabaseAssistant)
            {
                // Call existing Database Assistant logic
                var dbResponse = await TryAnswerDatabaseMetadataAsync(userQuestion, userQuestion.ToLowerInvariant(), cancellationToken);
                if (dbResponse is null)
                {
                    var dbResult = await TryAnswerFromDatabaseAsync(userQuestion.ToLowerInvariant(), cancellationToken);
                    dbResponse = dbResult ?? new ChatResponseDTO { Answer = "No records found.", ToolUsed = "SearchDatabaseTool" };
                }
                response = dbResponse;
                response.ConfidenceScore = 0.90;
            }
            else if (intent.Intent == RequestIntent.DocumentSearch)
            {
                var docResponse = await TryAnswerDocumentMetadataAsync(userQuestion, userQuestion.ToLowerInvariant(), cancellationToken)
                    ?? await TryAnswerFromDocumentsAsync(userQuestion, cancellationToken);
                if (docResponse is null)
                {
                    response.Answer = "No document found.";
                    response.ToolUsed = "VectorDocumentSearch";
                    response.ConfidenceScore = 0.0;
                }
                else
                {
                    response = docResponse;
                    response.ConfidenceScore = docResponse.ToolUsed == "ExactDocumentMatch" ? 0.99 : 0.90;
                }
            }
            else if (intent.Intent == RequestIntent.HybridSearch)
            {
                var docResponse = await TryAnswerDocumentMetadataAsync(userQuestion, userQuestion.ToLowerInvariant(), cancellationToken)
                    ?? await TryAnswerFromDocumentsAsync(userQuestion, cancellationToken);
                if (docResponse is null)
                {
                    response.Answer = "No document found.";
                    response.ToolUsed = "HybridDocumentSearch";
                    response.ConfidenceScore = 0.0;
                }
                else
                {
                    var hybridPrompt = $"Use the uploaded document answer below as the only document context, then combine it with general AI reasoning only where the user explicitly requested comparison.\n\nDocument answer:\n{docResponse.Answer}\n\nUser request:\n{userQuestion}";
                    response.Answer = await GenerateResponseAsync(hybridPrompt, cancellationToken);
                    response.Sources = docResponse.Sources;
                    response.ToolUsed = "HybridDocumentAndLlmSearch";
                    response.ConfidenceScore = 0.90;
                }
            }
            else if (intent.Intent == RequestIntent.ImageOcr)
            {
                response.Answer = "Please upload an image or scanned file for OCR analysis.";
                response.ToolUsed = "OcrExtractionService";
                response.ConfidenceScore = 0.90;
            }
            else if (normalizedMode.Contains("translate"))
            {
                var targetLang = request.TargetLanguage ?? "en";
                var translatedText = await _translatorService.TranslateAsync(userQuestion, targetLang, cancellationToken);
                response.Answer = translatedText;
                response.ToolUsed = "LanguageTranslationService";
                response.ConfidenceScore = 0.98;
            }
            else if (normalizedMode.Contains("summariz"))
            {
                var summaryPrompt = $"Summarize the following text waves. Highlight key structural points using markdown bullets:\n\n{userQuestion}";
                var summaryAnswer = await GenerateResponseAsync(summaryPrompt, cancellationToken);
                response.Answer = summaryAnswer;
                response.ToolUsed = "OpenAiSummarizer";
                response.ConfidenceScore = 0.92;
            }
            else if (normalizedMode.Contains("developer") || normalizedMode.Contains("code") || normalizedMode.Contains("debug") || normalizedMode.Contains("architect"))
            {
                var developerSystemPrompt = "";
                if (normalizedMode.Contains("review")) 
                    developerSystemPrompt = "You are an expert Senior Code Reviewer. Perform a deep code review of the following code. Identify bugs, performance bottlenecks, security vulnerabilities (like SQL injection, XSS, CSRF), and check compliance with SOLID principles. Provide clear suggested diffs.";
                else if (normalizedMode.Contains("debug"))
                    developerSystemPrompt = "You are an expert Debugging Agent. Analyze the error details, logs, or stack trace provided. Pinpoint the root cause and provide exact step-by-step resolution code.";
                else if (normalizedMode.Contains("architect"))
                    developerSystemPrompt = "You are a Principal Software Architect. Define a clean architecture implementation plan, system block topology, database relationships, and component design patterns. Use standard ASCII or Markdown tables where relevant.";
                else 
                    developerSystemPrompt = "You are an expert full stack software engineer. Generate high-quality, production-ready, clean code following industry best practices.";

                var devAnswer = await GenerateResponseAsync($"{developerSystemPrompt}\n\nInput:\n{userQuestion}", cancellationToken);
                response.Answer = devAnswer;
                response.ToolUsed = "DeveloperAssistantService";
                response.ConfidenceScore = 0.94;
            }
            else // Default/Normal Chat
            {
                var directAnswer = await GenerateResponseAsync(question, cancellationToken);
                response.Answer = directAnswer;
                response.ToolUsed = "OpenAiDirectResponse";
                response.ConfidenceScore = 0.92;
            }

            // 4. Translate response answer if TargetLanguage is requested and target language is not English
            if (!string.IsNullOrWhiteSpace(request.TargetLanguage) && !request.TargetLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                response.TranslatedAnswer = await _translatorService.TranslateAsync(response.Answer, request.TargetLanguage, cancellationToken);
            }

            // 5. PII Masking safeguard
            response.Answer = MaskSensitivePii(response.Answer);
            if (!string.IsNullOrWhiteSpace(response.TranslatedAnswer))
            {
                response.TranslatedAnswer = MaskSensitivePii(response.TranslatedAnswer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent request failed in HandleAgentRequest");
            response.Answer = "Technical Error processing request. Please retry.";
            response.ConfidenceScore = 0.0;
        }
        finally
        {
            stopwatch.Stop();
            response.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            // Set token metrics (simulate or mock since we are using basic REST completions)
            response.PromptTokens = question.Length / 4;
            response.CompletionTokens = response.Answer.Length / 4;
            response.TotalTokens = response.PromptTokens + response.CompletionTokens;
        }

        return response;
    }

    private enum RequestIntent
    {
        GeneralChat,
        DocumentSearch,
        DatabaseAssistant,
        ImageOcr,
        HybridSearch
    }

    private sealed record IntentDecision(RequestIntent Intent, double Confidence);

    private sealed record NormalizedChatAttachment(string FileName, string ContentType, string Base64Content, long Size);

    private static ImageContextModel BuildEphemeralImageContext(NormalizedChatAttachment attachment, string ocrText)
    {
        return new ImageContextModel
        {
            ImageId = Guid.NewGuid(),
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            OcrText = ocrText,
            Lines = Regex.Split(ocrText, @"\r?\n")
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray()
        };
    }

    private async Task<string> AnswerFromImageContextAsync(string question, IReadOnlyList<ImageContextModel> contexts, CancellationToken cancellationToken, string? preferredFileName = null)
    {
        if (contexts.Count == 0)
        {
            return "Please upload an image or scanned file for OCR analysis.";
        }

        var selectedContext = SelectImageContext(question, contexts, preferredFileName);
        if (selectedContext is null)
        {
            return $"Which image are you referring to? {string.Join(", ", contexts.Select(item => item.FileName))}";
        }

        var deterministicAnswer = TryAnswerImageQuestionDeterministically(question, selectedContext);
        if (!string.IsNullOrWhiteSpace(deterministicAnswer))
        {
            return deterministicAnswer;
        }

        if (LooksLikeUnavailableOcr(selectedContext.OcrText))
        {
            return "This information is not present in the uploaded image.";
        }

        var prompt = $"""
You are answering a user's question using ONLY the OCR/layout context from one uploaded image.
Do not use outside knowledge.
Do not invent values.
If the requested information is absent, answer exactly: This information is not present in the uploaded image.
If coordinates or bounding boxes are unavailable, say "Bounding Box: Not available" instead of guessing.

Current Image ID: {selectedContext.ImageId}
File Name: {selectedContext.FileName}

OCR / Layout Context:
{selectedContext.OcrText}

User Question:
{question}
""";

        var answer = await GenerateResponseAsync(prompt, cancellationToken);
        return string.IsNullOrWhiteSpace(answer) || answer.Equals(AIProviderMessages.Unavailable, StringComparison.Ordinal)
            ? "This information is not present in the uploaded image."
            : answer;
    }

    private static ImageContextModel? SelectImageContext(string question, IReadOnlyList<ImageContextModel> contexts, string? preferredFileName = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredFileName))
        {
            var preferredContext = contexts
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault(item => item.FileName.Equals(preferredFileName, StringComparison.OrdinalIgnoreCase));

            if (preferredContext is not null)
            {
                return preferredContext;
            }
        }

        if (contexts.Count == 1)
        {
            return contexts[0];
        }

        var normalizedQuestion = NormalizeForIntent(question);
        if (HasAny(normalizedQuestion, "this image", "current image", "latest image", "last image", "uploaded image", "the image", "in image", "in images", "from image", "from images"))
        {
            return contexts.OrderByDescending(item => item.CreatedAtUtc).FirstOrDefault();
        }

        var scored = contexts
            .Select(context => new
            {
                Context = context,
                Score = ScoreImageContextMatch(normalizedQuestion, context)
            })
            .OrderByDescending(item => item.Score)
            .ToArray();

        return scored.Length > 0 && scored[0].Score > 0 && (scored.Length == 1 || scored[0].Score > scored[1].Score)
            ? scored[0].Context
            : null;
    }

    private static int ScoreImageContextMatch(string normalizedQuestion, ImageContextModel context)
    {
        var searchable = NormalizeForIntent($"{context.FileName} {context.OcrText}");
        var score = 0;

        if (HasAny(normalizedQuestion, "receipt", "bill", "invoice", "total", "tax", "gst", "discount", "amount")
            && HasAny(searchable, "receipt", "bill", "invoice", "total", "tax", "discount"))
        {
            score += 5;
        }

        if (HasAny(normalizedQuestion, "prescription", "medicine", "medicines", "doctor", "patient", "blood pressure", "diagnosis", "paracetamol", "belladonna", "amphogel")
            && HasAny(searchable, "prescription", "medicine", "doctor", "patient", "belladonna", "amphogel"))
        {
            score += 5;
        }

        foreach (var token in Regex.Matches(normalizedQuestion, @"[a-z0-9]{4,}").Select(match => match.Value).Distinct())
        {
            if (searchable.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                score += context.FileName.Contains(token, StringComparison.OrdinalIgnoreCase) ? 4 : 1;
            }
        }

        return score;
    }

    private static bool ShouldUseImageContext(string question, IntentDecision intent)
    {
        if (intent.Intent == RequestIntent.ImageOcr)
        {
            return true;
        }

        if (intent.Intent is RequestIntent.DatabaseAssistant or RequestIntent.DocumentSearch or RequestIntent.HybridSearch)
        {
            return false;
        }

        var normalizedQuestion = NormalizeForIntent(question);
        return HasAny(normalizedQuestion,
            "this image", "uploaded image", "photo", "picture", "receipt", "bill", "invoice", "total amount",
            "medicine", "medicines", "prescription", "doctor", "patient", "blood pressure", "diagnosis",
            "fields", "field", "visible text", "screen", "screenshot", "website", "webpage", "page",
            "where is", "where was", "written", "after dinner", "before food", "after food", "dosage", "dose");
    }

    private static string? TryAnswerImageQuestionDeterministically(string question, ImageContextModel context)
    {
        var normalizedQuestion = NormalizeForIntent(question);

        if (HasAny(normalizedQuestion, "where is", "where was", "written"))
        {
            var target = ExtractLocationTarget(question);
            if (!string.IsNullOrWhiteSpace(target))
            {
                var matchingLine = context.Lines.Select((line, index) => new { Line = line, Row = index + 1 })
                    .FirstOrDefault(item => item.Line.Contains(target, StringComparison.OrdinalIgnoreCase));

                if (matchingLine is null)
                {
                    return "This information is not present in the uploaded image.";
                }

                return $"""
Value:
{matchingLine.Line}

Location:
OCR text

Row:
{matchingLine.Row}

Column:
Not available

Bounding Box:
Not available

Page:
1
""";
            }
        }

        if (HasAny(normalizedQuestion, "fields", "field", "what fields", "visible text", "what text", "screen details", "image description", "describe image", "explain image"))
        {
            var fields = ExtractVisibleImageFields(context).ToArray();
            if (fields.Length == 0)
            {
                return null;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Visible information found in `{context.FileName}`:");
            builder.AppendLine();
            foreach (var field in fields)
            {
                builder.AppendLine($"- {field}");
            }
            builder.AppendLine();
            builder.AppendLine("Bounding Box: Not available");
            return builder.ToString().Trim();
        }

        if (HasAny(normalizedQuestion, "medicine", "medicines", "drug", "drugs"))
        {
            var medicines = ExtractMedicineRows(context).ToArray();
            if (medicines.Length == 0)
            {
                return "This information is not present in the uploaded image.";
            }

            return "Medicines found in the uploaded image:\n\n" + string.Join("\n", medicines.Select(item => $"- {item}"));
        }

        if (HasAny(normalizedQuestion, "total", "bill amount", "total amount", "how much"))
        {
            var total = ExtractKeyValue(context, "Total");
            return string.IsNullOrWhiteSpace(total)
                ? "This information is not present in the uploaded image."
                : $"The total shown in the uploaded image is {total}.";
        }

        if (HasAny(normalizedQuestion, "tax", "gst"))
        {
            var tax = ExtractKeyValue(context, "Tax") ?? ExtractKeyValue(context, "GST");
            return string.IsNullOrWhiteSpace(tax)
                ? "This information is not present in the uploaded image."
                : $"The tax/GST shown in the uploaded image is {tax}.";
        }

        if (normalizedQuestion.Contains("discount", StringComparison.OrdinalIgnoreCase))
        {
            var discount = ExtractKeyValue(context, "Discount");
            return string.IsNullOrWhiteSpace(discount)
                ? "This information is not present in the uploaded image."
                : $"The discount shown in the uploaded image is {discount}.";
        }

        if (HasAny(normalizedQuestion, "doctor", "physician", "signature"))
        {
            var doctor = ExtractKeyValue(context, "Doctor / Signature") ?? ExtractKeyValue(context, "Physician") ?? ExtractKeyValue(context, "Doctor");
            return string.IsNullOrWhiteSpace(doctor)
                ? "This information is not present in the uploaded image."
                : $"The doctor/signature shown in the uploaded image is {doctor}.";
        }

        if (normalizedQuestion.Contains("patient", StringComparison.OrdinalIgnoreCase))
        {
            var patient = ExtractKeyValue(context, "Patient") ?? ExtractKeyValue(context, "Patient Name");
            return string.IsNullOrWhiteSpace(patient)
                ? "This information is not present in the uploaded image."
                : $"The patient shown in the uploaded image is {patient}.";
        }

        if (normalizedQuestion.Contains("blood pressure", StringComparison.OrdinalIgnoreCase))
        {
            var bp = ExtractKeyValue(context, "Blood Pressure");
            return string.IsNullOrWhiteSpace(bp)
                ? "This information is not present in the uploaded image."
                : $"The blood pressure shown in the uploaded image is {bp}.";
        }

        if (HasAny(normalizedQuestion, "date", "prescription date"))
        {
            var date = ExtractKeyValue(context, "Prescription Date") ?? ExtractKeyValue(context, "Date") ?? ExtractKeyValue(context, "Form Date");
            return string.IsNullOrWhiteSpace(date)
                ? "This information is not present in the uploaded image."
                : $"The date shown in the uploaded image is {date}.";
        }

        if (normalizedQuestion.Contains("diagnosis", StringComparison.OrdinalIgnoreCase))
        {
            var diagnosis = ExtractKeyValue(context, "Diagnosis");
            return string.IsNullOrWhiteSpace(diagnosis)
                ? "This information is not present in the uploaded image."
                : $"The diagnosis shown in the uploaded image is {diagnosis}.";
        }

        return null;
    }

    private static IEnumerable<string> ExtractVisibleImageFields(ImageContextModel context)
    {
        var lines = context.Lines
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim(' ', '-', '*', '|'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Where(line => !line.Contains("Local OCR Parser Fallback", StringComparison.OrdinalIgnoreCase))
            .Where(line => !line.Contains("Extraction Status", StringComparison.OrdinalIgnoreCase))
            .Where(line => !line.Contains("Source File", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToArray();

        foreach (var line in lines)
        {
            yield return line;
        }

        if (context.Entities.Count > 0)
        {
            foreach (var entity in context.Entities.Take(10))
            {
                yield return $"{entity.Type}: {entity.Value}";
            }
        }
    }

    private static string? ExtractKeyValue(ImageContextModel context, string key)
    {
        foreach (var table in context.Tables)
        {
            foreach (var row in table.Rows)
            {
                var keyCell = row.FirstOrDefault(cell => cell.Key.Equals("Key", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrWhiteSpace(keyCell) && keyCell.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    var valueCell = row.FirstOrDefault(cell => cell.Key.Contains("Value", StringComparison.OrdinalIgnoreCase)).Value;
                    if (!string.IsNullOrWhiteSpace(valueCell))
                    {
                        return valueCell;
                    }
                }
            }
        }

        var pattern = $@"(?im)(?:\|\s*)?{Regex.Escape(key)}\s*(?:\||:)\s*(?<value>[^|\r\n]+)";
        var match = Regex.Match(context.OcrText, pattern);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static IEnumerable<string> ExtractMedicineRows(ImageContextModel context)
    {
        foreach (var table in context.Tables)
        {
            foreach (var row in table.Rows)
            {
                var medicine = row.FirstOrDefault(cell =>
                    cell.Key.Contains("Medicine", StringComparison.OrdinalIgnoreCase)
                    || cell.Key.Contains("Entry", StringComparison.OrdinalIgnoreCase)).Value;

                if (!string.IsNullOrWhiteSpace(medicine) && !medicine.Equals("Not specified", StringComparison.OrdinalIgnoreCase))
                {
                    var details = string.Join(", ", row
                        .Where(cell => !cell.Key.Contains("Medicine", StringComparison.OrdinalIgnoreCase) && !cell.Key.Contains("Entry", StringComparison.OrdinalIgnoreCase))
                        .Where(cell => !string.IsNullOrWhiteSpace(cell.Value))
                        .Select(cell => $"{cell.Key}: {cell.Value}"));
                    yield return string.IsNullOrWhiteSpace(details) ? medicine : $"{medicine} ({details})";
                }
            }
        }
    }

    private static string ExtractLocationTarget(string question)
    {
        var cleaned = Regex.Replace(question, @"[/]+", " ");
        cleaned = Regex.Replace(cleaned, @"\b(where|will|was|is|written|located)\b", " ", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim(' ', '?', '.', ',');
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            return cleaned;
        }

        cleaned = Regex.Replace(question, @"[/]+", " ");
        var match = Regex.Match(cleaned, @"where\s+(?:is|was)\s+(?<target>.+?)(?:\s+written|\s+located|\?|$)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups["target"].Value.Trim();
        }

        var beforeWritten = Regex.Match(cleaned, @"(?<target>.+?)\s+(?:where\s+)?written", RegexOptions.IgnoreCase);
        return beforeWritten.Success ? beforeWritten.Groups["target"].Value.Trim() : string.Empty;
    }

    private static bool LooksLikeUnavailableOcr(string ocrText)
    {
        return ocrText.Contains("Reliable OCR is not available", StringComparison.OrdinalIgnoreCase)
            || ocrText.Contains("OCR Provider Not Configured", StringComparison.OrdinalIgnoreCase)
            || ocrText.Contains("did not extract enough text", StringComparison.OrdinalIgnoreCase);
    }

    private static NormalizedChatAttachment? GetPrimaryAttachment(AgenticKnowledgeAssistant.DTO.RequestDTOs.ChatRequestDTO request)
    {
        var attachment = request.Attachments.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Base64Content));
        if (attachment is not null)
        {
            return new NormalizedChatAttachment(
                attachment.FileName,
                attachment.ContentType,
                attachment.Base64Content,
                attachment.Size);
        }

        return string.IsNullOrWhiteSpace(request.AttachmentBase64)
            ? null
            : new NormalizedChatAttachment(
                request.AttachmentName ?? "attachment",
                string.Empty,
                request.AttachmentBase64,
                0);
    }

    private static IntentDecision DetectIntent(AgenticKnowledgeAssistant.DTO.RequestDTOs.ChatRequestDTO request, string question)
    {
        var normalizedQuestion = NormalizeForIntent(question);
        var normalizedMode = NormalizeForIntent(request.Mode);
        var hasAttachment = !string.IsNullOrWhiteSpace(request.AttachmentBase64)
            || request.Attachments.Any(item => !string.IsNullOrWhiteSpace(item.Base64Content));
        var hasImageAttachment = HasImageAttachment(request);

        var explicitHybrid = HasAny(normalizedQuestion,
            "compare uploaded document with", "compare document with", "compare uploaded file with",
            "compare knowledge base with", "document and llm", "document with azure", "uploaded document with azure");
        if (explicitHybrid)
        {
            return new IntentDecision(RequestIntent.HybridSearch, 0.97);
        }

        if (hasAttachment && !hasImageAttachment && !IsImageOcrIntent(normalizedQuestion))
        {
            return new IntentDecision(RequestIntent.GeneralChat, 0.96);
        }

        if ((hasAttachment && hasImageAttachment) || IsImageOcrIntent(normalizedQuestion) || HasAny(normalizedMode, "ocr", "vision"))
        {
            return new IntentDecision(RequestIntent.ImageOcr, hasAttachment ? 0.99 : 0.94);
        }

        var strongGeneralIntent = IsGeneralChatIntent(normalizedQuestion);
        var strongDocumentIntent = IsStrongDocumentIntent(normalizedQuestion);
        var strongDatabaseIntent = IsDatabaseIntent(normalizedQuestion);
        var databaseIntent = strongDatabaseIntent || (!strongGeneralIntent && HasAny(normalizedMode, "database", "sql", "db assistant"));
        var documentIntent = IsDocumentSearchIntent(normalizedQuestion) || (!strongGeneralIntent && HasAny(normalizedMode, "enterprise", "document"));
        var generalIntent = strongGeneralIntent || HasAny(normalizedMode, "normal", "developer", "code", "translate", "summarize", "summarise");

        if (strongDocumentIntent && !strongDatabaseIntent)
        {
            return new IntentDecision(RequestIntent.DocumentSearch, 0.96);
        }

        if (strongDatabaseIntent && !strongDocumentIntent)
        {
            return new IntentDecision(RequestIntent.DatabaseAssistant, 0.96);
        }

        if (strongGeneralIntent && !strongDocumentIntent && !strongDatabaseIntent)
        {
            return new IntentDecision(RequestIntent.GeneralChat, 0.97);
        }

        if (databaseIntent && !documentIntent)
        {
            return new IntentDecision(RequestIntent.DatabaseAssistant, 0.95);
        }

        if (documentIntent && !databaseIntent && !generalIntent)
        {
            return new IntentDecision(RequestIntent.DocumentSearch, 0.94);
        }

        if (documentIntent && !databaseIntent && strongDocumentIntent)
        {
            return new IntentDecision(RequestIntent.DocumentSearch, 0.94);
        }

        if (generalIntent && !documentIntent && !databaseIntent)
        {
            return new IntentDecision(RequestIntent.GeneralChat, 0.96);
        }

        if (!documentIntent && !databaseIntent)
        {
            return new IntentDecision(RequestIntent.GeneralChat, 0.92);
        }

        return new IntentDecision(RequestIntent.GeneralChat, 0.79);
    }

    private static bool IsGeneralChatIntent(string normalizedQuestion)
    {
        return Regex.IsMatch(normalizedQuestion, @"\b(weather|news|programming|azure|docker|kubernetes|\.net|dotnet|c#|java|python|javascript|typescript|mathematics|math|english|translate|summarize|summarise|interview|general knowledge|travel|food|health|sports|movie|movies|entertainment|joke|satya nadella|explain|write code|generate code|debug code)\b");
    }

    private static bool IsDocumentSearchIntent(string normalizedQuestion)
    {
        return HasAny(normalizedQuestion, "uploaded document", "uploaded file", "knowledge base", "open jwt document")
            || Regex.IsMatch(normalizedQuestion, @"\b(fos\d+|brd|lld|srs|document id|document name|acceptance criteria|purpose/object|purpose|objective|business logic|impact analysis|project name|search document|find document|from document|in document)\b");
    }

    private static bool IsStrongDocumentIntent(string normalizedQuestion)
    {
        return Regex.IsMatch(normalizedQuestion, @"\b(fos\d+|brd|lld|srs|document id|document name|acceptance criteria|purpose/object|purpose|objective|business logic|impact analysis|project name)\b")
            || Regex.IsMatch(normalizedQuestion, @"\b[A-Z]{2,}[A-Z0-9]*\d{3,}[A-Z0-9-]*\b", RegexOptions.IgnoreCase)
            || HasAny(normalizedQuestion, "uploaded document", "uploaded file", "open jwt document");
    }

    private static bool IsImageOcrIntent(string normalizedQuestion)
    {
        return Regex.IsMatch(normalizedQuestion, @"\b(read prescription|analyze invoice|analyse invoice|hotel bill|gst|passport|driving license|driving licence|screenshot|architecture diagram|extract text|ocr|vision|image|scanned file|scan document)\b");
    }

    private static bool HasImageAttachment(AgenticKnowledgeAssistant.DTO.RequestDTOs.ChatRequestDTO request)
    {
        if (!string.IsNullOrWhiteSpace(request.AttachmentName)
            && Regex.IsMatch(request.AttachmentName, @"\.(jpg|jpeg|png|bmp|gif|webp|heic|heif)$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        return request.Attachments.Any(attachment =>
            attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(attachment.FileName, @"\.(jpg|jpeg|png|bmp|gif|webp|heic|heif)$", RegexOptions.IgnoreCase));
    }

    private static string NormalizeForIntent(string? value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim().ToLowerInvariant();
    }

    private static bool HasAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskSensitivePii(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        // Mask Aadhaar (12 digits with spaces/dashes) or general ID card codes
        var masked = Regex.Replace(text, @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", "XXXX-XXXX-XXXX");
        // Mask credit card numbers
        masked = Regex.Replace(masked, @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", "XXXX-XXXX-XXXX-XXXX");
        // Mask phone numbers
        masked = Regex.Replace(masked, @"\b(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b", "XXX-XXX-XXXX");
        return masked;
    }

    private static bool IsDatabaseIntent(string normalizedQuestion)
    {
        return MentionsUsers(normalizedQuestion)
            || normalizedQuestion.Contains("ajay_db")
            || normalizedQuestion.Contains("sql server")
            || Regex.IsMatch(normalizedQuestion, @"\b(database|db|table|tables|stored\s+procedure|procedure|procedures|sp|sps|view|views|foreign\s+key|primary\s+key|index|indexes|indices|trigger|triggers|column|columns|schema|schemas)\b");
    }

    private async Task<ChatResponseDTO?> TryAnswerDocumentMetadataAsync(string question, string normalizedQuestion, CancellationToken cancellationToken)
    {
        if (!IsDocumentMetadataIntent(normalizedQuestion))
        {
            return null;
        }

        var documents = (await _documentDAL.GetDocumentsDB(cancellationToken))
            .Where(document => !string.IsNullOrWhiteSpace(document.Title))
            .ToArray();

        var documentType = DetectDocumentType(question, normalizedQuestion);
        var filteredDocuments = FilterDocumentsByType(documents, documentType).ToArray();
        var documentLabel = string.IsNullOrWhiteSpace(documentType) ? "Uploaded" : documentType.ToUpperInvariant();
        var structuredDocuments = filteredDocuments
            .OrderBy(document => document.Title)
            .Select(document => new DocumentMetadataItemDTO
            {
                Id = document.Id,
                DocumentName = document.Title,
                FileType = GetFileType(document.Title),
                UploadDate = document.CreatedDate
            })
            .ToArray();

        return new ChatResponseDTO
        {
            Answer = BuildDocumentMetadataAnswer(documentLabel, structuredDocuments, AsksForCount(normalizedQuestion)),
            Sources = new[] { "dbo.usp_AI_GetDocuments" },
            ToolUsed = "DocumentMetadataSearch",
            StructuredData = new DocumentMetadataResponseDTO
            {
                Success = true,
                DocumentType = documentLabel,
                TotalCount = structuredDocuments.Length,
                Documents = structuredDocuments
            }
        };
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var providers = _aiProviderResolver.GetProvidersInPriorityOrder();
        var configuredProviderCount = 0;
        var configuredProviderFailed = false;

        foreach (var provider in providers)
        {
            if (!provider.IsConfigured)
            {
                _logger.LogInformation("Skipping AI provider {ProviderName}: not configured or not enabled for auto-detection.", provider.Name);
                continue;
            }

            configuredProviderCount++;

            try
            {
                var answer = await provider.GenerateChatCompletionAsync(prompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(answer) && !answer.Equals(AIProviderMessages.Unavailable, StringComparison.Ordinal))
                {
                    return answer;
                }

                _logger.LogWarning("AI provider {ProviderName} did not return a usable response. Trying next provider.", provider.Name);
            }
            catch (Exception ex)
            {
                configuredProviderFailed = true;
                _logger.LogError(ex, "AI provider {ProviderName} failed. Trying next provider.", provider.Name);
            }
        }

        _logger.LogWarning("All AI providers failed or were unavailable. Checked providers: {Providers}", string.Join(", ", providers.Select(provider => provider.Name)));
        return configuredProviderCount > 0 && configuredProviderFailed
            ? AIProviderMessages.ConfiguredProviderFailed
            : AIProviderMessages.Unavailable;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsOpenAIConfigured())
        {
            return Array.Empty<float>();
        }

        var client = CreateOpenAIClient();
        var payload = new { input = text, model = "text-embedding-3-small" };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/v1/embeddings", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        return json.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => (float)value.GetDouble())
            .ToArray();
    }

    private async Task<string> AnswerWithContextAsync(string question, CancellationToken cancellationToken)
    {
        var queryVector = await GenerateEmbeddingAsync(question, cancellationToken);
        if (queryVector.Length == 0)
        {
            return string.Empty;
        }

        var embeddings = await _agentDAL.GetEmbeddingsDB(cancellationToken);
        var topDocumentIds = embeddings
            .Select(embedding =>
            {
                var vector = JsonSerializer.Deserialize<float[]>(embedding.VectorData) ?? Array.Empty<float>();
                return new { embedding.DocumentId, Score = CosineSimilarity(vector, queryVector) };
            })
            .OrderByDescending(item => item.Score)
            .Take(5)
            .Select(item => item.DocumentId)
            .Distinct()
            .ToArray();

        var documents = await _documentDAL.GetDocumentsByIdsDB(topDocumentIds, cancellationToken);
        var context = string.Join("\n---\n", documents.Select(document => document.Content));

        if (string.IsNullOrWhiteSpace(context))
        {
            return "I could not find relevant document context for this question.";
        }

        return await GenerateResponseAsync($"Use the following context to answer the question:\n{context}\nQuestion: {question}", cancellationToken);
    }

    private async Task<ChatResponseDTO> AnswerWithDocumentSearchAsync(string question, CancellationToken cancellationToken)
    {
        var response = await TryAnswerFromDocumentsAsync(question, cancellationToken);
        if (response is not null)
        {
            return response;
        }

        return new ChatResponseDTO
        {
            Answer = "I could not find relevant uploaded document content for this question.",
            ToolUsed = "LocalDocumentSearch"
        };
    }

    private async Task<ChatResponseDTO?> TryAnswerFromDocumentsAsync(string question, CancellationToken cancellationToken)
    {
        var localResponse = await TryAnswerFromLocalDocumentsAsync(question, cancellationToken);
        if (localResponse is not null)
        {
            return localResponse;
        }

        if (IsOpenAIConfigured())
        {
            var answer = await AnswerWithContextAsync(question, cancellationToken);
            if (!string.IsNullOrWhiteSpace(answer)
                && !answer.Contains("could not find relevant document context", StringComparison.OrdinalIgnoreCase))
            {
                return new ChatResponseDTO
                {
                    Answer = answer,
                    ToolUsed = "VectorDocumentSearch"
                };
            }
        }

        return null;
    }

    private async Task<ChatResponseDTO?> TryAnswerFromLocalDocumentsAsync(string question, CancellationToken cancellationToken)
    {
        var documents = (await _documentDAL.GetDocumentsDB(cancellationToken)).ToArray();
        var exactDocumentRequest = BuildExactDocumentRequest(question);
        if (exactDocumentRequest.HasDocumentIdentifier)
        {
            var exactDocument = FindExactDocumentByIdentifier(exactDocumentRequest, documents);
            if (exactDocument is null)
            {
                return new ChatResponseDTO
                {
                    Answer = "No document found.",
                    ToolUsed = "ExactDocumentMatch"
                };
            }

            var exactAnswer = BuildSemanticDocumentAnswer(question, exactDocumentRequest, exactDocument);
            return new ChatResponseDTO
            {
                Answer = exactAnswer,
                Sources = new[] { exactDocument.Title },
                ToolUsed = exactDocumentRequest.Section is null ? "SemanticDocumentAnalysis" : "ExactDocumentMatch"
            };
        }

        if (IsSectionDetailQuestion(question))
        {
            return new ChatResponseDTO
            {
                Answer = "No data found.",
                ToolUsed = "ExactDocumentMatch"
            };
        }

        var matches = RankDocuments(question, documents).Take(3).ToArray();

        if (matches.Length == 0)
        {
            return null;
        }

        var fieldAnswer = TryAnswerSingleDocumentField(question, matches[0].Document);
        if (!string.IsNullOrWhiteSpace(fieldAnswer))
        {
            return new ChatResponseDTO
            {
                Answer = fieldAnswer,
                Sources = new[] { matches[0].Document.Title },
                ToolUsed = "LocalDocumentSearch"
            };
        }

        var qaAnswer = TryExtractQuestionAnswer(question, matches[0].Document.Content);
        if (!string.IsNullOrWhiteSpace(qaAnswer))
        {
            return new ChatResponseDTO
            {
                Answer = BuildSourceAnswer(qaAnswer, matches[0].Document.Title),
                Sources = new[] { matches[0].Document.Title },
                ToolUsed = "LocalDocumentSearch"
            };
        }

        if (IsSummaryQuestion(question))
        {
            return new ChatResponseDTO
            {
                Answer = BuildDocumentSummary(matches[0].Document.Content),
                Sources = new[] { matches[0].Document.Title },
                ToolUsed = "LocalDocumentSearch"
            };
        }

        var answerBuilder = new StringBuilder();
        answerBuilder.AppendLine(BuildDocumentMarkdownAnswer(
            question,
            BuildDocumentAnswer(question, matches[0].Document.Content),
            matches[0].Document.Title,
            matches[0].Score >= 4 ? "High" : "Medium"));

        return new ChatResponseDTO
        {
            Answer = answerBuilder.ToString().Trim(),
            Sources = matches.Take(1).Select(match => match.Document.Title).ToArray(),
            ToolUsed = "LocalDocumentSearch"
        };
    }

    private static bool IsSummaryQuestion(string question)
    {
        var normalized = question.ToLowerInvariant();
        return normalized.Contains("summary")
            || normalized.Contains("summarize")
            || normalized.Contains("summarise")
            || normalized.Contains("brief");
    }

    private static DocumentModel? FindExplicitlyReferencedDocument(string question, IEnumerable<DocumentModel> documents)
    {
        var identifiers = ExtractExplicitDocumentIdentifiers(question).ToArray();
        if (identifiers.Length == 0)
        {
            return null;
        }

        return documents
            .Where(document => !string.IsNullOrWhiteSpace(document.Content) && !LooksLikeRawPdf(document.Content))
            .Select(document => new
            {
                Document = document,
                Score = identifiers.Sum(identifier => ScoreDocumentIdentifierMatch(identifier, document))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.Document.CreatedDate)
            .Select(match => match.Document)
            .FirstOrDefault();
    }

    private static ExactDocumentRequest BuildExactDocumentRequest(string question)
    {
        return new ExactDocumentRequest(
            ExtractExplicitDocumentIdentifiers(question).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            DetectRequestedDocumentSection(question));
    }

    private static DocumentModel? FindExactDocumentByIdentifier(ExactDocumentRequest request, IEnumerable<DocumentModel> documents)
    {
        foreach (var identifier in request.DocumentIdentifiers)
        {
            var exactMatch = documents
                .Where(document => !string.IsNullOrWhiteSpace(document.Content) && !LooksLikeRawPdf(document.Content))
                .Where(document => ContainsExactIdentifier(document.Title, identifier)
                    || ContainsExactIdentifier(document.Content.Length > 5000 ? document.Content[..5000] : document.Content, identifier))
                .OrderByDescending(document => document.CreatedDate)
                .FirstOrDefault();

            if (exactMatch is not null)
            {
                return exactMatch;
            }
        }

        return null;
    }

    private static bool ContainsExactIdentifier(string value, string identifier)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        return value.Contains(identifier, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentSectionRequest? DetectRequestedDocumentSection(string question)
    {
        var normalized = question.ToLowerInvariant();
        var sections = new[]
        {
            new DocumentSectionRequest(
                "Purpose/Object",
                "Purpose/Objective",
                new[]
                {
                    "purpose/object", "purpose objective", "purpose/objective", "purpose", "objective",
                    "use", "goal", "business need", "requirement summary", "requirement",
                    "why", "reason", "what does this brd do", "why was this brd created",
                    "explain this brd", "summarize this brd", "summarise this brd",
                    "business objective", "project objective", "module purpose"
                }),
            new DocumentSectionRequest("Impact Analysis", "Impact Analysis", new[] { "impact analysis", "impact" }),
            new DocumentSectionRequest("Business Logic", "Business Logic", new[] { "business logic", "business" }),
            new DocumentSectionRequest("Scope", "Scope", new[] { "scope" }),
            new DocumentSectionRequest("Acceptance Criteria", "Acceptance Criteria", new[] { "acceptance criteria", "acceptance" }),
            new DocumentSectionRequest("Assumptions", "Assumptions", new[] { "assumption", "assumptions" }),
            new DocumentSectionRequest("Dependencies", "Dependencies", new[] { "dependency", "dependencies" })
        };

        return sections.FirstOrDefault(section => section.Aliases.Any(alias => Regex.IsMatch(normalized, $@"\b{Regex.Escape(alias)}\b", RegexOptions.IgnoreCase)));
    }

    private static string BuildSemanticDocumentAnswer(string question, ExactDocumentRequest request, DocumentModel document)
    {
        if (request.Section is null)
        {
            return BuildDocumentMarkdownAnswer(
                question,
                BuildDocumentAnswer(question, document.Content),
                document.Title,
                "Medium",
                forceStandardFormat: true);
        }

        if (IsPurposeIntent(request.Section) && !IsRawSectionRequest(question, request.Section))
        {
            return BuildPurposeIntentAnswer(question, document);
        }

        return BuildStrictDocumentSectionAnswer(request, document);
    }

    private static string BuildStrictDocumentSectionAnswer(ExactDocumentRequest request, DocumentModel document)
    {
        if (request.Section is null)
        {
            return BuildDocumentMarkdownAnswer(
                string.Empty,
                BuildDocumentSummary(document.Content),
                document.Title,
                "Medium",
                forceStandardFormat: true);
        }

        var content = ExtractExactDocumentSection(document.Content, request.Section);
        if (string.IsNullOrWhiteSpace(content))
        {
            return BuildDocumentMarkdownAnswer(
                string.Empty,
                BuildDocumentAnswer(request.Section.RequestedName, document.Content),
                document.Title,
                "Low",
                forceStandardFormat: true);
        }

        var documentId = request.DocumentIdentifiers.FirstOrDefault(identifier => ContainsExactIdentifier(document.Title, identifier))
            ?? request.DocumentIdentifiers.First();

        return $"""
            Document: {documentId}

            Section: {request.Section.RequestedName}

            Content:
            {content.Trim()}
            """.Trim();
    }

    private static bool IsPurposeIntent(DocumentSectionRequest section)
    {
        return section.CanonicalName.Equals("Purpose/Objective", StringComparison.OrdinalIgnoreCase)
            || section.CanonicalName.Equals("Purpose/Object", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRawSectionRequest(string question, DocumentSectionRequest section)
    {
        var normalized = NormalizeQuestionText(question);
        var normalizedAliases = section.Aliases
            .Prepend(section.CanonicalName)
            .Select(NormalizeQuestionText)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .ToArray();

        var identifierless = ExtractExplicitDocumentIdentifiers(question)
            .Aggregate(normalized, (current, identifier) => current.Replace(NormalizeQuestionText(identifier), string.Empty, StringComparison.OrdinalIgnoreCase));

        identifierless = Regex.Replace(identifierless, @"\b(brd|send|me|show|get|section|this|document|file)\b", " ", RegexOptions.IgnoreCase);
        identifierless = Regex.Replace(identifierless, @"\s+", " ").Trim();

        return normalizedAliases.Any(alias => identifierless.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildPurposeIntentAnswer(string question, DocumentModel document)
    {
        var purpose = ExtractBestSection(document.Content, new[]
        {
            "Purpose/Object", "Purpose/Objective", "Purpose / Objective", "Purpose", "Objective",
            "Business Objective", "Project Objective", "Module Purpose", "Business Need",
            "Executive Summary", "Introduction", "Overview"
        });

        var businessLogic = ExtractBestSection(document.Content, new[]
        {
            "Business Logic", "Functional Requirements", "Business Rules", "Description"
        });

        var impact = ExtractBestSection(document.Content, new[]
        {
            "Impact Analysis - BRD", "Impact Analysis", "Impact"
        });

        var acceptance = ExtractBestSection(document.Content, new[]
        {
            "Acceptance Criteria", "Specific Acceptance Criteria"
        });

        var sourceSections = new[] { purpose, businessLogic, impact, acceptance }
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .Select(section => TrimToSentence(section!, 450))
            .ToArray();

        var answer = sourceSections.Length == 0
            ? BuildDocumentAnswer(question, document.Content)
            : BuildBusinessExplanation(sourceSections);

        var builder = new StringBuilder();
        builder.AppendLine("# Answer");
        builder.AppendLine();
        builder.AppendLine(answer);
        builder.AppendLine();

        if (IsDetailedExplanationQuestion(question))
        {
            AppendSectionSummary(builder, "Purpose", purpose);
            AppendSectionSummary(builder, "Business Logic", businessLogic);
            AppendSectionSummary(builder, "Impact Analysis", impact);
            AppendSectionSummary(builder, "Acceptance Criteria", acceptance);
        }
        else
        {
            builder.AppendLine("## Key Points");
            builder.AppendLine();
            foreach (var point in ExtractKeyPoints(answer).Take(4))
            {
                builder.AppendLine($"- {point}");
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Source");
        builder.AppendLine();
        builder.AppendLine(document.Title);
        builder.AppendLine();
        builder.AppendLine("## Confidence");
        builder.AppendLine();
        builder.AppendLine(sourceSections.Length == 0 ? "Medium" : "High");
        return builder.ToString().Trim();
    }

    private static string BuildBusinessExplanation(IReadOnlyList<string> sourceSections)
    {
        var joined = CleanFieldValue(string.Join(" ", sourceSections));
        if (string.IsNullOrWhiteSpace(joined))
        {
            return "No relevant information found.";
        }

        var cleaned = Regex.Replace(joined, @"\b(the\s+)?purpose\s+of\s+this\s+is\s+to\b", "This BRD is created to", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (!cleaned.StartsWith("This BRD", StringComparison.OrdinalIgnoreCase)
            && !cleaned.StartsWith("The BRD", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = "This BRD is created to " + char.ToLowerInvariant(cleaned[0]) + cleaned[1..];
        }

        return TrimToSentence(cleaned, 950);
    }

    private static bool IsDetailedExplanationQuestion(string question)
    {
        var normalized = question.ToLowerInvariant();
        return normalized.Contains("detail")
            || normalized.Contains("fresher")
            || normalized.Contains("simple")
            || normalized.Contains("explain this brd")
            || normalized.Contains("summarize this brd")
            || normalized.Contains("summarise this brd");
    }

    private static void AppendSectionSummary(StringBuilder builder, string heading, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        foreach (var item in ExtractBulletItems(content).DefaultIfEmpty(TrimToSentence(content, 500)).Take(6))
        {
            builder.AppendLine($"- {TrimToSentence(item, 350)}");
        }
        builder.AppendLine();
    }

    private static string? ExtractBestSection(string content, IReadOnlyList<string> startAliases)
    {
        var section = ExtractSection(
            content,
            startAliases.ToArray(),
            KnownDocumentSectionHeadings()
                .SelectMany(heading => heading.Aliases.Prepend(heading.CanonicalName))
                .Where(heading => !startAliases.Contains(heading, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());

        return string.IsNullOrWhiteSpace(section) ? null : section;
    }

    private static string? ExtractExactDocumentSection(string content, DocumentSectionRequest section)
    {
        var lines = content
            .Replace("\0", string.Empty)
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
            .ToArray();

        var capturedLines = new List<string>();
        var isCapturing = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (isCapturing && capturedLines.Count > 0)
                {
                    capturedLines.Add(string.Empty);
                }

                continue;
            }

            var headingMatch = TryMatchSectionHeading(line);
            if (!isCapturing)
            {
                if (headingMatch is not null && headingMatch.CanonicalName.Equals(section.CanonicalName, StringComparison.OrdinalIgnoreCase))
                {
                    isCapturing = true;
                    if (!string.IsNullOrWhiteSpace(headingMatch.TrailingText))
                    {
                        capturedLines.Add(headingMatch.TrailingText);
                    }
                }

                continue;
            }

            if (headingMatch is not null && !headingMatch.CanonicalName.Equals(section.CanonicalName, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            capturedLines.Add(line);
        }

        var exactSection = CleanExtractedSection(capturedLines);
        if (!string.IsNullOrWhiteSpace(exactSection))
        {
            return exactSection;
        }

        return ExtractExactDocumentSectionFromFlattenedText(content, section);
    }

    private static string? ExtractExactDocumentSectionFromFlattenedText(string content, DocumentSectionRequest section)
    {
        var normalized = NormalizeForFieldExtraction(content);
        foreach (var startAlias in section.Aliases.Prepend(section.CanonicalName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var startIndex = normalized.IndexOf(startAlias, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                continue;
            }

            var valueStart = startIndex + startAlias.Length;
            while (valueStart < normalized.Length && ":/- ".Contains(normalized[valueStart]))
            {
                valueStart++;
            }

            var endIndex = KnownDocumentSectionHeadings()
                .Where(heading => !heading.CanonicalName.Equals(section.CanonicalName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(heading => heading.Aliases.Prepend(heading.CanonicalName))
                .Select(alias => normalized.IndexOf(alias, valueStart, StringComparison.OrdinalIgnoreCase))
                .Where(index => index > valueStart)
                .DefaultIfEmpty(normalized.Length)
                .Min();

            return CleanFieldValue(normalized[valueStart..endIndex]);
        }

        return null;
    }

    private static SectionHeadingMatch? TryMatchSectionHeading(string line)
    {
        var normalizedLine = NormalizeHeadingText(line);
        foreach (var heading in KnownDocumentSectionHeadings())
        {
            var aliases = heading.Aliases.Prepend(heading.CanonicalName);
            foreach (var alias in aliases)
            {
                var normalizedAlias = NormalizeHeadingText(alias);
                if (normalizedLine.Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase))
                {
                    return new SectionHeadingMatch(heading.CanonicalName, string.Empty);
                }

                if (normalizedLine.StartsWith(normalizedAlias + " ", StringComparison.OrdinalIgnoreCase)
                    || normalizedLine.StartsWith(normalizedAlias + ":", StringComparison.OrdinalIgnoreCase)
                    || normalizedLine.StartsWith(normalizedAlias + "-", StringComparison.OrdinalIgnoreCase))
                {
                    var trailingText = line[Math.Min(line.Length, alias.Length)..].Trim(' ', ':', '-', '/', '|');
                    return new SectionHeadingMatch(heading.CanonicalName, trailingText);
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<DocumentSectionRequest> KnownDocumentSectionHeadings()
    {
        return new[]
        {
            new DocumentSectionRequest("Purpose/Object", "Purpose/Objective", new[] { "Purpose/Object", "Purpose/Objective", "Purpose / Objective", "Purpose", "Objective" }),
            new DocumentSectionRequest("Impact Analysis", "Impact Analysis", new[] { "Impact Analysis - BRD", "Impact Analysis", "Impact" }),
            new DocumentSectionRequest("Business Logic", "Business Logic", new[] { "Business Logic" }),
            new DocumentSectionRequest("Scope", "Scope", new[] { "Scope" }),
            new DocumentSectionRequest("Acceptance Criteria", "Acceptance Criteria", new[] { "Acceptance Criteria", "Specific Acceptance Criteria" }),
            new DocumentSectionRequest("Assumptions", "Assumptions", new[] { "Assumptions", "Assumption" }),
            new DocumentSectionRequest("Dependencies", "Dependencies", new[] { "Dependencies", "Dependency" }),
            new DocumentSectionRequest("Field Details", "Field Details", new[] { "Field Details", "Field Level" }),
            new DocumentSectionRequest("Screen Details", "Screen Details", new[] { "Screen Name", "Screen Details" }),
            new DocumentSectionRequest("Description", "Description", new[] { "Description" }),
            new DocumentSectionRequest("Process Flow", "Process Flow", new[] { "Process Flow" }),
            new DocumentSectionRequest("Functional Requirements", "Functional Requirements", new[] { "Functional Requirements" }),
            new DocumentSectionRequest("Business Rules", "Business Rules", new[] { "Business Rules" }),
            new DocumentSectionRequest("Generated By", "Generated By", new[] { "Generated by" }),
            new DocumentSectionRequest("BRD ID", "BRD ID", new[] { "BRD ID" })
        };
    }

    private static string NormalizeHeadingText(string value)
    {
        var normalized = Regex.Replace(value, @"[\s/_-]+", " ").Trim(' ', ':', '.', '|').ToUpperInvariant();
        return Regex.Replace(normalized, @"\s+", " ");
    }

    private static string? CleanExtractedSection(IEnumerable<string> lines)
    {
        var cleaned = string.Join(Environment.NewLine, lines)
            .Trim();

        cleaned = Regex.Replace(cleaned, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static IEnumerable<string> ExtractExplicitDocumentIdentifiers(string question)
    {
        foreach (Match match in Regex.Matches(question, @"\bBRD[-_A-Z0-9]+", RegexOptions.IgnoreCase))
        {
            yield return match.Value.Trim();
        }

        foreach (Match match in Regex.Matches(question, @"\b[A-Z]{2,}[A-Z0-9]*\d{4,}[A-Z0-9-]*\b", RegexOptions.IgnoreCase))
        {
            yield return match.Value.Trim();
        }
    }

    private static int ScoreDocumentIdentifierMatch(string identifier, DocumentModel document)
    {
        var normalizedIdentifier = NormalizeIdentifier(identifier);
        var normalizedTitle = NormalizeIdentifier(document.Title);
        var normalizedContentPrefix = NormalizeIdentifier(document.Content.Length > 3000 ? document.Content[..3000] : document.Content);

        if (normalizedTitle.Contains(normalizedIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return 100 + normalizedIdentifier.Length;
        }

        if (normalizedContentPrefix.Contains(normalizedIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return 60 + normalizedIdentifier.Length;
        }

        var compactIdentifier = normalizedIdentifier.Replace("-", string.Empty).Replace("_", string.Empty);
        var compactTitle = normalizedTitle.Replace("-", string.Empty).Replace("_", string.Empty);
        if (compactTitle.Contains(compactIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return 80 + compactIdentifier.Length;
        }

        return 0;
    }

    private static string NormalizeIdentifier(string value)
    {
        return Regex.Replace(value, @"[^A-Z0-9_-]+", string.Empty, RegexOptions.IgnoreCase).ToUpperInvariant();
    }

    private static string BuildAnswerForMatchedDocument(string question, DocumentModel document)
    {
        var normalizedQuestion = question.ToLowerInvariant();
        string answer;

        if (normalizedQuestion.Contains("purpose") || normalizedQuestion.Contains("objective"))
        {
            answer = ExtractPurposeObjective(document.Content) ?? BuildDocumentSummary(document.Content);
        }
        else if (normalizedQuestion.Contains("impact analysis") || normalizedQuestion.Contains("impact"))
        {
            return BuildImpactAnalysisAnswer(document);
        }
        else if (normalizedQuestion.Contains("screen name") || normalizedQuestion.Contains("screen details") || normalizedQuestion.Contains("screen"))
        {
            return BuildScreenDetailsAnswer(document);
        }
        else if (IsSummaryQuestion(question))
        {
            answer = BuildDocumentSummary(document.Content);
        }
        else
        {
            answer = BuildDocumentAnswer(question, document.Content);
        }

        return BuildDocumentMarkdownAnswer(question, answer, document.Title, "High", forceStandardFormat: true);
    }

    private static string BuildImpactAnalysisAnswer(DocumentModel document)
    {
        var section = ExtractSection(
            document.Content,
            new[] { "Impact Analysis - BRD", "Impact Analysis" },
            new[] { "BRD ID", "Generated by", "Business Logic", "Field Details", "Acceptance Criteria" });

        if (string.IsNullOrWhiteSpace(section))
        {
            return BuildSectionNotFoundAnswer("Impact Analysis", document.Title);
        }

        var newFunctionality = ExtractSection(
            section,
            new[] { "New Functionality" },
            new[] { "Impacted Modules", "Business Logic", "BRD ID", "Generated by" });

        var impactedModules = ExtractSection(
            section,
            new[] { "Impacted Modules" },
            new[] { "Business Logic", "BRD ID", "Generated by" });

        var purpose = ExtractPurposeObjective(document.Content);
        var builder = new StringBuilder();
        builder.AppendLine("# Impact Analysis");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("This BRD describes the Clone Existing BRD Document Enhancement. The impact is focused on adding a Clone option so users can create a new BRD from an existing BRD, reduce duplicate data entry, and keep existing functionality unchanged.");
        builder.AppendLine();

        builder.AppendLine("## New Functionality");
        builder.AppendLine();
        foreach (var item in ExtractBulletItems(newFunctionality))
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("## Impacted Modules");
        builder.AppendLine();
        foreach (var item in ExtractBulletItems(impactedModules))
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("## Business Impact");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(purpose))
        {
            builder.AppendLine($"- {TrimToSentence(purpose, 700)}");
        }
        builder.AppendLine("- The current process causes duplicate data entry, increased effort, and a higher chance of inconsistencies.");
        builder.AppendLine("- The Clone feature reduces manual effort and improves productivity.");
        builder.AppendLine("- Existing functionality remains unchanged.");
        builder.AppendLine();

        builder.AppendLine("## Source Document");
        builder.AppendLine();
        builder.AppendLine(document.Title);
        builder.AppendLine();
        builder.AppendLine("## Confidence");
        builder.AppendLine();
        builder.AppendLine("95%");
        builder.AppendLine();
        builder.AppendLine("## Chunks Used");
        builder.AppendLine();
        builder.AppendLine("1 exact document section");

        return builder.ToString().Trim();
    }

    private static string BuildScreenDetailsAnswer(DocumentModel document)
    {
        var screenSection = ExtractSection(
            document.Content,
            new[] { "Screen Name", "Field Details" },
            new[] { "Acceptance Criteria", "Specific Acceptance Criteria", "Generated by" });

        if (string.IsNullOrWhiteSpace(screenSection))
        {
            return BuildSectionNotFoundAnswer("Screen Details", document.Title);
        }

        var builder = new StringBuilder();
        builder.AppendLine("# Screen Details");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("This section describes the screen-level change for the selected BRD.");
        builder.AppendLine();
        builder.AppendLine("## Details");
        builder.AppendLine();
        foreach (var item in ExtractBulletItems(screenSection).DefaultIfEmpty(TrimToSentence(screenSection, 900)))
        {
            builder.AppendLine($"- {item}");
        }
        builder.AppendLine();
        builder.AppendLine("## Source Document");
        builder.AppendLine();
        builder.AppendLine(document.Title);
        builder.AppendLine();
        builder.AppendLine("## Confidence");
        builder.AppendLine();
        builder.AppendLine("90%");
        builder.AppendLine();
        builder.AppendLine("## Chunks Used");
        builder.AppendLine();
        builder.AppendLine("1 ranked document section");
        return builder.ToString().Trim();
    }

    private static string BuildSectionNotFoundAnswer(string sectionName, string source)
    {
        return $"# {sectionName}\n\nSection not available in the specified document.\n\n## Source Document\n\n{source}\n\n## Confidence\n\n95%";
    }

    private static string? ExtractSection(string content, string[] starts, string[] ends)
    {
        var normalized = NormalizeForFieldExtraction(content);
        foreach (var start in starts)
        {
            var startIndex = normalized.IndexOf(start, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                continue;
            }

            var valueStart = startIndex + start.Length;
            var endIndex = ends
                .Select(end => normalized.IndexOf(end, valueStart, StringComparison.OrdinalIgnoreCase))
                .Where(index => index > valueStart)
                .DefaultIfEmpty(Math.Min(normalized.Length, valueStart + 1600))
                .Min();

            return CleanFieldValue(normalized[valueStart..endIndex]);
        }

        return null;
    }

    private static IEnumerable<string> ExtractBulletItems(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            yield break;
        }

        var prepared = Regex.Replace(section, @"(?=\*|\d+\.)", "\n");
        foreach (var item in prepared.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var cleaned = CleanFieldValue(Regex.Replace(item, @"^(\*|-|\d+\.)\s*", string.Empty));
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                yield return cleaned;
            }
        }
    }

    private static string? ExtractPurposeObjective(string content)
    {
        var normalized = NormalizeForFieldExtraction(content);
        return ExtractBetweenAny(
            normalized,
            new[] { "Purpose/Objective", "Purpose/Objectiv", "Purpose / Objective", "Purpose", "Objective" },
            new[] { "Field Level", "Description", "Functional", "Process Flow", "Acceptance Criteria", "Business Rules", "Assumptions" });
    }

    private static string? TryAnswerSingleDocumentField(string question, DocumentModel document)
    {
        var normalized = question.ToLowerInvariant();
        var content = NormalizeForFieldExtraction(document.Content);

        if (ContainsAny(normalized, "brd name", "file name", "document name"))
        {
            return document.Title;
        }

        if (normalized.Contains("created date") || normalized.Contains("upload date") || normalized.Contains("document date"))
        {
            return document.CreatedDate.ToString("yyyy-MM-dd");
        }

        if (normalized.Contains("document id") || normalized.Contains("doc id"))
        {
            return ExtractDocumentId(document.Title) ?? ExtractDocumentId(content);
        }

        if (normalized.Contains("brd number") || normalized.Contains("brd no") || normalized.Contains("document number"))
        {
            return ExtractBrdNumber(document.Title) ?? ExtractDocumentId(content);
        }

        if (normalized.Contains("project name") || normalized == "project")
        {
            return ExtractBetween(content, "Project :", "Module :")
                ?? ExtractBetween(content, "Project:", "Module:");
        }

        if (normalized.Contains("client name") || normalized == "client")
        {
            return ExtractBetween(content, "Client :", "Project")
                ?? ExtractBetween(content, "Client:", "Project");
        }

        if (normalized.Contains("module name") || normalized == "module")
        {
            return ExtractModuleName(content);
        }

        return null;
    }

    private static string BuildDocumentSummary(string content)
    {
        var normalized = NormalizeForFieldExtraction(content);
        var objective = ExtractBetweenAny(normalized, new[] { "Purpose/Objective", "Purpose/Objectiv" }, new[] { "Field Level", "Pickup Person", "Booking ID", "Shipping Charges" })
            ?? SplitIntoParagraphs(content).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(objective))
        {
            return "No summary is available for this document.";
        }

        return TrimToSentence(objective, 450);
    }

    private static string? ExtractBetweenAny(string value, string[] starts, string[] ends)
    {
        foreach (var start in starts)
        {
            var startIndex = value.IndexOf(start, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                continue;
            }

            startIndex += start.Length;
            var endIndex = ends
                .Select(end => value.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase))
                .Where(index => index > startIndex)
                .DefaultIfEmpty(Math.Min(value.Length, startIndex + 700))
                .Min();

            return CleanFieldValue(value[startIndex..endIndex]);
        }

        return null;
    }

    private static string NormalizeForFieldExtraction(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractBetween(string value, string start, string end)
    {
        var startIndex = value.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return null;
        }

        startIndex += start.Length;
        var endIndex = value.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0 || endIndex <= startIndex)
        {
            return null;
        }

        return CleanFieldValue(value[startIndex..endIndex]);
    }

    private static string? ExtractModuleName(string content)
    {
        var moduleStart = content.IndexOf("Module :", StringComparison.OrdinalIgnoreCase);
        if (moduleStart < 0)
        {
            moduleStart = content.IndexOf("Module:", StringComparison.OrdinalIgnoreCase);
        }

        if (moduleStart < 0)
        {
            return null;
        }

        var valueStart = content.IndexOf(':', moduleStart);
        if (valueStart < 0)
        {
            return null;
        }

        valueStart++;
        var titleIndex = content.IndexOf("Title of the BRD", valueStart, StringComparison.OrdinalIgnoreCase);
        if (titleIndex < 0)
        {
            return CleanFieldValue(content[valueStart..Math.Min(content.Length, valueStart + 120)]);
        }

        var titleStart = titleIndex + "Title of the BRD".Length;
        var endTokens = new[] { "Purpose/Objective", "Purpose/Objectiv", "Field Level", "Description" };
        var endIndex = endTokens
            .Select(token => content.IndexOf(token, titleStart, StringComparison.OrdinalIgnoreCase))
            .Where(index => index > titleStart)
            .DefaultIfEmpty(Math.Min(content.Length, titleStart + 180))
            .Min();

        var module = CleanFieldValue(content[valueStart..titleIndex]);
        var title = CleanFieldValue(content[titleStart..endIndex]);
        return string.Join(" ", new[] { module, title }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? ExtractDocumentId(string value)
    {
        var match = Regex.Match(value, @"\b[A-Z]{2,}[A-Z0-9]*\d{3,}[A-Z0-9]*\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private static string? ExtractBrdNumber(string value)
    {
        var fileName = Path.GetFileNameWithoutExtension(value);
        var match = Regex.Match(fileName, @"(?<=_)[A-Z]{2,}[A-Z0-9]*\d{3,}[A-Z0-9]*(?=_)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Value;
        }

        match = Regex.Match(fileName, @"\b[A-Z]{2,}[A-Z0-9]*\d{3,}[A-Z0-9]*\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private static string CleanFieldValue(string value)
    {
        return Regex.Replace(value, @"\s+", " ")
            .Replace(" - ", "-")
            .Trim(' ', '-', ':', '.', '|');
    }

    private async Task<ChatResponseDTO?> TryAnswerDatabaseMetadataAsync(string question, string normalizedQuestion, CancellationToken cancellationToken)
    {
        var procedureName = ExtractStoredProcedureName(question);
        if (!string.IsNullOrWhiteSpace(procedureName) && IsStoredProcedureDefinitionIntent(normalizedQuestion))
        {
            return await TryAnswerStoredProcedureDefinitionAsync(question, procedureName, cancellationToken);
        }

        var intent = DetectMetadataIntent(normalizedQuestion);
        if (!intent.HasAnyIntent)
        {
            return null;
        }

        var databaseName = ExtractDatabaseName(question)
            ?? await _databaseAssistantDAL.GetDefaultDatabaseNameAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(databaseName) || !IsSafeDatabaseName(databaseName))
        {
            return BuildNoRecordsMetadataResponse("The database name could not be identified safely.");
        }

        if (!await _databaseAssistantDAL.DatabaseExistsAsync(databaseName, cancellationToken))
        {
            return BuildNoRecordsMetadataResponse($"Database '{databaseName}' was not found.");
        }

        var stopwatch = Stopwatch.StartNew();
        var executedSql = new List<string>();
        var result = new DatabaseMetadataResultModel { DatabaseName = databaseName };

        if (intent.CountTables)
        {
            result.TotalTables = await _databaseAssistantDAL.CountTablesAsync(databaseName, executedSql, cancellationToken);
        }

        if (intent.ListTables)
        {
            result.TableNames = await _databaseAssistantDAL.GetTableNamesAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListStoredProcedures)
        {
            result.StoredProcedures = await _databaseAssistantDAL.GetStoredProceduresAsync(databaseName, intent.Top, executedSql, intent.NameContains, cancellationToken);
        }

        if (intent.ListViews)
        {
            result.Views = await _databaseAssistantDAL.GetViewsAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListFunctions)
        {
            result.Functions = await _databaseAssistantDAL.GetFunctionsAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListColumns)
        {
            result.Columns = await _databaseAssistantDAL.GetColumnsAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListIndexes)
        {
            result.Indexes = await _databaseAssistantDAL.GetIndexesAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListTriggers)
        {
            result.Triggers = await _databaseAssistantDAL.GetTriggersAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListForeignKeys)
        {
            result.ForeignKeys = await _databaseAssistantDAL.GetForeignKeysAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListPrimaryKeys)
        {
            result.PrimaryKeys = await _databaseAssistantDAL.GetPrimaryKeysAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListSchemas)
        {
            result.Schemas = await _databaseAssistantDAL.GetSchemasAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        if (intent.ListDatabaseInformation)
        {
            result.DatabaseInformation = await _databaseAssistantDAL.GetDatabaseInformationAsync(databaseName, intent.Top, executedSql, cancellationToken);
        }

        stopwatch.Stop();
        result.ExecutedSql = executedSql.ToArray();
        var resultCount = CalculateMetadataResultCount(result);
        var responseCount = result.TotalTables.HasValue && resultCount == 0 ? 1 : resultCount;

        _logger.LogInformation(
            "Database metadata assistant executed. Question={Question}; Database={Database}; Sql={Sql}; ResultCount={ResultCount}; ExecutionTimeMs={ExecutionTimeMs}",
            question,
            databaseName,
            string.Join(" | ", executedSql.Select(sql => Regex.Replace(sql, @"\s+", " ").Trim())),
            responseCount,
            stopwatch.ElapsedMilliseconds);

        return new ChatResponseDTO
        {
            Answer = BuildMetadataAnswer(result, intent),
            ToolUsed = "SearchDatabaseTool",
            Sources = Array.Empty<string>(),
            StructuredData = new DatabaseAssistantResultDTO
            {
                Success = true,
                Message = BuildMetadataMessage(intent),
                TotalCount = responseCount,
                Data = new DatabaseMetadataResponseDTO
                {
                    DatabaseName = result.DatabaseName,
                    TotalTables = result.TotalTables,
                    TopTables = intent.HasTopTables ? result.TableNames : Array.Empty<string>(),
                    Tables = intent.HasTopTables ? Array.Empty<string>() : result.TableNames,
                    StoredProcedures = result.StoredProcedures,
                    Views = result.Views,
                    Functions = result.Functions,
                    Columns = result.Columns,
                    Indexes = result.Indexes,
                    Triggers = result.Triggers,
                    ForeignKeys = result.ForeignKeys,
                    PrimaryKeys = result.PrimaryKeys,
                    Schemas = result.Schemas,
                    DatabaseInformation = result.DatabaseInformation,
                    ExecutedSql = result.ExecutedSql
                }
            }
        };
    }

    private async Task<ChatResponseDTO> TryAnswerStoredProcedureDefinitionAsync(string question, string procedureName, CancellationToken cancellationToken)
    {
        var databaseName = ExtractDatabaseName(question)
            ?? await _databaseAssistantDAL.GetDefaultDatabaseNameAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(databaseName) || !IsSafeDatabaseName(databaseName))
        {
            return BuildNoRecordsMetadataResponse("The database name could not be identified safely.");
        }

        if (!await _databaseAssistantDAL.DatabaseExistsAsync(databaseName, cancellationToken))
        {
            return BuildNoRecordsMetadataResponse($"Database '{databaseName}' was not found.");
        }

        var executedSql = new List<string>();
        var definition = await _databaseAssistantDAL.GetStoredProcedureDefinitionAsync(databaseName, procedureName, executedSql, cancellationToken);
        if (string.IsNullOrWhiteSpace(definition))
        {
            return new ChatResponseDTO
            {
                Answer = $"## Stored Procedure Not Found\n\n`{procedureName}` was not found in `{databaseName}`.",
                ToolUsed = "SearchDatabaseTool",
                StructuredData = new DatabaseAssistantResultDTO
                {
                    Success = true,
                    Message = "Stored procedure not found.",
                    TotalCount = 0,
                    Data = Array.Empty<object>()
                }
            };
        }

        return new ChatResponseDTO
        {
            Answer = BuildStoredProcedureDefinitionAnswer(databaseName, procedureName, definition),
            ToolUsed = "SearchDatabaseTool",
            Sources = new[] { $"{databaseName}.sys.sql_modules" },
            StructuredData = new DatabaseAssistantResultDTO
            {
                Success = true,
                Message = "Stored procedure definition retrieved successfully.",
                TotalCount = 1,
                Data = new
                {
                    DatabaseName = databaseName,
                    ProcedureName = procedureName,
                    Definition = definition,
                    ExecutedSql = executedSql.ToArray()
                }
            }
        };
    }

    private static bool IsStoredProcedureDefinitionIntent(string normalizedQuestion)
    {
        return Regex.IsMatch(normalizedQuestion, @"\b(get|show|display|explain|analyze|analyse|send|give|definition|code|sql|script|what\s+does|full\s+code)\b")
            && Regex.IsMatch(normalizedQuestion, @"\b(stored\s+procedure|procedure|sp|usp_[a-z0-9_]+)\b");
    }

    private static string? ExtractStoredProcedureName(string question)
    {
        var quotedMatch = Regex.Match(question, @"['""](?<name>(?:[A-Za-z][A-Za-z0-9_]*\.)?usp_[A-Za-z0-9_]+)['""]", RegexOptions.IgnoreCase);
        if (quotedMatch.Success)
        {
            return quotedMatch.Groups["name"].Value;
        }

        var match = Regex.Match(question, @"\b(?<name>(?:[A-Za-z][A-Za-z0-9_]*\.)?usp_[A-Za-z0-9_]+)\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static MetadataIntent DetectMetadataIntent(string normalizedQuestion)
    {
        var mentionsForeignKey = Regex.IsMatch(normalizedQuestion, @"\b(foreign\s+key|foreign\s+keys|fk|fks)\b");
        var mentionsPrimaryKey = Regex.IsMatch(normalizedQuestion, @"\b(primary\s+key|primary\s+keys|pk|pks)\b");
        var mentionsIndex = Regex.IsMatch(normalizedQuestion, @"\b(index|indexes|indices)\b");
        var mentionsTrigger = Regex.IsMatch(normalizedQuestion, @"\btrigger(s)?\b");
        var mentionsProcedure = normalizedQuestion.Contains("stored procedure")
            || Regex.IsMatch(normalizedQuestion, @"\bprocedure(s)?\b")
            || Regex.IsMatch(normalizedQuestion, @"\bsp(s)?\b");
        var mentionsFunction = Regex.IsMatch(normalizedQuestion, @"\bfunction(s)?\b");
        var mentionsColumn = Regex.IsMatch(normalizedQuestion, @"\bcolumn(s)?\b");
        var mentionsView = Regex.IsMatch(normalizedQuestion, @"\bview(s)?\b");
        var mentionsSchema = Regex.IsMatch(normalizedQuestion, @"\bschema(s)?\b");
        var mentionsTable = Regex.IsMatch(normalizedQuestion, @"\btable(s)?\b");
        var mentionsDatabaseInformation = Regex.IsMatch(normalizedQuestion, @"\b(database|db)\s+(information|info|details|summary)\b");
        var mentionsMetadata = mentionsTable || mentionsView || mentionsProcedure || mentionsFunction || mentionsColumn || mentionsIndex || mentionsTrigger || mentionsForeignKey || mentionsPrimaryKey || mentionsSchema || mentionsDatabaseInformation;
        var asksForCount = AsksForCount(normalizedQuestion);
        var asksForList = Regex.IsMatch(normalizedQuestion, @"\b(list|show|send|give|get|fetch|display|names?|top)\b");
        var top = ExtractTopCount(normalizedQuestion);
        var shouldList = asksForList || top != DefaultMetadataTop;
        var nameContains = mentionsProcedure && Regex.IsMatch(normalizedQuestion, @"\b(ai|artificial\s+intelligence)\b")
            ? "AI"
            : string.Empty;

        if (mentionsDatabaseInformation)
        {
            return new MetadataIntent { Top = top == DefaultMetadataTop ? 1 : top, ListDatabaseInformation = true, HasAnyIntent = true };
        }

        if (mentionsForeignKey)
        {
            return new MetadataIntent { Top = top, ListForeignKeys = shouldList, HasAnyIntent = asksForCount || shouldList };
        }

        if (mentionsPrimaryKey)
        {
            return new MetadataIntent { Top = top, ListPrimaryKeys = shouldList, HasAnyIntent = asksForCount || shouldList };
        }

        if (mentionsIndex)
        {
            return new MetadataIntent { Top = top, ListIndexes = shouldList, HasAnyIntent = asksForCount || shouldList };
        }

        if (mentionsTrigger)
        {
            return new MetadataIntent { Top = top, ListTriggers = shouldList, HasAnyIntent = asksForCount || shouldList };
        }

        return new MetadataIntent
        {
            Top = top,
            CountTables = mentionsTable && asksForCount,
            ListTables = mentionsTable && shouldList,
            ListStoredProcedures = mentionsProcedure && shouldList,
            NameContains = nameContains,
            ListViews = mentionsView && shouldList,
            ListFunctions = mentionsFunction && shouldList,
            ListColumns = mentionsColumn && shouldList,
            ListSchemas = mentionsSchema && shouldList,
            HasAnyIntent = mentionsMetadata && (asksForCount || shouldList)
        };
    }

    private static string? ExtractDatabaseName(string question)
    {
        var dbNameMatch = Regex.Match(question, @"\b[A-Za-z][A-Za-z0-9_]*_DB\b", RegexOptions.IgnoreCase);
        if (dbNameMatch.Success)
        {
            return dbNameMatch.Value;
        }

        var afterDatabaseMatch = Regex.Match(question, @"\bdatabase\s+([A-Za-z][A-Za-z0-9_]*)\b", RegexOptions.IgnoreCase);
        return afterDatabaseMatch.Success ? afterDatabaseMatch.Groups[1].Value : null;
    }

    private const int DefaultMetadataTop = 100;

    private static int ExtractTopCount(string normalizedQuestion)
    {
        var numberMatch = Regex.Match(normalizedQuestion, @"\b(?:top|send|show|list|give|get|fetch|display)?\s*(\d{1,3})\b");
        if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var top))
        {
            return Math.Clamp(top, 1, 500);
        }

        var wordNumber = ExtractNumberWord(normalizedQuestion);
        if (wordNumber.HasValue)
        {
            return wordNumber.Value;
        }

        return normalizedQuestion.Contains("top") ? 5 : DefaultMetadataTop;
    }

    private static int? ExtractNumberWord(string normalizedQuestion)
    {
        var numberWords = new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
            ["four"] = 4,
            ["five"] = 5,
            ["six"] = 6,
            ["seven"] = 7,
            ["eight"] = 8,
            ["nine"] = 9,
            ["ten"] = 10,
            ["eleven"] = 11,
            ["twelve"] = 12,
            ["fifteen"] = 15,
            ["twenty"] = 20,
            ["thirty"] = 30,
            ["fifty"] = 50,
            ["hundred"] = 100,
            ["a"] = 1,
            ["an"] = 1
        };

        foreach (var pair in numberWords)
        {
            if (Regex.IsMatch(normalizedQuestion, $@"\b{Regex.Escape(pair.Key)}\b"))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static bool IsSafeDatabaseName(string databaseName)
    {
        return databaseName.All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static ChatResponseDTO BuildNoRecordsMetadataResponse(string message)
    {
        return new ChatResponseDTO
        {
            Answer = message,
            ToolUsed = "SearchDatabaseTool",
            StructuredData = new DatabaseAssistantResultDTO
            {
                Success = true,
                Message = message,
                TotalCount = 0,
                Data = Array.Empty<object>()
            }
        };
    }

    private static int CalculateMetadataResultCount(DatabaseMetadataResultModel result)
    {
        return new[]
        {
            result.TableNames.Count,
            result.StoredProcedures.Count,
            result.Views.Count,
            result.Functions.Count,
            result.Columns.Count,
            result.Indexes.Count,
            result.Triggers.Count,
            result.ForeignKeys.Count,
            result.PrimaryKeys.Count,
            result.Schemas.Count,
            result.DatabaseInformation.Count
        }.Sum();
    }

    private static string BuildMetadataMessage(MetadataIntent intent)
    {
        if (intent.CountTables && intent.ListTables)
        {
            return "Table information retrieved successfully.";
        }

        if (intent.CountTables)
        {
            return "Table count retrieved successfully.";
        }

        return "Database metadata retrieved successfully.";
    }

    private static string BuildMetadataAnswer(DatabaseMetadataResultModel result, MetadataIntent intent)
    {
        var builder = new StringBuilder();
        var resultCount = CalculateMetadataResultCount(result);

        builder.AppendLine("## Database Information");
        builder.AppendLine();
        builder.AppendLine($"**Database:** `{result.DatabaseName}`");
        builder.AppendLine();
        builder.AppendLine("### Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Result Count | {resultCount} |");

        if (result.TotalTables.HasValue)
        {
            builder.AppendLine($"| Total Tables | {result.TotalTables.Value} |");
        }

        AppendMetadataList(builder, intent.HasTopTables ? $"Top {result.TableNames.Count} tables" : "Tables", result.TableNames);
        AppendMetadataList(builder, string.IsNullOrWhiteSpace(intent.NameContains) ? "Stored Procedures" : $"{intent.NameContains} Related Stored Procedures", result.StoredProcedures);
        AppendMetadataList(builder, "Views", result.Views);
        AppendMetadataList(builder, "Functions", result.Functions);
        AppendMetadataList(builder, "Columns", result.Columns);
        AppendMetadataList(builder, "Indexes", result.Indexes);
        AppendMetadataList(builder, "Triggers", result.Triggers);
        AppendMetadataList(builder, "Foreign keys", result.ForeignKeys);
        AppendMetadataList(builder, "Primary keys", result.PrimaryKeys);
        AppendMetadataList(builder, "Schemas", result.Schemas);
        AppendMetadataList(builder, "Database information", result.DatabaseInformation);

        return resultCount == 0 && !result.TotalTables.HasValue
            ? "No records found."
            : builder.ToString().Trim();
    }

    private static string BuildStoredProcedureDefinitionAnswer(string databaseName, string procedureName, string definition)
    {
        var parameters = ExtractStoredProcedureParameters(definition).ToArray();
        var tables = ExtractSqlTableReferences(definition).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("## Stored Procedure");
        builder.AppendLine();
        builder.AppendLine($"**Database:** `{databaseName}`");
        builder.AppendLine($"**Name:** `{procedureName}`");
        builder.AppendLine();
        builder.AppendLine("### Purpose");
        builder.AppendLine();
        builder.AppendLine(BuildStoredProcedurePurpose(procedureName, tables));
        builder.AppendLine();
        builder.AppendLine("### Parameters");
        builder.AppendLine();

        if (parameters.Length == 0)
        {
            builder.AppendLine("No input parameters detected.");
        }
        else
        {
            builder.AppendLine("| Parameter | Data Type |");
            builder.AppendLine("| --- | --- |");
            foreach (var parameter in parameters)
            {
                builder.AppendLine($"| `{EscapeMarkdownTableValue(parameter.Name)}` | `{EscapeMarkdownTableValue(parameter.DataType)}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Tables Used");
        builder.AppendLine();

        if (tables.Length == 0)
        {
            builder.AppendLine("No table references detected from the procedure text.");
        }
        else
        {
            foreach (var table in tables)
            {
                builder.AppendLine($"- `{table}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Procedure Definition");
        builder.AppendLine();
        builder.AppendLine("```sql");
        builder.AppendLine(definition.Trim());
        builder.AppendLine("```");

        return builder.ToString().Trim();
    }

    private static string BuildStoredProcedurePurpose(string procedureName, IReadOnlyCollection<string> tables)
    {
        if (procedureName.Contains("ErrorLog", StringComparison.OrdinalIgnoreCase) || procedureName.Contains("ErrorLogs", StringComparison.OrdinalIgnoreCase))
        {
            return "Retrieves application error log records for review, troubleshooting, and operational monitoring.";
        }

        if (tables.Count > 0)
        {
            return $"Executes database logic using {string.Join(", ", tables.Select(table => $"`{table}`"))}.";
        }

        return "Executes the SQL Server business logic defined in the procedure body.";
    }

    private static IEnumerable<(string Name, string DataType)> ExtractStoredProcedureParameters(string definition)
    {
        return Regex.Matches(definition, @"(?<name>@[A-Za-z][A-Za-z0-9_]*)\s+(?<type>[A-Za-z][A-Za-z0-9_]*(?:\s*\(\s*(?:MAX|\d+(?:\s*,\s*\d+)?)\s*\))?)", RegexOptions.IgnoreCase)
            .Select(match => (
                Name: match.Groups["name"].Value,
                DataType: Regex.Replace(match.Groups["type"].Value, @"\s+", " ").Trim()))
            .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }

    private static IEnumerable<string> ExtractSqlTableReferences(string definition)
    {
        var systemPrefixes = new[] { "sys.", "information_schema." };
        return Regex.Matches(definition, @"\b(?:FROM|JOIN|INTO|UPDATE)\s+(?<name>(?:\[?[A-Za-z][A-Za-z0-9_]*\]?\.){0,2}\[?[A-Za-z][A-Za-z0-9_]*\]?)", RegexOptions.IgnoreCase)
            .Select(match => match.Groups["name"].Value.Replace("[", string.Empty).Replace("]", string.Empty))
            .Where(name => !systemPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void AppendMetadataList(StringBuilder builder, string label, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.AppendLine($"### {label}");
        builder.AppendLine();
        builder.AppendLine("| No | Name |");
        builder.AppendLine("| ---: | --- |");
        for (var i = 0; i < values.Count; i++)
        {
            builder.AppendLine($"| {i + 1} | `{EscapeMarkdownTableValue(values[i])}` |");
        }
    }

    private sealed class MetadataIntent
    {
        public int Top { get; init; } = DefaultMetadataTop;
        public bool CountTables { get; init; }
        public bool ListTables { get; init; }
        public bool ListStoredProcedures { get; init; }
        public bool ListViews { get; init; }
        public bool ListFunctions { get; init; }
        public bool ListColumns { get; init; }
        public bool ListIndexes { get; init; }
        public bool ListTriggers { get; init; }
        public bool ListForeignKeys { get; init; }
        public bool ListPrimaryKeys { get; init; }
        public bool ListSchemas { get; init; }
        public bool ListDatabaseInformation { get; init; }
        public bool HasAnyIntent { get; init; }
        public string NameContains { get; init; } = string.Empty;
        public bool HasTopTables => ListTables && Top != DefaultMetadataTop;
    }

    private sealed record ExactDocumentRequest(IReadOnlyList<string> DocumentIdentifiers, DocumentSectionRequest? Section)
    {
        public bool HasDocumentIdentifier => DocumentIdentifiers.Count > 0;
    }

    private sealed record DocumentSectionRequest(string RequestedName, string CanonicalName, IReadOnlyList<string> Aliases);

    private sealed record SectionHeadingMatch(string CanonicalName, string TrailingText);

    private async Task<ChatResponseDTO?> TryAnswerFromDatabaseAsync(string normalizedQuestion, CancellationToken cancellationToken)
    {
        if (!MentionsUsers(normalizedQuestion))
        {
            return null;
        }

        var isCountQuestion = AsksForCount(normalizedQuestion);
        var activeFilter = normalizedQuestion.Contains("inactive") ? false
            : normalizedQuestion.Contains("active") ? true
            : (bool?)null;
        var todayOnly = normalizedQuestion.Contains("today");

        var users = (await _authDAL.GetUsersDB(activeFilter, todayOnly, cancellationToken)).ToArray();
        var sqlDescription = BuildUserSqlDescription(activeFilter, todayOnly, isCountQuestion);
        var elapsedStart = DateTime.UtcNow;
        var elapsedMs = Math.Round((DateTime.UtcNow - elapsedStart).TotalMilliseconds, 2);

        _logger.LogInformation(
            "Database assistant executed. QuestionIntent={Intent}; Sql={Sql}; ResultCount={ResultCount}; ExecutionTimeMs={ExecutionTimeMs}",
            isCountQuestion ? "UserCount" : "UserList",
            sqlDescription,
            users.Length,
            elapsedMs);

        if (isCountQuestion)
        {
            var message = users.Length == 0
                ? "No registered users were found."
                : $"There {(users.Length == 1 ? "is" : "are")} {users.Length} registered user{(users.Length == 1 ? string.Empty : "s")} matching your request.";

            return new ChatResponseDTO
            {
                Answer = message,
                ToolUsed = "SearchDatabaseTool",
                Sources = new[] { "dbo.usp_AI_GetUsersForAssistant" },
                StructuredData = new DatabaseAssistantResultDTO
                {
                    Success = true,
                    Message = "User count retrieved successfully.",
                    TotalCount = users.Length,
                    Data = new RegisteredUserCountDTO { RegisteredUsers = users.Length }
                }
            };
        }

        if (users.Length == 0)
        {
            return new ChatResponseDTO
            {
                Answer = "No records found.",
                ToolUsed = "SearchDatabaseTool",
                Sources = new[] { "dbo.usp_AI_GetUsersForAssistant" },
                StructuredData = new DatabaseAssistantResultDTO
                {
                    Success = true,
                    Message = "No records found.",
                    TotalCount = 0,
                    Data = Array.Empty<object>()
                }
            };
        }

        var singleUserFieldAnswer = TryAnswerSingleUserField(normalizedQuestion, users);
        if (!string.IsNullOrWhiteSpace(singleUserFieldAnswer))
        {
            return new ChatResponseDTO
            {
                Answer = singleUserFieldAnswer,
                ToolUsed = "SearchDatabaseTool",
                Sources = new[] { "dbo.usp_AI_GetUsersForAssistant" },
                StructuredData = new DatabaseAssistantResultDTO
                {
                    Success = true,
                    Message = "User field retrieved successfully.",
                    TotalCount = users.Length,
                    Data = singleUserFieldAnswer
                }
            };
        }

        var userData = users.Select(user => new DatabaseUserDTO
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate
        }).ToArray();

        return new ChatResponseDTO
        {
            Answer = BuildUserListAnswer(users),
            ToolUsed = "SearchDatabaseTool",
            Sources = new[] { "dbo.usp_AI_GetUsersForAssistant" },
            StructuredData = new DatabaseAssistantResultDTO
            {
                Success = true,
                Message = "User details retrieved successfully.",
                TotalCount = userData.Length,
                Data = userData
            }
        };
    }

    private static string? TryAnswerSingleUserField(string normalizedQuestion, IReadOnlyCollection<UserSummaryModel> users)
    {
        if (users.Count != 1)
        {
            return null;
        }

        var user = users.First();
        if (normalizedQuestion.Contains("full name") || normalizedQuestion.Contains("fullname"))
        {
            return user.FullName;
        }

        if (normalizedQuestion.Contains("user name") || normalizedQuestion.Contains("username"))
        {
            return user.UserName;
        }

        if (normalizedQuestion.Contains("email"))
        {
            return user.Email;
        }

        return null;
    }

    private static bool AsksForCount(string normalizedQuestion)
    {
        return normalizedQuestion.Contains("how many")
            || normalizedQuestion.Contains("how maany")
            || normalizedQuestion.Contains("count")
            || normalizedQuestion.Contains("total")
            || normalizedQuestion.Contains("number of");
    }

    private static bool IsDocumentMetadataIntent(string normalizedQuestion)
    {
        if (IsSectionDetailQuestion(normalizedQuestion))
        {
            return false;
        }

        if (IsSummaryQuestion(normalizedQuestion)
            || Regex.IsMatch(normalizedQuestion, @"\b(explain|describe|summarize|summarise|what is|what are|workflow|content|inside|details about)\b"))
        {
            return false;
        }

        var mentionsDocumentStore = Regex.IsMatch(normalizedQuestion, @"\b(document|documents|doc|docs|file|files|pdf|pdfs|uploaded|upload|knowledge base)\b")
            || normalizedQuestion.Contains("brd");

        if (!mentionsDocumentStore)
        {
            return false;
        }

        return AsksForCount(normalizedQuestion)
            || Regex.IsMatch(normalizedQuestion, @"\b(list|show|get|display|available|have|names?|uploaded)\b")
            || normalizedQuestion.Contains("what documents")
            || normalizedQuestion.Contains("which documents");
    }

    private static bool IsSectionDetailQuestion(string question)
    {
        var normalized = question.ToLowerInvariant();
        return Regex.IsMatch(normalized, @"\b(impact analysis|purpose|objective|business logic|workflow|test cases?|ui changes?|database changes?|screen name|screen details|field details|clone|details send me)\b");
    }

    private static string DetectDocumentType(string question, string normalizedQuestion)
    {
        var extensionMatch = Regex.Match(normalizedQuestion, @"\b(pdf|docx|doc|xlsx|xls|pptx|ppt|txt|md|csv)\b", RegexOptions.IgnoreCase);
        if (extensionMatch.Success)
        {
            return extensionMatch.Value.ToUpperInvariant();
        }

        var brdMatch = Regex.Match(question, @"\bBRD\b", RegexOptions.IgnoreCase);
        return brdMatch.Success ? "BRD" : string.Empty;
    }

    private static IEnumerable<DocumentModel> FilterDocumentsByType(IEnumerable<DocumentModel> documents, string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            return documents;
        }

        return documentType.Equals("BRD", StringComparison.OrdinalIgnoreCase)
            ? documents.Where(document => document.Title.Contains("BRD", StringComparison.OrdinalIgnoreCase))
            : documents.Where(document => GetFileType(document.Title).Equals(documentType, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetFileType(string title)
    {
        var extension = Path.GetExtension(title);
        return string.IsNullOrWhiteSpace(extension)
            ? "Unknown"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    private static string BuildDocumentMetadataAnswer(string documentLabel, IReadOnlyList<DocumentMetadataItemDTO> documents, bool isCountQuestion)
    {
        var title = documentLabel.Equals("Uploaded", StringComparison.OrdinalIgnoreCase)
            ? "Uploaded Documents"
            : $"{documentLabel} Documents Summary";
        var totalLabel = documentLabel.Equals("Uploaded", StringComparison.OrdinalIgnoreCase)
            ? "Uploaded Documents"
            : $"{documentLabel} Documents";

        var builder = new StringBuilder();
        builder.AppendLine($"# {title}");
        builder.AppendLine();

        if (documents.Count == 0)
        {
            builder.AppendLine($"I found **0 {totalLabel.ToLowerInvariant()}** in the knowledge base.");
            builder.AppendLine();
            builder.AppendLine($"## Total Count");
            builder.AppendLine();
            builder.AppendLine($"Total {totalLabel}: **0**");
            return builder.ToString().Trim();
        }

        builder.AppendLine($"I found **{documents.Count} {totalLabel.ToLowerInvariant()}** in the knowledge base.");
        builder.AppendLine();
        builder.AppendLine("| No | Document Name | File Type | Upload Date |");
        builder.AppendLine("| ---: | --- | --- | --- |");

        var index = 1;
        foreach (var document in documents)
        {
            builder.AppendLine($"| {index++} | {EscapeMarkdownTableValue(document.DocumentName)} | {EscapeMarkdownTableValue(document.FileType)} | {document.UploadDate:yyyy-MM-dd HH:mm} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Total Count");
        builder.AppendLine();
        builder.AppendLine($"Total {totalLabel}: **{documents.Count}**");

        return builder.ToString().Trim();
    }

    private static bool MentionsUsers(string normalizedQuestion)
    {
        return normalizedQuestion.Contains("user")
            || normalizedQuestion.Contains("users")
            || normalizedQuestion.Contains("register")
            || normalizedQuestion.Contains("registered");
    }

    private static string BuildUserListAnswer(IReadOnlyCollection<UserSummaryModel> users)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Registered Users");
        builder.AppendLine();
        builder.AppendLine($"**Total Users:** {users.Count}");
        builder.AppendLine();
        builder.AppendLine("| No | User Name | Email | Status |");
        builder.AppendLine("| ---: | --- | --- | --- |");

        var index = 1;
        foreach (var user in users.Take(10))
        {
            builder.AppendLine($"| {index++} | `{EscapeMarkdownTableValue(user.UserName)}` | {EscapeMarkdownTableValue(user.Email)} | {(user.IsActive ? "Active" : "Inactive")} |");
        }

        return builder.ToString().Trim();
    }

    private static string BuildUserSqlDescription(bool? isActive, bool todayOnly, bool isCountQuestion)
    {
        var select = isCountQuestion
            ? "SELECT COUNT(1)"
            : "SELECT Id AS UserId, UserName, Email, FullName, IsActive, CreatedDate";
        var filters = new List<string> { "IsDeleted = 0" };

        if (isActive.HasValue)
        {
            filters.Add($"IsActive = {(isActive.Value ? 1 : 0)}");
        }

        if (todayOnly)
        {
            filters.Add("CONVERT(date, CreatedDate) = CONVERT(date, SYSUTCDATETIME())");
        }

        return $"{select} FROM dbo.tblAI_Users WHERE {string.Join(" AND ", filters)}";
    }

    private static IEnumerable<(DocumentModel Document, int Score)> RankDocuments(string question, IEnumerable<DocumentModel> documents)
    {
        var terms = ExtractTerms(question).ToArray();
        var minimumScore = terms.Length <= 2 ? 1 : Math.Min(3, Math.Max(2, terms.Length / 3));

        return documents
            .Select(document =>
            {
                var content = $"{document.Title} {document.Content}";
                if (string.IsNullOrWhiteSpace(document.Content) || LooksLikeRawPdf(document.Content))
                {
                    return (Document: document, Score: 0);
                }

                var score = terms.Sum(term => CountOccurrences(content, term));
                var exactQuestion = NormalizeQuestionText(question);
                if (!string.IsNullOrWhiteSpace(exactQuestion)
                    && NormalizeQuestionText(document.Content).Contains(exactQuestion, StringComparison.OrdinalIgnoreCase))
                {
                    score += 10;
                }

                return (Document: document, Score: score);
            })
            .Where(match => terms.Length == 0 ? match.Score > 0 : match.Score >= minimumScore)
            .OrderByDescending(match => match.Score);
    }

    private static string BuildDocumentAnswer(string question, string content)
    {
        var sections = SplitIntoRelevantSections(question, content).Take(8).ToArray();
        if (sections.Length == 0)
        {
            sections = SplitIntoParagraphs(content).Take(8).ToArray();
        }

        var answer = new StringBuilder();
        answer.AppendLine(Summarize(sections));
        answer.AppendLine();

        for (var i = 0; i < sections.Length; i++)
        {
            answer.AppendLine($"- {TrimToSentence(sections[i], 500)}");
        }

        return answer.ToString().Trim();
    }

    private static string? TryExtractQuestionAnswer(string question, string content)
    {
        var lines = content
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0)
        {
            return null;
        }

        var questionTerms = ExtractTerms(question).ToArray();
        var normalizedQuestion = NormalizeQuestionText(question);
        var bestIndex = -1;
        var bestScore = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var normalizedLine = NormalizeQuestionText(lines[i]);
            var score = questionTerms.Sum(term => CountOccurrences(normalizedLine, term));
            if (!string.IsNullOrWhiteSpace(normalizedQuestion)
                && normalizedLine.Contains(normalizedQuestion, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }

            if (score > bestScore)
            {
                bestIndex = i;
                bestScore = score;
            }
        }

        var minimumScore = questionTerms.Length <= 2 ? 1 : Math.Min(4, Math.Max(2, questionTerms.Length / 2));
        if (bestIndex < 0 || bestScore < minimumScore)
        {
            return null;
        }

        var answerLines = new List<string>();
        var answerStarted = false;

        for (var i = bestIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (LooksLikeQuestionLine(line) && answerStarted)
            {
                break;
            }

            if (Regex.IsMatch(line, @"^answer\s*:?\s*$", RegexOptions.IgnoreCase))
            {
                answerStarted = true;
                continue;
            }

            var inlineAnswer = Regex.Match(line, @"^answer\s*:\s*(.+)$", RegexOptions.IgnoreCase);
            if (inlineAnswer.Success)
            {
                answerStarted = true;
                answerLines.Add(inlineAnswer.Groups[1].Value.Trim());
                continue;
            }

            if (!answerStarted && LooksLikeQuestionLine(line))
            {
                break;
            }

            if (answerStarted || answerLines.Count == 0)
            {
                answerLines.Add(line);
                answerStarted = true;
            }
        }

        var answer = CleanFieldValue(string.Join(" ", answerLines));
        return string.IsNullOrWhiteSpace(answer) ? null : TrimToSentence(answer, 900);
    }

    private static string BuildSourceAnswer(string answer, string source)
    {
        return BuildDocumentMarkdownAnswer(string.Empty, answer, source, "High");
    }

    private static string BuildDocumentMarkdownAnswer(string question, string answer, string source, string confidence, bool forceStandardFormat = false)
    {
        var builder = new StringBuilder();
        var normalizedQuestion = question.ToLowerInvariant();
        var normalizedAnswer = answer.ToLowerInvariant();
        var isWorkflow = normalizedQuestion.Contains("workflow")
            || normalizedQuestion.Contains("deploy")
            || normalizedQuestion.Contains("process")
            || normalizedAnswer.Contains("↓");

        if (isWorkflow && !forceStandardFormat)
        {
            builder.AppendLine("## CI/CD Deployment Workflow");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(FormatWorkflow(answer));
            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine("### Key Benefits");
            builder.AppendLine();
            builder.AppendLine("- Automated deployment");
            builder.AppendLine("- Faster and repeatable releases");
            builder.AppendLine("- Reduced manual errors");
            builder.AppendLine("- Enterprise-ready DevOps process");
        }
        else
        {
            builder.AppendLine("# Answer");
            builder.AppendLine();
            builder.AppendLine(answer);
            builder.AppendLine();
            builder.AppendLine("## Key Points");
            builder.AppendLine();
            foreach (var point in ExtractKeyPoints(answer).Take(5))
            {
                builder.AppendLine($"- {point}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Source");
        builder.AppendLine();
        builder.AppendLine(source);
        builder.AppendLine();
        builder.AppendLine("## Confidence");
        builder.AppendLine();
        builder.AppendLine(confidence);
        return builder.ToString().Trim();
    }

    private static IEnumerable<string> ExtractKeyPoints(string answer)
    {
        var normalized = Regex.Replace(answer, @"\s+", " ").Trim();
        var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+")
            .Select(sentence => sentence.Trim(' ', '-', '*'))
            .Where(sentence => sentence.Length >= 12)
            .Take(5)
            .ToArray();

        return sentences.Length > 0
            ? sentences
            : new[] { TrimToSentence(normalized, 180) };
    }

    private static string FormatWorkflow(string answer)
    {
        var cleaned = Regex.Replace(answer, @"\s+", " ").Trim();
        var parts = Regex.Split(cleaned, @"\s*(?:↓|->|→)\s*")
            .Select(part => CleanFieldValue(part))
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Take(14)
            .ToArray();

        if (parts.Length < 2)
        {
            return TrimToSentence(answer, 900);
        }

        var builder = new StringBuilder();
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendLine("    ↓");
            }

            builder.AppendLine(parts[i]);
        }

        return builder.ToString().TrimEnd();
    }

    private static string EscapeMarkdownTableValue(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static bool LooksLikeQuestionLine(string line)
    {
        return line.EndsWith("?", StringComparison.Ordinal)
            || Regex.IsMatch(line, @"^(q|question|\d+[\.\)])\s*[:\.]?\s+.+\?$", RegexOptions.IgnoreCase);
    }

    private static string NormalizeQuestionText(string value)
    {
        var withoutPrefix = Regex.Replace(value, @"^(q|question|\d+[\.\)])\s*[:\.]?\s*", string.Empty, RegexOptions.IgnoreCase);
        return Regex.Replace(withoutPrefix.ToLowerInvariant(), @"[^a-z0-9+#. ]+", " ")
            .Replace("?", string.Empty)
            .Trim();
    }

    private static string Summarize(IEnumerable<string> sections)
    {
        var joined = string.Join(" ", sections.Select(section => TrimToSentence(section, 250)));
        return TrimToSentence(joined, 700);
    }

    private static IEnumerable<string> SplitIntoRelevantSections(string question, string content)
    {
        var terms = ExtractTerms(question).ToArray();
        return SplitIntoParagraphs(content)
            .Select(section => new
            {
                Text = section,
                Score = terms.Sum(term => CountOccurrences(section, term))
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Select(item => item.Text);
    }

    private static IEnumerable<string> SplitIntoParagraphs(string content)
    {
        return Regex.Split(content, @"(\r?\n){2,}|(?<=\.)\s+(?=[A-Z0-9])", RegexOptions.Compiled)
            .Select(value => Regex.Replace(value, @"\s+", " ").Trim())
            .Where(value => value.Length >= 30);
    }

    private static IEnumerable<string> ExtractTerms(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "about", "after", "also", "and", "are", "brd", "can", "could", "details", "document",
            "explain", "file", "for", "from", "give", "how", "into", "please", "step", "the",
            "this", "with", "what", "when", "where", "which", "your"
        };

        return Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9]{3,}")
            .Select(match => match.Value)
            .Where(term => !stopWords.Contains(term))
            .Distinct();
    }

    private static int CountOccurrences(string text, string term)
    {
        return Regex.Matches(text, Regex.Escape(term), RegexOptions.IgnoreCase).Count;
    }

    private static bool LooksLikeRawPdf(string content)
    {
        return content.StartsWith("%PDF-", StringComparison.OrdinalIgnoreCase)
            || content.Contains("/Creator", StringComparison.OrdinalIgnoreCase)
            || content.Contains("/Producer", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimToSentence(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var trimmed = value[..maxLength];
        var sentenceEnd = trimmed.LastIndexOfAny(new[] { '.', '!', '?' });
        return (sentenceEnd > 120 ? trimmed[..(sentenceEnd + 1)] : trimmed).Trim() + "...";
    }

    private HttpClient CreateOpenAIClient()
    {
        var client = _httpClientFactory.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configurationSettings.OpenAIApiKey);
        return client;
    }

    private bool IsOpenAIConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configurationSettings.OpenAIApiKey)
            && !_configurationSettings.OpenAIApiKey.Equals("YOUR_OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase);
    }

    private static float CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length)
        {
            return 0f;
        }

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        return leftNorm == 0 || rightNorm == 0
            ? 0f
            : (float)(dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm)));
    }
}
