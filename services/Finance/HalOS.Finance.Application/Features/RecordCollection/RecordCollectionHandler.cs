using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.RecordCollection;

/// <summary>
/// Alıcıdan tahsilat kaydeden handler (docs/03 M6). Cari hesabı taraf üzerinden bulur (yoksa açar),
/// domain <c>RecordCollection</c> ile alacak hareketi işler (BK-6 domain'de doğrulanır), SaveChanges
/// ile CollectionReceived event'i outbox'a atomik yazılır (docs/04 §10). Handler doğrudan yayın
/// yapmaz (docs/07 §5).
/// </summary>
internal sealed class RecordCollectionHandler : ICommandHandler<RecordCollectionCommand, Guid>
{
    private readonly ICurrentAccountRepository _accounts;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public RecordCollectionHandler(
        ICurrentAccountRepository accounts,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _accounts = accounts;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RecordCollectionCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByPartyIdAsync(request.PartyId, cancellationToken);
        if (account is null)
        {
            var open = CurrentAccount.Open(_tenantContext.TenantId, request.PartyId);
            if (open.IsFailure)
            {
                return Result.Failure<Guid>(open.Error);
            }

            account = open.Value;
            _accounts.Add(account);
        }

        var result = account.RecordCollection(request.Amount, request.Channel, refId: null, request.OccurredAt);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _accounts.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
