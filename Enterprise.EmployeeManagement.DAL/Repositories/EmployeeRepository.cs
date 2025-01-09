using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Context;

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
            try
            {
                return await _context.Employees.ToListAsync();
            }
            catch (Exception ex)
            {
                // Log exception here (e.g., using ILogger)
                throw new Exception("Error fetching employees.", ex);
            }
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            try
            {
                return await _context.Employees.FindAsync(id);
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception($"Error fetching employee with ID {id}.", ex);
            }
        }

        public async Task CreateEmployeeAsync(Employee employee)
        {
            try
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception("Error creating employee.", ex);
            }
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            var existingEmployee = await _context.Employees.FindAsync(employee.Id);
            if (existingEmployee == null)
            {
                throw new Exception($"Employee with ID {employee.Id} not found.");
            }

            _context.Entry(existingEmployee).CurrentValues.SetValues(employee);
            await _context.SaveChangesAsync();
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


    }
}
