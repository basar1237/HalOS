using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Features.RecordReading;

/// <summary>
/// Sensör okumasını işleyen handler (docs/04 §6). Depoyu okumalarıyla yükler, aggregate'in
/// <see cref="ColdStorageUnit.RecordReading"/> metodunu çağırır (idempotency + eşik değerlendirmesi
/// domain'de), tek <see cref="IUnitOfWork.SaveChangesAsync"/> ile kaydeder — eşik aşıldıysa üretilen
/// TemperatureThresholdBreached event'i aynı transaction'da outbox'a atomik yazılır (docs/04 §10).
/// </summary>
internal sealed class RecordReadingHandler : ICommandHandler<RecordReadingCommand>
{
    private readonly IColdStorageUnitRepository _units;
    private readonly IUnitOfWork _unitOfWork;

    public RecordReadingHandler(IColdStorageUnitRepository units, IUnitOfWork unitOfWork)
    {
        _units = units;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordReadingCommand request, CancellationToken cancellationToken)
    {
        var unit = await _units.GetByIdAsync(request.ColdStorageUnitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure(ColdStorageUnitErrors.NotFound);
        }

        var result = unit.RecordReading(
            request.ReadingId,
            request.TemperatureC,
            request.HumidityPercent,
            request.OccurredAt);

        if (result.IsFailure)
        {
            return result;
        }

        _units.Update(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
