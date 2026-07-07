using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.GetCurrentAccount;

/// <summary>Bir tarafın cari hesabını getiren query handler (docs/03 M6). Tenant filtreli (BK-8).</summary>
internal sealed class GetCurrentAccountHandler : IQueryHandler<GetCurrentAccountQuery, CurrentAccountDto>
{
    private readonly ICurrentAccountRepository _accounts;

    public GetCurrentAccountHandler(ICurrentAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Result<CurrentAccountDto>> Handle(GetCurrentAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByPartyIdAsync(request.PartyId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<CurrentAccountDto>(CurrentAccountErrors.NotFound);
        }

        return CurrentAccountDto.FromDomain(account);
    }
}
