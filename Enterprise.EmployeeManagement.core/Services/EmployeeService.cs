using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Enterprise.EmployeeManagement.core.Interfaces;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.DTO;
using System.Linq;
using Enterprise.EmployeeManagement.core.Common.Responses;


namespace Enterprise.EmployeeManagement.core.Services
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

        public async Task<ResponseMessage<bool>> CreateEmployeeAsync(EmployeeDTO employeeDto)
        {
            if (await _employeeRepository.IsEmailExistsAsync(employeeDto.Email))
            {
                return new ResponseMessage<bool> { Success = false, Message = "An employee with the same email already exists." };
            }

            var employee = _employeeMapper.MapToEntity(employeeDto);
            await _employeeRepository.CreateEmployeeAsync(employee);
            return new ResponseMessage<bool> { Success = true, Data = true };
        }

        public async Task<ResponseMessage<bool>> UpdateEmployeeAsync(int id, EmployeeDTO employeeDto)
        {
            var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                
                return new ResponseMessage<bool> { Success = false, Message = "Employee with ID {id} not found." };
            }


            if (existingEmployee.Email != employeeDto.Email && await _employeeRepository.IsEmailExistsAsync(employeeDto.Email))
            {
                return new ResponseMessage<bool> { Success = false, Message = "An employee with the same email already exists." };
            }

            existingEmployee = _employeeMapper.MapToEntity(employeeDto);
            await _employeeRepository.UpdateEmployeeAsync(existingEmployee);
            return new ResponseMessage<bool> { Success = true, Data = true };
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            return await _employeeRepository.DeleteEmployeeAsync(id);
        }
    }
}
