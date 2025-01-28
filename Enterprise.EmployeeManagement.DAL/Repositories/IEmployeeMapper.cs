using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public interface IEmployeeMapper
    {
        EmployeeDTO MapToDTO(Employee employee);
        Employee MapToEntity(EmployeeDTO employeeDto);
    }
}
