﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Models;
using static SyncSpaceBackend.Enums.Enum;

namespace SyncSpaceBackend.EntityConfiguration
{
    public class ProjectGroupConfiguration : IEntityTypeConfiguration<ProjectGroup>
    {
        public void Configure(EntityTypeBuilder<ProjectGroup> builder)
        {
            // Primary Key
            builder.HasKey(pg => pg.Id);

            // Required Properties
            builder.Property(pg => pg.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pg => pg.Description)
                .HasMaxLength(1000);

            builder.Property(pg => pg.CreatedAt)
                .IsRequired();

            builder.Property(pg => pg.CreatedById)
                .IsRequired();

            builder.Property(pg => pg.OrganizationId)
                .IsRequired();

            builder.Property(pg => pg.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(pg => pg.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(ProjectStatus.Planning);

            // Optional Properties
            builder.Property(pg => pg.StartDate)
                .IsRequired(false);

            builder.Property(pg => pg.EndDate)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(pg => pg.Name);
            builder.HasIndex(pg => pg.CreatedAt);
            builder.HasIndex(pg => pg.Status);
            builder.HasIndex(pg => pg.OrganizationId);

            // Relationships
            builder.HasOne(pg => pg.Organizations)
                .WithMany(o => o.Projects)
                .HasForeignKey(pg => pg.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pg => pg.CreatedBy)
                .WithMany()
                .HasForeignKey(pg => pg.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pg => pg.Documents)
           .WithOne(d => d.Project)
           .HasForeignKey(d => d.ProjectGroupId)
           .OnDelete(DeleteBehavior.Cascade);

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

            builder.HasMany(pg => pg.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pg => pg.Milestones)
                .WithOne(m => m.Project)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            builder.HasQueryFilter(pg => pg.IsActive);

            builder.HasIndex(pg => new { pg.OrganizationId, pg.Name })
                .IsUnique();
        }
    }
}