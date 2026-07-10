using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Features.UpdateThresholds;

internal sealed class UpdateThresholdsHandler : ICommandHandler<UpdateThresholdsCommand>
{
    private readonly IColdStorageUnitRepository _units;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateThresholdsHandler(IColdStorageUnitRepository units, IUnitOfWork unitOfWork)
    {
        _units = units;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateThresholdsCommand request, CancellationToken cancellationToken)
    {
        var unit = await _units.GetByIdAsync(request.ColdStorageUnitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure(ColdStorageUnitErrors.NotFound);
        }

        var result = unit.UpdateThresholds(request.MinTempC, request.MaxTempC);
        if (result.IsFailure)
        {
            return result;
        }

        _units.Update(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
