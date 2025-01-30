using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.DTO;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public interface IRedisCacheService
    {
        Task<T> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
