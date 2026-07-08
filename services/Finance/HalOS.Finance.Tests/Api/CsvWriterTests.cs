using FluentAssertions;
using HalOS.Finance.Api;
using Xunit;

namespace HalOS.Finance.Tests.Api;

/// <summary>
/// <see cref="CsvWriter"/> RFC 4180 kaçış ve kültür-bağımsız biçim testleri (docs/06 S2.2 "dışa
/// aktarma"). Sales tarafıyla aynı davranış (her serviste küçük kopya; paylaşım YOK).
/// </summary>
public sealed class CsvWriterTests
{
    [Fact]
    public void Write_FieldWithCommaAndQuote_IsEscaped()
    {
        var csv = CsvWriter.Write(
            new[] { "kova", "not" },
            new[] { new[] { "0-15 gun", "Ali, \"Veli\"" } });

        // Virgül ve tırnak içeren ikinci alan sarılır, içteki tırnaklar ikilenir.
        csv.Should().Be("kova,not\r\n0-15 gun,\"Ali, \"\"Veli\"\"\"\r\n");
    }

    [Fact]
    public void Money_UsesInvariantCultureDotDecimal()
    {
        CsvWriter.Money(1000m).Should().Be("1000.00");
    }

    [Fact]
    public void Number_UsesInvariantCulture()
    {
        CsvWriter.Number(4).Should().Be("4");
    }
}
