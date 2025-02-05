using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Enterprise.EmployeeManagement.core.Utilities;
using Enterprise.EmployeeManagement.DAL.Services;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeCacheService _employeeCacheService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IEmployeeCacheService employeeCacheService,
            ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _employeeCacheService = employeeCacheService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            _logger.LogInformation("Index action accessed by {UserEmail}", userEmail);

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Unauthorized access attempt. Redirecting to login.");
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        [Route("api/employees")]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var cachedEmployees = await _employeeCacheService.GetAllEmployeesAsync();

                if (cachedEmployees.Any())
                {
                    _logger.LogInformation("Fetching employees from Redis cache.");
                    return Ok(cachedEmployees);
                }

                _logger.LogInformation("Fetching employees from database.");
                var employeesFromDb = await _employeeRepository.GetAllEmployeesAsync();

                if (employeesFromDb.Any())
                {
                    // Cache each employee
                    foreach (var employee in employeesFromDb)
                    {
                        await _employeeCacheService.SetEmployeeAsync(employee);
                    }
                }

                return Ok(employeesFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employees.");
                return StatusCode(500, new { error = "An error occurred while fetching employees." });
            }
        }

        [HttpGet]
        [Route("Employee/GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var cachedEmployee = await _employeeCacheService.GetEmployeeAsync(id);

                if (cachedEmployee != null)
                {
                    return Ok(cachedEmployee);
                }

                var employeeFromDb = await _employeeRepository.GetEmployeeByIdAsync(id);

                if (employeeFromDb == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeId}", id);
                    return NotFound(new { message = "Employee not found." });
                }

                await _employeeCacheService.SetEmployeeAsync(employeeFromDb);

                return Ok(employeeFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching details for employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while fetching employee details." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            try
            {
                if (await _employeeRepository.IsEmailExistsAsync(employee.Email))
                {
                    return BadRequest(new { success = false, message = "An employee with the same email already exists." });
                }

                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }

                await _employeeRepository.CreateEmployeeAsync(employee);
                await _employeeCacheService.SetEmployeeAsync(employee);

                return Ok(new { success = true, message = "Employee created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee: {Email}", employee.Email);
                return StatusCode(500, new { error = "An error occurred while creating the employee." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(employee.Id);
                if (existingEmployee == null)
                {
                    return NotFound(new { success = false, message = $"Employee with ID {employee.Id} not found." });
                }

                if (existingEmployee.Email != employee.Email && await _employeeRepository.IsEmailExistsAsync(employee.Email))
                {
                    return BadRequest(new { success = false, message = "An employee with the same email already exists." });
                }

                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }
                else
                {
                    employee.Password = existingEmployee.Password;
                }

                await _employeeRepository.UpdateEmployeeAsync(employee);
                await _employeeCacheService.SetEmployeeAsync(employee);

                return Ok(new { success = true, message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {EmployeeId}", employee.Id);
                return StatusCode(500, new { error = "An error occurred while updating the employee." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid employee ID." });
            }

            try
            {
                bool success = await _employeeRepository.DeleteEmployeeAsync(id);

                if (!success)
                {
                    return NotFound(new { error = $"Employee with ID {id} not found." });
                }

                await _employeeCacheService.RemoveEmployeeAsync(id);

                return Ok(new { message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request." });
            }
        }
    }
}