using HalOS.BuildingBlocks.Application;

namespace HalOS.Identity.Application.Features.Tenants.CreateTenant;

/// <summary>Yeni bir işletme (tenant) oluşturur ve deneme aboneliği başlatır.</summary>
public sealed record CreateTenantCommand(string Name) : ICommand<Guid>;
