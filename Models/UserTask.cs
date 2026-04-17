using TaskManagement.API.Common.Constants;

namespace TaskManagement.API.Models;

public class UserTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = TaskStatuses.Pending;
    public string Priority { get; set; } = TaskPriorities.Medium;
    public Guid CreatedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
    public User? Assignee { get; set; }
    public ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();
}
