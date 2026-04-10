using TaskManagement.API.DTOs;
using TaskManagement.API.Repositories;

namespace TaskManagement.API.Services;

public class HealthService : IHealthService
{
    private readonly IHealthRepository _healthRepository;

    public HealthService(IHealthRepository healthRepository)
    {
        _healthRepository = healthRepository;
    }

    public HealthResponseDto GetHealth()
    {
        var health = _healthRepository.GetHealth();

        return new HealthResponseDto
        {
            Status = health.Status,
            Message = health.Message,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
