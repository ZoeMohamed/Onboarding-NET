namespace TaskManagement.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserTask> CreatedTasks { get; set; } = new List<UserTask>();
    public ICollection<UserTask> AssignedTasks { get; set; } = new List<UserTask>();
    public ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();
}
