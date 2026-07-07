using Xunit;

namespace HalOS.Integration.Tests;

/// <summary>
/// Gerçek Postgres gerektiren integration testlerini işaretler. HALOS_TEST_POSTGRES ortam değişkeni
/// tanımlı değilse test SKIP edilir — `dotnet test` sırasında Postgres ayakta olmayabilir (docs/07 §7).
/// Finance/Sales/Party deseniyle birebir.
/// </summary>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HALOS_TEST_POSTGRES")))
        {
            Skip = "HALOS_TEST_POSTGRES ayarlı değil; Postgres gerektiren test atlandı.";
        }
    }
}
