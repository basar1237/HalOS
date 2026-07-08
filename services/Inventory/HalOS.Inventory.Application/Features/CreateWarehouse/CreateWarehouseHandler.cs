using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.CreateWarehouse;

/// <summary>
/// Depo oluşturan handler (docs/06 S2.1). Kod tenant içinde tekil olmalıdır (çakışırsa hata döner);
/// nihai tekillik UNIQUE(tenant_id, code) ile DB'de de korunur (çift savunma). Tenant
/// <see cref="ITenantContext"/>'ten çözülür (BK-8); domain <c>Warehouse.Create</c> ile aggregate
/// oluşturulur ve SaveChanges ile atomik kaydedilir. Finance.RecordAdvanceHandler deseniyle birebir.
///
/// Tenant başına TEK varsayılan depo değişmezi: yeni depo varsayılan (<c>IsDefault=true</c>) isteniyorsa
/// mevcut varsayılan(lar) aynı unit-of-work içinde düşürülür (<see cref="Warehouse.Demote"/>) ve yeni
/// depo tek varsayılan olur (docs/06 S2.1). Aynı atomik işlem: eski düşürme + yeni ekleme birlikte
/// kaydedilir. DB tarafında kısmi tekil indeks (WHERE is_default) çift savunma sağlar.
/// </summary>
internal sealed class CreateWarehouseHandler : ICommandHandler<CreateWarehouseCommand, Guid>
{
    private readonly IWarehouseRepository _warehouses;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarehouseHandler(
        IWarehouseRepository warehouses,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _warehouses = warehouses;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (await _warehouses.ExistsByCodeAsync(code, cancellationToken))
        {
            return Result.Failure<Guid>(WarehouseErrors.CodeAlreadyExists);
        }

        var created = Warehouse.Create(_tenantContext.TenantId, request.Name, code, request.IsDefault);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var warehouse = created.Value;

        // Tenant başına tek varsayılan depo (docs/06 S2.1): yeni depo varsayılan yapılıyorsa mevcut
        // varsayılan(lar)ı düşür. Bozuk verilere karşı savunma amaçlı TÜM varsayılanlar düşürülür.
        // Eski düşürme + yeni ekleme aynı SaveChanges'te atomik uygulanır.
        if (request.IsDefault)
        {
            foreach (var existingDefault in await _warehouses.ListDefaultsAsync(cancellationToken))
            {
                existingDefault.Demote();
                _warehouses.Update(existingDefault);
            }
        }

        _warehouses.Add(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }
}
