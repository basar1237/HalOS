using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Enums;

namespace HalOS.Identity.Application.Features.Tenants.CreateTenant;

internal sealed class CreateTenantHandler : ICommandHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenants;
    private readonly IRoleRepository _roles;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantHandler(
        ITenantRepository tenants,
        IRoleRepository roles,
        ISubscriptionRepository subscriptions,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _roles = roles;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        if (await _tenants.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
        {
            return Result.Failure<Guid>(
                new Error("Tenant.NameAlreadyInUse", "Bu işletme adı zaten kullanımda."));
        }

        var tenantResult = Tenant.Create(request.Name);
        if (tenantResult.IsFailure)
        {
            return Result.Failure<Guid>(tenantResult.Error);
        }

        var tenant = tenantResult.Value;
        _tenants.Add(tenant);

        // Her tenant için öntanımlı sistem rollerini seed et (docs/03 §3).
        var roles = Enum.GetValues<SystemRole>()
            .Select(sr => Role.Create(tenant.Id, sr))
            .ToList();
        _roles.AddRange(roles);

        // İskelet düzeyinde deneme aboneliği (docs/06 S0.4).
        _subscriptions.Add(Subscription.StartTrial(tenant.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}
