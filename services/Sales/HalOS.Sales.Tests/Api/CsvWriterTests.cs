using FluentAssertions;
using HalOS.Sales.Api;
using Xunit;

namespace HalOS.Sales.Tests.Api;

/// <summary>
/// <see cref="CsvWriter"/> RFC 4180 kaçış ve kültür-bağımsız biçim testleri (docs/06 S2.2 "dışa
/// aktarma"). Virgül/çift tırnak/yeni-satır içeren alanlar doğru sarılmalı; sayı/tarih
/// InvariantCulture (ondalık nokta) ile üretilmeli.
/// </summary>
public sealed class CsvWriterTests
{
    [Fact]
    public void Write_PlainFields_NoQuoting()
    {
        var csv = CsvWriter.Write(
            new[] { "a", "b" },
            new[] { new[] { "1", "2" } });

        csv.Should().Be("a,b\r\n1,2\r\n");
    }

    [Fact]
    public void Write_FieldWithComma_IsQuoted()
    {
        var csv = CsvWriter.Write(
            new[] { "ad" },
            new[] { new[] { "Ali, Veli" } });

        csv.Should().Be("ad\r\n\"Ali, Veli\"\r\n");
    }

    [Fact]
    public void Write_FieldWithDoubleQuote_IsQuotedAndDoubled()
    {
        var csv = CsvWriter.Write(
            new[] { "ad" },
            new[] { new[] { "12\" boru" } });

        // İçteki tırnak ikilenir, alan tırnakla sarılır.
        csv.Should().Be("ad\r\n\"12\"\" boru\"\r\n");
    }

    [Fact]
    public void Write_FieldWithNewline_IsQuoted()
    {
        var csv = CsvWriter.Write(
            new[] { "not" },
            new[] { new[] { "satir1\nsatir2" } });

        csv.Should().Be("not\r\n\"satir1\nsatir2\"\r\n");
    }

    [Fact]
    public void Money_UsesInvariantCultureDotDecimal()
    {
        // Türkçe kültürde ondalık ayıraç virgüldür; CSV her zaman nokta üretmeli.
        CsvWriter.Money(1234.5m).Should().Be("1234.50");
        CsvWriter.Money(0m).Should().Be("0.00");
    }

    [Fact]
    public void Date_UsesIsoFormat()
    {
        CsvWriter.Date(new DateTime(2026, 7, 6)).Should().Be("2026-07-06");
    }

    [Fact]
    public void WriteBytes_IsUtf8WithoutBom()
    {
        var bytes = CsvWriter.WriteBytes(new[] { "x" }, new[] { new[] { "ç" } });

        // BOM (EF BB BF) ile başlamamalı.
        bytes.Length.Should().BeGreaterThan(0);
        (bytes[0] == 0xEF && bytes.Length > 2 && bytes[1] == 0xBB && bytes[2] == 0xBF)
            .Should().BeFalse();
    }
}
