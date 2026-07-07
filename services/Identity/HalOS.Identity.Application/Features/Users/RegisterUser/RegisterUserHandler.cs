using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.ValueObjects;

namespace HalOS.Identity.Application.Features.Users.RegisterUser;

internal sealed class RegisterUserHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserHandler(
        IUserRepository users,
        ITenantRepository tenants,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _tenants = tenants;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<Guid>(
                new Error("Tenant.NotFound", "İşletme bulunamadı."));
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var email = emailResult.Value;

        // Tekillik ambient tenant'a göre değil, kayıt için verilen tenant'a göre kontrol edilir;
        // aksi halde anonim /auth/register'da ambient TenantId=Guid.Empty olduğundan kontrol
        // etkisiz kalır ve DB ham DbUpdateException fırlatır (docs/05 (tenant_id,email) tekilliği).
        if (await _users.ExistsByEmailInTenantAsync(request.TenantId, email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailAlreadyInUse);
        }

        var passwordHashResult = PasswordHash.Create(_passwordHasher.Hash(request.Password));
        if (passwordHashResult.IsFailure)
        {
            return Result.Failure<Guid>(passwordHashResult.Error);
        }

        var userResult = User.Register(
            request.TenantId,
            email,
            passwordHashResult.Value,
            request.FullName,
            request.Role);

        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        var user = userResult.Value;
        _users.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
