using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Enterprise.EmployeeManagement.core.Utilities;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(Login model)
        {
            if (ModelState.IsValid)
            {
                // Hash the entered password
                string hashedPassword = PasswordHasher.HashPassword(model.Password);

                // Verify against the hashed password in the database
                var employee = _context.Employees.SingleOrDefault(e => e.Email == model.Email && e.Password == hashedPassword);
                if (employee != null)
                {
                    // Store user information in session
                    HttpContext.Session.SetString("UserEmail", employee.Email);
                    HttpContext.Session.SetString("UserRole", employee.Role);

                    // Redirect to Employee Management Page
                    return RedirectToAction("Index", "Employee");
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            return View(model);
        }


    }
}
