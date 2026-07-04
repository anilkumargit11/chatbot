using AgenticKnowledgeAssistant.API.Services;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/[action]")]
public sealed class ChatController(IChatBAL chatBAL, BufferedCodeLogger codeLog) : ControllerBase
{
    [HttpPost("/api/chat")]
    public async Task<IActionResult> Chat(ChatRequestDTO request, CancellationToken cancellationToken)
    {
        request.UserId = GetCurrentUserId();
        codeLog.Code("Step 1", "Starting Chat BAL calling - Request", RedactForLogging(request));
        var response = await chatBAL.Chat(request, cancellationToken);
        codeLog.Code("Step 2", "Ending Chat BAL calling - Response", response);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost("/api/chat/session")]
    public async Task<IActionResult> CreateSession(
        [FromServices] IConversationMemoryService memoryService,
        [FromBody] CreateChatSessionRequestDTO request,
        CancellationToken cancellationToken)
    {
        var session = await memoryService.CreateSessionAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(session));
    }

    [HttpGet("/api/chat/session/{id:guid}")]
    public async Task<IActionResult> GetSession(
        [FromServices] IConversationMemoryService memoryService,
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await memoryService.GetSessionAsync(id, GetCurrentUserId(), cancellationToken);
        return session is null ? NotFound(Failure("Conversation not found.")) : Ok(Success(session));
    }

    [HttpGet("/api/chat/sessions")]
    public async Task<IActionResult> SearchSessions(
        [FromServices] IConversationMemoryService memoryService,
        [FromQuery] ConversationSearchRequestDTO request,
        CancellationToken cancellationToken)
    {
        var sessions = await memoryService.SearchSessionsAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(sessions, sessions.Count));
    }

    [HttpPatch("/api/chat/session/{id:guid}")]
    public async Task<IActionResult> UpdateSession(
        [FromServices] IConversationMemoryService memoryService,
        Guid id,
        [FromBody] UpdateChatSessionRequestDTO request,
        CancellationToken cancellationToken)
    {
        var updated = await memoryService.UpdateSessionAsync(id, GetCurrentUserId(), request, cancellationToken);
        return updated ? Ok(Success(new { sessionGuid = id })) : NotFound(Failure("Conversation not found."));
    }

    [HttpPost("/api/chat/message")]
    public async Task<IActionResult> SaveMessage(
        [FromServices] IConversationMemoryService memoryService,
        [FromBody] SaveChatMessageRequestDTO request,
        CancellationToken cancellationToken)
    {
        var message = await memoryService.SaveMessageAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(message));
    }

    [HttpGet("/api/chat/history/{sessionId:guid}")]
    public async Task<IActionResult> GetMessages(
        [FromServices] IConversationMemoryService memoryService,
        Guid sessionId,
        [FromQuery] int skip,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var messages = await memoryService.GetMessagesAsync(sessionId, GetCurrentUserId(), skip, take <= 0 ? 50 : take, cancellationToken);
        return Ok(Success(messages, messages.Count));
    }

    [HttpDelete("/api/chat/session/{id:guid}")]
    public async Task<IActionResult> DeleteSession(
        [FromServices] IConversationMemoryService memoryService,
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await memoryService.DeleteSessionAsync(id, GetCurrentUserId(), cancellationToken);
        return deleted ? Ok(Success(new { sessionGuid = id })) : NotFound(Failure("Conversation not found."));
    }

    [AllowAnonymous]
    [HttpGet]
    [HttpGet("/api/chat/health")]
    public IActionResult ChatHealth()
    {
        return Ok(new { status = "healthy", service = "chat" });
    }

    private static int ToHttpStatusCode(int returnCode)
    {
        return returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue("uid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("UserId");

        return int.TryParse(value, out var userId) && userId > 0 ? userId : 0;
    }

    private static Response<object> Success(object data, int totalCount = 1)
        => new()
        {
            ReturnCode = StatusCodes.Status200OK,
            ReturnMessage = "success",
            Data = data
        };

    private static Response<object> Failure(string message)
        => new()
        {
            ReturnCode = StatusCodes.Status404NotFound,
            ReturnMessage = message,
            Data = null
        };

    private static object RedactForLogging(ChatRequestDTO request)
        => new
        {
            request.Question,
            request.CountryCode,
            request.CurrencyCode,
            request.LanguageCode,
            request.Mode,
            request.SessionGuid,
            AttachmentBase64 = string.IsNullOrWhiteSpace(request.AttachmentBase64) ? null : $"[redacted:{request.AttachmentBase64.Length} chars]",
            request.AttachmentName,
            Attachments = request.Attachments.Select(attachment => new
            {
                attachment.FileName,
                attachment.ContentType,
                attachment.Size,
                Base64Content = string.IsNullOrWhiteSpace(attachment.Base64Content) ? null : $"[redacted:{attachment.Base64Content.Length} chars]"
            }).ToArray(),
            request.TargetLanguage,
            request.UserId
        };
}
