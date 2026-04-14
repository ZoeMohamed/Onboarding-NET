using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Models;

namespace TaskManagement.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserTask> Tasks => Set<UserTask>();
    public DbSet<ApprovalLog> ApprovalLogs => Set<ApprovalLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTask>()
            .HasOne(t => t.Creator)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserTask>()
            .HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApprovalLog>()
            .HasOne(a => a.Task)
            .WithMany(t => t.ApprovalLogs)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApprovalLog>()
            .HasOne(a => a.Reviewer)
            .WithMany(u => u.ApprovalLogs)
            .HasForeignKey(a => a.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
