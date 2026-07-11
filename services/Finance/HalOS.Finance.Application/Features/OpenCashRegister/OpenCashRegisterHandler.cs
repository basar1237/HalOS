using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.OpenCashRegister;

internal sealed class OpenCashRegisterHandler : ICommandHandler<OpenCashRegisterCommand, Guid>
{
    private readonly ICashRegisterRepository _registers;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public OpenCashRegisterHandler(ICashRegisterRepository registers, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    {
        _registers = registers;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(OpenCashRegisterCommand request, CancellationToken cancellationToken)
    {
        var result = CashRegister.Open(_tenantContext.TenantId, request.Name, request.Kind);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _registers.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
