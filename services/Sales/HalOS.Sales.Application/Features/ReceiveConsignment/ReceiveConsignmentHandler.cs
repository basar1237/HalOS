using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.ReceiveConsignment;

internal sealed class ReceiveConsignmentHandler : ICommandHandler<ReceiveConsignmentCommand, Guid>
{
    private readonly IConsignmentRepository _consignments;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveConsignmentHandler(
        IConsignmentRepository consignments,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IUnitOfWork unitOfWork)
    {
        _consignments = consignments;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ReceiveConsignmentCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new Consignment.ItemInput(i.ProductId, i.Quantity, i.Unit))
            .ToList();

        var result = Consignment.Receive(
            _tenantContext.TenantId,
            request.ProducerPartyId,
            request.ReceivedAt,
            request.DispatchNoteRef,
            _currentUser.UserId,
            items);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _consignments.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
