using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.GetProducerReceipt;

/// <summary>
/// Bir e-Müstahsil Makbuzunu (e-MM) kimliğiyle getirir (docs/03 M7; docs/03 §5 e-Belge Merkezi).
/// Tenant filtreli (BK-8).
/// </summary>
public sealed record GetProducerReceiptQuery(Guid ReceiptId) : IQuery<ProducerReceiptDto>;
