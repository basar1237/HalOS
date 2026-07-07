namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ bağlantı ayarları ("RabbitMq" bölümü, docs/04 ADR-006). MassTransit host'u bu
/// değerlerle yapılandırılır. Varsayılanlar yerel geliştirme (docker-compose) içindir; üretimde
/// <c>appsettings</c>/ortam değişkenleriyle geçersiz kılınır.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>Yapılandırma bölümü adı ("RabbitMq").</summary>
    public const string SectionName = "RabbitMq";

    /// <summary>Broker host adı (varsayılan localhost).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Sanal host (varsayılan "/").</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Kullanıcı adı (varsayılan guest).</summary>
    public string Username { get; set; } = "guest";

    /// <summary>Parola (varsayılan guest).</summary>
    public string Password { get; set; } = "guest";
}
