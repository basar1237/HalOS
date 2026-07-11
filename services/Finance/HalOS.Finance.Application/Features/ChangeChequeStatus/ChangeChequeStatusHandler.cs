using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.ChangeChequeStatus;

internal sealed class ChangeChequeStatusHandler : ICommandHandler<ChangeChequeStatusCommand, Guid>
{
    private readonly IChequeRepository _cheques;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeChequeStatusHandler(IChequeRepository cheques, IUnitOfWork unitOfWork)
    {
        _cheques = cheques;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ChangeChequeStatusCommand request, CancellationToken cancellationToken)
    {
        var cheque = await _cheques.GetByIdAsync(request.ChequeId, cancellationToken);
        if (cheque is null)
        {
            return Result.Failure<Guid>(ChequeErrors.NotFound);
        }

        var result = cheque.ChangeStatus(request.NewStatus);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _cheques.Update(cheque);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return cheque.Id;
    }
}
