namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// A domain error with a stable machine-readable <see cref="Code"/> and a human-readable
/// <see cref="Message"/>. Domain errors are meaningful (docs/07 §10); user-facing text is
/// Turkish while the code stays in English (docs/07 §3).
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>Represents the absence of an error (used by successful results).</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    public override string ToString() => $"{Code}: {Message}";
}
