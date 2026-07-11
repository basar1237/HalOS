using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Finance.Infrastructure.Persistence.Configurations;

/// <summary><c>cheque</c> tablosu eşlemesi (docs/11 §3.5). snake_case; para numeric(18,2) (BK-2).</summary>
internal sealed class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.ToTable("cheque");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.PartyId).HasColumnName("party_id");
        builder.Property(c => c.BankName).HasColumnName("bank_name").HasMaxLength(200);
        builder.Property(c => c.SerialNo).HasColumnName("serial_no").HasMaxLength(100);
        builder.Property(c => c.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(c => c.IssueDate).HasColumnName("issue_date");
        builder.Property(c => c.DueDate).HasColumnName("due_date");
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(c => c.CreatedOnUtc).HasColumnName("created_at");

        builder.HasIndex(c => new { c.TenantId, c.DueDate });
        builder.HasIndex(c => new { c.TenantId, c.Status });

        builder.Ignore(c => c.DomainEvents);
    }
}
