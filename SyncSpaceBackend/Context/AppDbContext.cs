using WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using SyncSpaceBackend.Models;
using SyncSpaceBackend.EntityConfiguration;
using System.Reflection.Emit;

namespace WebAPI.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProjectGroup> ProjectGroups { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>().ToTable("users");

            builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyConfiguration(new ProjectGroupConfiguration());
            builder.ApplyConfiguration(new ProjectMemberConfiguration());
        }
    }
}