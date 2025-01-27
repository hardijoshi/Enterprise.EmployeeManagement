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
            const string redisSetKey = "employee_ids"; // Redis set to hold all employee IDs

            try
            {
                // Check if employee IDs are stored in Redis set
                var employeeIds = await redisDb.SetMembersAsync(redisSetKey);
                if (employeeIds.Length > 0)
                {
                    _logger.LogInformation("Fetching employees from Redis cache (hashes).");

                    var employees = new List<Employee>();
                    foreach (var id in employeeIds)
                    {
                        var employeeHash = await redisDb.HashGetAllAsync($"employee:{id}");
                        if (employeeHash.Length > 0)
                        {
                            var employee = new Employee
                            {
                                Id = (int)employeeHash.FirstOrDefault(x => x.Name == "Id").Value,
                                FirstName = employeeHash.FirstOrDefault(x => x.Name == "First Name").Value.ToString(),
                                LastName = employeeHash.FirstOrDefault(x => x.Name == "Last Name").Value.ToString(),
                                Email = employeeHash.FirstOrDefault(x => x.Name == "Email").Value.ToString(),
                                MobileNumber = employeeHash.FirstOrDefault(x => x.Name == "Mobile Number").Value.ToString(),
                                Password = employeeHash.FirstOrDefault(x => x.Name == "Password").Value.ToString(),
                                Role = employeeHash.FirstOrDefault(x => x.Name == "Role").Value.ToString(),
                            };
                            employees.Add(employee);
                        }
                    }

                    return Ok(employees);
                }

                // If no employee data in cache, retrieve from database
                var employeesFromDb = await _employeeRepository.GetAllEmployeesAsync();
                _logger.LogInformation("Employees retrieved from database. Caching results.");

                foreach (var employee in employeesFromDb)
                {
                    // Store employee details in Redis as hash
                    var hashEntries = new HashEntry[]
                    {
                    new HashEntry("Id", employee.Id),
                    new HashEntry("First Name", employee.FirstName ?? string.Empty),
                    new HashEntry("Last Name", employee.LastName ?? string.Empty),
                    new HashEntry("Email", employee.Email ?? string.Empty),
                    new HashEntry("Mobile Number", employee.MobileNumber ?? string.Empty),
                    new HashEntry("Password", employee.Password ?? string.Empty),
                    new HashEntry("Role", employee.Role ?? string.Empty),
                    };

                    await redisDb.HashSetAsync($"employee:{employee.Id}", hashEntries);
                    await redisDb.SetAddAsync(redisSetKey, employee.Id); // Add employee ID to the Redis set
                }

                return Ok(employeesFromDb);
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
            string cacheKey = $"employee:{id}"; // Redis hash for individual employee

            try
            {
                // Retrieve the employee from Redis cache (hash)
                var employeeHash = await redisDb.HashGetAllAsync(cacheKey);
                if (employeeHash.Length > 0)
                {
                    _logger.LogInformation("Returning employee {EmployeeId} from Redis cache.", id);

                    var employee = new Employee
                    {
                        Id = (int)employeeHash.FirstOrDefault(x => x.Name == "Id").Value,
                        FirstName = employeeHash.FirstOrDefault(x => x.Name == "First Name").Value.ToString(),
                        LastName = employeeHash.FirstOrDefault(x => x.Name == "Last Name").Value.ToString(),
                        Email = employeeHash.FirstOrDefault(x => x.Name == "Email").Value.ToString(),
                        MobileNumber = employeeHash.FirstOrDefault(x => x.Name == "Mobile Number").Value.ToString(),
                        Password = employeeHash.FirstOrDefault(x => x.Name == "Password").Value.ToString(),
                        Role = employeeHash.FirstOrDefault(x => x.Name == "Role").Value.ToString(),
                    };

                    return Ok(employee);
                }

                // If not found in cache, fetch from database
                var employeeFromDb = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employeeFromDb == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeId}", id);
                    return NotFound(new { message = "Employee not found" });
                }

                _logger.LogInformation("Caching employee {EmployeeId} details.", id);

                // Store employee details in Redis as hash
                var hashEntries = new HashEntry[]
                {
                    new HashEntry("Id", employeeFromDb.Id),
                    new HashEntry("First Name", employeeFromDb.FirstName ?? string.Empty),
                    new HashEntry("Last Name", employeeFromDb.LastName ?? string.Empty),
                    new HashEntry("Email", employeeFromDb.Email ?? string.Empty),
                    new HashEntry("Mobile Number", employeeFromDb.MobileNumber ?? string.Empty),
                    new HashEntry("Password", employeeFromDb.Password ?? string.Empty),
                    new HashEntry("Role", employeeFromDb.Role ?? string.Empty),
                };

                await redisDb.HashSetAsync(cacheKey, hashEntries);

                return Ok(employeeFromDb);
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            try
            {
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }

                await _employeeRepository.CreateEmployeeAsync(employee);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employee_ids"); // Invalidate employee IDs set
                await redisDb.KeyDeleteAsync($"employee:{employee.Id}"); // Invalidate the new employee cache

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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { error = "Invalid data", details = ModelState });
            }

            try
            {
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }

                await _employeeRepository.UpdateEmployeeAsync(employee);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employee_ids"); // Invalidate employee IDs set
                await redisDb.KeyDeleteAsync($"employee:{employee.Id}"); // Invalidate the updated employee cache

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
