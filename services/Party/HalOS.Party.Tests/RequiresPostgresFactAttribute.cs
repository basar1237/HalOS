using Xunit;

namespace HalOS.Party.Tests;

/// <summary>
/// Gerçek Postgres gerektiren integration testlerini işaretler. HALOS_TEST_POSTGRES ortam
/// değişkeni tanımlı değilse test SKIP edilir — `dotnet test` sırasında Postgres ayakta
/// olmayabilir (docs/07 §7: dış bağımlılık ortam yoksa atlanır).
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
