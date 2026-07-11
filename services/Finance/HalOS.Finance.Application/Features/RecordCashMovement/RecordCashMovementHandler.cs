using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.RecordCashMovement;

internal sealed class RecordCashMovementHandler : ICommandHandler<RecordCashMovementCommand, Guid>
{
    private readonly ICashRegisterRepository _registers;
    private readonly IUnitOfWork _unitOfWork;

    public RecordCashMovementHandler(ICashRegisterRepository registers, IUnitOfWork unitOfWork)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RecordCashMovementCommand request, CancellationToken cancellationToken)
    {
        var register = await _registers.GetByIdAsync(request.RegisterId, cancellationToken);
        if (register is null)
        {
            return Result.Failure<Guid>(CashErrors.NotFound);
        }

        var result = register.Record(request.Direction, request.Amount, request.Description, request.OccurredAt);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _registers.RegisterNew(result.Value); // yeni hareketi EF'e Added bildir
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return register.Id;
    }
}
