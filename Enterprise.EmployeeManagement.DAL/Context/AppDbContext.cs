using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
    }
}
