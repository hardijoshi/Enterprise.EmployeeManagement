using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.core.Common.Responses;

namespace Enterprise.EmployeeManagement.core.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeDTO>> GetAllEmployeesAsync();
        Task<EmployeeDTO> GetEmployeeByIdAsync(int id);
        Task<ResponseMessage<bool>> CreateEmployeeAsync(EmployeeDTO employeeDto);
        Task<ResponseMessage<bool>> UpdateEmployeeAsync(int id, EmployeeDTO employeeDto);
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
