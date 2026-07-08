using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.Reports.CurrentAccountAgingReport;

/// <summary>
/// Cari yaşlandırma raporu (docs/03 M10 "cari yaşlandırma (okuma)"). Tenant'ın müstahsil hakediş
/// (Settlement) vadelerini referans tarihe (<paramref name="AsOfUtc"/>) göre gecikme yaşına göre
/// kovalara böler (güncel / 0-15 / 16-30 / 31+ gün) ve her kova için tutar + cari sayısı toplar.
/// SALT-OKUMA CQRS query — yeni tablo/servis yok; tenant global filter otomatik uygulanır (BK-8).
/// </summary>
/// <param name="AsOfUtc">Yaşlandırmanın hesaplandığı referans tarih (UTC).</param>
public sealed record CurrentAccountAgingReportQuery(
    DateTime AsOfUtc) : IQuery<CurrentAccountAgingReportDto>;
