using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.GetInvoice;

/// <summary>
/// Bir e-Fatura'yı (HAL) kimliğiyle getirir (docs/03 M8; docs/03 §5 e-Belge Merkezi). Tenant filtreli
/// (BK-8). e-MM GetProducerReceipt deseniyle birebir.
/// </summary>
public sealed record GetInvoiceQuery(Guid InvoiceId) : IQuery<InvoiceDto>;
