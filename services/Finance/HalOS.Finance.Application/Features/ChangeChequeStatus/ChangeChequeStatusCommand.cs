using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.ChangeChequeStatus;

/// <summary>Çek/senet durumunu değiştirir (tahsile ver / tahsil / karşılıksız / ciro / ödendi).</summary>
public sealed record ChangeChequeStatusCommand(Guid ChequeId, ChequeStatus NewStatus) : ICommand<Guid>;
