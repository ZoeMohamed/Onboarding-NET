using TaskManagement.API.Models;

namespace TaskManagement.API.Repositories;

public class HealthRepository : IHealthRepository
{
    public ApiHealth GetHealth()
    {
        return new ApiHealth
        {
            Status = "Healthy",
            Message = "TaskManagement.API is running"
        };
    }
}
