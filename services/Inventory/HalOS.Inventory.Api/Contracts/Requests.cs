namespace HalOS.Inventory.Api.Contracts;

/// <summary>
/// Fire (zayiat) kaydı isteği (docs/03 M9 / BK-7; docs/02 §57 Fire=Spoilage). Ürün + miktar +
/// gerekçe. BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz) — domain'de doğrulanır.
/// </summary>
public sealed record RecordSpoilageRequest(
    Guid ProductId,
    decimal Quantity,
    string Reason,
    DateTime OccurredAt);
