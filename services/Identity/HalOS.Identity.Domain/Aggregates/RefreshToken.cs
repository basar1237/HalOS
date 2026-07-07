using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.Aggregates;

/// <summary>
/// Refresh token entity'si — <see cref="User"/> aggregate'inin bir parçası (docs/04 ADR-009).
/// Ham token değeri saklanmaz; yalnızca hash'i tutulur. Rotasyon: kullanılan token iptal
/// edilir, yerine yenisi verilir.
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresOnUtc,
        DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
        CreatedOnUtc = createdOnUtc;
    }

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public Guid UserId { get; private set; }

    /// <summary>Ham token'ın hash'i (ham değer DB'de tutulmaz).</summary>
    public string TokenHash { get; private set; }

    public DateTime ExpiresOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? RevokedOnUtc { get; private set; }

    public bool IsActive => RevokedOnUtc is null && DateTime.UtcNow < ExpiresOnUtc;

    internal static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTime expiresOnUtc)
    {
        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresOnUtc,
            DateTime.UtcNow);
    }

    internal void Revoke() => RevokedOnUtc = DateTime.UtcNow;
}
