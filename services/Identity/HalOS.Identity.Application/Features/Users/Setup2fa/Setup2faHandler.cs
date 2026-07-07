using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Contracts;
using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Features.Users.Setup2fa;

internal sealed class Setup2faHandler : ICommandHandler<Setup2faCommand, TwoFactorSetupResult>
{
    private const string Issuer = "HalOS";

    private readonly ICurrentUserContext _currentUser;
    private readonly IUserRepository _users;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;

    public Setup2faHandler(
        ICurrentUserContext currentUser,
        IUserRepository users,
        ITotpService totpService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _users = users;
        _totpService = totpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TwoFactorSetupResult>> Handle(
        Setup2faCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<TwoFactorSetupResult>(
                new Error("Auth.Unauthenticated", "Kimlik doğrulaması gerekli."));
        }

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TwoFactorSetupResult>(UserErrors.NotFound);
        }

        var secret = _totpService.GenerateSecret();

        var setup = user.BeginTwoFactorSetup(secret);
        if (setup.IsFailure)
        {
            return Result.Failure<TwoFactorSetupResult>(setup.Error);
        }

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var uri = _totpService.BuildOtpAuthUri(secret, user.Email.Value, Issuer);

        return new TwoFactorSetupResult(secret, uri);
    }
}
