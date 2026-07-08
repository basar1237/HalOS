using Xunit;

namespace HalOS.Search.Tests;

/// <summary>
/// Gerçek Elasticsearch gerektiren integration testlerini işaretler (Postgres deseninin ES karşılığı).
/// HALOS_TEST_ELASTICSEARCH ortam değişkeni tanımlı değilse test SKIP edilir — `dotnet test` sırasında
/// ES ayakta olmayabilir (docs/07 §7: dış bağımlılık ortam yoksa atlanır). Ayarlıysa değeri ES URL'i
/// olarak kullanılabilir (yoksa http://localhost:9200 varsayılır).
/// </summary>
public sealed class RequiresElasticsearchFactAttribute : FactAttribute
{
    public const string EnvVarName = "HALOS_TEST_ELASTICSEARCH";

    public RequiresElasticsearchFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvVarName)))
        {
            Skip = $"{EnvVarName} ayarlı değil; Elasticsearch gerektiren test atlandı.";
        }
    }

    /// <summary>Testin kullanacağı ES URL'i (env değeri bir URL değilse varsayılan localhost).</summary>
    public static string ResolveUrl()
    {
        var value = Environment.GetEnvironmentVariable(EnvVarName);
        return Uri.TryCreate(value, UriKind.Absolute, out _) ? value! : "http://localhost:9200";
    }
}
