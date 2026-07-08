using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.GetProductPassport;

/// <summary>Bir künyeyi (ProductPassport) kimliğiyle getirir (docs/03 §5 e-Belge Merkezi). Tenant filtreli (BK-8).</summary>
public sealed record GetProductPassportQuery(Guid PassportId) : IQuery<ProductPassportDto>;
