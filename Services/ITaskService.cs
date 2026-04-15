using TaskManagement.API.DTOs;

namespace TaskManagement.API.Services;

public interface ITaskService
{
    Task<List<TaskResponseDto>> GetAllAsync();
    Task<TaskResponseDto> GetByIdAsync(Guid id);
    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, Guid userId);
    Task<TaskResponseDto> UpdateAsync(Guid id, UpdateTaskDto dto);
    Task DeleteAsync(Guid id);
    Task<TaskResponseDto> StartAsync(Guid id, Guid userId);
    Task<TaskResponseDto> CompleteAsync(Guid id, Guid userId);
    Task<TaskResponseDto> ApproveAsync(Guid id, Guid reviewerId, string? note);
    Task<TaskResponseDto> RejectAsync(Guid id, Guid reviewerId, string? note);
}
