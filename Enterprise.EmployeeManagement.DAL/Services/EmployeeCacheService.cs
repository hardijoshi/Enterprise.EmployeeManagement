using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.DTO;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZeroFormatter;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public class EmployeeCacheService : IEmployeeCacheService
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ILogger<EmployeeCacheService> _logger;
        private const string EmployeeHashKey = "employees";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        public EmployeeCacheService(
            IConnectionMultiplexer redisConnection,
            ILogger<EmployeeCacheService> logger)
        {
            _redisConnection = redisConnection;
            _logger = logger;
        }

        private static Employee FromDto(EmployeeDTO dto)
        {
            return new Employee
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = Enum.Parse<RoleType>(dto.Role),
                Email = dto.Email,
                MobileNumber = dto.MobileNumber,
                Tasks = new List<TaskEntity>() 
            };
        }

        public async Task<Employee?> GetEmployeeAsync(int id)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var value = await db.HashGetAsync(EmployeeHashKey, id.ToString());

                if (!value.HasValue)
                    return null;

                var serializedData = (byte[])value;
                return ZeroFormatterSerializer.Deserialize<Employee>(serializedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {Id} from cache", id);
                return null;
            }
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var hashEntries = await db.HashGetAllAsync(EmployeeHashKey);

                if (!hashEntries.Any())
                    return Enumerable.Empty<Employee>();

                var employees = new List<Employee>();
                foreach (var entry in hashEntries)
                {
                    try
                    {
                        var serializedData = (byte[])entry.Value;
                        var dto = ZeroFormatterSerializer.Deserialize<EmployeeDTO>(serializedData);
                        employees.Add(FromDto(dto));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing employee {Id}", entry.Name);
                        continue;
                    }
                }

                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees from cache");
                return Enumerable.Empty<Employee>();
            }
        }

        public async Task SetEmployeeAsync(Employee employee)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var serializedData = ZeroFormatterSerializer.Serialize(employee);

                await db.HashSetAsync(EmployeeHashKey, employee.Id.ToString(), serializedData);
                await db.KeyExpireAsync(EmployeeHashKey, CacheExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching employee {Id}", employee.Id);
                throw;
            }
        }

        public async Task RemoveEmployeeAsync(int id)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                await db.HashDeleteAsync(EmployeeHashKey, id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing employee {Id} from cache", id);
                throw;
            }
        }

        public async Task InvalidateEmployeeListCache()
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                await db.KeyDeleteAsync(EmployeeHashKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating employee list cache");
                throw;
            }
        }

        public async Task UpdateEmployeeListAsync(IEnumerable<int> employeeIds)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var currentEmployees = await GetAllEmployeesAsync();

                // Get employees to remove
                var currentIds = currentEmployees.Select(e => e.Id.ToString());
                var newIds = employeeIds.Select(id => id.ToString());
                var idsToRemove = currentIds.Except(newIds);

                // Remove employees not in the new list
                if (idsToRemove.Any())
                {
                    await db.HashDeleteAsync(EmployeeHashKey,
                        idsToRemove.Select(id => (RedisValue)id).ToArray());
                }

                await db.KeyExpireAsync(EmployeeHashKey, CacheExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee list cache");
                throw;
            }
        }
    }
}