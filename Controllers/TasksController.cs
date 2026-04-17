using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Common;
using TaskManagement.API.DTOs;
using TaskManagement.API.Services;

namespace TaskManagement.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _service;

    public TasksController(ITaskService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _service.GetAllAsync();
        return Ok(ApiResponseFactory.Success(tasks, "Tasks fetched successfully"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _service.GetByIdAsync(id);
        return Ok(ApiResponseFactory.Success(task, "Task fetched successfully"));
    }

    [HttpGet("{id}/approvals")]
    public async Task<IActionResult> GetApprovalLogs(Guid id)
    {
        var logs = await _service.GetApprovalLogsAsync(id);
        return Ok(ApiResponseFactory.Success(logs, "Approval logs fetched successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var userId = GetCurrentUserId();
        var task = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = task.Id },
            ApiResponseFactory.Success(task, "Task created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        var task = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponseFactory.Success(task, "Task updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponseFactory.Success(new { id }, "Task deleted successfully"));
    }

    [HttpPatch("{id}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        var userId = GetCurrentUserId();
        var task = await _service.StartAsync(id, userId);
        return Ok(ApiResponseFactory.Success(task, "Task started"));
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var userId = GetCurrentUserId();
        var task = await _service.CompleteAsync(id, userId);
        return Ok(ApiResponseFactory.Success(task, "Task marked as completed"));
    }

    [Authorize(Policy = "CanReviewTask")]
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewTaskDto dto)
    {
        var reviewerId = GetCurrentUserId();
        var task = await _service.ApproveAsync(id, reviewerId, dto.Note);
        return Ok(ApiResponseFactory.Success(task, "Task approved"));
    }

    [Authorize(Policy = "CanReviewTask")]
    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewTaskDto dto)
    {
        var reviewerId = GetCurrentUserId();
        var task = await _service.RejectAsync(id, reviewerId, dto.Note);
        return Ok(ApiResponseFactory.Success(task, "Task rejected"));
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("User identity is invalid.");
        }

        return userId;
    }
}
