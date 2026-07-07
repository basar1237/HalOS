using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>Sağlık uçları (docs/04 §8). /health canlılık, /ready hazır-olma. Finance/Sales deseniyle birebir.</summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });

    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "ready" });
}
