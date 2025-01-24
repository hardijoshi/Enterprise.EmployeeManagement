using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Enterprise.EmployeeManagement.core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Generic;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository employeeRepository, ITaskRepository taskRepository, ILogger<EmployeeController> logger, IConnectionMultiplexer redisConnection)
        {
            _employeeRepository = employeeRepository;
            _taskRepository = taskRepository;
            _redisConnection = redisConnection;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            _logger.LogInformation("Index action accessed. User: {UserEmail}", userEmail);

            if (userEmail == null)
            {
                _logger.LogWarning("Unauthorized access attempt to Index. Redirecting to login");
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        [Route("api/employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var redisDb = _redisConnection.GetDatabase();
            const string cacheKey = "employees";

            try
            {
                string cachedEmployees = await redisDb.StringGetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedEmployees))
                {
                    _logger.LogInformation("Returning employees from Redis cache.");
                    var employeesFromCache = JsonSerializer.Deserialize<List<Employee>>(cachedEmployees);
                    return Ok(employeesFromCache);
                }

                var employees = await _employeeRepository.GetAllEmployeesAsync();
                _logger.LogInformation("Employees retrieved from database. Caching result.");

                var serializedEmployees = JsonSerializer.Serialize(employees);
                await redisDb.StringSetAsync(cacheKey, serializedEmployees);

                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employees");
                return StatusCode(500, new { error = "An error occurred while fetching employees" });
            }
        }

        [HttpGet]
        [Route("Employee/GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var redisDb = _redisConnection.GetDatabase();
            string cacheKey = $"employee:{id}";

            try
            {
                string cachedEmployee = await redisDb.StringGetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedEmployee))
                {
                    _logger.LogInformation("Returning employee {EmployeeId} from Redis cache.", id);
                    var employeeFromCache = JsonSerializer.Deserialize<Employee>(cachedEmployee);
                    return Ok(employeeFromCache);
                }

                var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeId}", id);
                    return NotFound(new { message = "Employee not found" });
                }

                _logger.LogInformation("Caching employee {EmployeeId} details.", id);
                var serializedEmployee = JsonSerializer.Serialize(employee);
                await redisDb.StringSetAsync(cacheKey, serializedEmployee);

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee details for {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while fetching employee details" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Employee employee)
        {
            _logger.LogInformation("Attempting to create new employee: {Email}", employee.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Employee creation failed due to validation errors: {@Errors}", errors);
                return BadRequest(new { success = false, errors });
            }

            try
            {
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                    _logger.LogDebug("Password hashed for new employee");
                }

                await _employeeRepository.CreateEmployeeAsync(employee);
                _logger.LogInformation("Successfully created employee: {Email}", employee.Email);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employees"); // Invalidate employees cache

                return Ok(new { success = true, message = "Employee created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee: {Email}", employee.Email);
                return StatusCode(500, new { error = "An error occurred while creating the employee" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] Employee employee)
        {
            _logger.LogInformation("Attempting to update employee: {EmployeeId}", employee.Id);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Employee update validation failed: {@ValidationErrors}", errors);
                return BadRequest(new { error = "Invalid data", details = ModelState });
            }

            try
            {
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                    _logger.LogDebug("Password updated and hashed for employee {EmployeeId}", employee.Id);
                }

                await _employeeRepository.UpdateEmployeeAsync(employee);
                _logger.LogInformation("Successfully updated employee {EmployeeId}", employee.Id);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employees"); // Invalidate employees cache
                await redisDb.KeyDeleteAsync($"employee:{employee.Id}"); // Invalidate individual employee cache

                return Ok(new { message = "Employee updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", employee.Id);
                return StatusCode(500, new { error = "An error occurred while updating the employee" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Attempting to delete employee {EmployeeId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid employee ID provided for deletion: {EmployeeId}", id);
                return BadRequest(new { error = "Invalid employee ID" });
            }

            try
            {
                bool success = await _employeeRepository.DeleteEmployeeAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Employee not found for deletion: {EmployeeId}", id);
                    return NotFound(new { error = $"Employee with ID {id} not found." });
                }

                _logger.LogInformation("Successfully deleted employee {EmployeeId}", id);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employees"); 
                await redisDb.KeyDeleteAsync($"employee:{id}"); 

                return Ok(new { message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }
    }
}
