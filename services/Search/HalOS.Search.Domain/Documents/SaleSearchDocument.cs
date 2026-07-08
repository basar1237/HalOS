namespace HalOS.Search.Domain.Documents;

/// <summary>
/// Satış arama dokümanı (docs/06 S2.3). Sales servisinin <c>SaleCompleted</c> event'inden
/// indekslenir; Sales DB'sine DOKUNULMAZ (CQRS ayrı okuma modeli, docs/04 ADR-007). Satış kimliği,
/// alıcı/müstahsil referansları, tutar ve tarih üzerinden aranabilir/gösterilebilir.
/// </summary>
public sealed class SaleSearchDocument : SearchDocument
{
    /// <summary>Kaynak <c>SaleTransaction.Id</c>. <see cref="MakeId"/> ile doküman Id'sine eşlenir.</summary>
    public required Guid SaleTransactionId { get; init; }

    /// <summary>Alıcı taraf referansı (Party ID — servisler arası FK yok, docs/05 §5).</summary>
    public required Guid BuyerPartyId { get; init; }

    /// <summary>Müstahsil taraf referansı (Party ID).</summary>
    public required Guid ProducerPartyId { get; init; }

    /// <summary>Satış brüt tutarı (decimal — asla float, BK-2).</summary>
    public required decimal GrossAmount { get; init; }

    /// <summary>Satışın gerçekleştiği an (UTC).</summary>
    public required DateTime SoldAt { get; init; }

    /// <inheritdoc />
    public override string SearchableText =>
        string.Join(
            " ",
            SaleTransactionId.ToString("N"),
            BuyerPartyId.ToString("N"),
            ProducerPartyId.ToString("N"),
            GrossAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SoldAt.ToString("yyyy-MM-dd"));

    /// <summary>Kaynak satış kimliğinden deterministik doküman Id üretir (idempotent upsert).</summary>
    public static string MakeId(Guid saleTransactionId) => $"{SearchDocumentType.Sale}:{saleTransactionId:N}";
}
