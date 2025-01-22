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
using System.IO;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<EmployeeController> _logger;



        public EmployeeController(IEmployeeRepository employeeRepository, ITaskRepository taskRepository, ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _taskRepository = taskRepository;
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
            _logger.LogDebug("Fetching all employees");
            try
            {
                var employees = await _employeeRepository.GetAllEmployeesAsync();
                _logger.LogInformation("Successfully retrieved {Count} employees", employees.Count());
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all employees");
                return StatusCode(500, new { error = "An error occurred while fetching employees" });
            }
        }

        [HttpGet]
        [Route("Employee/GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            _logger.LogDebug("Fetching details for employee {EmployeeId}", id);
            try
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeId}", id);
                    return NotFound(new { message = "Employee not found" });
                }
                _logger.LogInformation("Successfully retrieved details for employee {EmployeeId}", id);
                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching details for employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while fetching employee details" });
            }
        }

        [HttpGet]
        [Route("Employee/GetTasks/{employeeId}")]
        public async Task<IActionResult> GetEmployeeTasks(int employeeId)
        {
            _logger.LogDebug("Fetching tasks for employee {EmployeeId}", employeeId);
            try
            {
                var tasks = await _taskRepository.GetTasksByEmployeeIdAsync(employeeId);
                if (tasks == null || tasks.Count == 0)
                {
                    _logger.LogInformation("No tasks found for employee {EmployeeId}", employeeId);
                    return NotFound(new { message = "No tasks found for this employee" });
                }

                _logger.LogInformation("Retrieved {Count} tasks for employee {EmployeeId}", tasks.Count, employeeId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks for employee {EmployeeId}", employeeId);
                return StatusCode(500, new { error = "An error occurred while fetching employee tasks" });
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            _logger.LogDebug("Accessing employee creation form");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Employee employee)
        {
            _logger.LogInformation("Attempting to create new employee: {Email}", employee.Email);

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(employee.Password))
                    {
                        employee.Password = PasswordHasher.HashPassword(employee.Password);
                        _logger.LogDebug("Password hashed for new employee");
                    }

                    await _employeeRepository.CreateEmployeeAsync(employee);
                    _logger.LogInformation("Successfully created employee: {Email}", employee.Email);

                    return Json(new { success = true, message = "Employee created successfully." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating employee: {Email}", employee.Email);
                    return StatusCode(500, new { error = "An error occurred while creating the employee" });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Employee creation failed due to validation errors: {@Errors}", errors);
            return BadRequest(new { success = false, errors = errors });
        }

        [HttpPost]
        [Route("Employee/Update")]
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

                return Ok(new { message = "Employee updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", employee.Id);
                return StatusCode(500, new { error = "An error occurred while updating the employee" });
            }
        }

        [HttpGet]
        [Route("Employee/Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            _logger.LogDebug("Fetching employee {EmployeeId} for update", id);
            try
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for update: {EmployeeId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved employee {EmployeeId} for update", id);
                return View("Update", employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee {EmployeeId} for update", id);
                return StatusCode(500, new { error = "An error occurred while fetching the employee" });
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
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request", details = ex.Message });
            }
        }
    }
}