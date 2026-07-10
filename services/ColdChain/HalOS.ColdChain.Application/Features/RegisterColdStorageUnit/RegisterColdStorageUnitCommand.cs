using HalOS.BuildingBlocks.Application;

namespace HalOS.ColdChain.Application.Features.RegisterColdStorageUnit;

/// <summary>
/// Yeni bir soğuk hava deposu tanımlar (docs/04 §6, docs/06 S3.1). Ad zorunlu; alt eşik üst eşikten
/// küçük olmalı. İzlenecek sıcaklık aralığı burada belirlenir; okumalar bu aralığa göre değerlendirilir.
/// </summary>
public sealed record RegisterColdStorageUnitCommand(
    string Name,
    decimal MinTempC,
    decimal MaxTempC) : ICommand<Guid>;
