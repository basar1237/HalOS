using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Features.GetProductPassport;

/// <summary>Künyeyi kimliğiyle getiren query handler (docs/03 §5). Tenant filtreli (BK-8).</summary>
internal sealed class GetProductPassportHandler : IQueryHandler<GetProductPassportQuery, ProductPassportDto>
{
    private readonly IProductPassportRepository _passports;

    public GetProductPassportHandler(IProductPassportRepository passports)
    {
        _passports = passports;
    }

    public async Task<Result<ProductPassportDto>> Handle(GetProductPassportQuery request, CancellationToken cancellationToken)
    {
        var passport = await _passports.GetByIdAsync(request.PassportId, cancellationToken);
        if (passport is null)
        {
            return Result.Failure<ProductPassportDto>(ProductPassport.ProductPassportErrors.NotFound);
        }

        return ProductPassportDto.FromDomain(passport);
    }
}
