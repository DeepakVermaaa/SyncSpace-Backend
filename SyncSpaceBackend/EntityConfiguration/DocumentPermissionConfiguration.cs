using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class DocumentPermissionConfiguration : IEntityTypeConfiguration<DocumentPermission>
    {
        public void Configure(EntityTypeBuilder<DocumentPermission> builder)
        {
            // Primary Key
            builder.HasKey(dp => dp.Id);

            // Required Properties
            builder.Property(dp => dp.DocumentId)
                .IsRequired();

            builder.Property(dp => dp.UserId)
                .IsRequired();

            builder.Property(dp => dp.PermissionLevel)
                .IsRequired()
                .HasConversion<string>();

            // Indexes
            builder.HasIndex(dp => new { dp.DocumentId, dp.UserId })
                .IsUnique();

            // Relationships
            builder.HasOne(dp => dp.Document)
                .WithMany(d => d.Permissions)
                .HasForeignKey(dp => dp.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dp => dp.User)
                .WithMany()
                .HasForeignKey(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
