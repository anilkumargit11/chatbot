using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Route("api/[action]")]
public sealed class StatusController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/api/status")]
    public IActionResult Status()
    {
        return Ok(new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = "success",
            Data = new StatusResponseDTO
            {
                Name = "Agentic Knowledge Assistant API",
                Version = "1.0.0",
                Status = "operational",
                Timestamp = DateTime.UtcNow,
                Endpoints = new
                {
                    chat = "/api/chat",
                    document = "/api/document",
                    auth = "/api/auth/token",
                    tools = "/api/tools"
                }
            }
        });
    }

    [AllowAnonymous]
    [HttpGet("/api/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
