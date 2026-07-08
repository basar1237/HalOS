using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Domain.Enums;
using HalOS.Party.Domain.Events;
using HalOS.Party.Domain.ValueObjects;

namespace HalOS.Party.Domain.Aggregates;

/// <summary>
/// Taraf (Cari kart) aggregate kökü (docs/02 §1.1, §3.1; docs/05 §3.2). Müstahsil/alıcı/tüccar/
/// taşıyıcı kimliğini, rollerini ve müstahsile özel stopaj profilini tutar. Tenant'a bağlıdır
/// (ITenantOwned → global query filter, docs/07 §6 / BK-8).
///
/// Değişmezler (docs/02 §3.1):
/// - Tenant içinde (tenant_id, tckn) ve (tenant_id, vkn) tekildir (dolu olanlar) — DB unique
///   index + Application katmanında ön-kontrol ile korunur (docs/05 §3.2).
/// - Müstahsil (Producer) rolü taşıyan tarafın stopaj profili (<see cref="WithholdingProfile"/>)
///   tanımlı olmalı — tenant varsayılanı yoksa profil zorunludur.
/// - En az bir rol taşımalıdır.
/// </summary>
public sealed class Party : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>TCKN uzunluğu (11 hane) — docs/03/05 format kuralı.</summary>
    public const int TcknLength = 11;

    /// <summary>VKN uzunluğu (10 hane) — docs/03/05 format kuralı.</summary>
    public const int VknLength = 10;

    private readonly List<PartyRole> _roles = new();

    private Party(
        Guid id,
        Guid tenantId,
        string displayName,
        string? tckn,
        string? vkn,
        string? taxOffice,
        string? phone,
        string? address,
        bool keepsRecords,
        WithholdingProfile? withholdingProfile,
        DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        DisplayName = displayName;
        Tckn = tckn;
        Vkn = vkn;
        TaxOffice = taxOffice;
        Phone = phone;
        Address = address;
        KeepsRecords = keepsRecords;
        WithholdingProfile = withholdingProfile;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    /// <summary>ORM materialization only.</summary>
    private Party()
    {
        DisplayName = string.Empty;
    }

    public Guid TenantId { get; private set; }

    /// <summary>Görünen ad (docs/05 <c>display_name</c>).</summary>
    public string DisplayName { get; private set; }

    /// <summary>TC Kimlik No — bireysel/müstahsil (docs/05 <c>tckn</c>). 11 hane veya null.</summary>
    public string? Tckn { get; private set; }

    /// <summary>Vergi Kimlik No — tüzel (docs/05 <c>vkn</c>). 10 hane veya null.</summary>
    public string? Vkn { get; private set; }

    public string? TaxOffice { get; private set; }

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Müstahsil kayıt tutuyor mu — e-MM gerekliliğini belirler (docs/05 §3.2, BK-4).</summary>
    public bool KeepsRecords { get; private set; }

    /// <summary>
    /// Müstahsile özel stopaj/Bağ-Kur oran override'ı (docs/02 §3.1). Tenant varsayılanı
    /// yeterliyse null olabilir; ancak Producer rolü için tanımlı olmalıdır (değişmez).
    /// </summary>
    public WithholdingProfile? WithholdingProfile { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<PartyRole> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Yeni bir taraf kaydı oluşturur. En az bir rol zorunludur; Producer rolü varsa stopaj
    /// profili tanımlı olmalıdır. TCKN/VKN format kontrolü burada yapılır (docs/02 §3.1).
    /// </summary>
    public static Result<Party> Register(
        Guid tenantId,
        string? displayName,
        string? tckn,
        string? vkn,
        string? taxOffice,
        string? phone,
        string? address,
        bool keepsRecords,
        WithholdingProfile? withholdingProfile,
        IReadOnlyCollection<PartyRoleType> roles)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure<Party>(PartyErrors.DisplayNameRequired);
        }

        var normalizedName = displayName.Trim();
        if (normalizedName.Length > 200)
        {
            return Result.Failure<Party>(PartyErrors.DisplayNameTooLong);
        }

        var normalizedTckn = Normalize(tckn);
        if (normalizedTckn is not null && !IsAllDigits(normalizedTckn, TcknLength))
        {
            return Result.Failure<Party>(PartyErrors.InvalidTckn);
        }

        var normalizedVkn = Normalize(vkn);
        if (normalizedVkn is not null && !IsAllDigits(normalizedVkn, VknLength))
        {
            return Result.Failure<Party>(PartyErrors.InvalidVkn);
        }

        if (roles is null || roles.Count == 0)
        {
            return Result.Failure<Party>(PartyErrors.RoleRequired);
        }

        // Müstahsil değişmezi: Producer rolü stopaj profili gerektirir (docs/02 §3.1).
        if (roles.Contains(PartyRoleType.Producer) && withholdingProfile is null)
        {
            return Result.Failure<Party>(PartyErrors.ProducerRequiresWithholdingProfile);
        }

        var party = new Party(
            Guid.NewGuid(),
            tenantId,
            normalizedName,
            normalizedTckn,
            normalizedVkn,
            Normalize(taxOffice),
            Normalize(phone),
            Normalize(address),
            keepsRecords,
            withholdingProfile,
            DateTime.UtcNow);

        foreach (var role in roles.Distinct())
        {
            party._roles.Add(PartyRole.Create(party.Id, tenantId, role));
        }

        // Arama okuma modeli (Search servisi, docs/06 S2.3) için kimlik numarası (TCKN varsa o, yoksa
        // VKN) ve rol(ler) event'le taşınır — consumer tekil sorgu yapmadan tam arama dokümanı kurar
        // (docs/07 §5). PartyType enum değil metindir (Contracts servis domain'ine bağlanamaz).
        var taxNumber = normalizedTckn ?? normalizedVkn;
        var partyType = string.Join(",", roles.Distinct().Select(r => r.ToString()));
        party.RaiseDomainEvent(
            new PartyRegistered(party.Id, tenantId, party.DisplayName, taxNumber, partyType, party.CreatedOnUtc));

        // Müstahsil rolüyle birlikte stopaj profili tanımlıysa Sales oran senkronu için
        // cross-service event yayınla (hakediş doğruluğu — docs/02 §6). Profil zaten Producer
        // değişmezi gereği dolu.
        party.RaiseWithholdingProfileChangedIfProducer();

        return party;
    }

    /// <summary>Kimlik/iletişim alanlarını ve kayıt tutma/stopaj profilini günceller.</summary>
    public Result Update(
        string? displayName,
        string? taxOffice,
        string? phone,
        string? address,
        bool keepsRecords,
        WithholdingProfile? withholdingProfile)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure(PartyErrors.DisplayNameRequired);
        }

        var normalizedName = displayName.Trim();
        if (normalizedName.Length > 200)
        {
            return Result.Failure(PartyErrors.DisplayNameTooLong);
        }

        // Producer değişmezi güncellemede de korunur: müstahsilse profil kaldırılamaz.
        if (HasRole(PartyRoleType.Producer) && withholdingProfile is null)
        {
            return Result.Failure(PartyErrors.ProducerRequiresWithholdingProfile);
        }

        DisplayName = normalizedName;
        TaxOffice = Normalize(taxOffice);
        Phone = Normalize(phone);
        Address = Normalize(address);
        KeepsRecords = keepsRecords;
        WithholdingProfile = withholdingProfile;

        // Müstahsil güncellendiğinde her seferinde cross-service event yayınla (yalnız oran
        // değişiminde DEĞİL): Integration servisinin e-MM kararı için müstahsilin güncel
        // KeepsRecords bilgisine ihtiyacı var (BK-4), Sales de okuma modelini senkronlar
        // (hakediş doğruluğu — docs/02 §6). Profil Producer değişmezi gereği dolu.
        RaiseWithholdingProfileChangedIfProducer();

        return Result.Success();
    }

    /// <summary>Tarafa yeni bir rol ekler. Producer eklenirken stopaj profili tanımlı olmalıdır.</summary>
    public Result AddRole(PartyRoleType type)
    {
        if (HasRole(type))
        {
            return Result.Failure(PartyErrors.RoleAlreadyExists);
        }

        if (type == PartyRoleType.Producer && WithholdingProfile is null)
        {
            return Result.Failure(PartyErrors.ProducerRequiresWithholdingProfile);
        }

        _roles.Add(PartyRole.Create(Id, TenantId, type));

        // Yeni Producer rolü eklendiyse (profil zaten dolu — yukarıdaki değişmez) müstahsilin
        // oran profilini Sales'e senkronla (docs/02 §6). Diğer roller oran taşımaz.
        if (type == PartyRoleType.Producer)
        {
            RaiseWithholdingProfileChangedIfProducer();
        }

        return Result.Success();
    }

    public bool HasRole(PartyRoleType type) => _roles.Any(r => r.Type == type);

    /// <summary>Tarafı pasifleştirir (master veri soft-delete, docs/05 §1). İdempotenttir.</summary>
    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(PartyErrors.AlreadyInactive);
        }

        IsActive = false;
        RaiseDomainEvent(new PartyDeactivated(Id, TenantId, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Taraf müstahsil (Producer) ise ve stopaj profili tanımlıysa
    /// <see cref="ProducerWithholdingProfileChanged"/> cross-service event'ini raise eder —
    /// Sales servisi oran okuma modelini (ProducerRateProfile) günceller (docs/02 §6, hakediş
    /// doğruluğu), Integration servisi ise e-MM kararı için müstahsilin kayıt tutma durumunu alır
    /// (docs/02 §1.3, BK-4). Event outbox'a atomik yazılır (SaveChanges yazıcısı otomatik alır —
    /// docs/04 §10). Party'de GERÇEKTEN var olan oranları (zirai stopaj + çiftçi Bağ-Kur) ve
    /// e-MM gerekliliğini belirleyen <see cref="KeepsRecords"/> bilgisini taşır. Müstahsil
    /// olmayan Party'ler için hiçbir şey yapmaz.
    /// </summary>
    private void RaiseWithholdingProfileChangedIfProducer()
    {
        if (!HasRole(PartyRoleType.Producer) || WithholdingProfile is null)
        {
            return;
        }

        RaiseDomainEvent(new ProducerWithholdingProfileChanged(
            TenantId,
            Id,
            WithholdingProfile.AgriWithholdingRate,
            WithholdingProfile.FarmerSskRate,
            KeepsRecords,
            DateTime.UtcNow));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsAllDigits(string value, int expectedLength) =>
        value.Length == expectedLength && value.All(char.IsDigit);
}

public static class PartyErrors
{
    public static readonly Error DisplayNameRequired =
        new("Party.DisplayNameRequired", "Taraf adı zorunludur.");

    public static readonly Error DisplayNameTooLong =
        new("Party.DisplayNameTooLong", "Taraf adı çok uzun.");

    public static readonly Error InvalidTckn =
        new("Party.InvalidTckn", "TCKN 11 haneli rakamlardan oluşmalıdır.");

    public static readonly Error InvalidVkn =
        new("Party.InvalidVkn", "VKN 10 haneli rakamlardan oluşmalıdır.");

    public static readonly Error RoleRequired =
        new("Party.RoleRequired", "En az bir rol tanımlanmalıdır.");

    public static readonly Error RoleAlreadyExists =
        new("Party.RoleAlreadyExists", "Bu rol tarafa zaten atanmış.");

    public static readonly Error ProducerRequiresWithholdingProfile =
        new("Party.ProducerRequiresWithholdingProfile",
            "Müstahsil (Producer) rolü için stopaj profili tanımlı olmalıdır.");

    public static readonly Error TcknAlreadyInUse =
        new("Party.TcknAlreadyInUse", "Bu TCKN bu işletmede zaten kayıtlı.");

    public static readonly Error VknAlreadyInUse =
        new("Party.VknAlreadyInUse", "Bu VKN bu işletmede zaten kayıtlı.");

    public static readonly Error NotFound =
        new("Party.NotFound", "Taraf bulunamadı.");

    public static readonly Error AlreadyInactive =
        new("Party.AlreadyInactive", "Taraf zaten pasif.");
}
