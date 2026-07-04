using AgenticKnowledgeAssistant.API.Services;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/[action]")]
public sealed class DocumentController(IDocumentBAL documentBAL, BufferedCodeLogger codeLog) : ControllerBase
{
    [HttpPost]
    [HttpPost("/api/document/upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file, CancellationToken cancellationToken)
    {
        codeLog.Code("Step 1", "Starting UploadDocument BAL calling - Request", file?.FileName ?? string.Empty);
        var response = await documentBAL.UploadDocument(file, cancellationToken);
        codeLog.Code("Step 2", "Ending UploadDocument BAL calling - Response", response);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet]
    [HttpGet("/api/document")]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var response = await documentBAL.GetDocuments(cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet]
    [HttpGet("/api/document/search")]
    public async Task<IActionResult> SearchDocuments([FromQuery] string q, CancellationToken cancellationToken)
    {
        var response = await documentBAL.SearchDocuments(q, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpDelete]
    [HttpDelete("/api/document/{id:int}")]
    public async Task<IActionResult> DeleteDocument([FromRoute] int id, CancellationToken cancellationToken)
    {
        var response = await documentBAL.DeleteDocument(id, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [AllowAnonymous]
    [HttpGet]
    [HttpGet("/api/document/health")]
    public IActionResult DocumentHealth()
    {
        return Ok(new { status = "healthy", service = "document" });
    }

    private static int ToHttpStatusCode(int returnCode)
    {
        return returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
    }
}
