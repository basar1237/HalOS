using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");

        // Email value object'i tek kolona sahip owned-benzeri conversion.
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value).Value);

        builder.Property(u => u.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(200);
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>();
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(u => u.TwoFactorSecret).HasColumnName("two_factor_secret").HasMaxLength(256);

        // E-posta tenant içinde tekil (docs/02 §3.1 benzeri değişmez).
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        // Refresh token'lar User aggregate'inin parçası (bağlı entity koleksiyonu).
        // Koleksiyona _refreshTokens alanı üzerinden erişilir (kapsülleme korunur).
        builder.OwnsMany(u => u.RefreshTokens, tokens =>
        {
            tokens.ToTable("refresh_token");
            tokens.WithOwner().HasForeignKey(t => t.UserId);
            tokens.HasKey(t => t.Id);

            // Id domain'de üretilir (Guid.NewGuid); store ÜRETMEZ. ValueGeneratedNever olmazsa EF,
            // takipli User'a login sırasında eklenen yeni token'ı (client-set Guid) MEVCUT satır sanıp
            // Modified işler → var olmayan satıra UPDATE → DbUpdateConcurrencyException (StockItem deseni).
            tokens.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            tokens.Property(t => t.UserId).HasColumnName("user_id");
            tokens.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(256);
            tokens.Property(t => t.ExpiresOnUtc).HasColumnName("expires_on_utc");
            tokens.Property(t => t.CreatedOnUtc).HasColumnName("created_on_utc");
            tokens.Property(t => t.RevokedOnUtc).HasColumnName("revoked_on_utc");

            tokens.HasIndex(t => t.TokenHash);
        });

        builder.Navigation(u => u.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(u => u.DomainEvents);
    }
}
