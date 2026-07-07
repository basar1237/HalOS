using FluentAssertions;
using HalOS.Identity.Infrastructure.Authentication;
using OtpNet;
using Xunit;

namespace HalOS.Identity.Tests.Infrastructure;

public sealed class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSecret_ThenVerify_ValidCode_Succeeds()
    {
        var secret = _sut.GenerateSecret();

        // Authenticator uygulamasının üreteceği kodu Otp.NET ile hesapla.
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        var code = totp.ComputeTotp();

        _sut.VerifyCode(secret, code).Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_WrongCode_Fails()
    {
        var secret = _sut.GenerateSecret();

        _sut.VerifyCode(secret, "000000").Should().BeFalse();
    }

    [Fact]
    public void BuildOtpAuthUri_ContainsIssuerAndSecret()
    {
        var uri = _sut.BuildOtpAuthUri("SECRET", "ali@hal.com", "HalOS");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=SECRET");
        uri.Should().Contain("issuer=HalOS");
    }
}
