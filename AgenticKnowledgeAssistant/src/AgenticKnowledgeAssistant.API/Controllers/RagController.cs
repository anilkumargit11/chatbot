using System.Security.Claims;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/rag")]
public sealed class RagController(IRagService ragService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var response = await ragService.UploadAsync(file, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost("index")]
    public async Task<IActionResult> Index(RagIndexRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await ragService.IndexAsync(request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(RagSearchRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await ragService.SearchAsync(request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(RagChatRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await ragService.ChatAsync(request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet("document/{id:long}")]
    public async Task<IActionResult> GetDocument(long id, CancellationToken cancellationToken)
    {
        var response = await ragService.GetDocumentAsync(id, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpDelete("document/{id:long}")]
    public async Task<IActionResult> DeleteDocument(long id, CancellationToken cancellationToken)
    {
        var response = await ragService.DeleteDocumentAsync(id, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue("uid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("UserId");

        return int.TryParse(value, out var userId) && userId > 0 ? userId : 0;
    }

    private static int ToHttpStatusCode(int returnCode)
        => returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
}
