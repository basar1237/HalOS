using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Contracts;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.ValueObjects;

namespace HalOS.Identity.Application.Features.Users.Login;

internal sealed class LoginHandler : ICommandHandler<LoginCommand, AuthenticationResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITotpService totpService,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _totpService = totpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthenticationResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            // Numaralandırmayı önlemek için jenerik kimlik hatası döndürülür.
            return Result.Failure<AuthenticationResult>(UserErrors.InvalidCredentials);
        }

        // Login tenant çözümlemeden önce olur → global filter atlanır.
        var user = await _users.GetByEmailAsync(
            emailResult.Value,
            ignoreTenantFilter: true,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(UserErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(UserErrors.Inactive);
        }

        if (!_passwordHasher.Verify(user.PasswordHash.Value, request.Password))
        {
            return Result.Failure<AuthenticationResult>(UserErrors.InvalidCredentials);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                return Result.Failure<AuthenticationResult>(UserErrors.TwoFactorRequired);
            }

            if (!_totpService.VerifyCode(user.TwoFactorSecret!, request.TwoFactorCode))
            {
                return Result.Failure<AuthenticationResult>(UserErrors.TwoFactorInvalidCode);
            }
        }

        var tokens = _tokenService.CreateTokenPair(user);

        user.IssueRefreshToken(
            _tokenService.HashRefreshToken(tokens.RefreshToken),
            tokens.RefreshTokenExpiresOnUtc);

        // NOT: user, GetByEmailAsync ile TAKİPLİ (tracked) yüklenir → yeni owned RefreshToken'ı
        // EF change tracker Added olarak algılar. Burada _users.Update(user) ÇAĞIRILMAZ: Update
        // tüm grafiği (yeni token dahil) Modified işaretler ve var olmayan satıra UPDATE →
        // DbUpdateConcurrencyException (0 satır). SaveChanges tek başına doğru şekilde INSERT eder.
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
