using TaskManagement.API.DTOs;

namespace TaskManagement.API.Services;

public interface ITaskService
{
    Task<List<TaskResponseDto>> GetAllAsync();
    Task<TaskResponseDto?> GetByIdAsync(Guid id);
    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, Guid userId);
    Task<TaskResponseDto?> UpdateAsync(Guid id, UpdateTaskDto dto);
    Task<bool> DeleteAsync(Guid id);
}
