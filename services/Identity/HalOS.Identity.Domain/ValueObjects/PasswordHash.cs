using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.ValueObjects;

/// <summary>
/// Kullanıcı parolasının hash'lenmiş halini tutan value object. Hash üretimi
/// Infrastructure katmanının sorumluluğudur (IPasswordHasher); Domain yalnızca
/// hazır hash'i taşır. Düz parola asla Domain'e girmez.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private PasswordHash(string value)
    {
        Value = value;
    }

    private PasswordHash()
    {
        Value = string.Empty;
    }

    public string Value { get; }

    public static Result<PasswordHash> Create(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result.Failure<PasswordHash>(
                new Error("PasswordHash.Empty", "Parola hash'i boş olamaz."));
        }

        return new PasswordHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
