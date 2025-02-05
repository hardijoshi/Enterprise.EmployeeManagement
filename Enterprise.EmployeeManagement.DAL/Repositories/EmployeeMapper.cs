using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;

namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public class EmployeeMapper : IEmployeeMapper
    {
        public EmployeeDTO MapToDTO(Employee employee)
        {
            if (employee == null)
            {
                return null;
            }

            return new EmployeeDTO
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Role = employee.Role.ToString(), // Enum to string
                Email = employee.Email,
                MobileNumber = employee.MobileNumber
            };
        }

        public Employee MapToEntity(EmployeeDTO employeeDto)
        {
            if (employeeDto == null)
            {
                return null;
            }

            return new Employee
            {
                Id = employeeDto.Id,
                FirstName = employeeDto.FirstName,
                LastName = employeeDto.LastName,
                Role = Enum.Parse<RoleType>(employeeDto.Role), // String to Enum
                Email = employeeDto.Email,
                MobileNumber = employeeDto.MobileNumber
            };
        }
    }
}
