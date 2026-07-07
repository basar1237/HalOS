using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Contracts;
using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Features.Users.RefreshToken;

internal sealed class RefreshTokenHandler
    : ICommandHandler<RefreshTokenCommand, AuthenticationResult>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(
        IUserRepository users,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthenticationResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var user = await _users.GetByActiveRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(UserErrors.RefreshTokenInvalid);
        }

        // Eski token'ı iptal et (rotasyon), yenisini ver.
        var revoke = user.RevokeRefreshToken(tokenHash);
        if (revoke.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(revoke.Error);
        }

        var tokens = _tokenService.CreateTokenPair(user);
        user.IssueRefreshToken(
            _tokenService.HashRefreshToken(tokens.RefreshToken),
            tokens.RefreshTokenExpiresOnUtc);
        _users.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            tokens.AccessToken,
            tokens.AccessTokenExpiresOnUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresOnUtc,
            user.Id,
            user.TenantId,
            user.Email.Value,
            user.Role.ToString());
    }
}
