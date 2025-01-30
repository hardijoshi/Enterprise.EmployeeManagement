using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.Models;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public interface IEmployeeCacheService
    {
        Task<Employee?> GetEmployeeAsync(int id);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task SetEmployeeAsync(Employee employee);
        Task RemoveEmployeeAsync(int id);
        Task InvalidateEmployeeListCache();
        Task UpdateEmployeeListAsync(IEnumerable<int> employeeIds);
    }
}
