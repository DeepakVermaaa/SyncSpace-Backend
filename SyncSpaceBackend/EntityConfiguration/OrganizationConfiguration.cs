using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Models;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organizations>
    {
        public void Configure(EntityTypeBuilder<Organizations> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Domain)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Description)
                .HasMaxLength(500);

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(o => o.Domain).IsUnique();
            builder.HasIndex(o => o.Name);

            // Relationships
            builder.HasMany(o => o.Users)
                .WithOne(u => u.Organizations)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Projects)
                .WithOne(p => p.Organizations)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(o => o.IsActive);
        }
    }
}
