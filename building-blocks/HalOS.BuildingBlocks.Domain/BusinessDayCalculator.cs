namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// İş günü hesaplayıcı (docs/03 §4 BK-3). Verilen tarihe belirtilen sayıda İŞ GÜNÜ ekler:
/// Cumartesi/Pazar ve resmi tatiller atlanır. Sabit (her yıl aynı güne düşen) Türk resmi
/// tatilleri gömülüdür: 1 Ocak (Yılbaşı), 23 Nisan (Ulusal Egemenlik ve Çocuk Bayramı),
/// 1 Mayıs (Emek ve Dayanışma Günü), 19 Mayıs (Atatürk'ü Anma, Gençlik ve Spor Bayramı),
/// 15 Temmuz (Demokrasi ve Millî Birlik Günü), 30 Ağustos (Zafer Bayramı),
/// 29 Ekim (Cumhuriyet Bayramı).
///
/// Hareketli dini bayramlar (Ramazan/Kurban) ay takvimine bağlı olduğundan HARDCODE EDİLMEZ;
/// bunlar çağıran katman tarafından <paramref name="additionalHolidays"/> kümesiyle enjekte
/// edilir (örn. tenant konfigürasyonu / tatil takvimi servisi). Böylece Domain harici pakete
/// bağımlı olmadan (sadece BCL) esnek kalır.
/// </summary>
public static class BusinessDayCalculator
{
    /// <summary>
    /// Sabit tarihli (her yıl aynı gün/ay) Türk resmi tatilleri (gün, ay) çiftleri. Hareketli
    /// dini bayramlar burada YER ALMAZ — bunlar <c>additionalHolidays</c> ile enjekte edilir.
    /// </summary>
    private static readonly (int Day, int Month)[] FixedPublicHolidays =
    {
        (1, 1),    // Yılbaşı
        (23, 4),   // Ulusal Egemenlik ve Çocuk Bayramı
        (1, 5),    // Emek ve Dayanışma Günü
        (19, 5),   // Atatürk'ü Anma, Gençlik ve Spor Bayramı
        (15, 7),   // Demokrasi ve Millî Birlik Günü
        (30, 8),   // Zafer Bayramı
        (29, 10),  // Cumhuriyet Bayramı
    };

    /// <summary>
    /// <paramref name="start"/> tarihine <paramref name="businessDays"/> iş günü ekler; hafta
    /// sonlarını (Cumartesi/Pazar) ve resmi tatilleri (sabit + <paramref name="additionalHolidays"/>)
    /// atlar. <paramref name="businessDays"/> 0 ise başlangıç günü (hafta sonu/tatil olsa bile bir
    /// sonraki iş gününe taşınmadan) aynen döner.
    /// </summary>
    /// <param name="start">Başlangıç tarih-zamanı (saat kısmı korunur).</param>
    /// <param name="businessDays">Eklenecek iş günü sayısı (negatif olamaz).</param>
    /// <param name="additionalHolidays">
    /// Sabit resmi tatillere EK olarak atlanacak günler (hareketli dini bayramlar vb.). Yalnızca
    /// tarih kısmı (<see cref="DateOnly"/>) dikkate alınır; null ise yalnızca sabit tatiller uygulanır.
    /// </param>
    public static DateTime AddBusinessDays(
        DateTime start,
        int businessDays,
        IReadOnlySet<DateOnly>? additionalHolidays = null)
    {
        if (businessDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(businessDays), "İş günü sayısı negatif olamaz.");
        }

        var date = start;
        var added = 0;

        while (added < businessDays)
        {
            date = date.AddDays(1);
            if (!IsWeekend(date) && !IsHoliday(date, additionalHolidays))
            {
                added++;
            }
        }

        return date;
    }

    /// <summary>
    /// <paramref name="date"/> bir iş günü mü? Hafta sonu ve resmi tatil (sabit +
    /// <paramref name="additionalHolidays"/>) iş günü DEĞİLDİR.
    /// </summary>
    public static bool IsBusinessDay(
        DateTime date,
        IReadOnlySet<DateOnly>? additionalHolidays = null)
        => !IsWeekend(date) && !IsHoliday(date, additionalHolidays);

    private static bool IsWeekend(DateTime date)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static bool IsHoliday(DateTime date, IReadOnlySet<DateOnly>? additionalHolidays)
    {
        foreach (var (day, month) in FixedPublicHolidays)
        {
            if (date.Day == day && date.Month == month)
            {
                return true;
            }
        }

        return additionalHolidays is not null
            && additionalHolidays.Contains(DateOnly.FromDateTime(date));
    }
}
