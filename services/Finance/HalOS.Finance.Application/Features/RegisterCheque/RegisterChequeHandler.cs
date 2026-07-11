using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.RegisterCheque;

internal sealed class RegisterChequeHandler : ICommandHandler<RegisterChequeCommand, Guid>
{
    private readonly IChequeRepository _cheques;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterChequeHandler(
        IChequeRepository cheques,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _cheques = cheques;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterChequeCommand request, CancellationToken cancellationToken)
    {
        var result = Cheque.Create(
            _tenantContext.TenantId,
            request.Kind,
            request.Direction,
            request.PartyId,
            request.BankName,
            request.SerialNo,
            request.Amount,
            request.IssueDate,
            request.DueDate,
            request.Note);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _cheques.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
