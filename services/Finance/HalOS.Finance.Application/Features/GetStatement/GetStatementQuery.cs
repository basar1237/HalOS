using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.GetStatement;

/// <summary>
/// Bir tarafın cari ekstresini (hareketler + bakiye) getirir (docs/03 §5 "Cari Detay/Ekstre").
/// Hareketler oluşma zamanına göre artan sıralanır.
/// </summary>
public sealed record GetStatementQuery(Guid PartyId) : IQuery<StatementDto>;
