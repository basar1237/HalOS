using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>consignment</c> tablosu eşlemesi (docs/05 §3.4). snake_case kolonlar (docs/07 §3). Müstahsil
/// referansı ID ile (FK değil — docs/05 §5). Kalemler aggregate'in parçasıdır (_items).
/// </summary>
internal sealed class ConsignmentConfiguration : IEntityTypeConfiguration<Consignment>
{
    public void Configure(EntityTypeBuilder<Consignment> builder)
    {
        builder.ToTable("consignment");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(c => c.ReceivedAt).HasColumnName("received_at");
        builder.Property(c => c.DispatchNoteRef).HasColumnName("dispatch_note_ref").HasMaxLength(100);
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.CreatedOnUtc).HasColumnName("created_at");

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.ConsignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Sık filtre: müstahsile göre mal geliş (docs/05 §6 (tenant_id, <alan>)).
        builder.HasIndex(c => new { c.TenantId, c.ProducerPartyId });

        builder.Ignore(c => c.DomainEvents);
    }
}
