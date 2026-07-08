using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HalOS.Gateway.Tests;

/// <summary>
/// Gateway'in kendi sağlık ucu (yönlendirme yok) uçtan uca yanıt veriyor mu. Arka servisler
/// gerekmeden çalışır (proxy'siz uç).
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }

    [Fact]
    public async Task UnmappedPath_ReturnsNotFound()
    {
        // Hiçbir rotaya uymayan yol (ör. /api/bilinmeyen) → 404. Proxy yalnız tanımlı
        // /api/{servis} önekleri için devreye girer.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/bilinmeyen/x");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
