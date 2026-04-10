using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Services;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var response = _healthService.GetHealth();
        return Ok(response);
    }
}
