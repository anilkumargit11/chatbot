using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/[action]")]
public sealed class ToolsController : ControllerBase
{
    [HttpGet]
    [HttpGet("/api/tools/date")]
    public IActionResult GetCurrentDate()
    {
        var now = DateTime.UtcNow;
        return Ok(new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = "success",
            Data = new
            {
                date = now.ToString("o"),
                unixTimestamp = new DateTimeOffset(now).ToUnixTimeSeconds()
            }
        });
    }

    [HttpGet]
    [HttpGet("/api/tools/search-files")]
    public IActionResult SearchFiles([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.InvalidRequest,
                ReturnMessage = "Query is required"
            });
        }

        return Ok(new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = "success",
            Data = new
            {
                query,
                results = new List<object>
                {
                    new { fileName = "document1.pdf", path = "/documents/document1.pdf", relevance = 0.95 },
                    new { fileName = "document2.docx", path = "/documents/document2.docx", relevance = 0.87 }
                }
            }
        });
    }

    [HttpGet]
    [HttpGet("/api/tools/search-database")]
    public IActionResult SearchDatabase([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.InvalidRequest,
                ReturnMessage = "Query is required"
            });
        }

        return Ok(new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = "success",
            Data = new
            {
                query,
                results = new List<object>
                {
                    new { id = 1, title = "Invoice #001", amount = 1500.00, date = "2024-01-15" },
                    new { id = 2, title = "Invoice #002", amount = 2500.00, date = "2024-02-20" }
                }
            }
        });
    }

    [AllowAnonymous]
    [HttpGet]
    [HttpGet("/api/tools/health")]
    public IActionResult ToolsHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
