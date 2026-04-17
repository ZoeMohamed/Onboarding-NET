using TaskManagement.API.Common.Constants;
using TaskManagement.API.Common.Exceptions;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using TaskManagement.API.Repositories;

namespace TaskManagement.API.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<TaskResponseDto>> GetAllAsync()
    {
        var tasks = await _repository.GetAllAsync();
        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskResponseDto> GetByIdAsync(Guid id)
    {
        var task = await GetTaskOrThrowAsync(id);
        return MapToDto(task);
    }

    public async Task<List<ApprovalLogResponseDto>> GetApprovalLogsAsync(Guid taskId)
    {
        await GetTaskOrThrowAsync(taskId);

        var logs = await _repository.GetApprovalLogsByTaskIdAsync(taskId);
        return logs.Select(MapApprovalLogToDto).ToList();
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, Guid userId)
    {
        ValidateCreateInput(dto);

        await EnsureUserExistsAsync(userId, "Creator user was not found.");

        if (dto.AssignedTo.HasValue)
        {
            await EnsureUserExistsAsync(dto.AssignedTo.Value, "Assigned user was not found.");
        }

        var task = new UserTask
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Priority = NormalizePriority(dto.Priority),
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            CreatedBy = userId,
            Status = TaskStatuses.Pending
        };

        var created = await _repository.CreateAsync(task);
        _logger.LogInformation("Task created: {TaskId} by user {UserId}", created.Id, userId);

        return MapToDto(created);
    }

    public async Task<TaskResponseDto> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var existing = await GetTaskOrThrowAsync(id);
        ValidateUpdateInput(dto);

        if (dto.AssignedTo.HasValue)
        {
            await EnsureUserExistsAsync(dto.AssignedTo.Value, "Assigned user was not found.");
            existing.AssignedTo = dto.AssignedTo;
        }

        existing.Title = dto.Title?.Trim() ?? existing.Title;
        existing.Description = dto.Description?.Trim() ?? existing.Description;
        existing.Priority = dto.Priority is null ? existing.Priority : NormalizePriority(dto.Priority);
        existing.DueDate = dto.DueDate ?? existing.DueDate;

        var updated = await _repository.UpdateAsync(id, existing)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        _logger.LogInformation("Task updated: {TaskId}", id);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"Task with id '{id}' was not found.");
        }

        _logger.LogInformation("Task deleted: {TaskId}", id);
    }

    public async Task<TaskResponseDto> StartAsync(Guid id, Guid userId)
    {
        var task = await GetTaskOrThrowAsync(id);
        EnsureTaskActor(task, userId);
        EnsureStatus(task, TaskStatuses.Pending, "Only tasks with status 'Pending' can be started.");

        task.Status = TaskStatuses.InProgress;
        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        _logger.LogInformation("Task started: {TaskId} by user {UserId}", id, userId);
        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> CompleteAsync(Guid id, Guid userId)
    {
        var task = await GetTaskOrThrowAsync(id);
        EnsureTaskActor(task, userId);
        EnsureStatus(task, TaskStatuses.InProgress, "Only tasks with status 'InProgress' can be completed.");

        task.Status = TaskStatuses.Completed;
        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        _logger.LogInformation("Task completed: {TaskId} by user {UserId}", id, userId);
        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> ApproveAsync(Guid id, Guid reviewerId, string? note)
    {
        var task = await GetTaskOrThrowAsync(id);

        if (task.CreatedBy == reviewerId)
        {
            throw new BusinessRuleException("Creator cannot approve their own task.");
        }

        EnsureStatus(task, TaskStatuses.Completed, "Only tasks with status 'Completed' can be approved.");
        task.Status = TaskStatuses.Approved;

        await _repository.AddApprovalLogAsync(new ApprovalLog
        {
            TaskId = task.Id,
            ReviewedBy = reviewerId,
            Action = ApprovalActions.Approved,
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        _logger.LogInformation("Task approved: {TaskId} by reviewer {ReviewerId}", id, reviewerId);
        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> RejectAsync(Guid id, Guid reviewerId, string? note)
    {
        var task = await GetTaskOrThrowAsync(id);

        if (task.CreatedBy == reviewerId)
        {
            throw new BusinessRuleException("Creator cannot reject their own task.");
        }

        EnsureStatus(task, TaskStatuses.Completed, "Only tasks with status 'Completed' can be rejected.");
        task.Status = TaskStatuses.Rejected;

        await _repository.AddApprovalLogAsync(new ApprovalLog
        {
            TaskId = task.Id,
            ReviewedBy = reviewerId,
            Action = ApprovalActions.Rejected,
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        _logger.LogInformation("Task rejected: {TaskId} by reviewer {ReviewerId}", id, reviewerId);
        return MapToDto(updated);
    }

    private async Task<UserTask> GetTaskOrThrowAsync(Guid taskId)
    {
        return await _repository.GetByIdAsync(taskId)
            ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");
    }

    private async Task EnsureUserExistsAsync(Guid userId, string errorMessage)
    {
        if (!await _repository.UserExistsAsync(userId))
        {
            throw new ValidationException(errorMessage);
        }
    }

    private static void ValidateCreateInput(CreateTaskDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            errors.Add("Title is required.");
        }

        if (!IsPriorityValid(dto.Priority))
        {
            errors.Add("Priority must be one of: Low, Medium, High.");
        }

        if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow)
        {
            errors.Add("DueDate cannot be in the past.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Task creation validation failed.", errors);
        }
    }

    private static void ValidateUpdateInput(UpdateTaskDto dto)
    {
        var errors = new List<string>();

        if (dto.Title is not null && string.IsNullOrWhiteSpace(dto.Title))
        {
            errors.Add("Title cannot be empty.");
        }

        if (dto.Priority is not null && !IsPriorityValid(dto.Priority))
        {
            errors.Add("Priority must be one of: Low, Medium, High.");
        }

        if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow)
        {
            errors.Add("DueDate cannot be in the past.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Task update validation failed.", errors);
        }
    }

    private static bool IsPriorityValid(string? priority)
    {
        return !string.IsNullOrWhiteSpace(priority) && TaskPriorities.Allowed.Contains(priority);
    }

    private static string NormalizePriority(string priority)
    {
        return priority.Trim().ToLowerInvariant() switch
        {
            "low" => TaskPriorities.Low,
            "medium" => TaskPriorities.Medium,
            "high" => TaskPriorities.High,
            _ => priority
        };
    }

    private static void EnsureTaskActor(UserTask task, Guid userId)
    {
        var isCreator = task.CreatedBy == userId;
        var isAssignee = task.AssignedTo.HasValue && task.AssignedTo.Value == userId;

        if (!isCreator && !isAssignee)
        {
            throw new ForbiddenException("Only creator or assignee can perform this action.");
        }
    }

    private static void EnsureStatus(UserTask task, string requiredStatus, string errorMessage)
    {
        if (!task.Status.Equals(requiredStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(errorMessage);
        }
    }

    private static TaskResponseDto MapToDto(UserTask task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        CreatedBy = task.CreatedBy,
        AssignedTo = task.AssignedTo,
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    private static ApprovalLogResponseDto MapApprovalLogToDto(ApprovalLog log) => new()
    {
        Id = log.Id,
        TaskId = log.TaskId,
        ReviewedBy = log.ReviewedBy,
        Action = log.Action,
        Note = log.Note,
        CreatedAt = log.CreatedAt
    };
}
