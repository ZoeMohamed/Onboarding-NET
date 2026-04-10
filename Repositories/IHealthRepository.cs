using TaskManagement.API.Models;

namespace TaskManagement.API.Repositories;

public interface IHealthRepository
{
    ApiHealth GetHealth();
}
