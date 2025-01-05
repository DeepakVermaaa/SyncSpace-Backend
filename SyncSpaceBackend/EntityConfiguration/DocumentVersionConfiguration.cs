using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
    {
        public void Configure(EntityTypeBuilder<DocumentVersion> builder)
        {
            // Primary Key
            builder.HasKey(dv => dv.Id);

            // Required Properties
            builder.Property(dv => dv.DocumentId)
                .IsRequired();

            builder.Property(dv => dv.VersionNumber)
                .IsRequired();

            builder.Property(dv => dv.FilePath)
                .IsRequired();

            builder.Property(dv => dv.FileExtension)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(dv => dv.FileSize)
                .IsRequired();

            builder.Property(dv => dv.UploadedAt)
                .IsRequired();

            builder.Property(dv => dv.UploadedById)
                .IsRequired();

            builder.Property(dv => dv.Comment)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(dv => new { dv.DocumentId, dv.VersionNumber })
                .IsUnique();

            // Relationships
            builder.HasOne(dv => dv.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dv => dv.UploadedBy)
                .WithMany()
                .HasForeignKey(dv => dv.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
