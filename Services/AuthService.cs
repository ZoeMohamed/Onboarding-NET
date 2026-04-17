using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.API.Common.Constants;
using TaskManagement.API.Common.Exceptions;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;

namespace TaskManagement.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim();

        ValidateRegistrationInput(dto, username, email);

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());

        if (usernameExists)
        {
            _logger.LogWarning("Registration rejected because username already exists: {Username}", username);
            throw new ValidationException("Registration failed.", new[] { "Username is already used." });
        }

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());

        if (emailExists)
        {
            _logger.LogWarning("Registration rejected because email already exists: {Email}", email);
            throw new ValidationException("Registration failed.", new[] { "Email is already used." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRoles.User,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {UserId} ({Username})", user.Id, user.Username);

        return new RegisterResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var username = dto.Username.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for username: {Username}", username);
            throw new ValidationException("Invalid credentials", new[] { "Username or password is incorrect." });
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new ValidationException("JWT key is missing.");
        var issuer = jwtSection["Issuer"] ?? throw new ValidationException("JWT issuer is missing.");
        var audience = jwtSection["Audience"] ?? throw new ValidationException("JWT audience is missing.");
        var expireMinutes = int.TryParse(jwtSection["ExpireMinutes"], out var minutes) ? minutes : 60;

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expireMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        _logger.LogInformation("User login successful: {UserId} ({Username})", user.Id, user.Username);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }

    private static void ValidateRegistrationInput(RegisterRequestDto dto, string username, string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            errors.Add("Password must be at least 6 characters.");
        }

        if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            errors.Add("ConfirmPassword must match Password.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Registration validation failed.", errors);
        }
    }
}
