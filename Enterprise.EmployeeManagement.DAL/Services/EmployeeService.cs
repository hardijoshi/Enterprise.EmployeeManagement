using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using System.Threading.Tasks;
using System.Linq;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeMapper _employeeMapper;

        public EmployeeService(IEmployeeRepository employeeRepository, IEmployeeMapper employeeMapper)
        {
            _employeeRepository = employeeRepository;
            _employeeMapper = employeeMapper;
        }

        public async Task<List<EmployeeDTO>> GetAllEmployeesAsync()
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            return employees.Select(e => _employeeMapper.MapToDTO(e)).ToList();
        }

        public async Task<EmployeeDTO> GetEmployeeByIdAsync(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            return _employeeMapper.MapToDTO(employee);
        }

        public async Task CreateEmployeeAsync(EmployeeDTO employeeDto)
        {
            if (await _employeeRepository.IsEmailExistsAsync(employeeDto.Email))
            {
                throw new Exception("An employee with the same email already exists.");
            }

            var employee = _employeeMapper.MapToEntity(employeeDto);
            await _employeeRepository.CreateEmployeeAsync(employee);
        }

        public async Task UpdateEmployeeAsync(int id, EmployeeDTO employeeDto)
        {
            var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                throw new Exception($"Employee with ID {id} not found.");
            }

            if (existingEmployee.Email != employeeDto.Email && await _employeeRepository.IsEmailExistsAsync(employeeDto.Email))
            {
                throw new Exception("An employee with the same email already exists.");
            }

            existingEmployee = _employeeMapper.MapToEntity(employeeDto);
            await _employeeRepository.UpdateEmployeeAsync(existingEmployee);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            return await _employeeRepository.DeleteEmployeeAsync(id);
        }
    }
}
