using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Party.Api.Controllers;

/// <summary>Sağlık uçları (docs/04 §8). /health canlılık, /ready hazır-olma.</summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });

    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "ready" });
}
