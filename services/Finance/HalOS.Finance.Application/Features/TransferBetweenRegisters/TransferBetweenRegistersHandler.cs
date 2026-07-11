using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.TransferBetweenRegisters;

internal sealed class TransferBetweenRegistersHandler : ICommandHandler<TransferBetweenRegistersCommand, Guid>
{
    private readonly ICashRegisterRepository _registers;
    private readonly IUnitOfWork _unitOfWork;

    public TransferBetweenRegistersHandler(ICashRegisterRepository registers, IUnitOfWork unitOfWork)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(TransferBetweenRegistersCommand request, CancellationToken cancellationToken)
    {
        if (request.FromRegisterId == request.ToRegisterId)
        {
            return Result.Failure<Guid>(CashErrors.SameRegister);
        }

        var from = await _registers.GetByIdAsync(request.FromRegisterId, cancellationToken);
        var to = await _registers.GetByIdAsync(request.ToRegisterId, cancellationToken);
        if (from is null || to is null)
        {
            return Result.Failure<Guid>(CashErrors.NotFound);
        }

        var desc = request.Description ?? "Virman";
        var outMove = from.Record(CashDirection.Out, request.Amount, $"{desc} → {to.Name}", request.OccurredAt);
        if (outMove.IsFailure)
        {
            return Result.Failure<Guid>(outMove.Error);
        }

        var inMove = to.Record(CashDirection.In, request.Amount, $"{desc} ← {from.Name}", request.OccurredAt);
        if (inMove.IsFailure)
        {
            return Result.Failure<Guid>(inMove.Error);
        }

        _registers.RegisterNew(outMove.Value);
        _registers.RegisterNew(inMove.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return from.Id;
    }
}
