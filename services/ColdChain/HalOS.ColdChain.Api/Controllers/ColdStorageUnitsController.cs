using HalOS.ColdChain.Api.Authorization;
using HalOS.ColdChain.Api.Contracts;
using HalOS.ColdChain.Application.Features.GetUnit;
using HalOS.ColdChain.Application.Features.ListReadings;
using HalOS.ColdChain.Application.Features.ListUnits;
using HalOS.ColdChain.Application.Features.RecordReading;
using HalOS.ColdChain.Application.Features.RegisterColdStorageUnit;
using HalOS.ColdChain.Application.Features.UpdateThresholds;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.ColdChain.Api.Controllers;

/// <summary>
/// Soğuk hava deposu ve sensör okuması uçları (docs/04 §6, docs/06 S3.1). Tenant JWT claim'inden
/// çözülür (BK-8). RBAC (docs/03 §3): tanımla/eşik yaz Patron/Yönetici; okuma gönder Patron/Yönetici/
/// Depo; görüntüleme Patron/Yönetici/Depo.
/// </summary>
[ApiController]
[Route("cold-storage-units")]
[Authorize]
public sealed class ColdStorageUnitsController : ControllerBase
{
    private readonly ISender _sender;

    public ColdStorageUnitsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yeni soğuk hava deposu tanımlar. Yetki: Patron/Yönetici.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ColdChainWrite)]
    public async Task<IActionResult> Register(
        RegisterColdStorageUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterColdStorageUnitCommand(request.Name, request.MinTempC, request.MaxTempC),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Sayfalanmış soğuk hava deposu listesi. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ColdChainRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListUnitsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Tekil soğuk hava deposu (son okuma özetiyle). Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ColdChainRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUnitQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Deponun sıcaklık eşiklerini günceller. Yetki: Patron/Yönetici.</summary>
    [HttpPut("{id:guid}/thresholds")]
    [Authorize(Policy = AuthorizationPolicies.ColdChainWrite)]
    public async Task<IActionResult> UpdateThresholds(
        Guid id,
        UpdateThresholdsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateThresholdsCommand(id, request.MinTempC, request.MaxTempC),
            cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Depoya bir sensör okuması gönderir (docs/04 §6). Eşik dışıysa alarm event'i yayınlanır.
    /// Yetki: Patron/Yönetici/Depo (saha/cihaz operatörü). readingId ile idempotent.
    /// </summary>
    [HttpPost("{id:guid}/readings")]
    [Authorize(Policy = AuthorizationPolicies.ReadingWrite)]
    public async Task<IActionResult> RecordReading(
        Guid id,
        RecordReadingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RecordReadingCommand(
                id,
                request.ReadingId,
                request.TemperatureC,
                request.HumidityPercent,
                request.OccurredAt ?? DateTime.UtcNow),
            cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Deponun son sensör okumaları (OccurredAt azalan). Okuma yetkisi.</summary>
    [HttpGet("{id:guid}/readings")]
    [Authorize(Policy = AuthorizationPolicies.ColdChainRead)]
    public async Task<IActionResult> ListReadings(
        Guid id,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListReadingsQuery(id, limit), cancellationToken);
        return result.ToActionResult(this);
    }
}
