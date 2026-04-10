using TaskManagement.API.DTOs;

namespace TaskManagement.API.Services;

public interface IHealthService
{
    HealthResponseDto GetHealth();
}
