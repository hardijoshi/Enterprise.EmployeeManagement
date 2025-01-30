using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<TaskEntity> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define relationships
            modelBuilder.Entity<TaskEntity>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.AssignedEmployeeId);

            modelBuilder.Entity<TaskEntity>()
               .HasOne(t => t.Reviewer)
               .WithMany()
               .HasForeignKey(t => t.ReviewerId);

            // Define property configurations outside of relationships
            modelBuilder.Entity<TaskEntity>()
                .Property(t => t.StartDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<TaskEntity>()
                .Property(t => t.DeadlineDate)
                .IsRequired();
        }
    }
}
