namespace HalOS.Identity.Application.Abstractions;

/// <summary>Parola hash'leme/doğrulama port'u (impl. Infrastructure'da).</summary>
public interface IPasswordHasher
{
    /// <summary>Düz parolayı hash'ler.</summary>
    string Hash(string password);

    /// <summary>Düz parolayı saklanan hash ile karşılaştırır.</summary>
    bool Verify(string hashedPassword, string providedPassword);
}
