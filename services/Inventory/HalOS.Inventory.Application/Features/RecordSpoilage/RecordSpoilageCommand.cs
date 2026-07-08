using HalOS.BuildingBlocks.Application;

namespace HalOS.Inventory.Application.Features.RecordSpoilage;

/// <summary>
/// Bir ürün için fire (zayiat) kaydeder (docs/02 §57 Fire=Spoilage; §237 SpoilageRecorded; docs/03
/// M9 / BK-7). Fire, stok kaleminin kalanını AZALTAN bir çıkış hareketidir. BK-7: fire mevcut stoğu
/// aşamaz (kalan negatif olamaz). Yetki: Patron/Yönetici/Depo (docs/03 §3).
/// </summary>
/// <param name="ProductId">Fire kaydedilen ürün (Product ID).</param>
/// <param name="Quantity">Fire miktarı (pozitif, NUMERIC(18,3) — decimal, BK-2).</param>
/// <param name="Reason">Fire gerekçesi (zorunlu — çürüme/ezilme vb.).</param>
/// <param name="OccurredAt">Firenin gerçekleştiği an.</param>
public sealed record RecordSpoilageCommand(
    Guid ProductId,
    decimal Quantity,
    string Reason,
    DateTime OccurredAt) : ICommand<Guid>;
