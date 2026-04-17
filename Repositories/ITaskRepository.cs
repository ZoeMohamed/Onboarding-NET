using TaskManagement.API.Models;

namespace TaskManagement.API.Repositories;

public interface ITaskRepository
{
    Task<List<UserTask>> GetAllAsync();
    Task<UserTask?> GetByIdAsync(Guid id);
    Task<UserTask> CreateAsync(UserTask task);
    Task<UserTask?> UpdateAsync(Guid id, UserTask task);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UserExistsAsync(Guid userId);
    Task<ApprovalLog> AddApprovalLogAsync(ApprovalLog log);
    Task<List<ApprovalLog>> GetApprovalLogsByTaskIdAsync(Guid taskId);
}
