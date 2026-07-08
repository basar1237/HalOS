using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.Reports.SpoilageAnalysisReport;

/// <summary>
/// Detaylı fire analizi raporu (docs/06 S2.1 detaylı fire analizi). Verilen aralıkta ürün bazlı
/// toplam giriş, toplam fire ve fire oranı (%). SALT-OKUMA CQRS query — yeni tablo/servis yok;
/// tenant global filter otomatik uygulanır (BK-8). Finance yaşlandırma raporu query deseniyle birebir.
/// </summary>
/// <param name="FromUtc">Analiz aralığı başlangıcı (UTC, dahil).</param>
/// <param name="ToUtc">Analiz aralığı bitişi (UTC, dahil).</param>
public sealed record SpoilageAnalysisReportQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IQuery<SpoilageAnalysisReportDto>;
