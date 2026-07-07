using System.Text.RegularExpressions;
using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.ValueObjects;

/// <summary>
/// E-posta adresi value object'i. Yapısal eşitliğe sahiptir ve oluşturulurken
/// biçim doğrulaması yapar; geçersiz değer bir <see cref="Result{T}"/> hatası döner.
/// </summary>
public sealed partial class Email : ValueObject
{
    public const int MaxLength = 320;

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>ORM materialization only.</summary>
    private Email()
    {
        Value = string.Empty;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<Email>(EmailErrors.TooLong);
        }

        if (!EmailRegex().IsMatch(normalized))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // Basit ama pratik bir doğrulama; RFC'nin tamamı hedeflenmez.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}

public static class EmailErrors
{
    public static readonly Error Empty =
        new("Email.Empty", "E-posta adresi boş olamaz.");

    public static readonly Error TooLong =
        new("Email.TooLong", "E-posta adresi çok uzun.");

    public static readonly Error InvalidFormat =
        new("Email.InvalidFormat", "E-posta adresi geçersiz.");
}
