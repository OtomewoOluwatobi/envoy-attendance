using envoy_attendance.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace EnvoyAttendance.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Interest> Interests { get; set; }  
        public DbSet<Member> Members { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // setting unique constraints
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.Email)
                .IsUnique();

            // Configure Attendance relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany()
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Event)
                .WithMany()
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Interest relationship
            modelBuilder.Entity<Interest>()
                .HasOne(i => i.Member)
                .WithMany()
                .HasForeignKey(i => i.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interest>()
                .HasOne(i => i.Event)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Event relationship
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Admin)
                .WithMany()
                .HasForeignKey(e => e.AdminId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Event && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Event eventEntity)
                {
                    eventEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
