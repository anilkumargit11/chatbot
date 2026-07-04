using System.Security.Claims;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/memory")]
public sealed class MemoryController(ILongTermMemoryService memoryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SaveMemory(SaveMemoryRequestDTO request, CancellationToken cancellationToken)
    {
        var memory = await memoryService.SaveMemoryAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(memory));
    }

    [HttpGet]
    public async Task<IActionResult> GetMemories([FromQuery] MemorySearchRequestDTO request, CancellationToken cancellationToken)
    {
        var memories = await memoryService.SearchMemoriesAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(memories));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await memoryService.GetCategoriesAsync(cancellationToken);
        return Ok(Success(categories));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMemory([FromQuery] MemorySearchRequestDTO request, CancellationToken cancellationToken)
    {
        var memories = await memoryService.SearchMemoriesAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(Success(memories));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetMemory(long id, CancellationToken cancellationToken)
    {
        var memory = await memoryService.GetMemoryAsync(id, GetCurrentUserId(), cancellationToken);
        return memory is null ? NotFound(Failure("Memory not found.")) : Ok(Success(memory));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateMemory(long id, UpdateMemoryRequestDTO request, CancellationToken cancellationToken)
    {
        var updated = await memoryService.UpdateMemoryAsync(id, GetCurrentUserId(), request, cancellationToken);
        return updated ? Ok(Success(new { memoryId = id })) : NotFound(Failure("Memory not found."));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteMemory(long id, CancellationToken cancellationToken)
    {
        var deleted = await memoryService.DeleteMemoryAsync(id, GetCurrentUserId(), cancellationToken);
        return deleted ? Ok(Success(new { memoryId = id })) : NotFound(Failure("Memory not found."));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue("uid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("UserId");

        return int.TryParse(value, out var userId) && userId > 0 ? userId : 0;
    }

    private static Response<object> Success(object data)
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
}
