using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Enterprise.EmployeeManagement.core.MailService;
using Enterprise.EmployeeManagement.core.Interfaces;
using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.core.Services;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using NLog.Web;
using NLog.Extensions.Logging;
using Enterprise.EmployeeManagement.DAL.Services;
using StackExchange.Redis;
using Autofac;

namespace Enterprise.EmployeeManagement.Web
{
    public class Startup
    {
        private ILogger<Startup> _logger;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

        }

        public IConfiguration Configuration { get; }

        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace); 
                logging.AddNLog();
            });
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = ConfigurationOptions.Parse(Configuration["RedisCacheOptions:Configuration"], true);
                return ConnectionMultiplexer.Connect(configuration);
            });
            
            services.AddControllers();


            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            });
            services.AddControllersWithViews();
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    Configuration.GetConnectionString("DefaultConnection")));
            

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<TaskService>()
                   .As<ITaskService>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<TaskRepository>()
                   .As<ITaskRepository>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<TaskMapper>()
                  .As<ITaskMapper>()
                  .InstancePerLifetimeScope();

            builder.RegisterType<EmployeeService>()
                   .As<IEmployeeService>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<EmployeeRepository>()
                   .As<IEmployeeRepository>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<EmployeeMapper>()
                   .As<IEmployeeMapper>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<EmailService>()
                   .As<IEmailService>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<EmployeeCacheService>()
                   .As<IEmployeeCacheService>()
                   .InstancePerLifetimeScope();

            builder.RegisterType<RedisCacheService>()
                   .As<IRedisCacheService>()
                   .InstancePerLifetimeScope();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            _logger = logger;
            _logger.LogInformation("Starting application...");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

           
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();



            app.UseEndpoints(endpoints =>
            {
                
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Account}/{action=Login}/{id?}");
                endpoints.MapControllers();



            });

        }
    }
}
