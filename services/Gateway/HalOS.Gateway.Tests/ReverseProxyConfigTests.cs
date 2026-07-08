using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace HalOS.Gateway.Tests;

/// <summary>
/// YARP yapılandırmasının (appsettings "ReverseProxy") beklenen rotalar/kümelerle yüklendiğini
/// doğrular. Rota→küme eşlemesi ve destinasyon adreslerinin varlığı sözleşmedir; frontend bu
/// /api/{servis} öneklerine güvenir. Arka servis GEREKMEZ (yalnız yüklenen config incelenir).
/// </summary>
public sealed class ReverseProxyConfigTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReverseProxyConfigTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private IProxyConfig LoadConfig()
    {
        var provider = _factory.Services.GetRequiredService<IProxyConfigProvider>();
        return provider.GetConfig();
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("party")]
    [InlineData("sales")]
    [InlineData("finance")]
    [InlineData("integration")]
    [InlineData("inventory")]
    [InlineData("search")]
    [InlineData("notification-hub")]
    [InlineData("ai")]
    public void Route_IsConfigured(string routeId)
    {
        var config = LoadConfig();

        config.Routes.Should().Contain(r => r.RouteId == routeId);
    }

    [Theory]
    [InlineData("sales", "/api/sales/{**catch-all}")]
    [InlineData("finance", "/api/finance/{**catch-all}")]
    [InlineData("notification-hub", "/hubs/{**catch-all}")]
    public void Route_MatchesExpectedPath(string routeId, string expectedPath)
    {
        var config = LoadConfig();

        var route = config.Routes.Single(r => r.RouteId == routeId);
        route.Match.Path.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("party")]
    [InlineData("sales")]
    [InlineData("finance")]
    [InlineData("integration")]
    [InlineData("inventory")]
    [InlineData("search")]
    [InlineData("notification")]
    [InlineData("ai")]
    public void Cluster_HasReachableDestinationAddress(string clusterId)
    {
        var config = LoadConfig();

        var cluster = config.Clusters.Single(c => c.ClusterId == clusterId);
        cluster.Destinations.Should().NotBeNull();
        cluster.Destinations!.Should().NotBeEmpty();
        cluster.Destinations!.Values
            .Should()
            .OnlyContain(d => !string.IsNullOrWhiteSpace(d.Address));
    }

    [Fact]
    public void ApiRoutes_StripServicePrefix()
    {
        // /api/{servis} önekini kaldıran transform olmalı → arka servis kök yolundan (ör.
        // /reports/daily) yanıt verir. (Hub ve ai rotaları önek KORUR; onlar hariç.)
        var config = LoadConfig();
        var prefixed = new[]
        {
            "identity", "party", "sales", "finance",
            "integration", "inventory", "search",
        };

        foreach (var routeId in prefixed)
        {
            var route = config.Routes.Single(r => r.RouteId == routeId);
            route.Transforms
                .Should()
                .Contain(
                    t => t.ContainsKey("PathRemovePrefix"),
                    "'{0}' rotası /api/{0} önekini kaldırmalı",
                    routeId);
        }
    }
}
