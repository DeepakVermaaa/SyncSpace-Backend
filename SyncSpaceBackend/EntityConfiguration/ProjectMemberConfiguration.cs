using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Models;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.Role)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(pm => pm.JoinedAt)
                .IsRequired();

            builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();
        }
    }

}
