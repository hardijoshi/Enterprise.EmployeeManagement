﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public interface IEmployeeRepository
    {

        Task<List<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task CreateEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> IsEmailExistsAsync(string email);
        Task<string> GetEmployeeNameById(int employeeId);
    }
}
