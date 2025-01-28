using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Enterprise.EmployeeManagement.core.Utilities;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            ITaskRepository taskRepository,
            ILogger<EmployeeController> logger,
            IConnectionMultiplexer redisConnection)
        {
            _employeeRepository = employeeRepository;
            _taskRepository = taskRepository;
            _logger = logger;
            _redisConnection = redisConnection;
        }

        // INDEX
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

        // GET ALL EMPLOYEES
        [HttpGet]
        [Route("api/employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var redisDb = _redisConnection.GetDatabase();
            const string redisSetKey = "employee_ids";

            try
            {
                var employeeIds = await redisDb.SetMembersAsync(redisSetKey);

                if (employeeIds.Length > 0)
                {
                    _logger.LogInformation("Fetching employees from Redis cache.");
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
                                Role = (RoleType)Enum.Parse(typeof(RoleType), employeeHash.FirstOrDefault(x => x.Name == "Role").Value.ToString())
                            };
                            employees.Add(employee);
                        }
                    }

                    return Ok(employees);
                }

                // Fetch employees from the database
                var employeesFromDb = await _employeeRepository.GetAllEmployeesAsync();

                foreach (var employee in employeesFromDb)
                {
                    var hashEntries = new HashEntry[]
                    {
                        new HashEntry("Id", employee.Id),
                        new HashEntry("First Name", employee.FirstName ?? string.Empty),
                        new HashEntry("Last Name", employee.LastName ?? string.Empty),
                        new HashEntry("Email", employee.Email ?? string.Empty),
                        new HashEntry("Mobile Number", employee.MobileNumber ?? string.Empty),
                        new HashEntry("Password", employee.Password ?? string.Empty),
                        new HashEntry("Role", employee.Role.ToString()),
                    };

                    await redisDb.HashSetAsync($"employee:{employee.Id}", hashEntries);
                    await redisDb.SetAddAsync(redisSetKey, employee.Id);
                }

                return Ok(employeesFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all employees.");
                return StatusCode(500, new { error = "An error occurred while fetching employees." });
            }
        }

        // GET EMPLOYEE DETAILS
        [HttpGet]
        [Route("Employee/GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var redisDb = _redisConnection.GetDatabase();
            string cacheKey = $"employee:{id}";

            try
            {
                var employeeHash = await redisDb.HashGetAllAsync(cacheKey);

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
                        Role = (RoleType)Enum.Parse(typeof(RoleType), employeeHash.FirstOrDefault(x => x.Name == "Role").Value.ToString())
                    };

                    return Ok(employee);
                }

                var employeeFromDb = await _employeeRepository.GetEmployeeByIdAsync(id);

                if (employeeFromDb == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeId}", id);
                    return NotFound(new { message = "Employee not found." });
                }

                var hashEntries = new HashEntry[]
                {
                    new HashEntry("Id", employeeFromDb.Id),
                    new HashEntry("First Name", employeeFromDb.FirstName ?? string.Empty),
                    new HashEntry("Last Name", employeeFromDb.LastName ?? string.Empty),
                    new HashEntry("Email", employeeFromDb.Email ?? string.Empty),
                    new HashEntry("Mobile Number", employeeFromDb.MobileNumber ?? string.Empty),
                    new HashEntry("Password", employeeFromDb.Password ?? string.Empty),
                    new HashEntry("Role", employeeFromDb.Role.ToString()),
                };

                await redisDb.HashSetAsync(cacheKey, hashEntries);

                return Ok(employeeFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching details for employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while fetching employee details." });
            }
        }

        // CREATE EMPLOYEE
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
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }

                await _employeeRepository.CreateEmployeeAsync(employee);

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync("employee_ids");

                return Ok(new { success = true, message = "Employee created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee: {Email}", employee.Email);
                return StatusCode(500, new { error = "An error occurred while creating the employee." });
            }
        }

        // UPDATE EMPLOYEE
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
                // Check if the employee exists in the database
                var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(employee.Id);
                if (existingEmployee == null)
                {
                    return NotFound(new { success = false, message = $"Employee with ID {employee.Id} not found." });
                }

                // Update password if provided (hash it)
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }
                else
                {
                    // Keep the old password if none is provided
                    employee.Password = existingEmployee.Password;
                }

                // Update the employee in the database
                await _employeeRepository.UpdateEmployeeAsync(employee);

                // Update employee details in Redis cache
                var redisDb = _redisConnection.GetDatabase();
                var hashEntries = new HashEntry[]
                {
                    new HashEntry("Id", employee.Id),
                    new HashEntry("First Name", employee.FirstName ?? string.Empty),
                    new HashEntry("Last Name", employee.LastName ?? string.Empty),
                    new HashEntry("Email", employee.Email ?? string.Empty),
                    new HashEntry("Mobile Number", employee.MobileNumber ?? string.Empty),
                    new HashEntry("Password", employee.Password ?? string.Empty),
                    new HashEntry("Role", employee.Role.ToString())
                };
                await redisDb.HashSetAsync($"employee:{employee.Id}", hashEntries);

                return Ok(new { success = true, message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {EmployeeId}", employee.Id);
                return StatusCode(500, new { error = "An error occurred while updating the employee." });
            }
        }


        // DELETE EMPLOYEE
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

                var redisDb = _redisConnection.GetDatabase();
                await redisDb.KeyDeleteAsync($"employee:{id}");

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
