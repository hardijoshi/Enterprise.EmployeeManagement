using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Models;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<TaskEntity>Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskEntity>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.AssignedEmployeeId);

                modelBuilder.Entity<TaskEntity>()
               .HasOne(t => t.Reviewer)
               .WithMany() 
               .HasForeignKey(t => t.ReviewerId);
        }
    }

}

