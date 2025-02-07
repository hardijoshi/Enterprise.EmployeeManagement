using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Repositories;
using System.Linq;


namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.ToListAsync();
        }
        


    
        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees.FindAsync(id);
        }

        public async Task CreateEmployeeAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            var existingEmployee = await _context.Employees.FindAsync(employee.Id);
            if (existingEmployee == null)
            {
                return false;
            }

            _context.Entry(existingEmployee).CurrentValues.SetValues(employee);
            return await _context.SaveChangesAsync() > 0;
        }



        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return false; // Employee not found
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true; // Successfully deleted
        }

        public async Task<string> GetEmployeeNameById(int employeeId)
        {
            var employee = await _context.Employees
                                         .Where(e => e.Id == employeeId)
                                         .FirstOrDefaultAsync();

            if (employee == null)
            {
                return null;
            }

            return $"{employee.FirstName} {employee.LastName}";
        }
        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _context.Employees.AnyAsync(e => e.Email == email);
        }


    }
}