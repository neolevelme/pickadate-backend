using Microsoft.AspNetCore.Mvc;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "pickadate.me-api",
        timestamp = DateTime.UtcNow
    });
}
