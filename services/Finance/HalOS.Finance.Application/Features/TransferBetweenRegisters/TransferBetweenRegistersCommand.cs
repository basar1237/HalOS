using HalOS.BuildingBlocks.Application;

namespace HalOS.Finance.Application.Features.TransferBetweenRegisters;

/// <summary>Kasalar arası virman (kaynaktan çıkış, hedefe giriş) — docs/11 §3.6.</summary>
public sealed record TransferBetweenRegistersCommand(
    Guid FromRegisterId,
    Guid ToRegisterId,
    decimal Amount,
    string? Description,
    DateTime OccurredAt) : ICommand<Guid>;
