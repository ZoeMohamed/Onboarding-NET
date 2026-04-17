using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.Models;

namespace TaskManagement.API.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserTask>> GetAllAsync()
    {
        return await _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserTask?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .Include(t => t.ApprovalLogs)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<UserTask> CreateAsync(UserTask task)
    {
        task.Id = Guid.NewGuid();
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<UserTask?> UpdateAsync(Guid id, UserTask updated)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return null;

        task.Title = updated.Title;
        task.Description = updated.Description;
        task.Priority = updated.Priority;
        task.Status = updated.Status;
        task.AssignedTo = updated.AssignedTo;
        task.DueDate = updated.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<ApprovalLog> AddApprovalLogAsync(ApprovalLog log)
    {
        log.Id = Guid.NewGuid();
        _context.ApprovalLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<List<ApprovalLog>> GetApprovalLogsByTaskIdAsync(Guid taskId)
    {
        return await _context.ApprovalLogs
            .Where(log => log.TaskId == taskId)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();
    }
}
