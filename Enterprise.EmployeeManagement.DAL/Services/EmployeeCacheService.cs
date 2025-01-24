using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using System.Linq;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public interface IEmployeeCacheService
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task CacheEmployeesAsync(IEnumerable<Employee> employees);
        Task InvalidateEmployeeCacheAsync();
    }

    public class EmployeeCacheService : IEmployeeCacheService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<EmployeeCacheService> _logger;

        private const string EmployeeListCacheKey = "AllEmployees";
        private const string EmployeeIndividualCacheKeyPrefix = "Employee_";

        public EmployeeCacheService(
            IEmployeeRepository employeeRepository,
            IMemoryCache memoryCache,
            ILogger<EmployeeCacheService> logger)
        {
            _employeeRepository = employeeRepository;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            if (_memoryCache.TryGetValue(EmployeeListCacheKey, out IEnumerable<Employee> cachedEmployees))
            {
                return cachedEmployees;
            }

            var employees = await _employeeRepository.GetAllEmployeesAsync();
            await CacheEmployeesAsync(employees);

            return employees;
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            string individualCacheKey = $"{EmployeeIndividualCacheKeyPrefix}{id}";

            if (_memoryCache.TryGetValue(individualCacheKey, out Employee cachedEmployee))
            {
                return cachedEmployee;
            }

            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee != null)
            {
                _memoryCache.Set(individualCacheKey, employee);
            }

            return employee;
        }

        public async Task CacheEmployeesAsync(IEnumerable<Employee> employees)
        {
            // Cache without any expiration
            _memoryCache.Set(EmployeeListCacheKey, employees);

            foreach (var employee in employees)
            {
                string individualCacheKey = $"{EmployeeIndividualCacheKeyPrefix}{employee.Id}";
                _memoryCache.Set(individualCacheKey, employee);
            }
        }

        public Task InvalidateEmployeeCacheAsync()
        {
            _memoryCache.Remove(EmployeeListCacheKey);
            return Task.CompletedTask;
        }
    }
}