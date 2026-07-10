using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Features.RegisterColdStorageUnit;

internal sealed class RegisterColdStorageUnitHandler : ICommandHandler<RegisterColdStorageUnitCommand, Guid>
{
    private readonly IColdStorageUnitRepository _units;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterColdStorageUnitHandler(
        IColdStorageUnitRepository units,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _units = units;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterColdStorageUnitCommand request, CancellationToken cancellationToken)
    {
        var result = ColdStorageUnit.Register(
            _tenantContext.TenantId,
            request.Name,
            request.MinTempC,
            request.MaxTempC);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _units.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
