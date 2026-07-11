using HalOS.Finance.Api.Contracts;
using HalOS.Finance.Application.Features.ListCashRegisters;
using HalOS.Finance.Application.Features.OpenCashRegister;
using HalOS.Finance.Application.Features.RecordCashMovement;
using HalOS.Finance.Application.Features.TransferBetweenRegisters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>Kasa uçları (docs/11 §3.6). Tenant JWT'den (BK-8).</summary>
[ApiController]
[Route("cash-registers")]
[Authorize]
public sealed class CashRegistersController : ControllerBase
{
    private readonly ISender _sender;

    public CashRegistersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCashRegistersQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Open(OpenCashRegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new OpenCashRegisterCommand(request.Name, request.Kind), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/movements")]
    public async Task<IActionResult> Record(Guid id, RecordCashMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordCashMovementCommand(
            id, request.Direction, request.Amount, request.Description, request.OccurredAt ?? DateTime.UtcNow);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(CashTransferRequest request, CancellationToken cancellationToken)
    {
        var command = new TransferBetweenRegistersCommand(
            request.FromRegisterId, request.ToRegisterId, request.Amount, request.Description, request.OccurredAt ?? DateTime.UtcNow);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
