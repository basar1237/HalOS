using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCashRegisters;

internal sealed class ListCashRegistersHandler : IQueryHandler<ListCashRegistersQuery, IReadOnlyList<CashRegisterDto>>
{
    private readonly ICashRegisterRepository _registers;

    public ListCashRegistersHandler(ICashRegisterRepository registers)
    {
        _registers = registers;
    }

    public async Task<Result<IReadOnlyList<CashRegisterDto>>> Handle(ListCashRegistersQuery request, CancellationToken cancellationToken)
    {
        var list = await _registers.ListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CashRegisterDto>>(list.Select(CashRegisterDto.FromDomain).ToList());
    }
}
