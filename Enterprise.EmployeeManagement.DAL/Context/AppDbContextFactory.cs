using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;

using Pomelo.EntityFrameworkCore.MySql.Storage;
using System.IO;
using Enterprise.EmployeeManagement.DAL;
using System;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Enterprise.EmployeeManagement.DAL.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Build the configuration to access appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  // Set the base path to the current directory
                .AddJsonFile(@"C:\Users\db2ha\source\repos\Enterprise.EmployeeManagement\Enterprise.EmployeeManagement\appsettings.json", optional: false, reloadOnChange: true) 
                .Build();

            // Create DbContextOptions using the connection string from appsettings.json
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Use MySQL with explicit ServerVersion or AutoDetect
            optionsBuilder.UseMySql(
                configuration.GetConnectionString("DefaultConnection")
            );


            return new AppDbContext(optionsBuilder.Options);
        }
    }

}
