using TaskManagement.API.Common.Exceptions;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using TaskManagement.API.Repositories;

namespace TaskManagement.API.Services;

public class TaskService : ITaskService
{
    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High"
    };

    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TaskResponseDto>> GetAllAsync()
    {
        var tasks = await _repository.GetAllAsync();
        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskResponseDto> GetByIdAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, Guid userId)
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
            Status = "Pending"
        };

        var created = await _repository.CreateAsync(task);
        return MapToDto(created);
    }

    public async Task<TaskResponseDto> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

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

        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"Task with id '{id}' was not found.");
        }
    }

    public async Task<TaskResponseDto> StartAsync(Guid id, Guid userId)
    {
        var task = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        EnsureTaskActor(task, userId);

        if (!task.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only tasks with status 'Pending' can be started.");
        }

        task.Status = "InProgress";
        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> CompleteAsync(Guid id, Guid userId)
    {
        var task = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        EnsureTaskActor(task, userId);

        if (!task.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only tasks with status 'InProgress' can be completed.");
        }

        task.Status = "Completed";
        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> ApproveAsync(Guid id, Guid reviewerId, string? note)
    {
        var task = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        if (task.CreatedBy == reviewerId)
        {
            throw new BusinessRuleException("Creator cannot approve their own task.");
        }

        if (!task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only tasks with status 'Completed' can be approved.");
        }

        task.Status = "Approved";

        await _repository.AddApprovalLogAsync(new ApprovalLog
        {
            TaskId = task.Id,
            ReviewedBy = reviewerId,
            Action = "Approved",
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToDto(updated);
    }

    public async Task<TaskResponseDto> RejectAsync(Guid id, Guid reviewerId, string? note)
    {
        var task = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        if (task.CreatedBy == reviewerId)
        {
            throw new BusinessRuleException("Creator cannot reject their own task.");
        }

        if (!task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only tasks with status 'Completed' can be rejected.");
        }

        task.Status = "Rejected";

        await _repository.AddApprovalLogAsync(new ApprovalLog
        {
            TaskId = task.Id,
            ReviewedBy = reviewerId,
            Action = "Rejected",
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        var updated = await _repository.UpdateAsync(id, task)
            ?? throw new NotFoundException($"Task with id '{id}' was not found.");

        return MapToDto(updated);
    }

    private async Task EnsureUserExistsAsync(Guid userId, string errorMessage)
    {
        if (!await _repository.UserExistsAsync(userId))
        {
            throw new ValidationException(errorMessage);
        }
    }

    private static bool IsPriorityValid(string? priority)
    {
        return !string.IsNullOrWhiteSpace(priority) && AllowedPriorities.Contains(priority);
    }

    private static string NormalizePriority(string priority)
    {
        return priority.Trim().ToLowerInvariant() switch
        {
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
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
}
