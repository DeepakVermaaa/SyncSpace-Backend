using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.EntityConfiguration
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            // Primary Key
            builder.HasKey(d => d.Id);

            // Required Properties
            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Description)
                .HasMaxLength(1000);

            builder.Property(d => d.FilePath)
                .IsRequired();

            builder.Property(d => d.FileExtension)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(d => d.FileSize)
                .IsRequired();

            builder.Property(d => d.UploadedAt)
                .IsRequired();

            builder.Property(d => d.UploadedById)
                .IsRequired();

            builder.Property(d => d.ProjectGroupId)
                .IsRequired();

            builder.Property(d => d.CurrentVersionId)
                .IsRequired();

            builder.Property(d => d.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(d => d.Name);
            builder.HasIndex(d => d.UploadedAt);
            builder.HasIndex(d => d.ProjectGroupId);

            // Relationships
            builder.HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            //builder.HasOne(d => d.Project)
            //    .WithMany()
            //    .HasForeignKey(d => d.ProjectGroupId)
            //    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Versions)
                .WithOne(dv => dv.Document)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Permissions)
                .WithOne(dp => dp.Document)
                .HasForeignKey(dp => dp.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
