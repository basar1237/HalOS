using HalOS.Identity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HalOS.Identity.Api.Authorization;

/// <summary>
/// RBAC rol politikaları (docs/03 §3, docs/04 §7). Roller: Patron/Owner, Yönetici/Manager,
/// Muhasebe/Accountant, Kasiyer/Cashier, Depo/Warehouse. Politika adları kod adıyla eşleşir.
/// </summary>
public static class AuthorizationPolicies
{
    public const string OwnerOnly = nameof(SystemRole.Owner);
    public const string ManagerOrAbove = "ManagerOrAbove";
    public const string AccountantOrAbove = "AccountantOrAbove";

    public static void AddHalOSPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(OwnerOnly, p =>
            p.RequireRole(SystemRole.Owner.ToString()));

        options.AddPolicy(ManagerOrAbove, p =>
            p.RequireRole(
                SystemRole.Owner.ToString(),
                SystemRole.Manager.ToString()));

        options.AddPolicy(AccountantOrAbove, p =>
            p.RequireRole(
                SystemRole.Owner.ToString(),
                SystemRole.Manager.ToString(),
                SystemRole.Accountant.ToString()));
    }
}
