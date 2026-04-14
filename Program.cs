using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.Repositories;
using TaskManagement.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();
var bootstrapUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Users.Any(u => u.Id == bootstrapUserId))
    {
        dbContext.Users.Add(new User
        {
            Id = bootstrapUserId,
            Username = "bootstrap-user",
            Email = "bootstrap@local.dev",
            PasswordHash = string.Empty,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var hasHttpsBinding = (builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty)
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (!app.Environment.IsDevelopment() || hasHttpsBinding)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
