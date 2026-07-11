using HalOS.Finance.Api.Contracts;
using HalOS.Finance.Application.Features.ChangeChequeStatus;
using HalOS.Finance.Application.Features.ListCheques;
using HalOS.Finance.Application.Features.RegisterCheque;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>Çek/Senet uçları (docs/11 §3.5). Tenant JWT'den (BK-8).</summary>
[ApiController]
[Route("cheques")]
[Authorize]
public sealed class ChequesController : ControllerBase
{
    private readonly ISender _sender;

    public ChequesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListChequesQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterChequeRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterChequeCommand(
            request.Kind, request.Direction, request.PartyId, request.BankName, request.SerialNo,
            request.Amount, request.IssueDate, request.DueDate, request.Note);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeChequeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeChequeStatusCommand(id, request.NewStatus), cancellationToken);
        return result.ToActionResult(this);
    }
}
