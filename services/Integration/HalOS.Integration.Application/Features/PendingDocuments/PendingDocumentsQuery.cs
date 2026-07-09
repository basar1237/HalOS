using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.PendingDocuments;

/// <summary>
/// Bekleyen (Draft/Failed) e-belge sayıları (dashboard). SALT-OKUMA CQRS; tenant filtreli (BK-8).
/// </summary>
public sealed record PendingDocumentsQuery : IQuery<PendingDocumentsDto>;
