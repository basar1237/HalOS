using HalOS.Sales.Api.Authorization;
using HalOS.Sales.Api.Contracts;
using HalOS.Sales.Application.Features.AddSaleLine;
using HalOS.Sales.Application.Features.CancelSale;
using HalOS.Sales.Application.Features.CompleteSale;
using HalOS.Sales.Application.Features.CreateSale;
using HalOS.Sales.Application.Features.GetSale;
using HalOS.Sales.Application.Features.ListSales;
using HalOS.Sales.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Sales.Api.Controllers;

/// <summary>
/// Satış (SaleTransaction) uçları (docs/03 M4/M5) — ÇEKİRDEK. Tenant JWT claim'inden çözülür ve
/// global query filter'a taşınır (BK-8). RBAC (docs/03 §3):
/// - Oluştur/satır/tamamla: Patron/Yönetici/Kasiyer.
/// - İptal: kısıtlı → Patron/Yönetici.
/// - Okuma: Patron/Yönetici/Muhasebe/Kasiyer.
/// </summary>
[ApiController]
[Route("sales")]
[Authorize]
public sealed class SalesController : ControllerBase
{
    private readonly ISender _sender;

    public SalesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yeni taslak satış oluşturur. Yetki: Patron/Yönetici/Kasiyer.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SaleWrite)]
    public async Task<IActionResult> Create(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSaleCommand(
            request.BuyerPartyId,
            request.ProducerPartyId,
            request.ConsignmentId,
            request.SoldAt,
            request.IsWithinMarket,
            request.OperationId,
            request.Term ?? SaleTerm.Cash);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Taslak satışa satır ekler. Yetki: Patron/Yönetici/Kasiyer.</summary>
    [HttpPost("{id:guid}/lines")]
    [Authorize(Policy = AuthorizationPolicies.SaleWrite)]
    public async Task<IActionResult> AddLine(
        Guid id,
        AddSaleLineRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddSaleLineCommand(id, request.ProductId, request.Quantity, request.Unit, request.UnitPrice);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Satışı tamamlar; kesinti/hakediş motorunu çalıştırır (docs/02 §4, BK-1/BK-2/BK-3). Yetki:
    /// Patron/Yönetici/Kasiyer.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.SaleWrite)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteSaleCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Satışı iptal eder (ters kayıt/flag; SİLİNMEZ — BK-9). Yetki kısıtlı: Patron/Yönetici.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.SaleCancel)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancelSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelSaleCommand(id, request.Reason), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Tekil satış (satırlar/kesinti/hakediş dahil). Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SaleRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSaleQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Sayfalanmış satış listesi (sold_at azalan; opsiyonel tarih aralığı). Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SaleRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListSalesQuery(page, pageSize, from, to), cancellationToken);
        return result.ToActionResult(this);
    }
}
