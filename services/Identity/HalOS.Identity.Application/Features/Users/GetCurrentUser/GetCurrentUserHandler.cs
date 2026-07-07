using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Contracts;
using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Features.Users.GetCurrentUser;

internal sealed class GetCurrentUserHandler
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IUserRepository _users;

    public GetCurrentUserHandler(ICurrentUserContext currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<CurrentUserDto>(
                new Error("Auth.Unauthenticated", "Kimlik doğrulaması gerekli."));
        }

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(UserErrors.NotFound);
        }

        return new CurrentUserDto(
            user.Id,
            user.TenantId,
            user.Email.Value,
            user.FullName,
            user.Role.ToString(),
            user.TwoFactorEnabled);
    }
}
