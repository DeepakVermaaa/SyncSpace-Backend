using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Models;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class ProjectGroupConfiguration : IEntityTypeConfiguration<ProjectGroup>
    {
        public void Configure(EntityTypeBuilder<ProjectGroup> builder)
        {
            builder.HasKey(pg => pg.Id);

            builder.Property(pg => pg.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pg => pg.Description)
                .HasMaxLength(500);

            builder.Property(pg => pg.CreatedAt)
                .IsRequired();

            // Relationships
            builder.HasOne(pg => pg.CreatedBy)
                .WithMany()
                .HasForeignKey(pg => pg.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pg => pg.ProjectMembers)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pg => pg.ChatRooms)
                .WithOne(cr => cr.ProjectGroup)
                .HasForeignKey(cr => cr.ProjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pg => pg.ChatMessages)
                .WithOne(cm => cm.ProjectGroup)
                .HasForeignKey(cm => cm.ProjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
