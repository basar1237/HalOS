using HalOS.BuildingBlocks.Domain;

namespace HalOS.Party.Domain.ValueObjects;

/// <summary>
/// Müstahsile özel stopaj/kesinti oran profili (docs/02 §1.3, §3.1). Zirai stopaj ve çiftçi
/// Bağ-Kur oranlarının taraf bazında tenant varsayılanını override etmesini sağlar. Tenant
/// varsayılanı yeterli ise profil <c>null</c> bırakılabilir (bkz. <c>Party.WithholdingProfile</c>).
///
/// Oranlar <see cref="decimal"/> (asla float/double — docs/07 §4 / BK-2); NUMERIC(7,4)
/// ölçeğine karşılık gelir (örn. 0.0200 = %2). Yapısal eşitliğe sahiptir.
/// </summary>
public sealed class WithholdingProfile : ValueObject
{
    /// <summary>Oranların üst sınırı (%100). Oran 0 ile 1 arasında olmalıdır.</summary>
    public const decimal MaxRate = 1m;

    private WithholdingProfile(decimal agriWithholdingRate, decimal farmerSskRate)
    {
        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
    }

    /// <summary>ORM materialization only.</summary>
    private WithholdingProfile()
    {
    }

    /// <summary>Zirai stopaj oranı (docs/02 §1.3 <c>AgriculturalWithholding</c>, tipik %2).</summary>
    public decimal AgriWithholdingRate { get; }

    /// <summary>Çiftçi Bağ-Kur (SGK) primi oranı (docs/02 §1.3 <c>FarmerSocialSecurity</c>, tipik %1).</summary>
    public decimal FarmerSskRate { get; }

    public static Result<WithholdingProfile> Create(decimal agriWithholdingRate, decimal farmerSskRate)
    {
        if (agriWithholdingRate < 0m || agriWithholdingRate > MaxRate)
        {
            return Result.Failure<WithholdingProfile>(WithholdingProfileErrors.AgriRateOutOfRange);
        }

        if (farmerSskRate < 0m || farmerSskRate > MaxRate)
        {
            return Result.Failure<WithholdingProfile>(WithholdingProfileErrors.SskRateOutOfRange);
        }

        return new WithholdingProfile(agriWithholdingRate, farmerSskRate);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AgriWithholdingRate;
        yield return FarmerSskRate;
    }
}

public static class WithholdingProfileErrors
{
    public static readonly Error AgriRateOutOfRange =
        new("WithholdingProfile.AgriRateOutOfRange", "Zirai stopaj oranı 0 ile 1 arasında olmalıdır.");

    public static readonly Error SskRateOutOfRange =
        new("WithholdingProfile.SskRateOutOfRange", "Çiftçi Bağ-Kur oranı 0 ile 1 arasında olmalıdır.");
}
