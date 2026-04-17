using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.API.Common.Constants;
using TaskManagement.API.Common;
using TaskManagement.API.Data;
using TaskManagement.API.Middlewares;
using TaskManagement.API.Models;
using TaskManagement.API.Repositories;
using TaskManagement.API.Services;

var builder = WebApplication.CreateBuilder(args);
var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT key is missing.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"Invalid value for '{entry.Key}'."
                    : error.ErrorMessage))
            .ToList();

        var response = ApiResponseFactory.Error<object?>("Validation failed", errors);
        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(
                    ApiResponseFactory.Error<object?>(
                        "Unauthorized",
                        new[] { "A valid bearer token is required." }),
                    serializerOptions);

                await context.Response.WriteAsync(payload);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(
                    ApiResponseFactory.Error<object?>(
                        "Forbidden",
                        new[] { "You do not have permission to access this resource." }),
                    serializerOptions);

                await context.Response.WriteAsync(payload);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReviewTask", policy =>
        policy.RequireRole(UserRoles.Admin, UserRoles.Manager));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    SeedUser(
        dbContext,
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "user",
        "user@local.dev",
        "User@123!",
        UserRoles.User);

    SeedUser(
        dbContext,
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "admin",
        "admin@local.dev",
        "Admin@123!",
        UserRoles.Admin);

    SeedUser(
        dbContext,
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "manager",
        "manager@local.dev",
        "Manager@123!",
        UserRoles.Manager);
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

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void SeedUser(AppDbContext dbContext, Guid id, string username, string email, string password, string role)
{
    var user = dbContext.Users.FirstOrDefault(u => u.Id == id);

    if (user == null)
    {
        dbContext.Users.Add(new User
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
        return;
    }

    user.Username = username;
    user.Email = email;
    user.Role = role;

    if (string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    dbContext.SaveChanges();
}
