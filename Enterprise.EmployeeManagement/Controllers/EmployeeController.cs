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


namespace Enterprise.EmployeeManagement.Web.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository employeeRepository, ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }


        [HttpGet]
        [Route("api/employees")] // API endpoint to get employees data as JSON
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            return Ok(employees); // Returns the employees as a JSON response
        }

        [HttpGet]
        [Route("Employee/GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found" });
            }

            return Ok(employee); // Return JSON data for the employee
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Employee employee)
        {
            if (ModelState.IsValid)
            {
                // Hash the password before saving
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    employee.Password = PasswordHasher.HashPassword(employee.Password);
                }

                await _employeeRepository.CreateEmployeeAsync(employee);

                // Return a JSON response for AJAX
                return Json(new { success = true, message = "Employee created successfully." });
            }

            // Return validation errors for AJAX
            return BadRequest(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }



        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] Employee employee)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _employeeRepository.CreateEmployeeAsync(employee);

        //        // Return a JSON response for AJAX
        //        return Json(new { success = true, message = "Employee created successfully." });
        //    }

        //    // Return validation errors for AJAX
        //    return BadRequest(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        //}


        //[HttpPost]
        ////[Route("employees/update/{id}")]
        //public async Task<IActionResult> Update(int id, Employee employee)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(employee);
        //    }

        //    employee.Id = id; // Ensure the ID is set
        //    await _employeeRepository.UpdateEmployeeAsync(employee);
        //    return RedirectToAction("Index");
        //}




        [HttpPost]
        [Route("Employee/Update")]
        public async Task<IActionResult> Update([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid data", details = ModelState });
            }

            // Check if the password is provided and hash it before saving
            if (!string.IsNullOrEmpty(employee.Password))
            {
                employee.Password = PasswordHasher.HashPassword(employee.Password);  // Hash the password
            }

            // Update the employee details in the database
            await _employeeRepository.UpdateEmployeeAsync(employee);

            return Ok(new { message = "Employee updated successfully" });
        }




        [HttpGet]
        [Route("Employee/Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View("Update", employee);
        }

       


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid employee ID" });
            }

            try
            {
                bool success = await _employeeRepository.DeleteEmployeeAsync(id);
                if (!success)
                {
                    return NotFound(new { error = $"Employee with ID {id} not found." });
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting employee with ID {id}.");
                return StatusCode(500, new { error = "An error occurred while processing your request", details = ex.Message });
            }
        }




    }
}
