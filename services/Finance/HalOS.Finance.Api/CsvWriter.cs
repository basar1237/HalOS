using System.Globalization;
using System.Text;

namespace HalOS.Finance.Api;

/// <summary>
/// Basit, harici pakete bağımlı OLMAYAN CSV serileştirici (docs/06 S2.2 "dışa aktarma"). RFC 4180
/// kaçış kuralı: virgül, çift tırnak veya satır sonu içeren alan çift tırnakla sarılır ve içindeki
/// çift tırnaklar ikilenir. Sayı/tarih biçimi kültür-bağımsız (<see cref="CultureInfo.InvariantCulture"/>).
/// Bu yardımcı yalnız Finance Api sunum katmanına aittir; Application query'leri değişmez.
/// Sales.CsvWriter deseniyle birebir (servisler arası kod paylaşımı YOK — her serviste küçük kopya).
/// </summary>
public static class CsvWriter
{
    // RFC 4180: satır sonu CRLF.
    private const string LineTerminator = "\r\n";

    /// <summary>Başlık + satırlardan CSV metni üretir. Hücreler çağıran tarafça biçimlenmiş string.</summary>
    public static string Write(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        AppendRow(sb, header);
        foreach (var row in rows)
        {
            AppendRow(sb, row);
        }

        return sb.ToString();
    }

    /// <summary>UTF-8 (BOM'suz) bayt dizisi — <c>FileContentResult</c> içeriği için.</summary>
    public static byte[] WriteBytes(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows) =>
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(Write(header, rows));

    /// <summary>Kültür-bağımsız ondalık biçim (nokta ayıraç, iki hane).</summary>
    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Kültür-bağımsız tam sayı biçimi.</summary>
    public static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Kültür-bağımsız ISO tarih (yyyy-MM-dd).</summary>
    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static void AppendRow(StringBuilder sb, IReadOnlyList<string> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(Escape(cells[i]));
        }

        sb.Append(LineTerminator);
    }

    /// <summary>
    /// RFC 4180 alan kaçışı: virgül / çift tırnak / CR / LF içeriyorsa alan çift tırnakla sarılır,
    /// içindeki çift tırnaklar ikilenir. Aksi halde alan olduğu gibi döner.
    /// </summary>
    private static string Escape(string field)
    {
        if (field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }
}
