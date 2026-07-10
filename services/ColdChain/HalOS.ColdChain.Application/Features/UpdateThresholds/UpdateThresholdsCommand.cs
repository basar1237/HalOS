using HalOS.BuildingBlocks.Application;

namespace HalOS.ColdChain.Application.Features.UpdateThresholds;

/// <summary>Bir soğuk hava deposunun izin verilen sıcaklık aralığını günceller (docs/04 §6).</summary>
public sealed record UpdateThresholdsCommand(
    Guid ColdStorageUnitId,
    decimal MinTempC,
    decimal MaxTempC) : ICommand;
