using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeDTO>> GetAllEmployeesAsync();
        Task<EmployeeDTO> GetEmployeeByIdAsync(int id);
        Task CreateEmployeeAsync(EmployeeDTO employeeDto);
        Task UpdateEmployeeAsync(int id, EmployeeDTO employeeDto);
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
