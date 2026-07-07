using HalOS.Identity.Api.Contracts;
using HalOS.Identity.Application.Features.Users.GetCurrentUser;
using HalOS.Identity.Application.Features.Users.Login;
using HalOS.Identity.Application.Features.Users.RefreshToken;
using HalOS.Identity.Application.Features.Users.RegisterUser;
using HalOS.Identity.Application.Features.Users.Setup2fa;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Identity.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yeni kullanıcı kaydı. İlk kurulumda tenant Id doğrudan verilir.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.TenantId,
            request.Email,
            request.Password,
            request.FullName,
            request.Role);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password, request.TwoFactorCode),
            cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>O anki kullanıcı için 2FA (TOTP) kurulumunu başlatır.</summary>
    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> Setup2fa(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new Setup2faCommand(), cancellationToken);
        return result.ToActionResult(this);
    }
}

[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;

    public MeController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.ToActionResult(this);
    }
}
