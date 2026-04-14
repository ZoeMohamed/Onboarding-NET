using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using TaskManagement.API.Repositories;

namespace TaskManagement.API.Services;

public class TaskService : ITaskService
{
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

    public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        return task == null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, Guid userId)
    {
        var task = new UserTask
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            CreatedBy = userId,
            Status = "Pending"
        };

        var created = await _repository.CreateAsync(task);
        return MapToDto(created);
    }

    public async Task<TaskResponseDto?> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return null;

        existing.Title = dto.Title ?? existing.Title;
        existing.Description = dto.Description ?? existing.Description;
        existing.Priority = dto.Priority ?? existing.Priority;
        existing.AssignedTo = dto.AssignedTo ?? existing.AssignedTo;
        existing.DueDate = dto.DueDate ?? existing.DueDate;

        var updated = await _repository.UpdateAsync(id, existing);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
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
