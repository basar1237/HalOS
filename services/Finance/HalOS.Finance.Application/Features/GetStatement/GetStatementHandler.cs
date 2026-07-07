using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.GetStatement;

/// <summary>Cari ekstre (hareketler + bakiye) query handler (docs/03 §5). Tenant filtreli (BK-8).</summary>
internal sealed class GetStatementHandler : IQueryHandler<GetStatementQuery, StatementDto>
{
    private readonly ICurrentAccountRepository _accounts;

    public GetStatementHandler(ICurrentAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Result<StatementDto>> Handle(GetStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByPartyIdAsync(request.PartyId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<StatementDto>(CurrentAccountErrors.NotFound);
        }

        return StatementDto.FromDomain(account);
    }
}
